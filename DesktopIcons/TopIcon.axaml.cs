using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace DesktopIcons;

public partial class TopIcon : Window
{
    private bool down = false;
    public bool canClose = false;
    internal string action = string.Empty;
    internal string icon = "Apps";
    internal string alt_icon = "";
    
    internal MainWindow myparent = null;
    private double opacity = 0;
    
    
    public TopIcon()
    {
        InitializeComponent();
        
        ShowInTaskbar = false;
        WindowStartupLocation = WindowStartupLocation.Manual;
        // prevent window borders from being rendered if not in a Linux DE
        if (!OperatingSystem.IsLinux())
        {
            //Console.WriteLine("Windows/Mac paranduste aktiveerimine...");
            this.ExtendClientAreaToDecorationsHint = false;
            this.SystemDecorations = SystemDecorations.None;
        }
        if (OperatingSystem.IsWindows())
        {
            // prevents maximizing the window, because the dumb Windows tablet mode tries to do that no matter what
            var handle = Win32Interop.GetWindowHandle(this);
            Win32Interop.SetWindowLongPtr(handle, Win32Interop.GWL_STYLE, Win32Interop.GetWindowLongPtr(handle, Win32Interop.GWL_STYLE) & ~Win32Interop.WS_MAXIMIZEBOX);
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public static Bitmap GetResource(string name)
    {
        object? resource = App.Current?.Resources[name];
        if (resource == null)
        {
            return new Bitmap(".");
        }
        else
        {
            Bitmap? bmp = (Bitmap?)((Image?)resource)?.Source;
            return bmp ?? new Bitmap(".");
        }
    }

    private void InputElement_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        bool special_icon = alt_icon != "";
        if (!myparent.locked && !special_icon) this.BeginMoveDrag(e);
        if (action == "special:apps" || (!action.StartsWith("special:")))
        {
            opacity = this.GetControl<Label>("Pic").Opacity;
            this.GetControl<Label>("Pic").Opacity = 0.75;
        }

        if ((e.ClickCount == 2) || (e.ClickCount == 1 && special_icon))
        {
            if (!action.StartsWith("special:"))
            {
                var args = "";
                if (action.Contains(' ') && !action.Contains('"'))
                {
                    args = string.Join(' ', action.Split(' ').Skip(1));
                    action = action.Split(' ')[0];
                }

                if (action.StartsWith('"') && action.EndsWith('"'))
                {
                    action = action.Substring(1, action.Length - 2);
                }
                Process p = new();
                p.StartInfo.FileName = action;
                if (OperatingSystem.IsWindows())
                {
                    p.StartInfo.FileName = p.StartInfo.FileName.Replace("/", "\\");
                }
                p.StartInfo.Arguments = args;
                p.StartInfo.UseShellExecute = true;
                p.Start();   
            }
            else
            {
                switch (action.Replace("special:", "").ToLower())
                {
                    case "apps":
                        if (!myparent.IsVisible)
                        {
                            myparent.Position = new PixelPoint(0, 0);
                            myparent.Show();
                        }
                        else { myparent.Hide(); }
                        break;
                    case "showhide":
                        myparent.ToggleChildren(this);
                        break;
                    // ReSharper disable once StringLiteralTypo
                    case "lockunlock":
                        myparent.ToggleChildLock(this);
                        break;
                    case "reset":
                        myparent.ResetChildren();
                        break;
                }
            }
        }
        down = true;
    }

    private void InputElement_OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        down = false;
        if (action == "special:apps" || (!action.StartsWith("special:")))
        {
            this.GetControl<Label>("Pic").Opacity = opacity;
        }

        if (!myparent.locked)
        {
            myparent.UpdatePositions();
        }
    }

    private void InputElement_OnPointerMoved(object? sender, PointerEventArgs e)
    {
        
    }

