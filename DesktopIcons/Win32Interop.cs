using Avalonia.Controls;
using Avalonia.Platform;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DesktopIcons
{
    class Win32Interop
    {
        // send to back
        private const int HWND_BOTTOM = 1;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOACTIVATE = 0x0010;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        // prevent tablet mode maximize
        public const int GWL_STYLE = -16;
        public const int WS_MAXIMIZEBOX = 0x00010000;

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        public void KeepWindowAtBottom(Window window)
        {
            window.Opened += (_, _) =>
            {
                var hwnd = GetWindowHandle(window);
                if (hwnd != IntPtr.Zero)
                {
                    SetWindowPos(hwnd, new IntPtr(HWND_BOTTOM), 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
                }
            };

            window.Activated += (_, _) =>
            {
                var hwnd = GetWindowHandle(window);
                if (hwnd != IntPtr.Zero)
                {
                    SetWindowPos(hwnd, new IntPtr(HWND_BOTTOM), 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
                }
            };
        }

        public static IntPtr GetWindowHandle(Window window)
        {
            return window.TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
        }
    }
}
