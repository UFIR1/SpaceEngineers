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
using Sandbox.Game.GameSystems;
using VRage.ModAPI;
using VRage.Game.ModAPI;

namespace Printer
{
    public sealed class Program : MyGridProgram
    {
        //------------BEGIN--------------
        IMyCockpit controlCockpit;
        //IMyTextSurface debugLcd;
        List<IMyPistonBase> pistonsX = new List<IMyPistonBase>();
        List<IMyPistonBase> pistonsY = new List<IMyPistonBase>();
        List<IMyPistonBase> pistonsZ = new List<IMyPistonBase>();

        IMyMotorStator rotorX = null;

        IMyMotorStator rotorY = null;

        public float speedMultiplier = 3;
        public float mouseSpeedXMultiplier = 0.3f;
        public float mouseSpeedYMultiplier = 0.01f;

        const string ReturnHead = "Return";

        public Program()
        {
            controlCockpit = (IMyCockpit)GridTerminalSystem.GetBlockWithName("PrinterCockpit");
            //debugLcd = ((IMyTextSurfaceProvider)GridTerminalSystem.GetBlockWithName("PrinterDebugLcd")).GetSurface(0);
            Runtime.UpdateFrequency = UpdateFrequency.Update1;
            GridTerminalSystem.GetBlockGroupWithName("PrinterPistonsX").GetBlocksOfType(pistonsX);
            GridTerminalSystem.GetBlockGroupWithName("PrinterPistonsY").GetBlocksOfType(pistonsY);
            GridTerminalSystem.GetBlockGroupWithName("PrinterPistonsZ").GetBlocksOfType(pistonsZ);
            rotorX = (IMyMotorStator)GridTerminalSystem.GetBlockWithName("PrinterRotorX");
            rotorY = (IMyMotorStator)GridTerminalSystem.GetBlockWithName("PrinterRotorY");

            foreach (var item in pistonsX)
            {
                item.Velocity = 0;
            }
        }

        public void Main(string args)
        {
            if (controlCockpit.IsUnderControl)
            {
                if (Runtime.UpdateFrequency != UpdateFrequency.Update1)
                {
                    Runtime.UpdateFrequency = UpdateFrequency.Update1;
                }
                var angle = (rotorX.Angle);

                Vector2 cockpitVector2 = new Vector2(controlCockpit.MoveIndicator.X, -1 * controlCockpit.MoveIndicator.Z);
                Vector2 rotation;
                Vector3 calculatedVector;
                CalculateRotatedVector(angle, cockpitVector2, out rotation, out calculatedVector);

                /*debugLcd.WriteText(
                    CombineStrings(
                        VectorToString(controlCockpit.MoveIndicator),
                        controlCockpit.RollIndicator.ToString(),
                        VectorToString(controlCockpit.RotationIndicator),
                        VectorToString(rotation),
                        VectorToString(calculatedVector)

                    ));*/
                MoveHead(calculatedVector);
            }
            else
            {
                if (Runtime.UpdateFrequency != UpdateFrequency.None)
                {
                    Runtime.UpdateFrequency = UpdateFrequency.None;
                }
                if (args == ReturnHead)
                {
                    MoveHead(new Vector3(-0.2, 0.2, -0.2));
                }
            }
        }

        private void MoveHead(Vector3 calculatedVector)
        {
            foreach (var item in pistonsX)
            {
                item.Velocity = calculatedVector.X * speedMultiplier;
            }
            foreach (var item in pistonsY)
            {
                item.Velocity = calculatedVector.Y * -1 * speedMultiplier * 3;
            }
            foreach (var item in pistonsZ)
            {
                item.Velocity = calculatedVector.Z * speedMultiplier;
            }
            rotorX.TargetVelocityRad = controlCockpit.RotationIndicator.Y * -1 * mouseSpeedXMultiplier;
            rotorY.TargetVelocityRad = controlCockpit.RotationIndicator.X * mouseSpeedYMultiplier;
        }

        private void CalculateRotatedVector(float angle, Vector2 cockpitVector2, out Vector2 rotation, out Vector3 calculatedVector)
        {
            float rotatedX, rotatedY;
            RotareOnAngle(angle, cockpitVector2, out rotation, out rotatedX, out rotatedY);
            calculatedVector = new Vector3(rotatedX, controlCockpit.MoveIndicator.Y, rotatedY);
        }

        private static void RotareOnAngle(float angle, Vector2 cockpitVector2, out Vector2 rotation, out float rotatedX, out float rotatedY)
        {
            rotation = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
            rotatedX = cockpitVector2.X * (float)Math.Cos(angle) - cockpitVector2.Y * (float)Math.Sin(angle);
            rotatedY = cockpitVector2.Y * (float)Math.Cos(angle) + cockpitVector2.X * (float)Math.Sin(angle);
        }

        public void Save()
        { }
        public string VectorToString(Vector3 vector)
        {
            return $"{vector.X.ToString("0.0")},{vector.Y.ToString("0.0")},{vector.Z.ToString("0.0")}";
        }
        public string VectorToString(Vector2 vector)
        {
            return $"{vector.X.ToString("0.00")},{vector.Y.ToString("0.00")}";
        }
        public string CombineStrings(params string[] strings)
        {
            return string.Join(".\n", strings);
        }
        //------------END--------------
    }
}