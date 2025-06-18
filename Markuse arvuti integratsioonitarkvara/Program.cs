using Avalonia;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MasCommon;

namespace Markuse_arvuti_integratsioonitarkvara
{
    internal class Program
    {


        public static readonly CommonConfig config = new()
        {
            AllowScheduledTasks = false,
            ShowLogo = true,
            AutostartNotes = false,
            PollRate = 5000
        };

        public static bool CodeOpen = false;

        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        [STAThread]
        public static void Main(string[] args)
        {
            try
            {
                BuildAvaloniaApp()
                    .StartWithClassicDesktopLifetime(args, ShutdownMode.OnExplicitShutdown);
            }
            catch (Exception ex) when (!Debugger.IsAttached) {
                CatchErrors(ex, "Program.Main");
            }
        }

        // Avalonia configuration, don't remove; also used by visual designer.
        private static AppBuilder BuildAvaloniaApp()
        {
            try {
                return AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace()
                .With(new MacOSPlatformOptions { ShowInDock = false });
            }
            catch (Exception ex) when (!Debugger.IsAttached)  {
                CatchErrors(ex, "Program.BuildAvaloniaApp");
                return null;
            }
        }

        public static void CatchErrors(Exception ex, string invoker)
        {
            var exePath = Environment.ProcessPath;
            if (!OperatingSystem.IsLinux())
            {
                Process.Start(new ProcessStartInfo(exePath!) { UseShellExecute = true, Arguments = "/e" });
            }
            File.WriteAllText(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/mas_error.log", $"----------------------------------------------\nMarkuse arvuti integratsioonitarkvara\n----------------------------------------------\n\nPeatasime Markuse arvuti integratsioonitarkvara probleemi tõttu. Palun käivitage see programm siluriga, et asja täpsemalt uurida.\n\nTehniline info:\n\nRakendus: {exePath ?? "?"}\nKuupäev ja kellaaeg: {DateTime.Now}\nVälja kutsuja: {invoker}\nErand: {ex.Message}\nKuhila jälg:\n{ex.StackTrace}");
            Environment.Exit(0);
        }

        public static void Restart()
        {
            var exePath = Environment.ProcessPath;
            Process.Start(new ProcessStartInfo(exePath!) { UseShellExecute = true });
            Environment.Exit(0);
        }
    }
}
