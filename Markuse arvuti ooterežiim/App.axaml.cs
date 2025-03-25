using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using System;
using System.IO;

namespace Markuse_arvuti_ooterežiim
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                if (desktop.Args.Length > 0)
                {
                    // some windows bs
                    switch (desktop.Args[0])
                    {
                        case "/p":
                            Environment.Exit(0);
                            return;
                        case "/c":
                            desktop.MainWindow = new SettingsWindow();
                            break;
                        case "/s":
                        case "/S":
                            desktop.MainWindow = new MainWindow();
                            break;
                        case "/t":
                            desktop.MainWindow = new MainWindow();
                            break;
                    }
                    if (desktop.Args[0].Contains("/c"))
                    {
                        if (desktop.Args[0].Contains(':'))
                        {
                            Program.settingsid = int.Parse(desktop.Args[0].Split(':')[1]);
                        }
                        desktop.MainWindow = new SettingsWindow();
                    }
                    else if (desktop.Args[0].Contains("/p"))
                    {
                        Environment.Exit(0);
                    }
                } else
                {
                    desktop.MainWindow = new MainWindow();
                }
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}