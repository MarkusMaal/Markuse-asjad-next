using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Data.Core;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Rendering.Composition;
using Avalonia.Threading;
using System;
using System.IO;
using Avalonia;
using Avalonia.Platform;
using System.Threading;
using Avalonia.Animation.Easings;
using Avalonia.Markup.Xaml;
using Microsoft.CodeAnalysis.CSharp.Scripting;

namespace Markuse_arvuti_ooterežiim
{
    public partial class MainWindow : Window
    {
        private ScreensaverOptions scr = new();
        int moves = 0;
        int dx = 5;
        int dy = 5;
        private MainWindowModel mwm;
        private static Random r = new();
        
        /// <summary>
        /// If first value true, move down, else move up. If second value true, move right, else move left.
        /// </summary>
        internal bool[] DownRight = [true, true];
        
        private PixelPoint CurrentPosition = new (-1, -1);

        private MainWindow child;

        private bool EnableBg = false;
        
        Bitmap background;
        private string mas_root;

        public MainWindow()
        {
            mas_root = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/.mas";
            if (File.Exists(mas_root + "/ScreensaverConfig.json"))
            {
                scr.Load(mas_root);
            }
            else
            {
                scr = new ScreensaverOptions()
                {
                    AnimationEasing = ScreensaverOptions.Easing.ExponentialEaseInOut,
                    AnimationInterval = 3,
                    BackgroundPath = mas_root + "/bg_common.png",
                    CustomImage = false,
                    DisplayBackground = false,
                    ImagePath = "",
                    ImageWidth = 128,
                };
                scr.Save(mas_root);
            }
            InitializeComponent();
            this.GetControl<Image>("LogoImageR").Width = scr.ImageWidth;
            this.GetControl<Image>("LogoImageR").Height = scr.ImageWidth;
            MoveNow();
            
            var commonBg = $"{mas_root}/bg_common.png";
            EnableBg = scr.DisplayBackground;
            if (EnableBg)
            {
                commonBg = scr.BackgroundPath;
            }
            if (File.Exists(commonBg))
            {
                using var ms = new FileStream(commonBg, FileMode.Open, FileAccess.Read);
                background = new Bitmap(ms);
                if (EnableBg) this.Background = new ImageBrush(background) { Stretch = Stretch.UniformToFill, Transform = new ScaleTransform(1+r.NextDouble()*2, 1+r.NextDouble()*2) };
            }

            if (scr.CustomImage && File.Exists(scr.ImagePath))
            {
                using var ms = new FileStream(scr.ImagePath, FileMode.Open, FileAccess.Read);
                this.GetControl<Image>("LogoImageR").Source = new Bitmap(ms);
            }
            else
            {
                this.GetControl<Image>("LogoImageR").Source = GetResource(Properties.Resources.mas_general);
            }
            this.Icon = new WindowIcon(GetResource(Properties.Resources.mas_general));

            DataContext = mwm;
            if (Screens.All.Count > Program.monitors)
            {
                Program.monitors++;
                child = new MainWindow
                {
                    WindowStartupLocation = WindowStartupLocation.Manual,
                    Position = new PixelPoint(Screens.All[Program.monitors - 1].WorkingArea.TopLeft.X, Screens.All[Program.monitors - 1].WorkingArea.TopLeft.Y),
                    DownRight = [r.Next(0, 1) == 1, r.Next(0, 1) == 1]
                };
                child.Show();
            }
            // make sure first window is on the first monitor (neccessary when window is opened from a secondary display)
            if (this.WindowStartupLocation != WindowStartupLocation.Manual)
            {
                this.WindowStartupLocation = WindowStartupLocation.Manual;
                this.Position = new PixelPoint(Screens.All[0].WorkingArea.TopLeft.X, Screens.All[0].WorkingArea.TopLeft.Y);
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private static Bitmap GetResource(byte[] resource)
        {
            using var ms = new MemoryStream(resource);
            return new Bitmap(ms);
        }

        private void MoveNow()
        {
            if (CurrentPosition is { X: < 0, Y: < 0 })
            {
                var primaryScreen = Screens.Primary ?? Screens.All[0];
                var boundsX = DownRight[1] ? primaryScreen.Bounds.Width - this.GetControl<Image>("LogoImageR").Width : 0;
                var boundsY = DownRight[0] ? primaryScreen.Bounds.Height - this.GetControl<Image>("LogoImageR").Height: 0;
                CurrentPosition = new PixelPoint(r.Next(0, (int)boundsX - (int)this.GetControl<Image>("LogoImageR").Width), r.Next(0, (int)boundsY - (int)this.GetControl<Image>("LogoImageR").Height));
            }
            var PreviousPosition = CurrentPosition;
            FigureOutNextPoint();
            
            var type = Type.GetType("Avalonia.Animation.Easings." + scr.AnimationEasing + ", Avalonia.Base");
            var easing = type != null ? (Easing)Activator.CreateInstance(type)! : new ExponentialEaseInOut();
            mwm = new MainWindowModel
            {
                StartingPoint = new Thickness(PreviousPosition.X, PreviousPosition.Y, 0, 0),
                ScreenDimensions = new Thickness(CurrentPosition.X, CurrentPosition.Y, 0, 0),
                Rotation = 360 * (DownRight[1] ? 1 : -1),
                Duration = TimeSpan.FromSeconds(scr.AnimationInterval),
                Easing = easing
            };
            this.DataContext = mwm;
            mwm.Duration = TimeSpan.FromSeconds(scr.AnimationInterval);

            var animation = (Animation)this.Resources["LogoAnimation"];
            animation.RunAsync(this.GetControl<Image>("LogoImageR"));
        }
        
        private void FigureOutNextPoint()
        {
            var primaryScreen = Screens.Primary ?? Screens.All[0];
            var boundsX = DownRight[1] ? primaryScreen.Bounds.Width - this.GetControl<Image>("LogoImageR").Width : 0;
            var boundsY = DownRight[0] ? primaryScreen.Bounds.Height - this.GetControl<Image>("LogoImageR").Height: 0;
            var nextX = CurrentPosition.X;
            var nextY = CurrentPosition.Y;
            var deltaX = DownRight[1] ? 1 : -1;
            var deltaY = DownRight[0] ? 1 : -1;
            var reachBounds = false;
            while (!reachBounds)
            {
                nextX += deltaX; 
                nextY += deltaY;

                if (nextX == boundsX)
                {
                    reachBounds = true;
                    DownRight[1] = !DownRight[1];
                }

                if (nextY != boundsY) continue;
                reachBounds = true;
                DownRight[0] = !DownRight[0];
            }
            CurrentPosition = new PixelPoint(nextX, nextY);
        }

        private void Window_KeyDown(object? sender, Avalonia.Input.KeyEventArgs e)
        {
            this.Close();
        }

        private void Window_PointerMoved(object? sender, Avalonia.Input.PointerEventArgs e)
        {
            if (moves < 50)
            {
                moves++;
                return;
            }
            this.Close();
        }

        private void Window_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            this.WindowState = WindowState.FullScreen;
            var timer = new DispatcherTimer();
            timer.Tick += (_, _) =>
            { 
                MoveNow();
                if (EnableBg) this.Background = new ImageBrush(background) { Stretch = Stretch.UniformToFill, Transform = new ScaleTransform(1+r.NextDouble()*2, 1+r.NextDouble()*2) };
            };
            timer.Interval = TimeSpan.FromSeconds((double)scr.AnimationInterval + 0.03);
            timer.Start();

            // hide mouse cursor
            new Thread(() =>
            {
                for (int i = 0; i < 10; i++)
                {
                    Thread.Sleep(100);
                    Dispatcher.UIThread.Post(() =>
                        this.Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.None));
                }
            }).Start();
        }

        private void Window_Closing(object? sender, Avalonia.Controls.WindowClosingEventArgs e)
        {
            // just let the app kill itself, it's just a screensaver, no sensitive data is found here
            Environment.Exit(0);
        }
    }
}