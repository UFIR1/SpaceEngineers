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


namespace SpaceEngineers.VehicleBaseController
{
    public sealed class Program : MyGridProgram
    {
        //------------BEGIN--------------
        List<IMyWindTurbine> windgenerators = new List<IMyWindTurbine>();
        List<IMySolarPanel> sungenerators = new List<IMySolarPanel>();
        List<IMyBatteryBlock> batteries = new List<IMyBatteryBlock>();
        float maxoutenergywind = 0;
        float currentenergywind = 0;
        float maxoutenergysun = 0;
        float currentenergysun = 0;
        float maxenergyonbase = 0;
        IMyTextSurfaceProvider display;
        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
            display = (IMyTextSurfaceProvider)GridTerminalSystem.GetBlockWithName("Экран статус ветра");
            EnergyFind();
        }

        public void Main(string args)
        {
            GridTerminalSystem.GetBlockGroupWithName("Солнечные батареи").GetBlocksOfType(sungenerators);
            GridTerminalSystem.GetBlockGroupWithName("Ветрогенераторы").GetBlocksOfType(windgenerators);
            maxoutenergywind = GetMaxOutWind() * 100;
            maxoutenergysun = GetMaxOutSun() * 100;
            currentenergywind = GetCurrentEnergyWind() * 100;
            currentenergysun = GetCurrentEnergySun() * 100;
            float currentpower = GetCurrentPower() / maxenergyonbase * 100;

            var mainLCD = display.GetSurface(0);
            mainLCD.WriteText(CombineStrings(
                $"Ветряки : {maxoutenergywind.ToString("00")} / {currentenergywind.ToString("00")}",
                $"Солнечные батареи : {maxoutenergysun.ToString("00")} / {currentenergysun.ToString("00")}",
                $"Батареи : {currentpower.ToString("000")}%"
                ));
        }
        private float GetMaxOutWind()
        {
            float summaxoutwind = 0;
            for (int i = 0; i < windgenerators.Count; i++)
            {
                if (windgenerators[i] != null)
                {
                    summaxoutwind += windgenerators[i].MaxOutput;
                }
            }
            return summaxoutwind;
        }
        private float GetMaxOutSun()
        {
            float summaxoutsun = 0;
            for (int i = 0; i < sungenerators.Count; i++)
            {
                if (sungenerators[i] != null)
                {
                    summaxoutsun += sungenerators[i].MaxOutput;
                }
            }
            return summaxoutsun;
        }
        private float GetCurrentEnergyWind()
        {
            float sumcurroutwind = 0;
            for (int i = 0; i < windgenerators.Count; i++)
            {
                if (windgenerators[i] != null)
                {
                    sumcurroutwind += windgenerators[i].CurrentOutput;
                }
            }
            return sumcurroutwind;
        }
        private float GetCurrentEnergySun()
        {
            float sumcurroutsun = 0;
            for (int i = 0; i < sungenerators.Count; i++)
            {
                if (sungenerators[i] != null)
                {
                    sumcurroutsun += sungenerators[i].CurrentOutput;
                }
            }
            return sumcurroutsun;
        }

        private void EnergyFind()
        {
            GridTerminalSystem.GetBlockGroupWithName("БатареиБаза").GetBlocksOfType(batteries);
            maxenergyonbase = GetSummMaxPower();
        }
        private float GetSummMaxPower()
        {
            float sumenergy = 0;
            for (int i = 0; i < batteries.Count; i++)
            {
                if (batteries[i] != null)
                {
                    sumenergy += batteries[i].MaxStoredPower;
                }
            }
            return sumenergy;
        }
        private float GetCurrentPower()
        {
            float sumenergy = 0;
            for (int i = 0; i < batteries.Count; i++)
            {
                if (batteries[i] != null)
                {
                    sumenergy += batteries[i].CurrentStoredPower;
                }
            }
            return sumenergy;
        }
        public void Save()
        {

        }
        public string CombineStrings(params string[] strings)
        {
            return string.Join(".\n", strings);
        }
        //------------END--------------
    }
}