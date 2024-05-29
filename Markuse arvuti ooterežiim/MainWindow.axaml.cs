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

namespace Markuse_arvuti_ooterežiim
{
    public partial class MainWindow : Window
    {
        int moves = 0;
        int dx = 5;
        int dy = 5;
        TranslateTransform tt = new TranslateTransform(0, 0);
        public MainWindow()
        {
            InitializeComponent();
            Bitmap icon;
            using (var ms = new MemoryStream(Properties.Resources.mas_general))
            {
                icon = new Bitmap(ms);
            }
            this.Icon = new WindowIcon(icon);
            this.LogoImage.Source = icon;
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
            PathGeometry animationPath = new PathGeometry();
            PathFigure pFigure = new PathFigure();
            pFigure.StartPoint = new Avalonia.Point(0, 0);
            BezierSegment pBezierSegment = new BezierSegment();
            pBezierSegment.Point1 = new Avalonia.Point(0, 0);
            pBezierSegment.Point2 = new Avalonia.Point(this.Width, 0);
            pBezierSegment.Point3 = new Avalonia.Point(this.Width, this.Height);

        }
    }
}