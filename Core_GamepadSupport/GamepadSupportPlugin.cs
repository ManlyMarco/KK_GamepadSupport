﻿using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using KK_GamepadSupport.Gamepad;
using KK_GamepadSupport.Navigation;
using KKAPI;

namespace KK_GamepadSupport
{
    [BepInProcess(KoikatuAPI.GameProcessName)]
#if KK
    [BepInProcess(KoikatuAPI.GameProcessNameSteam)]
#endif
    [BepInDependency(KoikatuAPI.GUID, KoikatuAPI.VersionConst)]
    [BepInPlugin(Guid, Constants.Name, Constants.Version)]
    public sealed class GamepadSupportPlugin : BaseUnityPlugin
    {
        public const string Version = Constants.Version; // Set the version in Directory.Build.props
        public const string Guid = "GamepadSupport";

        internal static new ManualLogSource Logger;
        internal static ConfigEntry<bool> CanvasDebug { get; private set; }

        private void Awake()
        {
            Logger = base.Logger;

            CanvasDebug = Config.Bind("Debug", "Show debug information", false, new ConfigDescription("Show debug information about visible canvases and selected objects", null, "Advanced"));

            if (Config.Bind("General", "Enable gamepad and keyboard support", true, "Turn the plugin on or off. Game restart is needed for changes to apply.").Value)
            {
                try
                {
                    // NEED to load the native dll BEFORE any class with DllImport is touched or the dll won't be found
                    DependencyLoader.LoadDependencies();
                }
                catch (Exception ex)
                {
                    Logger.Log(LogLevel.Message | LogLevel.Error, "GamepadSupport plugin failed to load: " + ex.Message);
                    return;
                }

                gameObject.AddComponent<CanvasCharmer>();
                gameObject.AddComponent<GamepadWhisperer>();
            }
        }
    }
}
