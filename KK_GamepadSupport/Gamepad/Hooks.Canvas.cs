using Harmony;
using UnityEngine.EventSystems;
using XInputDotNetPure;

namespace KK_GamepadSupport.Gamepad
{
    internal static partial class Hooks
    {
        private static class Canvas
        {
            public static void InitHooks(HarmonyInstance hi)
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
                    if (GamepadSupport.CurrentState.DPad.Right == ButtonState.Pressed)
                    {
                        __result = 1;
                        return false;
                    }
                    if (GamepadSupport.CurrentState.DPad.Left == ButtonState.Pressed)
                    {
                        __result = -1;
                        return false;
                    }
                }
                else if (axisName == "Vertical")
                {
                    if (GamepadSupport.CurrentState.DPad.Up == ButtonState.Pressed)
                    {
                        __result = 1;
                        return false;
                    }
                    if (GamepadSupport.CurrentState.DPad.Down == ButtonState.Pressed)
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
                    if (GamepadSupport.GetButtonDown(state => state.DPad.Right) || GamepadSupport.GetButtonDown(state => state.DPad.Left))
                    {
                        __result = true;
                        return false;
                    }
                }
                else if (buttonName == "Vertical")
                {
                    if (GamepadSupport.GetButtonDown(state => state.DPad.Up) || GamepadSupport.GetButtonDown(state => state.DPad.Down))
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