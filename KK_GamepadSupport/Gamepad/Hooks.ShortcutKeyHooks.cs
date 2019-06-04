using System;
using System.Collections.Generic;
using Harmony;
using Illusion.Component;
using UnityEngine;
using XInputDotNetPure;

namespace KK_GamepadSupport.Gamepad
{
    internal static partial class Hooks
    {
        private static class ShortcutKeyHooks
        {
            public static void InitHooks(HarmonyInstance hi)
            {
                hi.PatchAll(typeof(ShortcutKeyHooks));
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(ShortcutKey), "Update")]
            public static bool UpdateHook(ShortcutKey __instance)
            {
                if (_disabled) return true;

                if (GamepadSupport.CurrentState.IsConnected)
                {
                    foreach (var proc in __instance.procList)
                    {
                        if (proc.enabled)
                        {
                            if (_hotkeyBindings.TryGetValue(proc.keyCode, out var func) && GamepadSupport.GetButtonDown(func))
                            {
                                proc.call.Invoke();
                                return false;
                            }
                        }
                    }
                }

                return true;
            }

            private static readonly Dictionary<KeyCode, Func<GamePadState, ButtonState>> _hotkeyBindings = new Dictionary<KeyCode, Func<GamePadState, ButtonState>>
            {
                {KeyCode.F1, state => state.Buttons.Back },
                {KeyCode.F3, state => state.Buttons.Y },
                {KeyCode.F5, state => state.Buttons.Start },
                {KeyCode.Escape, state => state.Buttons.Guide }
            };
        }
    }
}
