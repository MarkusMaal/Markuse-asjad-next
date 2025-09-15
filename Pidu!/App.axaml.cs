using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace Pidu_
{
    public partial class App : Application
    {
        private MainWindow? MainWindow { get; set; }
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            MainWindow = new MainWindow();
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                Program.streamMode = (desktop.Args ?? []).Contains("/stream");
                desktop.MainWindow = MainWindow;
            }

            base.OnFrameworkInitializationCompleted();
        }

        private void NativeMenuItem_OnClick(object? sender, EventArgs e)
        {
            MainWindow?.Close();
        }
    }
}