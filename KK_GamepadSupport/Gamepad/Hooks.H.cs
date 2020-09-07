using HarmonyLib;
using UnityEngine;

namespace KK_GamepadSupport.Gamepad
{
    internal static partial class Hooks
    {
        private static class H
        {
            public static void InitHooks(Harmony hi)
            {
                hi.PatchAll(typeof(H));
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(HSprite), "Update")]
            public static void HSpriteUpdateHook(HSprite __instance)
            {
                if (_disabled) return;

                if (GamepadSupport.GetButtonDown(state => state.Buttons.X))
                    __instance.flags.click = HFlag.ClickKind.motionchange;

                if (GamepadSupport.GetButtonDown(state => state.Buttons.Y))
                    __instance.flags.click = HFlag.ClickKind.modeChange;

                if (GamepadSupport.GetButton(state => state.Buttons.LeftShoulder))
                {
                    __instance.flags.SpeedUpClick(-__instance.flags.rateWheelSpeedUp * Time.deltaTime * 6, 1f);

                    // Manual piston
                    if (GamepadSupport.GetButtonDown(state => state.Buttons.RightShoulder))
                        __instance.flags.click = HFlag.ClickKind.speedup;
                }
                else if (GamepadSupport.GetButton(state => state.Buttons.RightShoulder))
                {
                    __instance.flags.SpeedUpClick(__instance.flags.rateWheelSpeedUp * Time.deltaTime * 6, 1f);

                    // Manual piston
                    if (GamepadSupport.GetButtonDown(state => state.Buttons.LeftShoulder))
                        __instance.flags.click = HFlag.ClickKind.speedup;
                }
            }
        }
    }
}
