using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Interaktiivne_töölaud;

public partial class Playground : Window
{
    public Playground()
    {
        InitializeComponent();
    }

    private void Return_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        MainWindow mw = new();
        mw.Show();
        Close();
    }
}