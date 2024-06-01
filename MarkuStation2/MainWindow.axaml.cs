using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Threading;
using LibVLCSharp.Shared;
using System;
using System.IO;

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
        int position;
        int opacity = 0;
        int sel = 0;
        readonly Random rand = new();
        public MainWindow()
        {
            InitializeComponent();
            _libVLC = new LibVLC();
            mp = new MediaPlayer(_libVLC);
            mp2 = new MediaPlayer(_libVLC);
            position = (int)100;
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
                        if (!((VideoPlayer?.MediaPlayer?.IsPlaying ?? false) || VideoPlayer?.MediaPlayer?.State != VLCState.Ended) || (!File.Exists(mas_root + "/dont_play.txt")))
                        {
                            VideoPlayer.MediaPlayer.Media = null;
                            mp2.Media = null;
                            File.Delete(mas_root + "/_temp.mp4");
                            File.Delete(mas_root + "/_temp.wav");
                            mp2.Media = new(_libVLC, mas_root + "/_amb.wav");
                            mp2.Play();
                            VideoPlayer.IsVisible = false;
                            currentMenu = "enter";
                            timer.Interval = new TimeSpan(64000);
                            this.Dot1.Opacity = 0;
                            this.Dot2.Opacity = 0;
                            this.Dot3.Opacity = 0;
                            this.Dot4.Opacity = 0;
                            this.Dot1.RenderTransform = new RotateTransform(angle, 200, 0);
                            this.Dot2.RenderTransform = new RotateTransform(angle2, 200, 0);
                            this.Dot3.RenderTransform = new RotateTransform(angle3, 200, 0);
                            this.Dot4.RenderTransform = new RotateTransform(angle4, 200, 0);
                        }
                        break;
                    case "enter":
                        position -= 1;
                        opacity += 1;
                        if (opacity > 100)
                        {
                            opacity = 100;
                        }
                        this.Dot1.Opacity = (double)opacity/ 100.0;
                        this.Dot2.Opacity = (double)opacity / 100.0;
                        this.Dot3.Opacity = (double)opacity / 100.0;
                        this.Dot4.Opacity = (double)opacity / 100.0;
                        if ((opacity == 100))
                        {
                            currentMenu = "main";
                            return;
                        }
                        break;
                    case "main":
                        angle += rand.Next(1, 3);
                        angle = angle > 359 ? angle - 360 : angle;
                        angle2 += rand.Next(1, 3);
                        angle2 = angle2 > 359 ? angle2 - 360 : angle2;
                        angle3 += rand.Next(1, 3);
                        angle3 = angle3 > 359 ? angle3 - 360 : angle3;
                        angle4 += rand.Next(1, 3);
                        angle4 = angle4 > 359 ? angle4 - 360 : angle4;
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
                        this.Dot1.RenderTransform = new RotateTransform(angle, 200, 0);
                        this.Dot2.RenderTransform = new RotateTransform(angle2, 200, 0);
                        this.Dot3.RenderTransform = new RotateTransform(angle3, 200, 0);
                        this.Dot4.RenderTransform = new RotateTransform(angle4, 200, 0);
                        break;
                }
            };
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
            if (File.Exists(mas_root + "/dont_play.txt"))
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
                        EnterSound();
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

        private void EnterSound()
        {
            mp.Stop();
            mp.Media = new(_libVLC, mas_root + "/_enter.wav");
            mp.Play();
        }

        private void Window_KeyDown(object? sender, Avalonia.Input.KeyEventArgs e)
        {
            Navigate(e.Key);
        }
    }
}