    private void Control_OnLoaded(object? sender, RoutedEventArgs e)
    {
        this.GetControl<Label>("Pic").Width = this.Width;
        this.GetControl<Label>("Pic").Height = this.Height;
        this.GetControl<Label>("Pic").BackgroundSizing = BackgroundSizing.InnerBorderEdge;
        this.GetControl<Label>("Pic").Background = new ImageBrush(GetResource("TopIcon" + icon));
        this.GetControl<Polygon>("Glass").Points = [new Point(0,0), new Point(Width, 0), new Point(0, Height)];
        if (this.action == "special:mas")
        {
            this.GetControl<Polygon>("Glass").Fill = new SolidColorBrush(Colors.Transparent);
            Refocus();
            return;
        }
        UnfocusGlass();
        Refocus();
    }

    private void UnfocusGlass()
    {
        if (this.action == "special:mas")
        {
            this.GetControl<Polygon>("Glass").Fill = new SolidColorBrush(Colors.Transparent);
            return;
        }
        this.GetControl<Polygon>("Glass").Fill = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(new Point(Width / 2, 0), RelativeUnit.Absolute),
            EndPoint = new RelativePoint(new Point(Width / 2, Height), RelativeUnit.Absolute),
            GradientStops = [new GradientStop(Color.FromArgb(60, 255, 255, 255), -0.1), new GradientStop(Color.FromArgb(5, 255, 255, 255), 0.25),
                new GradientStop(Color.FromArgb(0, 255, 255, 255), 0.5)]
        };
    }

    private void FocusGlass()
    {
        if (this.action == "special:mas")
        {
            this.GetControl<Polygon>("Glass").Fill = new SolidColorBrush(Colors.Transparent);
            return;
        }
        this.GetControl<Polygon>("Glass").Fill = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(new Point(Width / 2, 0), RelativeUnit.Absolute),
            EndPoint = new RelativePoint(new Point(Width / 2, Height), RelativeUnit.Absolute),
            GradientStops = [new GradientStop(Color.FromArgb(80, 255, 255, 255), -0.1), new GradientStop(Color.FromArgb(25, 255, 255, 255), 0.25),
            new GradientStop(Color.FromArgb(0, 255, 255, 255), 0.5)]
        };
    }

    private void Refocus()
    {
        ShowInTaskbar = true;
        ShowInTaskbar = false;
    }

    public void ReplaceIcon(string name)
    {
        this.GetControl<Label>("Pic").Background = new ImageBrush(GetResource("TopIcon" + name));
    }

    private void Window_OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (!canClose)
        {
            canClose = e.CloseReason is (WindowCloseReason.ApplicationShutdown or WindowCloseReason.OSShutdown
                    or WindowCloseReason.OwnerWindowClosing);
        }
        e.Cancel = !canClose;
    }

    private void SetOpacity(double o)
    {
        this.GetControl<Border>("BgCol").Background = new SolidColorBrush(Color.Parse("#a0" + myparent.theme.R.ToString("X").PadLeft(2, '0') +
                                                          myparent.theme.G.ToString("X").PadLeft(2, '0') +
                                                          myparent.theme.B.ToString("X").PadLeft(2, '0')))
            { Opacity = o };
    }
    
    private void InputElement_OnPointerEntered(object? sender, PointerEventArgs e)
    {
        opacity = this.GetControl<Label>("Pic").Opacity;
        this.GetControl<Label>("Pic").Opacity = 1;
        if (this.GetControl<Border>("BgCol").Background.Opacity != 0)
        {
            SetOpacity(this.GetControl<Border>("BgCol").Background.Opacity + 0.25);
            FocusGlass();
        }
    }

    private void InputElement_OnPointerExited(object? sender, PointerEventArgs e)
    {
        this.GetControl<Label>("Pic").Opacity = opacity;
        if (this.GetControl<Border>("BgCol").Background.Opacity != 0)
        {
            SetOpacity(this.GetControl<Border>("BgCol").Background.Opacity - 0.25);
            UnfocusGlass();
        }
    }

    private void Window_Opened(object? sender, EventArgs e)
    {
        if (OperatingSystem.IsMacOS()) MacOSInterop.SetWindowProperties(this); // Specific for Mac users, uses native macOS APIs                        
        else if (OperatingSystem.IsWindows()) new Win32Interop().KeepWindowAtBottom(this); // Specific for Windows

        // since Linux uses many different window managers, they should use the rules for their specific window manager
        // (e.g. if you use KDE Plasma, set relevant KWin rules in plasma-settings)

        // the code here doesn't deal with it unfortunately...
    }
}