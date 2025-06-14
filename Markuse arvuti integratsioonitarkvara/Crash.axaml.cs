using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;

namespace Markuse_arvuti_integratsioonitarkvara;

public partial class Crash : Window
{
    public Crash()
    {
        InitializeComponent();
    }

    private void CloseButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Environment.Exit(0);
        this.Close();
    }

    private void ResetButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Program.Restart();
    }
}