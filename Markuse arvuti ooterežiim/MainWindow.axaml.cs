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

namespace Markuse_arvuti_ooterežiim
{
    public partial class MainWindow : Window
    {
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

        public MainWindow()
        {
            InitializeComponent();
            MoveNow();
            var commonBg = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/.mas/bg_common.png";
            if (File.Exists(commonBg))
            {
                using var ms = new FileStream(commonBg, FileMode.Open, FileAccess.Read);
                background = new Bitmap(ms);
                if (EnableBg) this.Background = new ImageBrush(background) { Stretch = Stretch.UniformToFill, Transform = new ScaleTransform(1+r.NextDouble()*2, 1+r.NextDouble()*2) };
            }
            this.Icon = new WindowIcon(GetResource(Properties.Resources.mas_general));
            this.LogoImageR.Source = GetResource(Properties.Resources.mas_general);

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
                var boundsX = DownRight[1] ? primaryScreen.Bounds.Width - LogoImageR.Width : 0;
                var boundsY = DownRight[0] ? primaryScreen.Bounds.Height - LogoImageR.Height: 0;
                CurrentPosition = new PixelPoint(r.Next(0, (int)boundsX - (int)LogoImageR.Width), r.Next(0, (int)boundsY - (int)LogoImageR.Height));
            }
            var PreviousPosition = CurrentPosition;
            FigureOutNextPoint();
            mwm = new MainWindowModel
            {
                StartingPoint = new Thickness(PreviousPosition.X, PreviousPosition.Y, 0, 0),
                ScreenDimensions = new Thickness(CurrentPosition.X, CurrentPosition.Y, 0, 0),
                Rotation = 360 * (DownRight[1] ? 1 : -1)
            };
            this.DataContext = mwm;
            var animation = (Animation)this.Resources["LogoAnimation"];
            animation.RunAsync(LogoImageR);
        }
        
        private void FigureOutNextPoint()
        {
            var primaryScreen = Screens.Primary ?? Screens.All[0];
            var boundsX = DownRight[1] ? primaryScreen.Bounds.Width - LogoImageR.Width : 0;
            var boundsY = DownRight[0] ? primaryScreen.Bounds.Height - LogoImageR.Height: 0;
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
            timer.Interval = TimeSpan.FromSeconds(3.03);
            timer.Start();

            // hide mouse cursor
            new Thread(() =>
            {
                Thread.Sleep(1000);
                Dispatcher.UIThread.Post(() => this.Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.None));
            }).Start();
        }

        private void Window_Closing(object? sender, Avalonia.Controls.WindowClosingEventArgs e)
        {
            // just let the app kill itself, it's just a screensaver, no sensitive data is found here
            Environment.Exit(0);
        }
    }
}