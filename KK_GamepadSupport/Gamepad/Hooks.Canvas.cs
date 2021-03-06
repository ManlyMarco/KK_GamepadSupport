﻿using HarmonyLib;
using UnityEngine.EventSystems;
using XInputDotNetPure;

namespace KK_GamepadSupport.Gamepad
{
    internal static partial class Hooks
    {
        private static class Canvas
        {
            public static void InitHooks(Harmony hi)
            {
                hi.PatchAll(typeof(Canvas));
            }

            /// <summary>
            /// Make DPad work in Canvas UI
            /// </summary>
            [HarmonyPrefix]
            [HarmonyPatch(typeof(BaseInput), nameof(BaseInput.GetAxisRaw))]
            public static bool GetAxisRawHook(string axisName, ref float __result)
            {
                if (_disabled) return true;

                if (axisName == "Horizontal")
                {
                    if (GamepadWhisperer.CurrentState.DPad.Right == ButtonState.Pressed)
                    {
                        __result = 1;
                        return false;
                    }
                    if (GamepadWhisperer.CurrentState.DPad.Left == ButtonState.Pressed)
                    {
                        __result = -1;
                        return false;
                    }
                }
                else if (axisName == "Vertical")
                {
                    if (GamepadWhisperer.CurrentState.DPad.Up == ButtonState.Pressed)
                    {
                        __result = 1;
                        return false;
                    }
                    if (GamepadWhisperer.CurrentState.DPad.Down == ButtonState.Pressed)
                    {
                        __result = -1;
                        return false;
                    }
                }
                return true;
            }

            /// <summary>
            /// Make DPad work in Canvas UI
            /// </summary>
            [HarmonyPrefix]
            [HarmonyPatch(typeof(BaseInput), nameof(BaseInput.GetButtonDown))]
            public static bool GetButtonDownHook(string buttonName, ref bool __result)
            {
                if (_disabled) return true;

                if (buttonName == "Horizontal")
                {
                    if (GamepadWhisperer.GetButtonDown(state => state.DPad.Right) || GamepadWhisperer.GetButtonDown(state => state.DPad.Left))
                    {
                        __result = true;
                        return false;
                    }
                }
                else if (buttonName == "Vertical")
                {
                    if (GamepadWhisperer.GetButtonDown(state => state.DPad.Up) || GamepadWhisperer.GetButtonDown(state => state.DPad.Down))
                    {
                        __result = true;
                        return false;
                    }
                }
                return true;
            }
        }
    }
}