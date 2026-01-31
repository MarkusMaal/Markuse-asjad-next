using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System;
using System.Diagnostics;

namespace Interaktiivne_töölaud;

public partial class Playground : Window
{
    public Playground()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void Return_Click(object? sender, RoutedEventArgs e)
    {
        MainWindow mw = new();
        mw.Show();
        Close();
    }

    private void StartProcess(object? sender, RoutedEventArgs e)
    {
        Button? label = ((Button?)sender);
        if ((label != null) && (label.Content != null))
        {
            Process p = new();
            switch (label.Content?.ToString())
            {
                case "MarkuStation 2":
                    p.StartInfo.FileName = $"{Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}/.mas/Markuse asjad/MarkuStation2";
                    break;
                case "Käivita pidu!":
                    p.StartInfo.FileName = $"{Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}/.mas/Markuse asjad/Pidu!";
                    break;
                default:
                    return;
            }
            if (OperatingSystem.IsWindows())
            {
                p.StartInfo.FileName += ".exe";
            }
            p.StartInfo.UseShellExecute = true;
            p.Start();
        }
    }
}