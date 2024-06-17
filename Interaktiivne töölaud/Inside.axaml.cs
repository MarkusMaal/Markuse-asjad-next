using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Diagnostics.Metrics;
using System;
using System.Diagnostics;
using System.IO;

namespace Interaktiivne_töölaud;

public partial class Inside : Window
{
    readonly string mas_root = $"{Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}/.mas";
    public Inside()
    {
        InitializeComponent();
    }

    private void ExitHouse_Click(object? sender, RoutedEventArgs e)
    {
        MainWindow mw = new();
        mw.Show();
        Close();
    }

    private void Attic_Click(object? sender, RoutedEventArgs e)
    {
        Windows win = new();
        win.Show();
        Close();
    }

    private void Office_Click(object? sender, RoutedEventArgs e)
    {
        Office ofc = new();
        ofc.Show();
        Close();
    }

    private void Files_Click(object? sender, RoutedEventArgs e)
    {
        Office ofc = new()
        {
            startDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
        };
        ofc.Show();
        Close();
    }

    private static void ProcessStart(string filename)
    {
        new Process()
        {
            StartInfo =
            {
                FileName = filename,
                UseShellExecute = true,
            }
        }.Start();
    }

    private void Media_Click(object? sender, RoutedEventArgs e)
    {
        if (OperatingSystem.IsWindows())
        {
            ProcessStart("wmplayer.exe");
        }
        else if (OperatingSystem.IsLinux())
        {
            ProcessStart("vlc");
        }
        else if (OperatingSystem.IsMacOS())
        {
            Process p = new()
            {
                StartInfo = new()
                {
                    FileName = "open",
                    Arguments = "-a QuickTime\\ Player",
                    UseShellExecute = true
                }
            };
            p.Start();
        }
    }

    private void Paint_Click(object? sender, RoutedEventArgs e)
    {
        if (OperatingSystem.IsWindows())
        {
            ProcessStart("mspaint");
        }
        else if (OperatingSystem.IsLinux())
        {
            ProcessStart("pinta");
        }
        else if (OperatingSystem.IsMacOS())
        {
            Process p = new()
            {
                StartInfo = new()
                {
                    FileName = "open",
                    Arguments = "-a Preview",
                    UseShellExecute = true
                }
            };
            p.Start();
        }
    }

    private void Logo_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        File.WriteAllText(mas_root + "/opencontext.log", "");
    }
}