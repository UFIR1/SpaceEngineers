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
using Sandbox.Game.Entities;
using System.IO.IsolatedStorage;
using Sandbox.Game.Weapons.Guns;

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
        const string ArmoredTag = nameof(ArmoredTag);

        const string MissileHydrogenTank = "Missile Hydrogen Tank";
        const string MissileHydrogenThruster = "Missile Hydrogen Thruster";
        const string MissileWarhead = "Missile Warhead HE";
        const string MissileAntenna = "Missile Antenna";
        const string RemoteControl = "Missile Remote Control";
        const string Gyro = "Missile Gyro";
        const string OwnerCarProgrammingBlock = "Программируемый блок ПТУР";

        IMyGasTank tank;
        IMyThrust thruster;
        IMyWarhead warhead;
        IMyRadioAntenna antenna;
        IMyRemoteControl remoteControl;
        IMyGyro gyro;
        IMyProgrammableBlock ownerCarProgram;

        IMyBroadcastListener AutoPilotFireListener;

        Target target = null;

        Vector3 cycleCenter;

        public Program()
        {
            AutoPilotFireListener = IGC.RegisterBroadcastListener(AutoPilotFireTag);

            ownerCarProgram = GridTerminalSystem.GetBlockWithName(OwnerCarProgrammingBlock) as IMyProgrammableBlock;
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

            var armoredMessage = new MissileArmoredMessage()
            {
                OwnerCarGridId = ownerCarProgram.CubeGrid.EntityId,
                MissileGridId = Me.CubeGrid.EntityId,
            };

            IGC.SendBroadcastMessage(DebugMissileTag,
          CombineStrings(
              armoredMessage.Serealize()
              ),
          TransmissionDistance.TransmissionDistanceMax);

            IGC.SendBroadcastMessage(ArmoredTag,
             armoredMessage.Serealize(),
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



                Start();
                CalculateCycleCenter();
            }
            if (target != null)
            {
                Move();
            }
        }
        public void CalculateCycleCenter()
        {
            var targetVector = target.Vector; //+ (targetOffset * 1000);
            var myPosition = remoteControl.GetPosition();
            var differenceVector = (targetVector - myPosition);
            cycleCenter = myPosition + (differenceVector / 2);
        }
        private void Start()
        {
            IGC.SendBroadcastMessage(GuidFireTag, ownerCarProgram.CubeGrid.EntityId.ToString(), TransmissionDistance.TransmissionDistanceMax);
            tank.Stockpile = false;
            thruster.Enabled = true;
            gyro.GyroOverride = true;
            warhead.IsArmed = true;
            antenna.Radius = float.MaxValue;
        }

        public void Move()
        {
            //remoteControl.FlightMode = FlightMode.OneWay;
            var position = remoteControl.GetPosition();
            var pitch = gyro.Pitch;
            var yaw = gyro.Yaw;
            var roll = gyro.Roll;

            var grav = Vector3D.Normalize(remoteControl.GetNaturalGravity());
            var matrixDown = remoteControl.WorldMatrix.Down;
            double angle = 0;// = grav.Dot(matrixDown);
                             //Vector3D axis = HrizonKeeper(grav, matrixDown, angle);

            Vector3D axis = TargetKeeper(grav);
            var asd = SetGyro(axis);



            //IGC.SendBroadcastMessage(DebugMissileTag,
            //    CombineStrings(
            //        $"Position: {VectorToString(position)}",
            //        $"Pitch: {pitch}",
            //        $"Yaw: {yaw}",
            //        $"Roll: {roll}",
            //        $"Target: {new TargetMessage(target).Serealize()}",
            //        $"Grav: {VectorToString(grav)}",
            //        $"MatrixDown: {VectorToString(matrixDown)}",
            //        $"Axis: {VectorToString(axis)}",
            //        $"Angle: {angle}",
            //        $"Gyro forces: {string.Join(",", asd.Select(x => x.ToString("0.00")))}. Avg: {asd.Average()}",
            //        $"Matrix: {MatrixToString(remoteControl.WorldMatrix)}"
            //        ),
            //    TransmissionDistance.TransmissionDistanceMax);

            var motorMultiplyer = (1 - Math.Abs(asd.Max()));
            motorMultiplyer *= motorMultiplyer * motorMultiplyer;
            var motorValue = motorMultiplyer * (thruster.MaxThrust * 0.3f);
            var startValue = (thruster.MaxThrust * 0.5f);
            var thrusterOverride = (startValue) + motorValue;
            thruster.ThrustOverride = thrusterOverride; //motorValue;


            IGC.SendBroadcastMessage(DebugMissileTag,
               CombineStrings(
                   $"Position: {VectorToString(position)}",
                   $"Target: {new TargetMessage(target).Serealize()}",
                   $"Axis: {VectorToString(axis)}",
                   $"Gyro forces: {string.Join(",", asd.Select(x => x.ToString("0.00")))}. Avg: {asd.Average()}",
                   $"motorMultiplyer: {motorMultiplyer}",
                   $"MotorValue: {motorValue}",
                   $"StartValue: {startValue}",
                   $"ThrustOverride: {thrusterOverride}",
                   $"Matrix: {MatrixToString(remoteControl.WorldMatrix)}"
                   ),
               TransmissionDistance.TransmissionDistanceMax);
        }
        private Vector3D TargetKeeper(Vector3D grav)
        {
            //var axis = grav.Cross(remoteControl.WorldMatrix.Right);
            // axis = Vector3D.Normalize(axis);
            // var distance = Vector3.Distance(target.Vector, remoteControl.GetPosition());
            // var targetOffset = Vector3.Normalize(target.Vector - Vector3.Zero) * (distance / 9 / Me.CubeGrid.LinearVelocity);
            var targetVector = target.Vector; //+ (targetOffset * 1000);
            var myPosition = remoteControl.GetPosition();
            var differenceVector = (targetVector - myPosition);
            var directionVector = Vector3.Normalize(differenceVector);
            //directionVector -= Vector3.Up * 0.2f;
            directionVector = Vector3.Normalize(directionVector + (Vector3.Normalize(remoteControl.GetNaturalGravity()) * -0.15f));
            //angle = (remoteControl.WorldMatrix.Up.Dot(targetPosition) / (remoteControl.WorldMatrix.Up.Length() * targetPosition.Length()));
            return directionVector;
        }
        private static Vector3D HrizonKeeper(ref Vector3D grav, Vector3D matrixDown, double angle)
        {
            var axis = grav.Cross(matrixDown);
            if (angle < 0)
            {
                axis = Vector3D.Normalize(axis);
            }

            return axis;
        }

        public string VectorToString(Vector3 vector)
        {
            return $"{vector.X.ToString("0.00")},{vector.Y.ToString("0.00")},{vector.Z.ToString("0.00")}";
        }
        public string MatrixToString(MatrixD matrix)
        {
            return $"{VectorToString(matrix.Up)}\n{VectorToString(matrix.Down)}\n{VectorToString(matrix.Forward)}\n{VectorToString(matrix.Right)}";
        }
        public float[] SetGyro(Vector3D axis)
        {
            gyro.Yaw = ((float)axis.Dot(gyro.WorldMatrix.Right) * 3);
            gyro.Pitch = ((float)axis.Dot(gyro.WorldMatrix.Down) * 3);
            gyro.Roll = ((float)axis.Dot(gyro.WorldMatrix.Forward) * 0.4f);
            return new[]
            {
                (float)axis.Dot(gyro.WorldMatrix.Right),
                (float)axis.Dot(gyro.WorldMatrix.Down),
                //(float)axis.Dot(gyro.WorldMatrix.Forward)
            };
        }


        public void Save()
        { }

        class MissileArmoredMessage
        {
            public long OwnerCarGridId { get; set; }
            public long MissileGridId { get; set; }
            public string Serealize()
            {
                var result = new StringBuilder();
                result.AppendLine(FormatProperty(nameof(OwnerCarGridId), OwnerCarGridId.ToString()));
                result.AppendLine(FormatProperty(nameof(MissileGridId), MissileGridId.ToString()));
                return result.ToString();
            }
            public static MissileArmoredMessage Deserealize(string input)
            {
                var result = new MissileArmoredMessage();
                var lines = input.Split('\n');
                foreach (var item in lines)
                {
                    var propLine = item.Split(':');
                    if (propLine[0] == nameof(OwnerCarGridId))
                    {
                        result.OwnerCarGridId = long.Parse(propLine[1]);
                    }
                    if (propLine[0] == nameof(MissileGridId))
                    {
                        result.MissileGridId = long.Parse(propLine[1]);
                    }
                }
                return result;
            }
            public string FormatProperty(string name, string value)
            {
                return $"{name}:{value}";
            }
        }
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
            public Vector3 Vector => new Vector3(X, Y, Z);
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
                for (int i = 0; i < lines.Length; i++)
                {
                    string item = lines[i];
                    var propLine = item.Split(':');
                    if (propLine[0] == nameof(MissileNumber))
                    {
                        result.MissileNumber = long.Parse(propLine[1]);
                    }
                    if (propLine[0] == nameof(Target))
                    {
                        var propLineСomposite = propLine.Where(x => x != propLine.First()).Append("\n" + lines[i + 1]).Append("\n" + lines[i + 2]);
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
