using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using LibUsbDotNet;
using System;

namespace FlashUnlock;

public partial class App : Application
{
    public override void Initialize()
    {
        if (OperatingSystem.IsWindows())
        {
            UsbDevice.ForceLibUsbWinBack = true;
        }
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }
}