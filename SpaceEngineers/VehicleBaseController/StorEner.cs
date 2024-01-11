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
using Sandbox.Game.Entities;
using VRage.Game.Voxels;
using VRage;

namespace SpaceEngineers.VehicleBaseController
{
    public sealed class Program : MyGridProgram
    {
        //------------BEGIN--------------
        List<IMyBatteryBlock> batteries = new List<IMyBatteryBlock>();
        List<IMyCargoContainer> storage = new List<IMyCargoContainer>();
        IMyCockpit Cockpit = null;
        float maxenergy = 0;
        float maxstorage = 0;

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
            Cockpit = (IMyCockpit)GridTerminalSystem.GetBlockWithName("Кокпит Майнер");
            StorageFind();
            EnergyFind();
        }

        private void StorageFind()
        {
            GridTerminalSystem.GetBlockGroupWithName("Контейнеры Майнер").GetBlocksOfType(storage);
        }

        private void EnergyFind()
        {
            GridTerminalSystem.GetBlockGroupWithName("Батареи Майнер").GetBlocksOfType(batteries);
            maxenergy = GetSummMaxPower();
        }

        public void Main(string args)
        {
            float currentenergy = GetCurrentPower() / maxenergy * 100;
            var leftLCD = Cockpit.GetSurface(1);
            maxstorage = StorageFillFactor();
            leftLCD.WriteText($"{maxstorage.ToString("000")} %\n{currentenergy.ToString("000")} %");
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
        private float StorageFillFactor()
        {
            float sumstorage = 0;
            for (int i = 0; i < storage.Count; i++)
            {
                if (storage[i] != null)
                {
                    sumstorage += storage[i].GetInventory().VolumeFillFactor * 100;
                }
            }
            sumstorage /= storage.Count;
            return sumstorage;
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
        { }

        //------------END--------------
    }
}