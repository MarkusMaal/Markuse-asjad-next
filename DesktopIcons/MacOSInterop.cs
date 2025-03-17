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
    }
}
