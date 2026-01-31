using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

namespace Markuse_arvuti_juhtpaneel;

public partial class MineUpdater : Window
{
    private bool CanClose = false;
    public MineUpdater()
    {
        InitializeComponent();
    }
    
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void UpdateStatusText(string data)
    {
        Dispatcher.UIThread.Post(() =>
        {
            this.GetControl<TextBlock>("StatusText").Text = data;
        });
    }

    private void Control_OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (!OperatingSystem.IsWindows()) this.GetControl<Label>("SearchDirs").Content = "Uute kataloogide otsimine";
        if (Design.IsDesignMode) return;
        var t = new Thread(() =>
        {
            var binary = App.root + "/Markuse asjad/MineGeneraator" + (OperatingSystem.IsWindows() ? ".exe" : (OperatingSystem.IsMacOS() ? ".app/Contents/MacOS/MineGeneraator" : ""));
            if (OperatingSystem.IsWindows()) binary = binary.Replace("/", "\\");
            var p = new Process();
            p.StartInfo = new ProcessStartInfo
            {
                FileName = binary,
                WindowStyle = ProcessWindowStyle.Hidden,
                RedirectStandardOutput = true,
                UseShellExecute = false
            };
            if (OperatingSystem.IsWindows())
            {
                foreach (var di in DriveInfo.GetDrives())
                {
                    // draiv vastab Markuse kaustad n�utele
                    if (File.Exists($"{di.RootDirectory.FullName}.userdata\\users.txt"))
                    {
                        p.StartInfo.Arguments = di.RootDirectory.FullName;
                    }
                }
            }
            p.Start();
            while (!p.HasExited)
            {
                UpdateStatusText(p.StandardOutput.ReadLine() ?? "Palun oota...");
            }
            Dispatcher.UIThread.Post(async void () =>
            {
                try
                {
                    var box = MessageBoxManager.GetMessageBoxStandard("Mine kausta uuendamine", "Valmis!", ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Success, WindowStartupLocation.CenterOwner);
                    await box.ShowWindowDialogAsync(this);
                    CanClose = true;
                    Close();
                }
                catch
                {
                    // ignored
                }
            });
        });
        t.Start();
    }

    private void Window_OnClosing(object? sender, WindowClosingEventArgs e)
    {
        e.Cancel = !CanClose;
    }
}