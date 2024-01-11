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
using System.IO;

namespace SpaceEngineers.RadioFacilityController1.Sender
{
    public sealed class Program : MyGridProgram
    {
        //------------BEGIN--------------

        //tags
        const string HangarControl = nameof(HangarControl);

        //controlMessage
        const string OpenHangarDoor = nameof(OpenHangarDoor);
        const string CloseHangarDoor = nameof(CloseHangarDoor);

        public Program()
        { }

        public void Main(string args)
        {
            if (args == OpenHangarDoor)
            {
                SendDoorOpen();
            }
            if (args == CloseHangarDoor)
            {
                SendDoorClose();
            }
        }
        public void SendDoorOpen()
        {
            var messageData = new HangarControlMessage
            {
                HangarNumber = 1,
                DoorOpen = true
            };
            IGC.SendBroadcastMessage(HangarControl, messageData.Serealize(), TransmissionDistance.TransmissionDistanceMax);
        }
        public void SendDoorClose()
        {
            var messageData = new HangarControlMessage
            {
                HangarNumber = 1,
                DoorOpen = false
            };
            IGC.SendBroadcastMessage(HangarControl, messageData.Serealize(), TransmissionDistance.TransmissionDistanceMax);
        }
        public void Save()
        { }


        class HangarControlMessage
        {
            public int HangarNumber { get; set; }
            public bool DoorOpen { get; set; }
            public string Serealize()
            {
                var result = new StringBuilder();
                result.AppendLine(FormatProperty(nameof(HangarNumber), HangarNumber.ToString()));
                result.AppendLine(FormatProperty(nameof(DoorOpen), DoorOpen.ToString()));
                return result.ToString();
            }
            public HangarControlMessage Deserealize(string input)
            {
                var result = new HangarControlMessage();
                var lines = input.Split('\n');
                foreach (var item in lines)
                {
                    var propLine = item.Split(':');
                    if (propLine[0] == nameof(HangarNumber))
                    {
                        result.HangarNumber = Convert.ToInt32(propLine[1]);
                    }
                    if (propLine[0] == nameof(DoorOpen))
                    {
                        result.DoorOpen = Convert.ToBoolean(propLine[1]);
                    }
                }
                return result;
            }
            public string FormatProperty(string name, string value)
            {
                return $"{name}:{value}";
            }
        }
        //------------END--------------
    }
}
