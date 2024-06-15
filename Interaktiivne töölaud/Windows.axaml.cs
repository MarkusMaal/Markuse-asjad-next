using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using System;
using System.Diagnostics;
using System.Threading;
using AvaloniaInside.SystemManager;
using AvaloniaInside.SystemManager.Monitor;
using System.Threading.Tasks;
using Avalonia.Media;
using System.Linq;

namespace Interaktiivne_töölaud;

public partial class Windows : Window
{
    DispatcherTimer dpt = new();
    public Windows()
    {
        var cts = new CancellationTokenSource();

        Task t = Task.Factory.StartNew(async () =>
        {
            int totalCores = 0;
            int totalUsage = 0;
            var cpuUsage = new CpuUsage();
            await foreach (var cpu in cpuUsage)
            {
                totalCores++;
                totalUsage = (int)cpu.Usage;
            }
            int avgUsage = totalUsage / totalCores;
            SolidColorBrush cpuBrush = new();
            if (avgUsage > 75)
            {
                cpuBrush.Color = Colors.Red;
            }
            else if (avgUsage < 50)
            {
                cpuBrush.Color = Colors.Green;
            }
            else
            {
                cpuBrush.Color = Colors.Yellow;
            }
            Cpu1Container.Background = cpuBrush;
        });
        AvaloniaInside.SystemManager.System.Init();
        Settings.DefaultNetworkInterface = "eth0";
        Settings.CpuUsageWatcherEnabled = true;
        Settings.NetworkOperationStateDetectionEnabled = true;
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
            try { 
                t.Start();

                var memoryUsage = new MemoryUsage();
                await foreach (var info in memoryUsage.WithCancellation(cts.Token))
                {
                    float percent = info.MemoryFree / info.MemorySize * 100;
                    if (percent > 75)
                    {
                        RamContainer.Background = new SolidColorBrush(Colors.Red);
                    }
                    else if (percent < 50)
                    {
                        RamContainer.Background = new SolidColorBrush(Colors.Lime);
                    }
                    else
                    {
                        RamContainer.Background = new SolidColorBrush(Colors.Yellow);
                    }
                }
            } catch
            {

            }
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

    

    private void Window_Loaded_1(object? sender, RoutedEventArgs e)
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
}