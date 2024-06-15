using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace Interaktiivne_töölaud;

public partial class archive : Window
{
    public archive()
    {
        InitializeComponent();
    }

    private void Return_Click(object? sender, RoutedEventArgs e)
    {
        Office ofc = new();
        ofc.Show();
        Close();
    }
}