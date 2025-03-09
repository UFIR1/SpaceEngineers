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
using System.Linq;
namespace Script.mI
{
    public sealed class Program : MyGridProgram
    {
        //------------BEGIN--------------
        List<IMyCargoContainer> storageStatus = new List<IMyCargoContainer>();
        List<IMyBatteryBlock> batteryBlock = new List<IMyBatteryBlock>();
        IMyTextSurface middleDisplay = null;

        IMyTextSurface debugDisplay = null;
        IMyTextSurface paramDisplay = null;
        string displayname = string.Empty;
        public Program()
        {
            var grid = Me.CubeGrid;
            
            debugDisplay = (Me as IMyTextSurfaceProvider).GetSurface(0);
            debugDisplay.WriteText("INIT");
            GridTerminalSystem.GetBlocksOfType<IMyBatteryBlock>(batteryBlock, x => x.CubeGrid.EntityId == grid.EntityId);
            GridTerminalSystem.GetBlocksOfType<IMyCargoContainer>(storageStatus, x => x.CubeGrid.EntityId == grid.EntityId);
            Runtime.UpdateFrequency = UpdateFrequency.Update1;
            paramDisplay = (Me as IMyTextSurfaceProvider).GetSurface(1);
            displayname = paramDisplay.GetText();

            debugDisplay.WriteText("displayname: " + displayname);
        }

        public void Main(string args)
        {
            displayname = Me.CustomData;
            debugDisplay.WriteText("displayname: " + displayname);
            if (string.IsNullOrEmpty(displayname))
            {
                return;
            }
            if (middleDisplay == null)
            {
                var displayProvider = GridTerminalSystem.GetBlockWithName(displayname);
                if (displayProvider == null)
                {
                    debugDisplay.WriteText("display with name: \"" + displayname + "\" not found");
                    return;
                }
                middleDisplay = (displayProvider as IMyTextSurfaceProvider).GetSurface(0);

            }

            float result = CalculateBatteryCharge(batteryBlock);
            float capacityStatus = CalculateStorageCapacity(storageStatus);
            middleDisplay.WriteText("Заполненность" + capacityStatus.ToString("0.000") + "\n" + "Заряд" + result.ToString());
        }

        private static float CalculateStorageCapacity(List<IMyCargoContainer> storageStatus)
        {
            float currentCapacity = 0;
            float maxCapacity = 0;
            for (int i = 0; i < storageStatus.Count; i++)
            {
                IMyInventory cargoInventory = storageStatus[i].GetInventory(0);
                maxCapacity += cargoInventory.MaxVolume.RawValue;
                currentCapacity += cargoInventory.CurrentVolume.RawValue;

            }
            float capacityStatus = currentCapacity / maxCapacity;
            return capacityStatus;
        }
        private static float CalculateBatteryCharge(List<IMyBatteryBlock> batteryBlock)
        {
            float maxCharge = 0;
            float currentCharge = 0;
            for (int i = 0; i < batteryBlock.Count; i++)
            {
                maxCharge += batteryBlock[i].MaxStoredPower;
                currentCharge += batteryBlock[i].CurrentStoredPower;

            }

            float result = currentCharge / maxCharge;
            return result;
        }

        public void Save()
        { }

        //------------END--------------
    }
}