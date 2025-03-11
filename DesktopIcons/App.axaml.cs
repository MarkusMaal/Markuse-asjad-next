using System;
using System.Diagnostics;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace DesktopIcons;

public class App : Application
{
    private static bool _exiting;
    public override void Initialize()
    {
        if (!_exiting)
        {
            AvaloniaXamlLoader.Load(this);
        }
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var args = desktop.Args;
            if (args.Length > 0)
            {
                if (args.Contains("--icons"))
                {
                    _exiting = true;
                    foreach (var key in Resources.Keys)
                    {
                        if (key.ToString().StartsWith("TopIcon"))
                        {
                            Console.WriteLine(key.ToString()![7..]);
                        }
                    }
                    Process.GetCurrentProcess().Kill();
                    return;
                }
            }

            if (!_exiting)
            {
                desktop.MainWindow = new MainWindow
                {
                    DataContext = new MainWindowModel(),
                };
            }
        }

        base.OnFrameworkInitializationCompleted();
    }
}