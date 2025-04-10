using Avalonia;
using System;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Transformation;

namespace Markuse_arvuti_juhtpaneel
{
    internal class Program
    {
        public static bool Launcherror = false;

        public static readonly byte[] Universe = [0x79, 0x61, 0x44, 0x2c, 0x78, 0x69, 0x50, 0x2c, 0x72, 0x61, 0x4d, 0x2c, 0x61,
            0x6e, 0x61, 0x42, 0x2c, 0x54, 0x52, 0x45, 0x2c, 0x61, 0x6e, 0x73, 0x45, 0x2c, 0x73, 0x6b, 0x55, 0x2c, 0x21,
            0x72, 0x65, 0x70, 0x75, 0x53, 0x2c, 0x53, 0x49, 0x49, 0x53, 0x2c, 0x69, 0x73, 0x73, 0x69, 0x2c, 0x72, 0x65,
            0x6c, 0x6c, 0x69, 0x46, 0x2c, 0x54, 0x45, 0x2c, 0xa2, 0x84, 0xe2, 0x74, 0x72, 0x45, 0x2c, 0x6c, 0x61, 0x69,
            0x63, 0x65, 0x70, 0x53, 0x20, 0x74, 0x72, 0x45, 0x2c, 0x74, 0x72, 0x45];
        
        public static IBrush BgCol { get; set; }
        public static IBrush FgCol { get; set; }
        
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        [STAThread]
        public static void Main(string[] args)
        {
            
            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args, ShutdownMode.OnExplicitShutdown);
        }

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();
    }
}
