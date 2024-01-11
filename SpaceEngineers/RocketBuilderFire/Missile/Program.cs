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
using System.Reflection;
using VRage.Scripting;
using System.Linq;

namespace SpaceEngineers.RocketBuilderFire.Missile
{
    public sealed class Program : MyGridProgram
    {
        //------------BEGIN--------------

        const string GuidFire = nameof(GuidFire);

        //Tags
        const string GuidFireTag = nameof(GuidFireTag);
        const string AutoPilotFireTag = nameof(AutoPilotFireTag);
        const string DebugMissileTag = nameof(DebugMissileTag);

        const string MissileHydrogenTank = "Missile Hydrogen Tank";
        const string MissileHydrogenThruster = "Missile Hydrogen Thruster";
        const string MissileWarhead = "Missile Warhead HE";
        const string MissileAntenna = "Missile Antenna";
        const string RemoteControl = "Missile Remote Control";
        const string Gyro = "Missile Gyro";

        IMyGasTank tank;
        IMyThrust thruster;
        IMyWarhead warhead;
        IMyRadioAntenna antenna;
        IMyRemoteControl remoteControl;
        IMyGyro gyro;

        IMyBroadcastListener AutoPilotFireListener;

        Target target = null;

        public Program()
        {
            AutoPilotFireListener = IGC.RegisterBroadcastListener(AutoPilotFireTag);


            Runtime.UpdateFrequency = UpdateFrequency.Update1;
            tank = GridTerminalSystem.GetBlockWithName(MissileHydrogenTank) as IMyGasTank;
            thruster = GridTerminalSystem.GetBlockWithName(MissileHydrogenThruster) as IMyThrust;
            warhead = GridTerminalSystem.GetBlockWithName(MissileWarhead) as IMyWarhead;
            antenna = GridTerminalSystem.GetBlockWithName(MissileAntenna) as IMyRadioAntenna;
            remoteControl = GridTerminalSystem.GetBlockWithName(RemoteControl) as IMyRemoteControl;
            gyro = GridTerminalSystem.GetBlockWithName(Gyro) as IMyGyro;
            this.Me.CustomData = (CombineStrings(
                $"tank:{tank != null}",
                $"thruster:{thruster != null}",
                $"warhead:{warhead != null}",
                $"antenna:{antenna != null}",
                $"remoteControl:{remoteControl != null}",
                $"gyro:{gyro != null}"));
            IGC.SendBroadcastMessage(DebugMissileTag,
               CombineStrings(
                   $"Ready"
                   ),
               TransmissionDistance.TransmissionDistanceMax);
        }

        public string CombineStrings(params string[] strings)
        {
            return string.Join(".\n", strings);
        }
        public void Main(string args)
        {
            if (args == GuidFire)
            {
                Start();
            }
            if (AutoPilotFireListener.HasPendingMessage)
            {
                var dirtyMessageData = AutoPilotFireListener.AcceptMessage().Data;

                IGC.SendBroadcastMessage(DebugMissileTag,
              CombineStrings(
                  dirtyMessageData.ToString()
                  ),
              TransmissionDistance.TransmissionDistanceMax);


                var fireMessage = AutoPilotFireMessage.Deserealize(dirtyMessageData.ToString());
                target = new Target(fireMessage.Target);
                IGC.SendBroadcastMessage(DebugMissileTag,
               CombineStrings(
                   $"TargetGeted"
                   ),
               TransmissionDistance.TransmissionDistanceMax);
                // Start();
            }
            if (target != null)
            {
                Move();
            }
        }

        private void Start()
        {
            IGC.SendBroadcastMessage(GuidFireTag, "", TransmissionDistance.TransmissionDistanceMax);
            tank.Stockpile = false;
            thruster.Enabled = true;
            warhead.IsArmed = true;
            antenna.Radius = float.MaxValue;
        }

        public void Move()
        {
            remoteControl.FlightMode = FlightMode.OneWay;
            var position = gyro.Position;
            var pitch = gyro.Pitch;
            var yaw = gyro.Yaw;
            var roll = gyro.Roll;

            var grav = Vector3D.Normalize(remoteControl.GetNaturalGravity());
            var axis = grav.Cross(remoteControl.WorldMatrix.Down);

            IGC.SendBroadcastMessage(DebugMissileTag,
                CombineStrings(
                    $"Position: {position.X},{position.Y},{position.Z}",
                    $"Pitch: {pitch}",
                    $"Yaw: {yaw}",
                    $"Roll: {roll}",
                    $"Target: {new TargetMessage(target).Serealize()}",
                    $"Grav: {grav.X},{grav.Y},{grav.Z}",
                    $"Axis: {axis.X},{axis.Y},{axis.Z}"
                    ),
                TransmissionDistance.TransmissionDistanceMax);

        }



        public void Save()
        { }


        class Target
        {
            public Target(TargetMessage message)
            {
                X = message.X;
                Y = message.Y;
                Z = message.Z;
            }
            public float X { get; set; }
            public float Y { get; set; }
            public float Z { get; set; }
        }
        class TargetMessage
        {
            public TargetMessage() { }
            public TargetMessage(Target target)
            {
                X = target.X;
                Y = target.Y;
                Z = target.Z;
            }
            public float X { get; set; }
            public float Y { get; set; }
            public float Z { get; set; }

            public string Serealize()
            {
                var result = new StringBuilder();
                result.AppendLine(FormatProperty(nameof(X), X.ToString()));
                result.AppendLine(FormatProperty(nameof(Y), Y.ToString()));
                result.AppendLine(FormatProperty(nameof(Z), Z.ToString()));

                return result.ToString();
            }
            public static TargetMessage Deserealize(string input)
            {
                var result = new TargetMessage();
                var lines = input.Split('\n');
                foreach (var item in lines)
                {
                    var propLine = item.Split(':');
                    if (propLine[0] == nameof(X))
                    {
                        result.X = (float)Convert.ToDouble(propLine[1]);
                    }
                    if (propLine[0] == nameof(Y))
                    {
                        result.Y = (float)Convert.ToDouble(propLine[1]);
                    }
                    if (propLine[0] == nameof(Z))
                    {
                        result.Z = (float)Convert.ToDouble(propLine[1]);
                    }
                }
                return result;
            }
            public string FormatProperty(string name, string value)
            {
                return $"{name}:{value}";
            }
        }
        class AutoPilotFireMessage
        {
            public long MissileNumber { get; set; }
            public TargetMessage Target { get; set; }
            public string Serealize()
            {
                var result = new StringBuilder();
                result.AppendLine(FormatProperty(nameof(MissileNumber), MissileNumber.ToString()));
                result.AppendLine(FormatProperty(nameof(Target), Target.Serealize()));
                return result.ToString();
            }
            public static AutoPilotFireMessage Deserealize(string input)
            {
                var result = new AutoPilotFireMessage();
                var lines = input.Split('\n');
                foreach (var item in lines)
                {
                    var propLine = item.Split(':');
                    if (propLine[0] == nameof(MissileNumber))
                    {
                        result.MissileNumber = long.Parse(propLine[1]);
                    }
                    if (propLine[0] == nameof(Target))
                    {
                        var propLineСomposite = propLine.Where(x => x != propLine.First());
                        result.Target = TargetMessage.Deserealize(string.Join(":", propLineСomposite));
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
