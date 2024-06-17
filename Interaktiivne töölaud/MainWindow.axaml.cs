using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Diagnostics;

namespace Interaktiivne_töölaud
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Enter_PointerReleased(object? sender, RoutedEventArgs e)
        {
            Inside inside = new();
            inside.Show();
            Close();
        }

        private void Playground_Click(object? sender, RoutedEventArgs e)
        {
            Playground pg = new();
            pg.Show();
            Close();
        }

        private void LogoutNow(object? sender, RoutedEventArgs e)
        {
            Process p = new();
            if (OperatingSystem.IsMacOS())
            {
                p.StartInfo.FileName = "osascript";
                p.StartInfo.Arguments = "-e 'tell application \\\"System Events\\\" to log out'";
            }
            else if (OperatingSystem.IsLinux())
            {
                p.StartInfo.FileName = "pkill"; // not the recommended method, but one that is guaranteed to work no matter what distro we're on
                p.StartInfo.Arguments = "-kill -u $USER";
            }
            else if (OperatingSystem.IsWindows())
            {
                p.StartInfo.FileName = "shutdown";
                p.StartInfo.Arguments = "/l";
            }
            p.StartInfo.UseShellExecute = true;
            p.Start();
        }
    }
}