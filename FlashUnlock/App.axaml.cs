using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using LibUsbDotNet;
using System;
using System.IO;
using System.Net;

namespace FlashUnlock;

public partial class App : Application
{
    public override void Initialize()
    {
        if (OperatingSystem.IsWindows())
        {
            UsbDevice.ForceLibUsbWinBack = true;
        }
        File.WriteAllBytes(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/.mas/flash_unlock_is_enabled.log", [0x01, 0x00, 0x00, 0x00]);
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