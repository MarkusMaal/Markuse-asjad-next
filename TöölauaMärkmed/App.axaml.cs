using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using System;
using System.IO;
using System.Linq;

namespace TöölauaMärkmed
{
    public partial class App : Application
    {
        public int activeindex = 1;
        public readonly string masRoot = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/.mas";
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                if (!(desktop.Args.Contains("-s")))
                {
                    desktop.MainWindow = new MainWindow();
                } else
                {
                    string[] configs = File.ReadAllText(masRoot + "/mas.cnf").Split(';');
                    if (configs.Length > 2)
                    {
                        if (configs[2] == "true")
                        {
                            File.WriteAllText(masRoot + "/noteopen.txt", "");
                            desktop.MainWindow = new MainWindow();
                        }
                        else
                        {
                            return;
                        }
                    }
                }
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}