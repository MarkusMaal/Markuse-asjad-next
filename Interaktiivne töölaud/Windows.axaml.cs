using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media;
using System.Linq;
using NickStrupat;
using Hardware.Info;
using System.IO;
using Avalonia;
using System.Text;
using System.Collections.Generic;

namespace Interaktiivne_töölaud;

public partial class Windows : Window
{
    DispatcherTimer dpt = new();
    private static HardwareInfo? hardwareInfo;
    double cpuUsage;
    double ramUsage;
    double diskUsage;
    bool stop = false;
    Thread collectUsage;
    public Windows()
    {
        cpuUsage = 0;
        ramUsage = 0;
        diskUsage = 0;
        var cts = new CancellationTokenSource();
        hardwareInfo = new HardwareInfo();
        collectUsage = new (() => // run in a separate thread to avoid interface freezes every second
        {
            while (true)
            {
                // Calculate CPU usage
                hardwareInfo.RefreshCPUList();
                hardwareInfo.RefreshDriveList();
                cpuUsage = hardwareInfo.CpuList.First().PercentProcessorTime;

                // Calculate RAM usage
                ComputerInfo ci = new();
                double used = ci.TotalPhysicalMemory - ci.AvailablePhysicalMemory;
                double ram = ci.TotalPhysicalMemory;
                ramUsage = used / ram * 100.0;

                Thread.Sleep(100);
                if (stop)
                {
                    break;
                }
            }
            stop = false;
        });
        collectUsage.Start();
        dpt.Interval = new TimeSpan(0, 0, 1);
        dpt.Tick += async (object? sender, EventArgs e) =>
        {
            int selection = ProcessBox.SelectedIndex;
            ProcessBox.Items.Clear();
            foreach (Process p in Process.GetProcesses())
            {
                if (p.MainWindowTitle.ToString() != "")
                {
                    string s = p.Responding ? "Töötab" : "Ei reageeri";
                    ProcessBox.Items.Add(p.MainWindowTitle + " (" + s + ") @" + p.ProcessName);
                }
            }
            ProcessBox.SelectedIndex = selection;

            // Brushes for status indicators
            SolidColorBrush ramBrush = new();
            SolidColorBrush cpuBrush = new();


            // RAM color
            if (ramUsage > 75.0) { ramBrush.Color = Colors.Red; }
            else if (ramUsage < 50.0) { ramBrush.Color = Colors.Lime; }
            else { ramBrush.Color = Colors.Yellow; }
            // CPU color
            if (cpuUsage > 75.0) { cpuBrush.Color = Colors.Red; }
            else if (cpuUsage < 50.0) { cpuBrush.Color = Colors.Lime; }
            else { cpuBrush.Color = Colors.Yellow; }


            bool isnetwork = System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable();
            bool isinternet = false;
            if (isnetwork)
            {
                try
                {
                    System.Net.IPHostEntry i = System.Net.Dns.GetHostEntry("www.google.com");
                    isinternet = true;
                }
                catch
                {
                    isinternet = false;
                }
            }
            NetContainer.Background = new SolidColorBrush(isinternet ? Colors.Lime : (isnetwork ? Colors.Yellow : Colors.Red));

            // apply colors
            RamContainer.Background = ramBrush;
            Cpu1Container.Background = cpuBrush;
        };
        InitializeComponent();
    }

    private void Bottom_Click(object? sender, RoutedEventArgs e)
    {
        Inside ins = new();
        ins.Show();
        this.Close();
    }

    private async void Run_Click(object? sender, RoutedEventArgs e)
    {
        Run run = new();
        await run.ShowDialog(this).WaitAsync(CancellationToken.None);
        if (run.ok)
        {
            Process p = new();
            p.StartInfo.FileName = run.ProgName.Text;
            p.StartInfo.UseShellExecute = true;
            p.Start();
        }
    }

    private void Restore_Shell_Click(object? sender, RoutedEventArgs e)
    {
        this.Close();
    }

    

    private async void Window_Loaded_1(object? sender, RoutedEventArgs e)
    {
        dpt.Start();
    }

