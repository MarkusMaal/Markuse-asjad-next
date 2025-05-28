using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using System;
using System.IO;
using System.Linq;
using MasCommon;

namespace TöölauaMärkmed
{
    public partial class App : Application
    {
        public int activeindex = 1;
        public readonly string masRoot = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/.mas";
        readonly CommonConfig config = new()
        {
            AutostartNotes = false,
        };
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                config.Load(masRoot);
                if (!desktop.Args.Contains("-s"))
                {
                    desktop.MainWindow = new MainWindow();
                } else
                {
                    if (config.AutostartNotes) { 
                        File.WriteAllText(masRoot + "/noteopen.txt", "");
                        desktop.MainWindow = new MainWindow();
                    }
                    else
                    {
                        return;
                    }
                }
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}