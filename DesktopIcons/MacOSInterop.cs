using Avalonia.Controls;
using System;
using System.Runtime.InteropServices;

namespace DesktopIcons
{
    // thx @almenscorner on Reddit
    public static class MacOSInterop
    {
        [DllImport("/System/Library/Frameworks/AppKit.framework/AppKit")]
        public static extern IntPtr objc_getClass(string className);

        [DllImport("/System/Library/Frameworks/AppKit.framework/AppKit")]
        public static extern IntPtr sel_registerName(string selector);

        [DllImport("/System/Library/Frameworks/AppKit.framework/AppKit")]
        public static extern void objc_msgSend(IntPtr receiver, IntPtr selector, IntPtr arg);

        public static void SetWindowLevel(IntPtr nsWindow, int level)
        {
            IntPtr setLevelSelector = sel_registerName("setLevel:");
            objc_msgSend(nsWindow, setLevelSelector, (IntPtr)level);
        }

        public static void SetIgnoresMouseEvents(IntPtr nsWindow, bool ignores)
        {
            IntPtr setIgnoresMouseEventsSelector = sel_registerName("setIgnoresMouseEvents:");
            objc_msgSend(nsWindow, setIgnoresMouseEventsSelector, (IntPtr)(ignores ? 1 : 0));
        }

        public static void SetCollectionBehavior(IntPtr nsWindow, int behavior)
        {
            IntPtr setCollectionBehaviorSelector = sel_registerName("setCollectionBehavior:");
            objc_msgSend(nsWindow, setCollectionBehaviorSelector, (IntPtr)behavior);
        }


        private static IntPtr GetNativeWindowHandle(Window w)
        {
            var platformHandle = w.TryGetPlatformHandle();
            return platformHandle?.Handle ?? IntPtr.Zero;
        }

        private enum NSWindowLevel
        {
            Normal = 0,       // just a normal window
            Desktop = -1,     // will only show on current workspace
            BelowDesktop = -2 // will show on all workspaces
        }

        public static void SetWindowProperties(Window w)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                var handle = GetNativeWindowHandle(w);
                if (handle != IntPtr.Zero)
                {
                    // Set the window level to desktop, behind all other windows
                    MacOSInterop.SetWindowLevel(handle, (int)NSWindowLevel.BelowDesktop);

                    // Make the window ignore mouse events to prevent it from coming to the foreground
                    MacOSInterop.SetIgnoresMouseEvents(handle, false);

                    // Set collection behavior to stick the window to the desktop
                    const int NSWindowCollectionBehaviorCanJoinAllSpaces = 1 << 0;
                    const int NSWindowCollectionBehaviorStationary = 1 << 4;
                    MacOSInterop.SetCollectionBehavior(handle, NSWindowCollectionBehaviorCanJoinAllSpaces | NSWindowCollectionBehaviorStationary);
                }
            }
        }

    }
}
