using System.Runtime.InteropServices;
using BepInEx.Logging;
using UnityEngine;

namespace KK_GamepadSupport.Gamepad
{
    public static class CursorEmulator
    {
        public static void LeftDown()
        {
            Native.mouse_event(Native.MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
        }

        public static void LeftUp()
        {
            Native.mouse_event(Native.MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
        }

        public static void RightDown()
        {
            Native.mouse_event(Native.MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, 0);
        }

        public static void RightUp()
        {
            Native.mouse_event(Native.MOUSEEVENTF_RIGHTUP, 0, 0, 0, 0);
        }

        public static void MoveCursor(Vector2 amount)
        {
            var x = (int)amount.x;
            var y = -(int)amount.y;
            Native.mouse_event(Native.MOUSEEVENTF_MOVE, x, y, 0, 0);
        }

        public static void OnUpdate()
        {
            if (!Application.isFocused)
            {
                if (_enabled)
                {
                    LeftUp();
                    RightUp();
                }
                _enabled = false;
            }

            // Deadzone for trigger presses
            var rPressed = _previousR ? GamepadWhisperer.CurrentState.Triggers.Right > 0.1f : GamepadWhisperer.CurrentState.Triggers.Right > 0.4f;
            var lPressed = _previousL ? GamepadWhisperer.CurrentState.Triggers.Left > 0.1f : GamepadWhisperer.CurrentState.Triggers.Left > 0.4f;

            var bothPressed = rPressed && lPressed;
            if (bothPressed != _previousBoth)
            {
                _previousBoth = bothPressed;
                if (bothPressed)
                {
                    _enabled = !_enabled;

                    GamepadSupportPlugin.Logger.Log(LogLevel.Message, "Cursor mode " + (_enabled ? "ON (LTrig and RTrig for mouse buttons)" : "OFF"));

                    // Fix stuck keys
                    if (!_enabled)
                    {
                        LeftUp();
                        RightUp();
                    }
                }
            }

            if (EmulatingCursor())
            {
                Cursor.visible = false;
                var rightStick = GamepadWhisperer.GetRightStick();
                if (rightStick.magnitude > 0)
                {
                    var expStick = new Vector2(rightStick.x * Mathf.Abs(rightStick.x), rightStick.y * Mathf.Abs(rightStick.y));
                    var amount = expStick * Time.deltaTime * 700;
                    MoveCursor(amount);
                }

                if (rPressed && !_previousR)
                    LeftDown();
                else if (!rPressed && _previousR)
                    LeftUp();

                if (lPressed && !_previousL)
                    RightDown();
                else if (!lPressed && _previousL)
                    RightUp();

                var scrollAmount = Mathf.RoundToInt(GamepadWhisperer.CurrentState.ThumbSticks.Left.Y * Native.WHEEL_DELTA * Time.deltaTime);
                if (scrollAmount != 0)
                    Native.mouse_event(Native.MOUSEEVENTF_WHEEL, 0, 0, scrollAmount, 0);
            }
            else
            {
                Cursor.visible = true;
            }

            _previousR = rPressed;
            _previousL = lPressed;
        }

        private static class Native
        {
            public const int MOUSEEVENTF_LEFTDOWN = 0x02;
            public const int MOUSEEVENTF_LEFTUP = 0x04;
            public const int MOUSEEVENTF_MOVE = 0x01;
            public const int MOUSEEVENTF_RIGHTDOWN = 0x08;
            public const int MOUSEEVENTF_RIGHTUP = 0x10;
            public const int MOUSEEVENTF_WHEEL = 0x0800;

            public const int WHEEL_DELTA = 120;

            [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
            public static extern void mouse_event(long dwFlags, long dx, long dy, long cButtons, long dwExtraInfo);
        }

        private static bool _enabled;
        public static bool EmulatingCursor()
        {
            return _enabled;
        }

        private static bool _previousBoth;
        private static bool _previousL;
        private static bool _previousR;
    }
}
