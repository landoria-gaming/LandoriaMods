using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Landoria.CharacterVault
{
    internal sealed class WindowsCloseInterceptor : IDisposable
    {
        private const int WindowProcedureIndex = -4;
        private const uint CloseMessage = 0x0010;
        private readonly WindowProcedure _windowProcedure;
        private IntPtr _windowHandle;
        private IntPtr _originalWindowProcedure;
        private bool _allowClose;
        private bool _closeRequested;

        internal WindowsCloseInterceptor()
        {
            _windowProcedure = HandleWindowMessage;
        }

        internal void EnsureInstalled()
        {
            if (_windowHandle != IntPtr.Zero || Environment.OSVersion.Platform != PlatformID.Win32NT)
            {
                return;
            }

            using (Process process = Process.GetCurrentProcess())
            {
                process.Refresh();
                IntPtr windowHandle = process.MainWindowHandle;
                if (windowHandle == IntPtr.Zero)
                {
                    return;
                }

                Marshal.GetLastWin32Error();
                IntPtr previous = SetWindowProcedure(windowHandle, _windowProcedure);
                if (previous == IntPtr.Zero && Marshal.GetLastWin32Error() != 0)
                {
                    CharacterVaultPlugin.Log.LogWarning(
                        "Could not intercept the Windows close button; normal quit protection remains active.");
                    return;
                }

                _windowHandle = windowHandle;
                _originalWindowProcedure = previous;
                CharacterVaultPlugin.Log.LogInfo("Windows close protection is active.");
            }
        }

        internal bool ConsumeCloseRequest()
        {
            if (!_closeRequested)
            {
                return false;
            }

            _closeRequested = false;
            return true;
        }

        internal void AuthorizeClose()
        {
            _allowClose = true;
        }

        public void Dispose()
        {
            if (_windowHandle == IntPtr.Zero || _originalWindowProcedure == IntPtr.Zero)
            {
                return;
            }

            SetWindowLongPtr(_windowHandle, WindowProcedureIndex, _originalWindowProcedure);
            _windowHandle = IntPtr.Zero;
            _originalWindowProcedure = IntPtr.Zero;
        }

        private IntPtr HandleWindowMessage(IntPtr window, uint message, IntPtr wParam, IntPtr lParam)
        {
            if (message == CloseMessage && !_allowClose)
            {
                _closeRequested = true;
                return IntPtr.Zero;
            }

            return CallWindowProc(_originalWindowProcedure, window, message, wParam, lParam);
        }

        private static IntPtr SetWindowProcedure(IntPtr window, WindowProcedure procedure)
        {
            return IntPtr.Size == 8
                ? SetWindowLongPtr64(window, WindowProcedureIndex, procedure)
                : new IntPtr(SetWindowLong32(window, WindowProcedureIndex, procedure));
        }

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate IntPtr WindowProcedure(IntPtr window, uint message, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong", SetLastError = true)]
        private static extern int SetWindowLong32(IntPtr window, int index, WindowProcedure procedure);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
        private static extern IntPtr SetWindowLongPtr64(IntPtr window, int index, WindowProcedure procedure);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
        private static extern IntPtr SetWindowLongPtr(IntPtr window, int index, IntPtr procedure);

        [DllImport("user32.dll")]
        private static extern IntPtr CallWindowProc(
            IntPtr previous, IntPtr window, uint message, IntPtr wParam, IntPtr lParam);
    }
}
