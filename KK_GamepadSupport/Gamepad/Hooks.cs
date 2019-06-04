using Harmony;

namespace KK_GamepadSupport.Gamepad
{
    internal static partial class Hooks
    {
        public static void InitHooks()
        {
            var hi = HarmonyInstance.Create(GamepadSupport.Guid);
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
    }
}
