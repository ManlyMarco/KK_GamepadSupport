
using BepInEx.Logging;
using HarmonyLib;

namespace KK_GamepadSupport.Gamepad
{
    internal static partial class Hooks
    {
        public static void InitHooks()
        {
            var hi = new Harmony(GamepadSupportPlugin.Guid + ".Gamepad");
            Camera.InitHooks(hi);
            Canvas.InitHooks(hi);
            MainGameMap.InitHooks(hi);
            ShortcutKeyHooks.InitHooks(hi);
            ADV.InitHooks(hi);
            H.InitHooks(hi);
        }

        private static bool _disabled;
        public static void RemoveHooks()
        {
            _disabled = true;
        }

        private static ManualLogSource Logger => GamepadSupportPlugin.Logger;
    }
}
