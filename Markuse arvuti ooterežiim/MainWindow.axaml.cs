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

namespace Markuse_arvuti_ooterežiim
{
    public partial class MainWindow : Window
    {
        int moves = 0;
        int dx = 5;
        int dy = 5;
        TranslateTransform tt = new TranslateTransform(0, 0);
        private MainWindowModel mwm;
        
        /// <summary>
        /// If first value true, move down, else move up. If second value true, move right, else move left.
        /// </summary>
        private bool[] DownRight = [true, true];
        
        private PixelPoint CurrentPosition = new PixelPoint(0, 0);
        
        Bitmap background;
        public MainWindow()
        {
            InitializeComponent();
            MoveNow();
            Bitmap icon;
            using (var ms = new MemoryStream(Properties.Resources.mas_general))
            {
                icon = new Bitmap(ms);
            }

            var commonBg = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/.mas/bg_common.png";
            if (File.Exists(commonBg))
            {
                using var ms = new FileStream(commonBg, FileMode.Open);
                background = new Bitmap(ms);
                this.Background = new ImageBrush(background) { Stretch = Stretch.UniformToFill};
            }

            this.Icon = new WindowIcon(icon);
            this.LogoImage.Source = icon;

            
            DataContext = mwm;
        }

        private void MoveNow()
        {
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
            animation.RunAsync(LogoImage);
        }
        
        private void FigureOutNextPoint()
        {
            var primaryScreen = Screens.Primary ?? Screens.All[0];
            var boundsX = DownRight[1] ? primaryScreen.Bounds.Width - LogoImage.Width : 0;
            var boundsY = DownRight[0] ? primaryScreen.Bounds.Height - LogoImage.Height: 0;
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
            
            var timer = new DispatcherTimer();
            timer.Tick += (_, _) =>
            { 
                MoveNow();
            };
            timer.Interval = TimeSpan.FromSeconds(3.03);
            timer.Start();
        }
    }
}