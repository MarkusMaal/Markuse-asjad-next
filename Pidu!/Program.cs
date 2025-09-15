using Avalonia;
using System;

namespace Pidu_
{
    internal class Program
    {
        /// <summary>
        /// If enabled, this app will write current playing song name to *mas_root*/songname.txt 
        /// </summary>
        public static bool streamMode = false;
        
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        [STAThread]
        public static void Main(string[] args) => BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);

        // Avalonia configuration, don't remove; also used by visual designer.
        private static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace()
                .With(new MacOSPlatformOptions { DisableDefaultApplicationMenuItems = true});
    }
}
