﻿using Avalonia;
using System;

namespace Markuse_arvuti_ooterežiim
{
    internal class Program
    {
        public static int monitors = 1;
        public static int settingsid = 0;

        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        [STAThread]
        public static void Main(string[] args) => BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace()
                .With(new MacOSPlatformOptions { DisableDefaultApplicationMenuItems = true});
    }
}
