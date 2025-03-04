using System;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace DesktopIcons;

public partial class TopIcon : Window
{
    private bool down = false;
    private bool canclose = false;
    internal string action = string.Empty;
    internal string icon = "Apps";
    internal MainWindow myparent = null;
    public TopIcon()
    {
        InitializeComponent();
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
        this.BeginMoveDrag(e);
        if (e.ClickCount == 2)
        {
            if (action != "special:apps")
            {
                string args = "";
                if (action.Contains("--new"))
                {
                    args = action.Split(' ')[1];
                    action = action.Split(' ')[0];
                }
                Process p = new();
                p.StartInfo.FileName = action;
                p.StartInfo.Arguments = args;
                p.StartInfo.UseShellExecute = true;
                p.Start();   
            }
            else
            {
                myparent.Show();
            }
        }
        down = true;
    }

    private void InputElement_OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        down = false;
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

    private void Window_OnClosing(object? sender, WindowClosingEventArgs e)
    {
        e.Cancel = !canclose;
    }
}