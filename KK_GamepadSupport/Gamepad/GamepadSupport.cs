using System;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using UnityEngine;
using XInputDotNetPure;

namespace KK_GamepadSupport.Gamepad
{
    [BepInProcess("Koikatu")]
    [BepInProcess("Koikatsu Party")]
    [BepInDependency(KKAPI.KoikatuAPI.GUID, "1.12")]
    [BepInPlugin(Guid, Guid, Metadata.Version)]
    // Run before any other MonoBehaviours
    [DefaultExecutionOrder(-100)]
    public partial class GamepadSupport : BaseUnityPlugin
    {
        public const string Guid = Metadata.BaseGuid + ".GamepadController";

        internal static new ManualLogSource Logger;

        private static GamePadState _currentState, _previousState;

        private void Awake()
        {
            Logger = base.Logger;

            try
            {
                DependencyLoader.LoadDependencies();
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Message | LogLevel.Error, "GamepadSupport plugin failed to load: " + ex.Message);
                enabled = false;
                return;
            }

            _currentState = _previousState = GamePad.GetState(PlayerIndex.One, GamePadDeadZone.IndependentAxes);
            Hooks.InitHooks();
        }

        private void OnDestroy()
        {
            Hooks.RemoveHooks();
        }

        private void Update()
        {
            _previousState = _currentState;
            _currentState = GamePad.GetState(PlayerIndex.One, GamePadDeadZone.IndependentAxes);

            // Go to sleep if no controller is detected
            if (!_currentState.IsConnected)
            {
                _previousState = _currentState;
                enabled = false;
                // Need to use invoke because coroutines don't run on disabled objects
                InvokeRepeating(nameof(TryWakeUp), 4f, 4f);
            }
            else
            {
                CursorEmulator.OnUpdate();

                // Simulate right click for cancelling out of menus
                // Ideal would be hooking Input.GetMouseKeyDown but it requires a detour
                if (GetButtonDown(state => state.Buttons.B))
                {
                    CursorEmulator.RightDown();
                    CursorEmulator.RightUp();
                }
            }
        }

        private void TryWakeUp()
        {
            if (GamePad.GetState(PlayerIndex.One, GamePadDeadZone.IndependentAxes).IsConnected)
            {
                CancelInvoke(nameof(TryWakeUp));
                enabled = true;
            }
        }

        public static GamePadState CurrentState => _currentState;

        /// <summary>
        /// True if gamepad button is currently pressed
        /// </summary>
        public static bool GetButton(Func<GamePadState, ButtonState> selector)
        {
            if (!_currentState.IsConnected) return false;
            return selector(_currentState) == ButtonState.Pressed;
        }

        /// <summary>
        /// True if gamepad button has just been released
        /// </summary>
        public static bool GetButtonUp(Func<GamePadState, ButtonState> selector)
        {
            if (!_currentState.IsConnected) return false;
            return selector(_currentState) == ButtonState.Released && selector(_previousState) == ButtonState.Pressed;
        }

        /// <summary>
        /// True if gamepad button has just been pressed
        /// </summary>
        public static bool GetButtonDown(Func<GamePadState, ButtonState> selector)
        {
            if (!_currentState.IsConnected) return false;
            return selector(_currentState) == ButtonState.Pressed && selector(_previousState) == ButtonState.Released;
        }

        public static Vector2 GetLeftStickDpadCombined()
        {
            var x = CurrentState.DPad.Left == ButtonState.Pressed ? -1f : (CurrentState.DPad.Right == ButtonState.Pressed ? 1f : CurrentState.ThumbSticks.Left.X);
            var y = CurrentState.DPad.Down == ButtonState.Pressed ? -1f : (CurrentState.DPad.Up == ButtonState.Pressed ? 1f : CurrentState.ThumbSticks.Left.Y);
            return new Vector2(x, y);
        }

        public static float GetLeftStickAngle()
        {
            var stickPosition = GetLeftStickDpadCombined();
            if (!Mathf.Approximately(stickPosition.x, 0f) || !Mathf.Approximately(stickPosition.y, 0f))
            {
                var absAngle = Vector2.Angle(Vector2.up, stickPosition);
                return stickPosition.x >= 0 ? absAngle : -absAngle;
            }
            return 0;
        }

        public static float GetRightStickAngle()
        {
            var stickPosition = GetRightStick();
            if (!Mathf.Approximately(stickPosition.x, 0f) || !Mathf.Approximately(stickPosition.y, 0f))
            {
                var absAngle = Vector2.Angle(Vector2.up, stickPosition);
                return stickPosition.x >= 0 ? absAngle : -absAngle;
            }
            return 0;
        }

        public static Vector2 GetRightStick()
        {
            var x = CurrentState.ThumbSticks.Right.X;
            var y = CurrentState.ThumbSticks.Right.Y;
            return new Vector2(x, y);
        }

        public static bool RightStickDown()
        {
            if (CursorEmulator.EmulatingCursor())
                return false;

            return Mathf.Approximately(_previousState.ThumbSticks.Right.X, 0f) && Mathf.Approximately(_previousState.ThumbSticks.Right.Y, 0f)
               && (!Mathf.Approximately(_currentState.ThumbSticks.Right.X, 0f) || !Mathf.Approximately(_currentState.ThumbSticks.Right.Y, 0f));
        }
    }
}
