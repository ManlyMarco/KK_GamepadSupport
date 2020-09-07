using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using KK_GamepadSupport.Gamepad;
using KK_GamepadSupport.Navigation;

namespace KK_GamepadSupport
{
    [BepInProcess("Koikatu")]
    [BepInProcess("Koikatsu Party")]
    [BepInDependency(KKAPI.KoikatuAPI.GUID, "1.12")]
    [BepInPlugin(Guid, Guid, Version)]
    public sealed class GamepadSupportPlugin : BaseUnityPlugin
    {
        public const string Version = "2.0";
        public const string Guid = "GamepadSupport";

        internal static new ManualLogSource Logger;
        internal static ConfigEntry<bool> CanvasDebug { get; private set; }

        private void Awake()
        {
            Logger = base.Logger;

            CanvasDebug = Config.Bind("Debug", "Show debug information", false, new ConfigDescription("Show debug information about visible canvases and selected objects", null, "Advanced"));

            if (Config.Bind("General", "Enable gamepad and keyboard support", true, "Turn the plugin on or off. Game restart is needed for changes to apply.").Value)
            {
                gameObject.AddComponent<CanvasCharmer>();
                gameObject.AddComponent<GamepadWhisperer>();
            }
        }
    }
}
