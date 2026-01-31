using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace Interaktiivne_töölaud;

public partial class Run : Window
{
    internal bool ok = false;

    public Run()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e)
    {
        this.Close();
    }

    private void OK_Click(object? sender, RoutedEventArgs e)
    {
        ok = true;
        this.Close();
    }

    private void TextBox_KeyDown(object? sender, Avalonia.Input.KeyEventArgs e)
    {
        if (e.Key == Avalonia.Input.Key.Enter)
        {
            ok = true;
            this.Close();
        }
    }

    private void Window_Loaded(object? sender, RoutedEventArgs e)
    {
        this.GetControl<TextBox>("ProgName").Focus();
    }
}