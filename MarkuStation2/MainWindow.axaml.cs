using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using LibVLCSharp.Shared;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace MarkuStation2
{
    public partial class MainWindow : Window
    {
        // VLC stuff
        public LibVLC _libVLC;
        public MediaPlayer mp;
        public MediaPlayer mp2;
        readonly string mas_root = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/.mas";
        readonly DispatcherTimer timer = new();
        private string currentMenu = "startup";
        int angle = 10;
        int angle2 = 40;
        int angle3 = 70;
        int angle4 = 100;
        int opacity = 0;
        int sel = 0;
        bool invert = false;
        int circle_offset = 0;
        readonly bool running = false;
        readonly bool nodata = true;
        int browser_idx = -1;
        readonly Random rand = new();
        DispatcherTimer moveCircle = new();
        readonly IDictionary<string, string> ms_games = new Dictionary<string, string>();
        public MainWindow()
        {
            InitializeComponent();
            _libVLC = new LibVLC();
            mp = new MediaPlayer(_libVLC);
            mp2 = new MediaPlayer(_libVLC);
            if (!Directory.Exists(mas_root))
            {
                Directory.CreateDirectory(mas_root);
            }
            string[] games = [];
            string[] execs = [];
            if (File.Exists(mas_root + "/ms_games.txt") && File.Exists(mas_root + "/ms_exec.txt"))
            {
                games = File.ReadAllText(mas_root + "/ms_games.txt", Encoding.UTF8).Split("\n").Skip(1).ToArray();
                execs = File.ReadAllText(mas_root + "/ms_exec.txt", Encoding.UTF8).Replace("\n", "").Split(";").Skip(1).ToArray();
            }
            int i = 0;
            foreach (string game in games) {
                if (game != "")
                {
                    ms_games[game] = execs[i].Replace("\\", "/") ;
                }
                i ++;
            }
            if (ms_games.Count > 0)
            {
                nodata = false;
                NoData.IsVisible = false;
            }

            foreach (Process p in Process.GetProcesses())
            {
                if (p.ProcessName.Contains("MarkuStation"))
                {
                    running = true;
                    break;
                }
            }
            // extract intro movie
            File.WriteAllBytes(mas_root + "/_temp.mp4", Properties.Resources.MarkuStation_startup_video);
            File.WriteAllBytes(mas_root + "/_temp.wav", GetStreamBytes(Properties.Resources.MarkuStation_startup));
            File.WriteAllBytes(mas_root + "/_amb.wav", GetStreamBytes(Properties.Resources.MarkuStation_ambient));
            File.WriteAllBytes(mas_root + "/_sel.wav", GetStreamBytes(Properties.Resources.MarkuStation_menu_select));
            File.WriteAllBytes(mas_root + "/_enter.wav", GetStreamBytes(Properties.Resources.MarkuStation_menu_enter));
            timer.Interval = new TimeSpan(0,0,1);
            timer.Tick += (object? sender, EventArgs e) =>
            {
                switch (currentMenu)
                {
                    case "startup":
                        if (!((VideoPlayer?.MediaPlayer?.IsPlaying ?? false) || VideoPlayer?.MediaPlayer?.State != VLCState.Ended) || (!running))
                        {
                            VideoPlayer.MediaPlayer.Media = null;
                            mp2.Media = null;
                            File.Delete(mas_root + "/_temp.mp4");
                            File.Delete(mas_root + "/_temp.wav");
                            if (running)
                            {
                                mp2.Media = new(_libVLC, mas_root + "/_amb.wav");
                                mp2.Play();
                            }
                            VideoPlayer.IsVisible = false;
                            currentMenu = "enter";
                            timer.Interval = new TimeSpan(64000);
                            Dot1.Opacity = 0;
                            Dot2.Opacity = 0;
                            Dot3.Opacity = 0;
                            Dot4.Opacity = 0;
                            Dot1.RenderTransform = new RotateTransform(angle, 200, 0);
                            Dot2.RenderTransform = new RotateTransform(angle2, 200, 0);
                            Dot3.RenderTransform = new RotateTransform(angle3, 200, 0);
                            Dot4.RenderTransform = new RotateTransform(angle4, 200, 0);
                        }
                        break;
                    case "enter":
                        opacity += 1;
                        if (opacity > 100)
                        {
                            opacity = 100;
                        }
                        Dot1.Opacity = opacity/ 100.0;
                        Dot2.Opacity = opacity / 100.0;
                        Dot3.Opacity = opacity / 100.0;
                        Dot4.Opacity = opacity / 100.0;
                        RotateCircle();
                        if ((opacity == 100))
                        {
                            Menu.IsVisible = true;
                            MenuHotkeys.IsVisible = true;
                            currentMenu = "main";
                            return;
                        }
                        break;
                    case "main":
                        if (sel < 0)
                        {
                            sel = 1;
                        } else if (sel > 1)
                        {
                            sel = 0;
                        }
                        if (sel == 0)
                        {
                            BrowserSel.Foreground = new SolidColorBrush(Colors.DeepSkyBlue);
                            ConfigSel.Foreground = new SolidColorBrush(Colors.WhiteSmoke);
                        } else
                        {
                            BrowserSel.Foreground = new SolidColorBrush(Colors.WhiteSmoke);
                            ConfigSel.Foreground = new SolidColorBrush(Colors.DeepSkyBlue);
                        }
                        RotateCircle();
                        break;
                    case "enter_version":
                        mp.Stop();
                        mp.Media = new(_libVLC, mas_root + "/_enter.wav");
                        mp.Play();
                        currentMenu = "fade_version";
                        opacity = 100;
                        RotateCircle();
                        break;
                    case "browser":
                        MoveCircle();
                        break;
                    case "enter_browser":
                        if (opacity > 0)
                        {
                            opacity -= 1;
                        }
                        Menu.IsVisible = false;
                        KeyEsc.IsVisible = false;
                        KeyReturn.Opacity = opacity / 100.0;
                        FunkyDots.Opacity = opacity / 100.0;
                        if (!mp.IsPlaying && (opacity == 0))
                        {
                            MenuHotkeys.IsVisible = false;
                            currentMenu = "fade_browser";
                            Browser.IsVisible = true;
                            opacity = 0;
                            WhiteDot.Margin = new Avalonia.Thickness(-this.Width / 2, 0, 0, 0);
                            if (!nodata)
                            {
                                Label noDataLabel = NoData;
                                GamePanel.Children.Clear();
                                Bitmap icon;
                                using (var ms = new MemoryStream(Properties.Resources.MarkuStation_awesome))
                                {
                                    icon = new Bitmap(ms);
                                }
                                foreach (KeyValuePair<string, string> kvp in ms_games)
                                {
                                    Image sImage = new()
                                    {
                                        Source = icon,
                                        Margin = new Thickness(20),
                                        Width = 64,
                                        Name = kvp.Key,
                                    };

                                    GamePanel.Children.Add(sImage);
                                }
                                GamePanel.Children.Add(noDataLabel);
                                browser_idx = 0;
                                GameName.IsVisible = true;
                                GameName.Content = ms_games.First().Key;
                                WhiteDot.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left;
                                WhiteDot.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top;
                            }
                        }
                        break;
                    case "exit_browser":
                        if (opacity > 0)
                        {
                            opacity -= 1;
                        }
                        if (opacity == 0)
                        {
                            Browser.Opacity = 0;
                            Browser.IsVisible = false;
                            Dot1.IsVisible = true;
                            Dot2.IsVisible = true;
                            Dot3.IsVisible = true;
                            Dot4.IsVisible = true;
                            mp2.Media = new(_libVLC, mas_root + "/_amb.wav");
                            mp2.Play();
                            currentMenu = "return_menu";
                        }
                        Browser.Opacity = opacity / 100.0;
                        break;
                    case "return_menu":
                        if (opacity < 100)
                        {
                            opacity += 1;
                        }
                        FunkyDots.Opacity = opacity / 100.0;
                        if (opacity == 100)
                        {
                            currentMenu = "main";
                            KeyEsc.Opacity = 1;
                            KeyReturn.Opacity = 1;
                            Menu.IsVisible = true;
                            KeyEsc.IsVisible = true;
                            KeyReturn.IsVisible = true;
                            FunkyDots.IsVisible = true;
                            MenuHotkeys.IsVisible = true;
                        }
                        RotateCircle();
                        break;
                    case "fade_browser":
                        opacity += 1;
                        Browser.Opacity = opacity / 100.0;
                        if (opacity == 100)
                        {
                            if (ms_games.Count > 0)
                            {
                                Image firstEl = (Image)GamePanel.Children[0];
                                MoveCircle(GetAbsolutePosition(firstEl));
                            }
                            WhiteDot.ZIndex = -1;
                            Browser.Opacity = 1;
                            currentMenu = "browser";
                        }
                        break;
                    case "fade_version":
                        opacity -= 5;
                        MenuHotkeys.Opacity = opacity / 100.0;
                        Menu.Opacity = opacity / 100.0;
                        Version.Opacity = 1.0 - (opacity / 100.0);
                        if (opacity == 0)
                        {
                            Version.Opacity = 1;
                            MenuHotkeys.Opacity = 0;
                            Menu.Opacity = 0;
                            currentMenu = "version";
                        }
                        RotateCircle();
                        break;
                    case "unfade_version":
                        if (opacity == 0)
                        {
                            mp.Stop();
                            mp.Media = new(_libVLC, mas_root + "/_enter.wav");
                            mp.Play();
                        }
                        opacity += 5;
                        MenuHotkeys.Opacity = opacity / 100.0;
                        Menu.Opacity = opacity / 100.0;
                        Version.Opacity = 1.0 - (opacity / 100.0);
                        if (opacity >= 100)
                        {
                            Version.Opacity = 0;
                            MenuHotkeys.Opacity = 1;
                            Menu.Opacity = 1;
                            currentMenu = "main";
                        }
                        RotateCircle();
                        break;
                    case "version":
                        RotateCircle();
                        break;
                    case "run_game":
                        if (opacity == 100)
                        {
                            MoveCircle(new Point(this.Width, this.Height), true);
                        } else if (opacity == 0)
                        {
                            VideoPlayer.MediaPlayer.Media = new(_libVLC, mas_root + "/_temp.mp4");
                            mp2.Media = new(_libVLC, mas_root + "/_temp.wav");
                            VideoPlayer.IsVisible = true;
                            VideoPlayer.MediaPlayer.Play();
                            mp2.Play();
                            currentMenu = "prepare_game";
                        }
                        opacity--;
                        Browser.Opacity = opacity / 100.0;
                        break;
                    case "prepare_game":
                        if (!((VideoPlayer?.MediaPlayer?.IsPlaying ?? false) || VideoPlayer?.MediaPlayer?.State != VLCState.Ended))
                        {
                            VideoPlayer.MediaPlayer.Media.Dispose();
                            mp.Media.Dispose();
                            if (File.Exists(ms_games[ms_games.Keys.ToArray()[browser_idx]]))
                            {
                                try
                                {
                                    File.Delete(mas_root + "/_temp.mp4");
                                    File.Delete(mas_root + "/_temp.wav");
                                } catch { }
                                Process p = new();
                                p.StartInfo.FileName = ms_games[ms_games.Keys.ToArray()[browser_idx]];
                                p.StartInfo.UseShellExecute = true;
                                p.Start();
                                Close();
                            } else
                            {
                                VideoPlayer.IsVisible = false;
                                currentMenu = "fade_browser";
                                moveCircle.Stop();
                                browser_idx = 0;
                                GameName.Content = ms_games.Keys.First();
                            }
                        }
                        break;
                }
            };
        }

        public void MoveCircle(Point p, bool slowly = false)
        {
            if (slowly)
            {
                if ((moveCircle.IsEnabled))
                {
                    moveCircle.Stop();
                }
                moveCircle.Interval = new TimeSpan(64000);
                moveCircle.Tick += (object? sender, EventArgs e) =>
                {
                    bool adjusted = false;
                    double marginX = WhiteDot.Margin.Left;
                    double marginY = WhiteDot.Margin.Top;
                    if (WhiteDot.Margin.Left < p.X)
                    {
                        marginX += 1;
                        adjusted = true;
                    }
                    else if (WhiteDot.Margin.Left > p.X)
                    {
                        marginX += -1;
                        adjusted = true;
                    }
                    if (WhiteDot.Margin.Top < p.Y)
                    {
                        marginY += 1;
                        adjusted = true;
                    }
                    else if (WhiteDot.Margin.Top > p.Y)
                    {
                        marginY += -1;
                        adjusted = true;
                    }
                    if (adjusted)
                    {
                        WhiteDot.Margin = new Thickness(marginX, marginY, 0, 0);
                    } else
                    {
                        moveCircle.Stop();
                        moveCircle = new();
                        return;
                    }
                };
                moveCircle.Start();
            } else
            {
                WhiteDot.Margin = new Thickness(p.X, p.Y, 0, 0);
            }
        }

        public static Point GetAbsolutePosition(Control control) // chatgpt helped a bit with this
        {
            if (control.GetVisualRoot() is not Control topLevel)
            {
                return new Point(0, 0);
            }
            var transform = control.TransformToVisual(topLevel);
            return transform?.Transform(new Point(0,0)) ?? new Point(0, 0);
        }

        private void MoveCircle()
        {
            if (nodata)
            {
                if (!invert)
                {
                    WhiteDot.RenderTransform = new TranslateTransform(circle_offset, circle_offset);
                    circle_offset += 1;
                    if (circle_offset > this.Height + WhiteDot.Height)
                    {
                        invert = !invert;
                        WhiteDot.Margin = new Avalonia.Thickness(0, 0, -this.Width / 4, 0);
                    }
                }
                else
                {
                    WhiteDot.RenderTransform = new TranslateTransform(-circle_offset, circle_offset);
                    circle_offset -= 1;
                    if (circle_offset < -WhiteDot.Height)
                    {
                        invert = !invert;
                        WhiteDot.Margin = new Avalonia.Thickness(-this.Width / 2, 0, 0, 0);
                    }
                }
            } else
            {
                if (!invert)
                {
                    circle_offset += 1;
                    if (circle_offset > 100)
                    {
                        invert = !invert;
                        circle_offset = 100;
                    }
                    WhiteDot.Opacity = circle_offset / 100.0;
                }
                else
                {
                    circle_offset -= 1;
                    if (circle_offset < 0)
                    {
                        invert = !invert;
                        circle_offset = 0;
                    }
                    WhiteDot.Opacity = circle_offset / 100.0;
                }
            }
        }

        private void RotateCircle()
        {
            angle += rand.Next(1, 3);
            angle = angle > 359 ? angle - 360 : angle;
            angle2 += rand.Next(1, 3);
            angle2 = angle2 > 359 ? angle2 - 360 : angle2;
            angle3 += rand.Next(1, 3);
            angle3 = angle3 > 359 ? angle3 - 360 : angle3;
            angle4 += rand.Next(1, 3);
            angle4 = angle4 > 359 ? angle4 - 360 : angle4;
            Dot1.RenderTransform = new RotateTransform(angle, 200, 0);
            Dot2.RenderTransform = new RotateTransform(angle2, 200, 0);
            Dot3.RenderTransform = new RotateTransform(angle3, 200, 0);
            Dot4.RenderTransform = new RotateTransform(angle4, 200, 0);
        }

        private static byte[] GetStreamBytes(UnmanagedMemoryStream ms)
        {
            byte[] bytes = [];
            int i = 0;
            using (UnmanagedMemoryStream umm = ms)
            {
                int buffer = umm.ReadByte();
                bytes = new byte[umm.Length];
                while (buffer > -1)
                {
                    bytes[i] = (byte)buffer;
                    i++;
                    buffer = umm.ReadByte();
                }
            }
            return bytes;
        }

        private void Window_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            timer.Start();
            VideoPlayer.MediaPlayer = mp;
            mp2.Media = new(_libVLC, mas_root + "/_temp.wav");
            VideoPlayer.MediaPlayer.Media = new(_libVLC, mas_root + "/_temp.mp4");
            if (running)
            {
                VideoPlayer.MediaPlayer.Play();
                mp2.Play();
            }
        }

        private void Navigate(Avalonia.Input.Key key)
        {
            if (currentMenu == "main")
            {
                switch (key)
                {
                    case Avalonia.Input.Key.Up:
                        SelSound();
                        sel -= 1;
                        break;
                    case Avalonia.Input.Key.Down:
                        SelSound();
                        sel -= 1;
                        break;
                    case Avalonia.Input.Key.Enter:
                        Enter();
                        break;
                    case Avalonia.Input.Key.Escape:
                        VersionContent.IsVisible = true;
                        ConfigContent.IsVisible = false;
                        currentMenu = "enter_version";
                        break;
                }
            } else if (currentMenu == "version")
            {
                currentMenu = "unfade_version";
            } else if (currentMenu == "browser")
            {
                switch (key)
                {
                    case Avalonia.Input.Key.Left:
                    case Avalonia.Input.Key.Up:
                        if (ms_games.Count > 0)
                        {
                            SelSound();
                            browser_idx -= 1;
                            if (browser_idx < 0)
                            {
                                browser_idx = ms_games.Count - 1;
                            }
                            GameName.Content = ms_games.Keys.ToArray()[browser_idx];
                            MoveCircle(GetAbsolutePosition(GamePanel.Children[browser_idx]));
                        }
                        break;
                    case Avalonia.Input.Key.Down:
                    case Avalonia.Input.Key.Right:
                        if (ms_games.Count > 0)
                        {
                            SelSound();
                            browser_idx += 1;
                            if (browser_idx >= ms_games.Count)
                            {
                                browser_idx = 0;
                            }
                            GameName.Content = ms_games.Keys.ToArray()[browser_idx];
                            MoveCircle(GetAbsolutePosition(GamePanel.Children[browser_idx]));
                        }
                        break;
                    case Avalonia.Input.Key.Escape:
                        currentMenu = "exit_browser";
                        break;
                    case Avalonia.Input.Key.Enter:
                        Enter();
                        break;
                }
            }
        }

        private void SelSound()
        {
            mp.Stop();
            mp.Media = new(_libVLC, mas_root + "/_sel.wav");
            mp.Play();
        }

        private void Enter()
        {
            if (currentMenu == "main")
            {
                mp.Stop();
                mp.Media = new(_libVLC, mas_root + "/_enter.wav");
                mp.Play();
                if (sel == 0)
                {
                    currentMenu = "enter_browser";
                    mp2.Stop();
                } else
                {
                    VersionContent.IsVisible = false;
                    ConfigContent.IsVisible = true;
                    currentMenu = "enter_version";
                }
            } else if ((currentMenu == "browser") && (ms_games.Count > 0))
            {
                mp.Stop();
                mp.Media = new(_libVLC, mas_root + "/_enter.wav");
                mp.Play();
                currentMenu = "run_game";
                File.WriteAllBytes(mas_root + "/_temp.mp4", Properties.Resources.MarkuStation_logo_animation);
                File.WriteAllBytes(mas_root + "/_temp.wav", GetStreamBytes(Properties.Resources.MarkuStation_logo));
            }
        }

        private void Window_KeyDown(object? sender, Avalonia.Input.KeyEventArgs e)
        {
            Navigate(e.Key);
        }

        private void Window_Closing(object? sender, WindowClosingEventArgs e)
        {
            try
            {
                DeleteIfExists(mas_root + "/_sel.wav");
                DeleteIfExists(mas_root + "/_enter.wav");
                DeleteIfExists(mas_root + "/_amb.wav");
                DeleteIfExists(mas_root + "/_temp.wav");
                DeleteIfExists(mas_root + "/_temp.mp4");
            } catch
            {
                Console.WriteLine("Failed to remove extracted temporary assets, please manually erase the files at \"" + mas_root + "\"");
            }
        }

        private static void DeleteIfExists(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}