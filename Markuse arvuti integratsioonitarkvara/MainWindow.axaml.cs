using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Primitives;
using Avalonia.Dialogs.Internal;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Markuse_arvuti_integratsioonitarkvara
{
    public partial class MainWindow : Window
    {
        private bool allowCode = true;
        private TrayIcon ti;
        private App app;
        private bool initialized = false;
        Color[] scheme;
        DispatcherTimer dispatcherTimer = new DispatcherTimer();
        public MainWindow()
        {
            InitializeComponent();
            app = (App)Application.Current;
            ti = app.GetTrayIcon();
            scheme = LoadTheme();
            if (Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/.masv"))
            {
                this.DeviceLabel.Content = "markuse virtuaalarvuti asjad";
                this.DevicePicture.Source = app.GetResource(Properties.Resources.mas_virtualpc);
            } else
            {
                this.DeviceLabel.Content = "markuse arvuti asjad";
                this.DevicePicture.Source = app.GetResource(Properties.Resources.mas_computers);
            }
            InitTimers();
        }

        private void InitTimers()
        {
            dispatcherTimer.Tick += new EventHandler(GeneralTimer);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 2);
            dispatcherTimer.Start();
        }

        private void GeneralTimer(object? sender, EventArgs e)
        {
            initialized = true;
            if (dispatcherTimer.Interval < new TimeSpan(0, 0, 5))
            {
                dispatcherTimer.Interval = new TimeSpan(0, 0, 5);
            }
            this.IsVisible = false;
            ReloadMenu();
            scheme = LoadTheme();
            ApplyTheme();
            CheckLogFiles();
        }

        private void CheckLogFiles()
        {
            if (File.Exists(app.mas_root + "/showabout.txt"))
            {
                File.Delete(app.mas_root + "/showabout.txt");
                //teaveMarkuseAsjadeKohtaToolStripMenuItem.PerformClick();
            }
            // M.A.I.A. ligipääsu taotlemine
            if (File.Exists(app.mas_root + @"/maia/request_permission.maia") || File.Exists(app.mas_root + "/maia/request_permission.mai"))
            {
                if (allowCode)
                {
                    ShowCode sc = new ShowCode();
                    sc.bg = scheme[0];
                    sc.fg = scheme[1];
                    sc.Show();
                }
                else
                {
                    try { File.Delete(app.mas_root + "/maia/request_permission.maia"); } catch { File.Delete(app.mas_root + "/maia/request_permission.mai"); }
                }
            }
        }

        private bool IconType() {
            if (OperatingSystem.IsWindows())
            {
                return File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile).ToString() + "\\Desktop\\Peida need töölauaikoonid.lnk");
            } else
            {
                return false;
            }
        }


        private void ReloadMenu()
        {
            //foreach (NativeMenuItemBase nmi in ti.Menu.Items)
            List<NativeMenuItem> toRemove = new List<NativeMenuItem>();
            for (int i = 0; i < ti.Menu.Items.Count; i++)
            {
                NativeMenuItemBase nmi = ti.Menu.Items[i];
                NativeMenuItem n = (NativeMenuItem)nmi;
                switch (n.Header)
                {
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
                                    Process p = new Process();
                                    p.StartInfo.FileName = app.mas_root + "/organize_desktop.bat";
                                    p.StartInfo.CreateNoWindow = true;
                                    p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                                    p.Start();
                                }
                                WaitInstall wi = new WaitInstall
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
                            if (!Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/.mas/vpc"))
                            {
                                toRemove.Add(n);
                            }
                        }
                        break;
                    case "Kuva kõik töölauaikoonid":
                    case "Peida need töölauaikoonid":
                        if (!initialized)
                        {
                            n.Click += (object? sender, EventArgs e) =>
                            {
                                NativeMenuItem nvmi = ((NativeMenuItem)sender);
                                if (nvmi.Header == "Kuva kõik töölauaikoonid")
                                {
                                    nvmi.Header = "Peida need töölauaikoonid";
                                } else
                                {
                                    nvmi.Header = "Kuva kõik töölauaikoonid";
                                }
                                if (OperatingSystem.IsWindows())
                                {
                                    Process p = new Process();
                                    p.StartInfo.FileName = app.mas_root + "/organize_desktop.bat";
                                    p.StartInfo.CreateNoWindow = true;
                                    p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                                    p.Start();
                                }
                            };
                        }
                        n.Header = IconType() ? "Peida need töölauaikoonid" : "Kuva kõik töölauaikoonid";
                        break;
                    case "M.A.I.A. serveri haldamine":

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
                                        sm.Icon = app.GetResource(Properties.Resources._new);
                                        sm.Click -= StartMaia;
                                        sm.Click += StartMaia;
                                    }
                                    else
                                    {
                                        sm.Header = "Peata";
                                        sm.Icon = app.GetResource(Properties.Resources.failure);
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
                                            Process pr = new Process();
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
                ti.Menu.Items.Remove(r);
            }
        }

        private void Window_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            ApplyTheme();
            ti.IsVisible = true;
            ReloadMenu();
        }

        private void StopMaia(object sender, EventArgs e) {
            Console.WriteLine("Stop M.A.I.A.");
            foreach (Process p in Process.GetProcesses()) {
                if (p.ProcessName.Contains("python") || p.ProcessName.Contains("py.exe")) {
                    p.Kill();
                }
            }
        }

        private void StartMaia(object sender, EventArgs e) {
            Console.WriteLine("Start M.A.I.A.");
            Process p = new Process();
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
            Process p = new Process();
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
            ApplyTheme(true);
        }

        private void SaveThemeBlack(object? sender, EventArgs e)
        {
            SaveTheme(Colors.Black, Colors.Silver);
            ApplyTheme(true);
        }

        private void SaveThemeBlue(object? sender, EventArgs e)
        {
            SaveTheme(Colors.MidnightBlue, Colors.White);
            ApplyTheme(true);
        }

        private void SaveThemeXmas(object? sender, EventArgs e)
        {
            SaveTheme(Colors.DarkRed, Colors.Lime);
            ApplyTheme(true);
        }

        /* Loads mas theme */
        Color[] LoadTheme()
        {
            string[] bgfg = File.ReadAllText(app.mas_root + "/scheme.cfg").Split(';');
            string[] bgs = bgfg[0].ToString().Split(':');
            string[] fgs = bgfg[1].ToString().Split(':');
            Color[] cols = { Color.FromArgb(255, byte.Parse(bgs[0]), byte.Parse(bgs[1]), byte.Parse(bgs[2])), Color.FromArgb(255, byte.Parse(fgs[0]), byte.Parse(fgs[1]), byte.Parse(fgs[2])) };
            return cols;
        }

        private void ApplyTheme(bool reopen = false)
        {
            app.Styles.Add(new Style(x => x.OfType<MenuItem>())
            {
                Setters =
                {
                    new Setter(TemplatedControl.BackgroundProperty, new SolidColorBrush(scheme[0])),
                    new Setter(TemplatedControl.ForegroundProperty, new SolidColorBrush(scheme[1]))
                }
            });
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
            this.scheme[0] = bg;
            this.scheme[1] = fg;
            File.WriteAllText(app.mas_root + "/scheme.cfg", bg.R.ToString() + ":" + bg.G.ToString() + ":" + bg.B.ToString() + ":;" + fg.R.ToString() + ":" + fg.G.ToString() + ":" + fg.B.ToString() + ":;");
        }


        private void ModifyContextText(string old, string nw)
        {
            foreach (NativeMenuItemBase nmi in ti.Menu.Items)
            {
                if (((NativeMenuItem)nmi).Header == old)
                {
                    ((NativeMenuItem)nmi).Header = nw;
                }
            }
        }
    }
}