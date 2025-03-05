using System;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
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
    internal MainWindow myparent = null;
    private double opacity = 0;
    
    
    public TopIcon()
    {
        InitializeComponent();
        
        ShowInTaskbar = false;
        WindowStartupLocation = WindowStartupLocation.Manual;
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
        bool special_icon = (action != "special:apps" && action.StartsWith("special:"));
        if (!myparent.locked && !special_icon) this.BeginMoveDrag(e);
        if (action == "special:apps" || (!action.StartsWith("special:")))
        {
            opacity = this.Pic.Opacity;
            this.Pic.Opacity = 0.75;
        }

        if ((e.ClickCount == 2) || (e.ClickCount == 1 && special_icon))
        {
            if (!action.StartsWith("special:"))
            {
                string args = "";
                if (action.Contains("--new"))
                {
                    args = action.Split(' ')[1];
                    action = action.Split(' ')[0];
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
                        myparent.Show();
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
            this.Pic.Opacity = opacity;
        }
    }

    private void InputElement_OnPointerMoved(object? sender, PointerEventArgs e)
    {
        
    }

    private void Control_OnLoaded(object? sender, RoutedEventArgs e)
    {
        this.Pic.Width = this.Width;
        this.Pic.Height = this.Height;
        this.Pic.BackgroundSizing = BackgroundSizing.InnerBorderEdge;
        this.Pic.Background = new ImageBrush(GetResource("TopIcon" + icon));
    }

    public void ReplaceIcon(string name)
    {
        this.Pic.Background = new ImageBrush(GetResource("TopIcon" + name));
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
        this.BgCol.Background = new SolidColorBrush(Color.Parse("#a0" + myparent.theme.R.ToString("X").PadLeft(2, '0') +
                                                          myparent.theme.G.ToString("X").PadLeft(2, '0') +
                                                          myparent.theme.B.ToString("X").PadLeft(2, '0')))
            { Opacity = o };
    }
    
    private void InputElement_OnPointerEntered(object? sender, PointerEventArgs e)
    {
        opacity = Pic.Opacity;
        Pic.Opacity = 1;
        if (this.BgCol.Background.Opacity != 0)
        {
            SetOpacity(this.BgCol.Background.Opacity + 0.25);
        }
    }

    private void InputElement_OnPointerExited(object? sender, PointerEventArgs e)
    {
        Pic.Opacity = opacity;
        if (this.BgCol.Background.Opacity != 0)
        {
            SetOpacity(this.BgCol.Background.Opacity - 0.25);
        }
    }
}