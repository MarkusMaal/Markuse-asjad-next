using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using LibUsbDotNet;
using LibUsbDotNet.Main;
using Microsoft.VisualBasic.FileIO;
using MsBox.Avalonia;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace FlashUnlock;

public partial class MainWindow : Window
{
    bool authenticated = false;
    List<Filler> fillers = new List<Filler>();
    string DeviceAuthID = "";
    string cdrive = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    string[] theme_data;
    string[] bg_data;
    bool dev_listen_ids = false;
    bool dev_killswitch = false;
    bool disable_post_unlock = false;
    bool first_time = true;
    List<String> last_devices = new();

    DispatcherTimer flashFinder = new();
    DispatcherTimer killTaskmgr = new();

    public MainWindow()
    {
        flashFinder.Interval = TimeSpan.FromMilliseconds(5000);
        killTaskmgr.Interval = TimeSpan.FromMilliseconds(100);
        flashFinder.Tick += FlashFinder_Tick;
        killTaskmgr.Tick += KillTaskmgr_Tick;
        this.WindowStartupLocation = WindowStartupLocation.Manual;
        this.Position = Screens.All[0].WorkingArea.Position;
        InitializeComponent();
    }

    private void FlashFinder_Tick(object? sender, EventArgs e)
    {
        authenticated = false;
        bool skip_devs = false;
        List<string> new_devices = new List<string>();
        foreach (UsbRegistry usbRegistry in UsbDevice.AllDevices)
        {
            if (usbRegistry.SymbolicName == DeviceAuthID)
            {
                authenticated = true;
                //Cursor.Show();
                if (fillers.Count > 0)
                {
                    foreach (Filler filler in fillers)
                    {
                        if (filler.IsVisible)
                        {
                            if (OperatingSystem.IsMacOS())
                            {
                                filler.WindowState = WindowState.Minimized;
                            }
                            filler.Hide();
                        }
                    }
                }
                if (OperatingSystem.IsMacOS())
                {
                    this.WindowState = WindowState.Minimized;
                }
                this.Hide();
            }
        }
        killTaskmgr.IsEnabled = !authenticated;
        if (!authenticated)
        {
            if (OperatingSystem.IsMacOS())
            {
                this.WindowState = WindowState.Maximized;
            }
            this.Show();
            this.Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.None);
            //Cursor.Position = new Point(0, 0);
            //Cursor.Hide();

            if (fillers.Count > 0)
            {
                for (int i = 0; i < fillers.Count; i++)
                {
                    Filler filler = fillers[i];
                    if (!filler.IsVisible)
                    {
                        filler.Position = Screens.All[i + 1].WorkingArea.Position;
                        filler.Show();
                    }
                }
            }
            var processes = Process.GetProcessesByName("explorer");
            if (OperatingSystem.IsLinux())
            {
                processes = Process.GetProcessesByName("plasmashell");
            }
            else if (OperatingSystem.IsMacOS())
            {
                processes = Process.GetProcessesByName("finder");
            }

            if (processes.Length > 0)
            {
                new Process
                {
                    StartInfo =
                    {
                        FileName = OperatingSystem.IsWindows() ? @"C:\Windows\System32\taskkill.exe" : OperatingSystem.IsLinux() ? "/usr/bin/kquitapp6" : "/usr/bin/killall",
                        Arguments = OperatingSystem.IsWindows() ? @"/F /IM explorer.exe" : OperatingSystem.IsLinux() ? "plasmashell" : "Finder",
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden
                    }
                }.Start();
            }
            clockLabel.Content = DateTime.Now.Hour.ToString().PadLeft(2, '0') + ":" + DateTime.Now.Minute.ToString().PadLeft(2, '0');
            if (first_time || !dev_killswitch)
            {
                label1.Content = "Autentimiseks sisestage Markuse mälupulk...";
            }
            if ((new_devices.Count > last_devices.Count) && (last_devices.Count > 0))
            {
                label1.Content = "Vale seade, proovige uuesti";
                last_devices.Clear();
                foreach (string device in new_devices)
                {
                    last_devices.Add(device);
                }
            }
            else if ((last_devices.Count == 0) || (last_devices.Count > new_devices.Count))
            {
                if (last_devices.Count > new_devices.Count)
                {
                    label1.Content = "Eemaldasite seadme";
                }
                last_devices.Clear();
                foreach (string device in new_devices)
                {
                    last_devices.Add(device);
                }
            }
            first_time = false;
        }
        else
        {
            killTaskmgr.Stop();
            label1.Content = "Lahti lukustamine...";
            Process[] processes = [];
            if (OperatingSystem.IsWindows())
            {
                processes = Process.GetProcessesByName("explorer");
            } else if (OperatingSystem.IsLinux())
            {
                processes = Process.GetProcessesByName("plasmashell");
            } else if (OperatingSystem.IsMacOS())
            {
                processes = Process.GetProcessesByName("finder");
            }
            if ( processes.Length == 0)
            {
                if (OperatingSystem.IsWindows())
                {
                    Process.Start(@"C:\Windows\explorer.exe");
                } else if (OperatingSystem.IsLinux())
                {
                    new Process
                    {
                        StartInfo =
                        {
                            FileName = "/usr/bin/bash",
                            Arguments = "-c \"nohup kstart5 plasmashell </dev/null >/dev/null 2>&1 &\"",
                            CreateNoWindow = true,
                            WindowStyle = ProcessWindowStyle.Hidden
                        }
                    }.Start();
                }
            }

            if (disable_post_unlock)
            {
                this.Close();
            }
        }
        if (File.Exists($"{cdrive}/.mas/stop_authenticate") && authenticated)
        {
            File.Delete($"{cdrive}/.mas/stop_authenticate");
            this.Close();
        }
        else if (File.Exists($"{cdrive}/.mas/stop_authenticate"))
        {
            label1.Content = "Mälupulga lukustust ei saa välja lülitada enne autentimist!";
        }
    }

    private async void Window_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        try
        {
            theme_data = File.ReadAllText(cdrive + "/.mas/scheme.cfg").Split(';')[1].Split(':').Take(3).ToArray();
            bg_data = File.ReadAllText(cdrive + "/.mas/scheme.cfg").Split(';')[0].Split(':').Take(3).ToArray();
            this.Foreground = new SolidColorBrush(Color.FromArgb(255, byte.Parse(theme_data[0]), byte.Parse(theme_data[1]), byte.Parse(theme_data[2])));
            this.Background = new SolidColorBrush(Color.FromArgb(255, byte.Parse(bg_data[0]), byte.Parse(bg_data[1]), byte.Parse(bg_data[2])));
            this.InnerGrid.Background = new ImageBrush(new Bitmap(cdrive + "/.mas/bg_common.png")) { Stretch = Stretch.Fill };
        }
        catch
        {
            await MessageBoxShow("See ei ole Markuse asjade süsteemi nõutele vastav arvuti.", "Programm ei tööta selles arvutis", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error);
            this.Close();
        }
        if (File.Exists(string.Format("{0}/.mas/flash_authenticate", cdrive)))
        {
            DeviceAuthID = File.ReadAllText(string.Format("{0}/.mas/flash_authenticate", cdrive));
            flashFinder.Start();
            if (OperatingSystem.IsMacOS())
            {
                this.WindowState = WindowState.Minimized;
            }
            this.Hide();
        }
        else
        {
            await MessageBoxShow("Ühtegi mälupulka pole kalibreeritud. Palun valige järgmisest menüüst mälupulk, mida soovite kasutada", "Ei saa lukustada", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Warning);

            if (OperatingSystem.IsMacOS())
            {
                this.WindowState = WindowState.Minimized;
            }
            this.Hide();

            FlashDevices fd = new();
            fd.Show();
            fd.Closed += (object? sender, EventArgs e) =>
            {
                if (fd.resultOK)
                {
                    DeviceAuthID = File.ReadAllText(string.Format("{0}/.mas/flash_authenticate", cdrive));
                    flashFinder.Start();
                }
                else
                {
                    authenticated = true;
                    this.Close();
                }
            };
        }
        if (Screens.All.Count > 1)
        {
            for (int i = 0; i < Screens.All.Count - 1; i++)
            {
                fillers.Add(new Filler());
                fillers[i].Foreground = this.Foreground;
                fillers[i].Background = this.Background;
                fillers[i].label1.Content = "Ekraan " + (i + 2).ToString() + " lukustatud";
                fillers[i].InnerGrid.Background = this.InnerGrid.Background;
            }
        }
    }

    // Reimplementation of WinForms MessageBox.Show
    private Task MessageBoxShow(string message, string caption = "Markuse arvuti juhtpaneel", MsBox.Avalonia.Enums.ButtonEnum buttons = MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon icon = MsBox.Avalonia.Enums.Icon.None)
    {
        var box = MessageBoxManager.GetMessageBoxStandard(caption, message, buttons, icon, WindowStartupLocation.CenterOwner);
        var result = box.ShowWindowDialogAsync(this);
        return result;
    }

    private void Window_Closing(object? sender, Avalonia.Controls.WindowClosingEventArgs e)
    {
        if (authenticated)
        {
            File.Delete(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/.mas/flash_unlock_is_enabled.log");
            Environment.Exit(0);
        }
        e.Cancel = !authenticated;
    }



    private void KillTaskmgr_Tick(object? sender, EventArgs e)
    {
        var taskmgrs = Process.GetProcessesByName("Taskmgr");
        if (taskmgrs.Length > 0)
        {
            new Process()
            {
                StartInfo =
                    {
                        FileName = @"C:\Windows\System32\taskkill.exe",
                        Arguments = @"/F /IM Taskmgr.exe",
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden
                    }
            }.Start();
        }
        this.Topmost = !this.Topmost;
        this.Focus();
    }

    private void Window_KeyDown(object? sender, Avalonia.Input.KeyEventArgs e)
    {

        if ((e.Key == Avalonia.Input.Key.Escape) && dev_killswitch)
        {
            killTaskmgr.IsEnabled = false;
            this.Topmost = false;
            label1.Content = "Arendaja režiim aktiveeriti";
        }
        else if (e.Key == Avalonia.Input.Key.Escape)
        {
            label1.Content = "Te ei ole arendaja, killswitchi ignoreeriti";
        }
        else if (e.Key == Avalonia.Input.Key.M)
        {
            label1.Content = GetLocalIPAddress();
        }
        else if (e.Key == Avalonia.Input.Key.T)
        {
            if (killTaskmgr.IsEnabled)
            {
                label1.Content = "Tegumihalduri blokk on aktiivne";
            }
            else
            {
                label1.Content = "Tegumihalduri blokk pole aktiivne";
            }
        }
        else if (e.Key == Avalonia.Input.Key.E)
        {
            var processes = Process.GetProcessesByName("explorer");
            if (processes.Length == 0)
            {
                label1.Content = "Windowsi liides on peatatud";
            }
            else
            {
                label1.Content = "Windowsi liides on avatud";
            }
        }
        else if (e.Key == Avalonia.Input.Key.D)
        {
            if (disable_post_unlock)
            {
                label1.Content = "Tavaline avamismeetod";
            }
            else
            {
                label1.Content = "Keela pärast avamist";
            }
            disable_post_unlock = !disable_post_unlock;
        }
    }

    public static string GetLocalIPAddress()
    {
        string localIP;
        using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
        {
            socket.Connect("8.8.8.8", 65530);
            IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
            localIP = endPoint.Address.ToString();
        }
        return "IP aadress: " + localIP;
    }
}