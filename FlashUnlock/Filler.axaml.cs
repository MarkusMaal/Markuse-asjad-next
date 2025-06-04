using Avalonia.Controls;
using Avalonia.Interactivity;

namespace FlashUnlock;

public partial class Filler : Window
{
    public Filler()
    {
        InitializeComponent();
    }

    private void Control_OnLoaded(object? sender, RoutedEventArgs e)
    {
        this.Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.None);
    }
}