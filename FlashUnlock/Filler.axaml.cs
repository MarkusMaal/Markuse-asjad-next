using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace FlashUnlock;

public partial class Filler : Window
{
    public Filler()
    {
        InitializeComponent();
    }
    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    private void Control_OnLoaded(object? sender, RoutedEventArgs e)
    {
        this.Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.None);
    }
}