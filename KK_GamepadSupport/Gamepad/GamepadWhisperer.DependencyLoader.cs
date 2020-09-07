using System;
using System.IO;
using System.Runtime.InteropServices;

namespace KK_GamepadSupport.Gamepad
{
    public partial class GamepadWhisperer
    {
        private static class DependencyLoader
        {
            [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
            private static extern IntPtr LoadLibrary(string fileName);
            [DllImport("__Internal", CharSet = CharSet.Ansi)]
            private static extern void mono_dllmap_insert(IntPtr assembly, string dll, string func, string tdll, string tfunc);

            public static void LoadDependencies()
            {
                var assemblyPath = Path.GetDirectoryName(typeof(GamepadWhisperer).Assembly.Location);

                // Don't use .dll to avoid bepinex trying to load it and throwing an error
                var nativeLibFileName = "XInputInterface.lib";
                var nativeDllPath = Path.Combine(assemblyPath, nativeLibFileName);
                if (LoadLibrary(nativeDllPath) == IntPtr.Zero)
                    throw new IOException($"Failed to load {nativeDllPath}, verify that the file exists and is not corrupted.");
                // Needed to let the non-standard extension to work with dllimport
                mono_dllmap_insert(IntPtr.Zero, "XInputInterface", null, nativeLibFileName, null);

                var managedDllPath = Path.Combine(assemblyPath, "XInputDotNetPure.dll");
                AppDomain.CurrentDomain.Load(File.ReadAllBytes(managedDllPath));
            }
        }
    }
}