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

namespace SpaceEngineers.RadioFacilityController1.Listener
{
    public sealed class Program : MyGridProgram
    {
        //------------BEGIN--------------
        const string HangarControl = nameof(HangarControl);

        const string hangar1DoorsName = "Ангар1";

        List<IMyDoor> hangar1Doors = new List<IMyDoor>();

        IMyBroadcastListener listener;
        MyIGCMessage message = new MyIGCMessage();
        IMyTextSurface debugLcd;

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
            listener = IGC.RegisterBroadcastListener(HangarControl);
            debugLcd = (Me).GetSurface(0);

            GridTerminalSystem.GetBlockGroupWithName(hangar1DoorsName).GetBlocksOfType(hangar1Doors);

        }

        public void Main(string args)
        {
            if (listener.HasPendingMessage)
            {
                message = listener.AcceptMessage();
                debugLcd.WriteText(MessageToString(message));
                var messageData = HangarControlMessage.Deserealize(message.Data.ToString());
                DoorProcess(messageData);
            }
        }

        private void DoorProcess(HangarControlMessage messageData)
        {
            foreach (var item in hangar1Doors)
            {
                if (messageData.DoorOpen)
                {
                    item.OpenDoor();
                }
                else
                {
                    item.CloseDoor();
                }
            }
        }

        public string MessageToString(MyIGCMessage message)
        {
            return $"Tag: {message.Tag};\nData: {message.Data};\nSource: {message.Source};";
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
            public static HangarControlMessage Deserealize(string input)
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
