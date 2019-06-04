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
            if (EmulatingCursor())
            {
                var amount = GamepadSupport.GetRightStick() * Time.deltaTime * 700;
                if (amount.magnitude > 0)
                    MoveCursor(amount);

                if (GamepadSupport.GetButtonDown(state => state.Buttons.LeftStick))
                    LeftDown();
                else if (GamepadSupport.GetButtonUp(state => state.Buttons.LeftStick))
                    LeftUp();

                if (GamepadSupport.GetButtonDown(state => state.Buttons.RightStick))
                    RightDown();
                else if (GamepadSupport.GetButtonUp(state => state.Buttons.RightStick))
                    RightUp();

                var scrollAmount = Mathf.RoundToInt(GamepadSupport.CurrentState.ThumbSticks.Left.Y * Native.WHEEL_DELTA * Time.deltaTime);
                if (scrollAmount != 0)
                {
                    BepInEx.Logger.Log(LogLevel.Info, "scroll");
                    Native.mouse_event(Native.MOUSEEVENTF_WHEEL, 0, 0, scrollAmount, 0);
                }
            }
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

        public static bool EmulatingCursor()
        {
            return GamepadSupport.CurrentState.Triggers.Right > 0.01f && GamepadSupport.CurrentState.Triggers.Left > 0.01f;
        }
    }
}