    private void ComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox c)
        {
            switch (c.SelectedIndex)
            {
                case 0:
                    dpt.Interval = new TimeSpan(0, 0, 0, 0, 100);
                    break;
                case 1:
                    dpt.Interval = new TimeSpan(0, 0, 0, 0, 500);
                    break;
                case 2:
                    dpt.Interval = new TimeSpan(0, 0, 1);
                    break;
                case 3:
                    dpt.Interval = new TimeSpan(0, 0, 2);
                    break;
                case 4:
                    dpt.Interval = new TimeSpan(0, 0, 5);
                    break;
            }
        }
    }

    private void Window_Closing(object? sender, WindowClosingEventArgs e)
    {
        stop = true; // wait for thread to stop before continuing with close procedure
        while (stop)
        {
            e.Cancel = true;
        }
        e.Cancel = false;
    }

    private void Shutdown_Click(object? sender, RoutedEventArgs e)
    {
        Process p = new();
        if (OperatingSystem.IsLinux())
        {
            p.StartInfo.FileName = "qbus";
            p.StartInfo.Arguments = "org.kde.Shutdown /Shutdown logoutAndShutdown";
        }
        else if (OperatingSystem.IsWindows())
        {
            p.StartInfo.FileName = "shutdown";
            p.StartInfo.Arguments = "/s /t 0";
        }
        else if (OperatingSystem.IsMacOS())
        {
            p.StartInfo.FileName = "osascript";
            p.StartInfo.Arguments = "-e 'tell app \"System Events\" to shut down'";
        }
        p.StartInfo.UseShellExecute = true;
        p.Start();
    }

    private void Restart_Click(object? sender, RoutedEventArgs e)
    {
        Process p = new();
        if (OperatingSystem.IsLinux())
        {
            p.StartInfo.FileName = "qbus";
            p.StartInfo.Arguments = "org.kde.Shutdown /Shutdown logoutAndReboot";
        }
        else if (OperatingSystem.IsWindows())
        {
            p.StartInfo.FileName = "shutdown";
            p.StartInfo.Arguments = "/r /t 0";
        }
        else if (OperatingSystem.IsMacOS())
        {
            p.StartInfo.FileName = "osascript";
            p.StartInfo.Arguments = "-e 'tell app \"System Events\" to restart'";
        }
        p.StartInfo.UseShellExecute = true;
        p.Start();
    }

    private void Restart_ITS(object? sender, RoutedEventArgs e)
    {
        string? exePath = Environment.ProcessPath;
        if (exePath != null)
        {
            Process.Start(new ProcessStartInfo(exePath) { UseShellExecute = true });
            Environment.Exit(0);
        }
    }

    private void CloseWindow_Click(object? sender, RoutedEventArgs e)
    {
        string? processName = ProcessBox.SelectedItems[0]?.ToString().Split("@")[1];
        if (processName == null)
        {
            return;
        }
        foreach (Process p in Process.GetProcesses())
        {
            if (p.ProcessName == processName)
            {
                p.CloseMainWindow();
            }
        }
    }

    private void EndProcess_Click(object? sender, RoutedEventArgs e)
    {
        string? processName = ProcessBox.SelectedItems[0]?.ToString().Split("@")[1];
        if (processName == null)
        {
            return;
        }
        foreach (Process p in Process.GetProcesses())
        {
            if (p.ProcessName == processName)
            {
                p.Kill();
            }
        }
    }

    private void Taskmgr_Click(object? sender, RoutedEventArgs e )
    {
        Process p = new();
        if (OperatingSystem.IsLinux())
        {
            p.StartInfo.FileName = "ksysguard";
        }
        else if (OperatingSystem.IsWindows())
        {
            p.StartInfo.FileName = "taskmgr";
        }
        else if (OperatingSystem.IsMacOS())
        {
            p.StartInfo.FileName = "open";
            p.StartInfo.Arguments = "-a Activity\\ Monitor";
        }
        p.StartInfo.UseShellExecute = true;
        p.Start();
    }
}