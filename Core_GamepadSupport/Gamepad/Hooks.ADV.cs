using ADV;
using HarmonyLib;

namespace KK_GamepadSupport.Gamepad
{
    internal static partial class Hooks
    {
        private static class ADV
        {
            public static void InitHooks(Harmony hi)
            {
                hi.PatchAll(typeof(ADV));
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(KeyInput), nameof(KeyInput.SkipButton), MethodType.Getter)]
            public static bool SkipButtonHook(ref bool __result)
            {
                if (_disabled) return true;
                if (GamepadWhisperer.GetButton(state => state.Buttons.X))
                {
                    __result = true;
                    return false;
                }
                return true;
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(KeyInput), nameof(KeyInput.BackLogTextPageNext), MethodType.Getter)]
            public static bool BackLogTextPageNextHook(ref bool __result)
            {
                if (_disabled) return true;

                if (GamepadWhisperer.RightStickDown() && WithinAngle(GamepadWhisperer.GetRightStickAngle(), 90))
                {
                    __result = true;
                    return false;
                }

                return true;
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(KeyInput), nameof(KeyInput.BackLogTextPageBack), MethodType.Getter)]
            public static bool BackLogTextPageBackHook(ref bool __result)
            {
                if (_disabled) return true;

                if (GamepadWhisperer.RightStickDown() && WithinAngle(GamepadWhisperer.GetRightStickAngle(), -90))
                {
                    __result = true;
                    return false;
                }

                return true;
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(KeyInput), nameof(KeyInput.BackLogTextNext))]
            public static void BackLogTextNext(ref KeyInput.Data __result)
            {
                if (_disabled) return;

                if (GamepadWhisperer.RightStickDown() && WithinAngle(GamepadWhisperer.GetRightStickAngle(), 0))
                    __result.isKey = true;
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(KeyInput), nameof(KeyInput.BackLogTextBack))]
            public static void BackLogTextBack(ref KeyInput.Data __result)
            {
                if (_disabled) return;

                if (GamepadWhisperer.RightStickDown() && WithinAngle(GamepadWhisperer.GetRightStickAngle(), 180))
                    __result.isKey = true;
            }

            private static bool WithinAngle(float val, float angle)
            {
                return WithinRange(val, angle - 45f, angle + 45f);
            }
            private static bool WithinRange(float val, float min, float max)
            {
                return val >= min && val < max;
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(KeyInput), nameof(KeyInput.TextNext))]
            public static void TextNext(ref KeyInput.Data __result)
            {
                if (_disabled) return;

                if (GamepadWhisperer.GetButtonDown(state => state.Buttons.B))
                    __result.isKey = true;
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(KeyInput), nameof(KeyInput.WindowNoneButton))]
            public static void WindowNoneButton(ref KeyInput.Data __result)
            {
                if (_disabled) return;

                if (GamepadWhisperer.GetButtonDown(state => state.Buttons.Y))
                    __result.isKey = true;
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(KeyInput), nameof(KeyInput.WindowNoneButtonCancel))]
            public static void WindowNoneButtonCancel(ref KeyInput.Data __result)
            {
                if (_disabled) return;
                if (GamepadWhisperer.GetButtonDown(state => state.Buttons.Y) || GamepadWhisperer.GetButtonDown(state => state.Buttons.B))
                    __result.isKey = true;
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(KeyInput), nameof(KeyInput.BackLogButton))]
            public static void BackLogButton(ref KeyInput.Data __result)
            {
                if (_disabled) return;

                if (GamepadWhisperer.RightStickDown() || GamepadWhisperer.GetButtonDown(state => state.Buttons.RightStick) && !CursorEmulator.EmulatingCursor())
                    __result.isKey = true;
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(KeyInput), nameof(KeyInput.BackLogButtonCancel))]
            public static void BackLogButtonCancel(ref KeyInput.Data __result)
            {
                if (_disabled) return;

                if (GamepadWhisperer.GetButtonDown(state => state.Buttons.B) || GamepadWhisperer.GetButtonDown(state => state.Buttons.RightStick) && !CursorEmulator.EmulatingCursor())
                    __result.isKey = true;
            }
        }
    }
}
