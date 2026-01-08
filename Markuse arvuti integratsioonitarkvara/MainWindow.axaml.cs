using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Avalonia.Platform;
using MasCommon;

namespace Markuse_arvuti_integratsioonitarkvara
{
    public partial class MainWindow : Window
    {
        public bool allowCode = true;
        private readonly App app;
        private bool initialized = false;
        public Color[] scheme;
        public readonly DispatcherTimer dispatcherTimer = new();
        private Watchers watchers;
        public MainWindow()
        {
            try
            {
                app = (App)Application.Current;
                InitializeComponent();
                if (File.Exists(app.mas_root + "/scheme.cfg")) {
                    scheme = LoadTheme();
                    this.Backglass.Fill = new SolidColorBrush(Color.FromArgb(128, scheme[0].R, scheme[0].G, scheme[0].B));
                    if (File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) +
                                    "/Android.lnk"))
                    {
                        this.DeviceLabel.Content = "markuse tahvelarvuti asjad";
                        this.DevicePicture.Source = app.GetResource(AssetLoader.Open(new Uri("avares://Markuse arvuti integratsioonitarkvara/Resources/mas_tablet.png")));
                    }

                    if (Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) +
                                         "/.masv"))
                    {
                        this.DeviceLabel.Content = "markuse virtuaalarvuti asjad";
                        this.DevicePicture.Source = app.GetResource(AssetLoader.Open(new Uri("avares://Markuse arvuti integratsioonitarkvara/Resources/mas_virtualpc.png")));
                    }
                    else
                    {
                        this.DeviceLabel.Content = "markuse arvuti asjad";
                        this.DevicePicture.Source = app.GetResource(AssetLoader.Open(new Uri("avares://Markuse arvuti integratsioonitarkvara/Resources/mas_computers.png")));
                    }
                }
                if (app.dev)
                {
                    return;
                }
                this.watchers = new Watchers(this, app.mas_root);

                new Thread(() =>
                {
                    if (!OperatingSystem.IsLinux())
                    {
                        Dispatcher.UIThread.Post(() =>
                        {
                            //Console.WriteLine("Windows/Mac paranduste aktiveerimine...");
                            this.ExtendClientAreaToDecorationsHint = false;
                            this.SystemDecorations = SystemDecorations.None;
                        });
                    }

                    if (app.vf.IsVerified() && Verifile.CheckFiles(Verifile.FileScope.IntegrationSoftware) && !app.croot)
                    {
                        app.InitSettings();
                        Dispatcher.UIThread.Post(() =>
                        {
                            if (app.showsplash) return;
                            this.IsVisible = false;
                            this.Opacity = 0;
                            if (OperatingSystem.IsMacOS())
                            {
                                this.WindowState = WindowState.Minimized;
                                this.Width = 0;
                                this.Height = 0;
                            }
                        });
                        scheme = LoadTheme();
                        InitTimers();
                    }
                    else if (app.croot)
                    {
                        Dispatcher.UIThread.Post(() =>
                        {
                            RerootForm rf = new();
                            rf.Show();
                        });
                    }
                    else
                    {
                        Dispatcher.UIThread.Post(() =>
                        {
                            new VerifileFail().Show();
                            Close();
                        });
                    }
                }).Start();
            }
            catch (Exception ex) when (!Debugger.IsAttached)
            {
                var exePath = Environment.ProcessPath;
                Process.Start(new ProcessStartInfo(exePath!) { UseShellExecute = true });
                File.WriteAllText(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/mas_error.log", $"-------------------------------\n\nMarkuse arvuti integratsioonitarkvara\n\n-------------------------------\n\nTaaskäivitamise Markuse arvuti integratsioonitarkvara probleemi tõttu. Palun käivitage see programm siluriga, et asja täpsemalt uurida.\n\nTehniline info:\n\nKuupäev ja kellaaeg: {DateTime.Now}\nErand: {ex.Message}\nKuhila jälg:\n{ex.StackTrace}");
                Environment.Exit(0);
            }
        }

        private void InitTimers()
        {
            dispatcherTimer.Tick += GeneralTimer;
            dispatcherTimer.Interval = new TimeSpan(0, 0, app.showsplash ? 2 : 0);
            dispatcherTimer.Start();
        }

        private void CheckEvents()
        {
            //erisündmused
            if (app.specialevents is not { Length: > 0 }) return;
            //käi läbi erisündmuste massiivist
            foreach (var element in app.specialevents)
            {
                //loo alamelemendid
                string[] subelements = element.Split('-');
                if (subelements.Length < 3) continue;
                //loo kp elemendid
                string[] dateelements = [subelements[0].ToString(), subelements[1].ToString(), subelements[2].ToString()];
                //kui kp pole sama, siis ignoreeri
                if (!(dateelements[0].ToString() == DateTime.Today.Day.ToString() && dateelements[1].ToString() == DateTime.Today.Month.ToString() && dateelements[2].ToString() == DateTime.Today.Year.ToString()))
                {
                    continue;
                }
                //loo kellaaja elemendid
                string[] timestamp = [subelements[3].ToString(), subelements[4].ToString(), subelements[5].ToString()];
                //kui kellaaeg varasem, siis ignoreeri
                if (!(Convert.ToInt32(timestamp[0].ToString()) <= DateTime.Now.Hour && Convert.ToInt32(timestamp[1].ToString()) <= DateTime.Now.Minute))
                {
                    continue;
                }
                //leia failinimi
                string file = subelements[6].ToString();
                //leia binraarne muutuja
                bool willclose = Convert.ToBoolean(subelements[7].ToString());
                //loo protsess
                Process p = new();
                p.StartInfo.FileName = file;
                p.StartInfo.Arguments = subelements[8].ToString();
                p.Start();
                //sulge programm kui willclose on tõene
                if (willclose)
                {
                    app.croot = true;
                    Environment.Exit(0);
                }
                //eemalda elemendid specialevent massiivist
                app.specialevents = null;
                break;
            }
        }

        public void CheckVerifile()
        {
            if (app.vf.MakeAttestation() == "VERIFIED") return;
            if (app.vf.MakeAttestation() == "BYPASS")
            {
                app.vf = new Verifile();
                if (app.vf.MakeAttestation() == "VERIFIED") return;
            }
            Console.WriteLine("Verifile kontroll nurjus, programmi sulgemine");
            Environment.Exit(255);
        }

        private void GeneralTimer(object? sender, EventArgs e)
        {
            initialized = true;
            app.ResetTrayIcon();
            Program.config.Load(app.mas_root);
            
            if (dispatcherTimer.Interval < new TimeSpan(0, 0, 5))
            {
                dispatcherTimer.Interval = TimeSpan.FromMilliseconds(Program.config.PollRate);
            }
            if (Program.config.AllowScheduledTasks)
            {
                CheckEvents();
                HideMe();
            } else
            {
                ReloadMenu();
                HideMe();
                dispatcherTimer.Stop();
            }
        }

        private void HideMe()
        {
            this.IsVisible = false;
            if (OperatingSystem.IsMacOS())
            {
                this.WindowState = WindowState.Minimized;
                this.Width = 0;
                this.Height = 0;
            }
        }

        private static bool IconType() {
            if (OperatingSystem.IsWindows())
            {
                return File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile).ToString() + "\\Desktop\\Peida need töölauaikoonid.lnk");
            } else
            {
                return false;
            }
        }


        public void ReloadMenu()
        {
            if ((app.GetTrayIcon() == null) || !app.GetTrayIcon().IsVisible) {
                return;
            }
            //foreach (NativeMenuItemBase nmi in ti.Menu.Items)
            List<NativeMenuItem> toRemove = [];
            for (int i = 0; i < app.GetTrayIcon().Menu.Items.Count; i++)
            {
                NativeMenuItemBase nmi = app.GetTrayIcon().Menu.Items[i];
                NativeMenuItem n = (NativeMenuItem)nmi;
                switch (n.Header)
                {
                    case "Käivita MarkuStation":
                        n.IsEnabled = true;
                        break;
                    case "Markuse mälupulk":
                    case "Ühtegi mälupulka pole sisestatud":
                        if (app.fmount != "")
                        {
                            n.Header = "Markuse mälupulk";
                        }
                        else
                        {
                            n.Header = "Ühtegi mälupulka pole sisestatud";
                            n.IsEnabled = false;
                        }
                        break;
                    case "Lülita mälupulga lukustus sisse":
                    case "Lülita mälupulga lukustus välja":
                        bool flashLocked = File.Exists(app.mas_root + "/flash_unlock_is_enabled.log");
                        n.Header = flashLocked ? "Lülita mälupulga lukustus välja" : "Lülita mälupulga lukustus sisse";
                        break;
                    case "Käivita Projekt ITS":
                        n.IsVisible = Program.FeatureSet.InteractiveDesktop;
                        break;
                    case "Värviskeem":
                        foreach (NativeMenuItemBase snmi in n.Menu.Items)
                        {
                            NativeMenuItem sm = (NativeMenuItem)snmi;
                            switch (sm.Header)
                            {
                                case "Valge":
                                    sm.Click -= SaveThemeWhite;
                                    sm.Click += SaveThemeWhite;
                                    break;
                                case "Öörežiim":
                                    sm.Click -= SaveThemeBlack;
                                    sm.Click += SaveThemeBlack;
                                    break;
                                case "Sinine":
                                    sm.Click -= SaveThemeBlue;
                                    sm.Click += SaveThemeBlue;
                                    break;
                                case "Jõulud":
                                    sm.Click -= SaveThemeXmas;
                                    sm.Click += SaveThemeXmas;
                                    break;
                            }
                        }
                        break;
                    case "Tarkvara paigaldamise režiim":
                        if (!initialized)
                        {
                            if (!OperatingSystem.IsWindows())
                            {
                                toRemove.Add(n);
                            }
                            n.Click += (object? sender, EventArgs e) =>
                            {
                                bool redo = !IconType();
                                if (redo)
                                {
                                    Process p = new();
                                    p.StartInfo.FileName = app.mas_root + "/organize_desktop.bat";
                                    p.StartInfo.CreateNoWindow = true;
                                    p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                                    p.Start();
                                }
                                WaitInstall wi = new()
                                {
                                    Background = new SolidColorBrush(this.scheme[0]),
                                    Foreground = new SolidColorBrush(this.scheme[1]),
                                    redo = redo
                                };
                                wi.Show();
                            };
                        }
                        break;
                    case "Sünkroniseeri versioon":
                        if (!initialized)
                        {
                            if (!Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/.masv"))
                            {
                                toRemove.Add(n);
                            }
                        }
                        break;
                    case "Käivita virtuaalarvuti":
                        if (!initialized)
                        {
                            if (OperatingSystem.IsWindows() && !Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/.mas/vpc"))
                            {
                                toRemove.Add(n);
                            }
                        }
                        break;
                    case "Kuva kõik töölauaikoonid":
                    case "Peida need töölauaikoonid":
                        n.IsVisible = Program.FeatureSet.DesktopIcons;
                        if (!initialized)
                        {
                            n.Click += (object? sender, EventArgs e) =>
                            {
                                NativeMenuItem nvmi = ((NativeMenuItem)sender);
                                if (nvmi.Header == "Kuva kõik töölauaikoonid")
                                {
                                    app.CookToast("Töölauaikoonide kuvamine...");
                                    nvmi.Header = "Peida need töölauaikoonid";
                                } else
                                {
                                    app.CookToast("Töölauaikoonide peitmine...");
                                    nvmi.Header = "Kuva kõik töölauaikoonid";
                                }
                                if (OperatingSystem.IsWindows())
                                {
                                    Process p = new();
                                    p.StartInfo.FileName = app.mas_root + "/organize_desktop.bat";
                                    p.StartInfo.CreateNoWindow = true;
                                    p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                                    p.Start();
                                }
                            };
                        }
                        n.Header = IconType() ? "Peida need töölauaikoonid" : "Kuva kõik töölauaikoonid";
                        break;
                    case "Sulge see menüü":
                        if (!initialized) {
                            if (app.dev) {
                                n.Click += (object? sender, EventArgs e) => {
                                    this.Close();
                                };
                                n.Header = "Lõpeta silumine";
                            }
                        }
                        break;
                    case "Juurutamine":
                        n.IsVisible = !Program.FeatureSet.IsRooted;
                        if (!initialized) {
                            n.Click += (object? sender, EventArgs e) => {
                                RerootForm rf = new();
                                rf.Show();
                            };
                            if (!app.dev) {
                                toRemove.Add(n);
                            }
                        }
                        break;
                    case "M.A.I.A. serveri haldamine":
                        n.IsVisible = Program.FeatureSet.RemoteDesktop;

                        foreach (NativeMenuItemBase snmi in n.Menu.Items)
                        {
                            NativeMenuItem sm = (NativeMenuItem)snmi;
                            switch (sm.Header)
                            {
                                case "Käivita":
                                case "Peata":
                                    bool running = false;
                                    foreach (Process p in Process.GetProcesses())
                                    {
                                        if (p.ProcessName.Contains("python") || p.ProcessName.Contains("py.exe"))
                                        {
                                            running = true;
                                        }
                                    }
                                    if (!running)
                                    {
                                        sm.Header = "Käivita";
                                        sm.Icon = app.GetResource(AssetLoader.Open(new Uri("avares://Markuse arvuti integratsioonitarkvara/Resources/new.bmp")));
                                        sm.Click -= StartMaia;
                                        sm.Click += StartMaia;
                                    }
                                    else
                                    {
                                        sm.Header = "Peata";
                                        sm.Icon = app.GetResource(AssetLoader.Open(new Uri("avares://Markuse arvuti integratsioonitarkvara/Resources/failure.gif")));
                                        sm.Click -= StopMaia;
                                        sm.Click += StopMaia;
                                    }
                                    break;
                                case "Võimalda ligipääsu taotlemine":
                                    if (!initialized)
                                    {
                                        sm.Click += (object? sender, EventArgs e) =>
                                        {
                                            NativeMenuItem obj = ((NativeMenuItem)sender);
                                            obj.IsChecked = !obj.IsChecked;
                                            allowCode = obj.IsChecked;
                                        };
                                    }
                                    break;
                                case "Ava brauseris":
                                    if (!initialized)
                                    {
                                        sm.Click += (object? sender, EventArgs e) =>
                                        {
                                            Process pr = new();
                                            pr.StartInfo.FileName = "http://localhost:14414";
                                            pr.StartInfo.UseShellExecute = true;
                                            pr.Start();
                                        };
                                    }
                                    break;
                            }
                        }
                        break;
                }
            }
            foreach (NativeMenuItem r in toRemove)
            {
                app.GetTrayIcon().Menu.Items.Remove(r);
            }
        }

        private void Window_Loaded(object? sender, RoutedEventArgs e)
        {
            if (app.dev) return;
            ApplyTheme();
            new Thread(() =>
            {
                if (OperatingSystem.IsMacOS() && !Debugger.IsAttached)
                {
                    if (Process.GetProcesses().Any(p => p.ProcessName.Contains("DesktopIcons"))) return;
                    new Process
                    {
                        StartInfo =
                        {
                            FileName = "open",
                            Arguments = "-a \"" + app.mas_root + "/Markuse asjad/DesktopIcons.app\"",
                            UseShellExecute = false,
                        }
                    }.Start();
                }
            }).Start();
            ReloadMenu();
        }

        private void StopMaia(object sender, EventArgs e) {
            app.CookToast("M.A.I.A. serveri peatamine...");
            foreach (Process p in Process.GetProcesses()) {
                if (p.ProcessName.Contains("python") || p.ProcessName.Contains("py.exe")) {
                    p.Kill();
                }
            }
        }

        private void StartMaia(object sender, EventArgs e) {
            app.CookToast("M.A.I.A. serveri käivitamine...");
            Process p = new();
            if (!OperatingSystem.IsWindows()) {
                p.StartInfo.FileName = "python3";
            } else {
                p.StartInfo.FileName = "py.exe";
            }
            p.StartInfo.Arguments = app.mas_root + "/maia/server.py";
            p.StartInfo.UseShellExecute = true;
            p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            p.StartInfo.CreateNoWindow = true;
            p.Start();
        }


        public void Restart() {
            Process p = new();
            p.StartInfo.FileName = AppDomain.CurrentDomain.BaseDirectory + "/" + GetType().Namespace.Replace("_", " ");
            if (OperatingSystem.IsWindows()) {
                p.StartInfo.FileName += ".exe";
            }
            p.Start();
            this.Close();
        }

        private void SaveThemeWhite(object? sender, EventArgs e)
        {
            SaveTheme(Colors.White, Colors.Black);
            ApplyTheme();
        }

        private void SaveThemeBlack(object? sender, EventArgs e)
        {
            SaveTheme(Colors.Black, Colors.Silver);
            ApplyTheme();
        }

        private void SaveThemeBlue(object? sender, EventArgs e)
        {
            SaveTheme(Colors.MidnightBlue, Colors.White);
            ApplyTheme();
        }

        private void SaveThemeXmas(object? sender, EventArgs e)
        {
            SaveTheme(Colors.DarkRed, Colors.Lime);
            ApplyTheme();
        }

        /* Loads mas theme */
        public Color[] LoadTheme()
        {
            while (true)
            {
                try
                {
                    string[] bgfg = File.ReadAllText(app.mas_root + "/scheme.cfg").Split(';');
                    string[] bgs = bgfg[0].ToString().Split(':');
                    string[] fgs = bgfg[1].ToString().Split(':');
                    Color[] cols = [Color.FromArgb(255, byte.Parse(bgs[0]), byte.Parse(bgs[1]), byte.Parse(bgs[2])), Color.FromArgb(255, byte.Parse(fgs[0]), byte.Parse(fgs[1]), byte.Parse(fgs[2]))];
                    return cols;
                }
                catch { Thread.Sleep(100);  continue; }
            }
        }

        public void ApplyTheme()
        {
            if (!OperatingSystem.IsWindows()) return;
            try
            {
                new Thread(() =>
                {
                    Thread.Sleep(5000);
                    Dispatcher.UIThread.Post(() =>
                    {
                        app.Styles.Add(new Style(x => x.OfType<MenuItem>())
                        {
                            Setters =
                            {
                                new Setter(BackgroundProperty, new SolidColorBrush(scheme[0])),
                                new Setter(ForegroundProperty, new SolidColorBrush(scheme[1]))
                            }
                        });                        
                    });
                }).Start();
            }
            catch (Exception ex) when (!Debugger.IsAttached)
            {
                app.CookToast("Teema rakendamine nurjus!");
            }
            /*app.Styles.Add(new Style(x => x.OfType<MenuItem>().Class("White"))
            {
                Setters =
                {
                    new Setter(TemplatedControl.BackgroundProperty, new SolidColorBrush(Colors.White)),
                    new Setter(TemplatedControl.ForegroundProperty, new SolidColorBrush(Colors.Black))
                }
            });*/
        }

        private void SaveTheme(Color bg, Color fg)
        {
            app.CookToast("Teema salvestati");
            this.scheme[0] = bg;
            this.scheme[1] = fg;
            File.WriteAllText(app.mas_root + "/scheme.cfg", bg.R.ToString() + ":" + bg.G.ToString() + ":" + bg.B.ToString() + ":;" + fg.R.ToString() + ":" + fg.G.ToString() + ":" + fg.B.ToString() + ":;");
        }


        private void ModifyContextText(string old, string nw)
        {
            foreach (NativeMenuItemBase nmi in app.GetTrayIcon().Menu.Items)
            {
                if (((NativeMenuItem)nmi).Header == old)
                {
                    ((NativeMenuItem)nmi).Header = nw;
                }
            }
        }
    }
}