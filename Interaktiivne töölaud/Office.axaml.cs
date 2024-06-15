using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Interaktiivne_töölaud;

public partial class Office : Window
{
    public Office()
    {
        InitializeComponent();
    }

    private void Return_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Inside ins = new();
        ins.Show();
        Close();
    }

    private void Archive_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        archive arc = new();
        arc.Show();
        Close();
    }
}