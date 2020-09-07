using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using StrayTech;
using UnityEngine;

namespace KK_GamepadSupport.Gamepad
{
    internal static partial class Hooks
    {
        private static class Camera
        {
            public static void InitHooks(Harmony hi)
            {
                // Main game camera control
                var newMethodTpl = new HarmonyMethod(AccessTools.Method(typeof(Camera), nameof(CameraStateTpl)));
                var iCamState = typeof(ICameraState);
                var types = iCamState.Assembly.GetTypes().Where(p => !p.IsAbstract && iCamState.IsAssignableFrom(p));
                foreach (var type in types)
                {
                    var oldMethod = AccessTools.Method(type, nameof(ICameraState.UpdateCamera));
                    hi.Patch(oldMethod, null, null, newMethodTpl);
                }

                hi.PatchAll(typeof(Camera));
            }

            #region Main game camera

            public static IEnumerable<CodeInstruction> CameraStateTpl(IEnumerable<CodeInstruction> instructions)
            {
                var newMethod = AccessTools.Method(typeof(Camera), nameof(CustomGetAxis));

                foreach (var instruction in instructions)
                {
                    if (instruction.operand is MethodInfo m && m.Name == "GetAxis")
                        instruction.operand = newMethod;

                    yield return instruction;
                }
            }

            public static float CustomGetAxis(string axisName)
            {
                if (!CursorEmulator.EmulatingCursor())
                {
                    switch (axisName)
                    {
                        case "Mouse X":
                            var x = GamepadWhisperer.CurrentState.ThumbSticks.Right.X;
                            if (Mathf.Abs(x) > 0.01f)
                                return x * Mathf.Abs(x);
                            break;

                        case "Mouse Y":
                            var y = GamepadWhisperer.CurrentState.ThumbSticks.Right.Y;
                            if (Mathf.Abs(y) > 0.01f)
                                return y * Mathf.Abs(y);
                            break;

                        case "Mouse ScrollWheel":
                            var ltVal = GamepadWhisperer.CurrentState.Triggers.Left;
                            var rtVal = GamepadWhisperer.CurrentState.Triggers.Right;
                            if (ltVal > 0.01f || rtVal > 0.01f)
                                return (-ltVal + rtVal) * 0.01f;
                            break;
                    }
                }

                return Input.GetAxis(axisName);
            }

            #endregion

            #region H camera

            [HarmonyPostfix]
            [HarmonyPatch(typeof(BaseCameraControl_Ver2), "InputKeyProc")]
            public static void InputKeyProcPost(BaseCameraControl_Ver2 __instance, ref bool __result)
            {
                if (_disabled) return;

                if (OnProcessCameraControls(__instance))
                    __result = true;
            }

            private static readonly FieldInfo _camDatField = AccessTools.Field(typeof(BaseCameraControl_Ver2), "CamDat");
            private static readonly FieldInfo _transBaseField = AccessTools.Field(typeof(BaseCameraControl_Ver2), "transBase");

            private static bool OnProcessCameraControls(BaseCameraControl_Ver2 cameraControl)
            {
                if (CursorEmulator.EmulatingCursor()) return false;

                const float xRotSpeed = 1.3f;
                const float yRotSpeed = 0.8f;
                const float moveSpeed = 0.03f;
                // todo expose?
                const float speedMultiplier = 1f;

                var stick = GamepadWhisperer.CurrentState.ThumbSticks.Right;
                var axis = stick.X * Mathf.Abs(stick.X);
                var axis2 = stick.Y * Mathf.Abs(stick.Y);

                if (Mathf.Abs(axis2) < 0.01f && Mathf.Abs(axis) < 0.01f)
                    return false;

                var ltVal = GamepadWhisperer.CurrentState.Triggers.Left;
                var rtVal = GamepadWhisperer.CurrentState.Triggers.Right;
                var ltPressed = ltVal > 0.3;
                var rtPressed = rtVal > 0.3;

                var cameraData = (BaseCameraControl_Ver2.CameraData)_camDatField.GetValue(cameraControl);

                if (!ltPressed && !rtPressed)
                {
                    var y = axis * xRotSpeed * speedMultiplier;
                    var x = -1 * axis2 * yRotSpeed * speedMultiplier;
                    cameraData.Rot.y = (cameraData.Rot.y + y) % 360f;
                    cameraData.Rot.x = (cameraData.Rot.x + x) % 360f;
                }
                else if (!rtPressed)
                {
                    cameraData.Pos.y = cameraData.Pos.y + axis2 * moveSpeed * speedMultiplier;
                    cameraData.Dir.z = cameraData.Dir.z - axis * moveSpeed * speedMultiplier;
                    cameraData.Dir.z = Mathf.Min(0f, cameraData.Dir.z);
                }
                else if (!ltPressed)
                {
                    var zero = new Vector3(axis * moveSpeed * speedMultiplier, 0, axis2 * moveSpeed * speedMultiplier);
                    var transBase = (Transform)_transBaseField.GetValue(cameraControl);
                    if (transBase != null)
                        cameraData.Pos = cameraData.Pos + transBase.InverseTransformDirection(cameraControl.transform.TransformDirection(zero));
                    else
                        cameraData.Pos = cameraData.Pos + cameraControl.transform.TransformDirection(zero);
                }
                else
                {
                    return false;
                }

                _camDatField.SetValue(cameraControl, cameraData);

                return true;
            }

            #endregion
        }
    }
}