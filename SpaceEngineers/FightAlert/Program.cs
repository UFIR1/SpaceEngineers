using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using VRageMath;
using VRage.Game;
using Sandbox.ModAPI.Interfaces;
using Sandbox.ModAPI.Ingame;
using Sandbox.Game.EntityComponents;
using VRage.Game.Components;
using VRage.Collections;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;

namespace SpaceEngineers.FightAlert
{
    public sealed class Program : MyGridProgram
    {
        //------------BEGIN--------------

        const string alertTest = nameof(alertTest);
        const string armoredWindows = nameof(armoredWindows);
        const string alertSounds = nameof(alertSounds);
        const string alertSoundName = "Тревога 1";

        int alertsToResetTarget = 7;

        List<IMyLargeGatlingTurret> turrets = new List<IMyLargeGatlingTurret>();
        List<IMyLightingBlock> lightingBlocks = new List<IMyLightingBlock>();
        Dictionary<long, LightSetting> lightSettings = new Dictionary<long, LightSetting>();
        LightSetting alertSettings = new LightSetting()
        {
            Color = Color.Red,
            BlinkIntervalSeconds = 2.5f,
            BlinkLength = 90
        };

        List<IMyDoor> windows = new List<IMyDoor>();
        List<IMySoundBlock> soundBlocks = new List<IMySoundBlock>();

        IMyTextSurface debugLcd;

        bool isAlert = false;
        IMyCubeGrid Grid { get; set; }

        int currentAlertsCall = 0;

        public Program()
        {
            Grid = this.Me.CubeGrid;
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
            GridTerminalSystem.GetBlocksOfType(turrets, x => x.CubeGrid.EntityId == Grid.EntityId);
            GridTerminalSystem.GetBlocksOfType(lightingBlocks, x => x.CubeGrid.EntityId == Grid.EntityId);
            GridTerminalSystem.GetBlockGroupWithName(armoredWindows).GetBlocksOfType(windows);
            GridTerminalSystem.GetBlockGroupWithName(alertSounds).GetBlocksOfType(soundBlocks);
            debugLcd = (Me).GetSurface(0);
            //debugLcd.WriteText(turrets.Count.ToString());
        }

        public void Main(string args)
        {
            FlightAlertProcessEvent(args);
        }
        public void FlightAlertProcessEvent(string arg)
        {
            bool newAlert = AlertCalculate(arg);
            if (isAlert != newAlert)
            {
                isAlert = newAlert;
                if (isAlert)
                {
                    AlertOn();
                }
                else
                {
                    AlertOff();
                }
            }

        }

        private bool AlertCalculate(string arg)
        {
            bool newAlert = false;
            if (arg == alertTest)
            {
                newAlert = !isAlert;
            }
            else
            {
                var asd = "";
                var allertsTurrets = 0;
                foreach (var item in turrets)
                {
                    if (item.IsAimed)
                    {
                        asd += item.Name + ";";
                        item.ShootOnce();
                        allertsTurrets++;
                        
                    }
                }
                if (currentAlertsCall > alertsToResetTarget)
                {
                    currentAlertsCall = 0;
                    foreach (var item in turrets)
                    {
                        item.ResetTargetingToDefault();
                    }
                }
                if (allertsTurrets > 1)
                {
                    newAlert = true;
                    currentAlertsCall++;
                   
                }
                debugLcd.WriteText(
                    CombineStrings(
                        asd,
                        turrets.Count.ToString()
                    ));
            }

            return newAlert;
        }

        private void AlertOff()
        {
            foreach (var item in lightingBlocks)
            {
                var setting = lightSettings.GetValueOrDefault(item.EntityId);
                if (setting != null)
                {
                    SetSettings(item, setting);
                }
            }

            lightSettings.Clear();
            foreach (var item in windows)
            {
                item.OpenDoor();
            }
            foreach (var item in soundBlocks)
            {
                item.Stop();
            }
        }

        private void AlertOn()
        {
            lightSettings.Clear();
            foreach (var item in lightingBlocks)
            {
                var setting = new LightSetting()
                {
                    Radius = item.Radius,
                    Intensity = item.Intensity,
                    Falloff = item.Falloff,
                    BlinkOffset = item.BlinkOffset,
                    BlinkIntervalSeconds = item.BlinkIntervalSeconds,
                    BlinkLength = item.BlinkLength,
                    Color = item.Color
                };
                lightSettings.Add(item.EntityId, setting);

                var settingsToSet = alertSettings;
                SetSettings(item, settingsToSet);
            }
            foreach (var item in windows)
            {
                item.CloseDoor();
            }
            foreach (var item in soundBlocks)
            {
                item.LoopPeriod = float.MaxValue;
                item.SelectedSound = alertSoundName;
                item.Play();
            }
        }
        public string CombineStrings(params string[] strings)
        {
            return string.Join(".\n", strings);
        }
        private static void SetSettings(IMyLightingBlock item, LightSetting settingsToSet)
        {
            item.Color = settingsToSet.Color;
            item.BlinkIntervalSeconds = settingsToSet.BlinkIntervalSeconds;
            item.BlinkLength = settingsToSet.BlinkLength;
        }

        public void Save()
        { }

        class LightSetting
        {
            public float Radius { get; set; }
            public float Intensity { get; set; }
            public float Falloff { get; set; }
            public float BlinkOffset { get; set; }
            public float BlinkIntervalSeconds { get; set; }
            public float BlinkLength { get; set; }
            public Color Color { get; set; }
        }
        //------------END--------------
    }
}
