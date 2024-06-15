using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Diagnostics.Metrics;
using System;

namespace Interaktiivne_töölaud;

public partial class Inside : Window
{
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

}