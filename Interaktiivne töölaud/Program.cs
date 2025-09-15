using Avalonia;
using System;
using MasCommon;

namespace Interaktiivne_töölaud
{
    internal class Program
    {
        public static Verifile vf = new();
        
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
