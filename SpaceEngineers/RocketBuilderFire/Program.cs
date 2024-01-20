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
using Sandbox.Game.Entities.Cube;
using System.Linq;
using VRage.Scripting;

namespace SpaceEngineers.RocketBuilderFire
{
    public sealed class Program : MyGridProgram
    {
        //------------BEGIN--------------
        const string Build = nameof(Build);
        const string RefuelStart = nameof(RefuelStart);
        const string RefuelEnd = nameof(RefuelEnd);
        const string RemoveConnector = nameof(RemoveConnector);
        const string PrepareToShoot = nameof(PrepareToShoot);
        const string AutoMissileShoot = nameof(AutoMissileShoot);


        //
        const string Cockpit = "Кокпит";
        const string DebugMissileLcd = "Transparent Display";
        const string WeldersGroup = "Welders";
        const string PistonName = "ПоршеньЗаправщик";
        const string ConnectorName = "Малый коннектор заправщик";
        const string Grinder = "РезакРакета";
        const string Hinge = "Шарнир Ракета";
        const string MissileCatcher = "Захват";
        const string MissileHydrogenTank = "Missile Hydrogen Tank";
        const string MissileHydrogenThruster = "Missile Hydrogen Thruster";
        const string MissileWarhead = "Missile Warhead HE";
        const string MissileProgrammableBlock = "Missile Programmable Block";

        float pistonVelocity = 0.5f;

        //tags
        const string GuidFireTag = nameof(GuidFireTag);
        const string AutoPilotFireTag = nameof(AutoPilotFireTag);
        const string DebugMissileTag = nameof(DebugMissileTag);
        const string ArmoredTag = nameof(ArmoredTag);

        List<IMyFunctionalBlock> welders = new List<IMyFunctionalBlock>();
        IMyPistonBase piston;
        IMyShipConnector connector;
        IMyFunctionalBlock grinder;
        IMyMotorStator hinge;
        IMyLandingGear missileCatcher;

        IMyBroadcastListener GuideFireListener;
        IMyBroadcastListener DebugMissileListener;
        IMyBroadcastListener ArmoredMissileListener;

        IMyTextSurface debugMissileLcd;
        IMyTextSurface debugCarLcd;

        public List<long> missileIds = new List<long>();

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;

            GuideFireListener = IGC.RegisterBroadcastListener(GuidFireTag);
            DebugMissileListener = IGC.RegisterBroadcastListener(DebugMissileTag);
            ArmoredMissileListener = IGC.RegisterBroadcastListener(ArmoredTag);


            GridTerminalSystem.GetBlockGroupWithName(WeldersGroup).GetBlocksOfType(welders);
            piston = GridTerminalSystem.GetBlockWithName(PistonName) as IMyPistonBase;
            connector = GridTerminalSystem.GetBlockWithName(ConnectorName) as IMyShipConnector;
            grinder = GridTerminalSystem.GetBlockWithName(Grinder) as IMyFunctionalBlock;
            hinge = GridTerminalSystem.GetBlockWithName(Hinge) as IMyMotorStator;
            missileCatcher = GridTerminalSystem.GetBlockWithName(MissileCatcher) as IMyLandingGear;
            debugMissileLcd = (GridTerminalSystem.GetBlockWithName(DebugMissileLcd) as IMyTextSurfaceProvider).GetSurface(0);
            debugCarLcd = (GridTerminalSystem.GetBlockWithName(Cockpit) as IMyTextSurfaceProvider).GetSurface(1);
            var debugLcd = (Me).GetSurface(0);
            debugLcd.WriteText(CombineStrings(
                $"piston:{piston != null}",
                $"connector:{connector != null}",
                $"grinder:{grinder != null}",
                $"hinge:{hinge != null}",
                $"missileCatcher:{missileCatcher != null}",
                $"debugMissileLcd: {debugMissileLcd != null}"
                ));
            WriteMissileDebug("Reassembled");
        }
        public string CombineStrings(params string[] strings)
        {
            return string.Join(".\n", strings);
        }
        public void Main(string args)
        {
            if (args == Build)
            {
                BuildFunction();
            }
            if (args == RefuelStart)
            {
                RefuelFunction();
            }
            if (args == RefuelEnd)
            {
                connector.Disconnect();
                piston.Velocity = pistonVelocity;
            }
            if (args == RemoveConnector)
            {
                grinder.Enabled = true;
                hinge.TargetVelocityRad *= -1;
                missileCatcher.AutoLock = true;
            }
            if (args == PrepareToShoot && missileCatcher.IsLocked)
            {
                grinder.Enabled = false;
                hinge.TargetVelocityRad *= -1;
                missileCatcher.AutoLock = true;
            }
            if (GuideFireListener.HasPendingMessage)
            {
                var message = GuideFireListener.AcceptMessage();
                if (long.Parse(message.Data.ToString()) == Me.CubeGrid.EntityId)
                {
                    missileCatcher.Unlock();
                    missileCatcher.AutoLock = false;
                }
            }
            if (args == AutoMissileShoot)
            {
                WriteCarDebug($"Missileds: {missileIds.Count}");
                if (missileIds.Count > 0)
                {
                    var message = new AutoPilotFireMessage()
                    {
                        MissileNumber = missileIds.First(),
                        Target = new TargetMessage()
                        {
                            X = 54483.03f,
                            Y = -26995.35f,
                            Z = 7163.23f
                        }
                    };
                    WriteCarDebug(message.Serealize());
                    missileIds.Remove(message.MissileNumber);
                    IGC.SendBroadcastMessage(AutoPilotFireTag, message.Serealize(), TransmissionDistance.TransmissionDistanceMax);
                }
            }
            if (DebugMissileListener.HasPendingMessage)
            {
                try
                {
                    var message = DebugMissileListener.AcceptMessage();
                    WriteMissileDebug(message.Data.ToString());
                }
                catch { }
            }
            if (ArmoredMissileListener.HasPendingMessage)
            {
                var message = ArmoredMissileListener.AcceptMessage();
                WriteMissileDebug(message.Data.ToString());
                var missileArmored = MissileArmoredMessage.Deserealize(message.Data.ToString());
                if (missileArmored.OwnerCarGridId == Me.CubeGrid.EntityId)
                {
                    missileIds.Add(missileArmored.MissileGridId);
                }
            }
        }
        public void WriteMissileDebug(string message)
        {
            var debugNumber = Runtime.CurrentCallChainDepth;
            debugMissileLcd.WriteText($"{debugNumber}\n" + message);
        }
        public void WriteCarDebug(string message)
        {
            debugCarLcd.WriteText(message);
        }

        private void RefuelFunction()
        {
            foreach (var item in welders)
            {
                item.Enabled = false;
            }
            piston.Velocity = -1 * pistonVelocity;
            var tank = GridTerminalSystem.GetBlockWithName(MissileHydrogenTank) as IMyGasTank;
            tank.Stockpile = true;
            var thruster = GridTerminalSystem.GetBlockWithName(MissileHydrogenThruster) as IMyThrust;
            thruster.Enabled = false;
            var warhead = GridTerminalSystem.GetBlockWithName(MissileWarhead) as IMyWarhead;
            warhead.IsArmed = false;
            var programmingBlock = GridTerminalSystem.GetBlockWithName(MissileProgrammableBlock) as IMyProgrammableBlock;
            programmingBlock.Enabled = true;
            programmingBlock.TryRun("");
        }

        private void BuildFunction()
        {
            foreach (var item in welders)
            {
                item.Enabled = true;
            }
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
        class TargetMessage
        {
            public TargetMessage() { }
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
