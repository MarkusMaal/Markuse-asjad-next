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
        private MainWindow? MainWindow { get; set; }
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
            MainWindow = new MainWindow();
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                config.Load(masRoot);
                if (!desktop.Args.Contains("-s"))
                {
                    desktop.MainWindow = MainWindow;
                } else
                {
                    if (config.AutostartNotes) { 
                        File.WriteAllText(masRoot + "/noteopen.txt", "");
                        desktop.MainWindow = MainWindow;
                    }
                    else
                    {
                        return;
                    }
                }
            }

            base.OnFrameworkInitializationCompleted();
        }

        private void NativeMenuItem_OnClick(object? sender, EventArgs e)
        {
            MainWindow?.Close();
        }
    }
}