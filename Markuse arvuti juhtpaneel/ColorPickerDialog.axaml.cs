using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace Markuse_arvuti_juhtpaneel;

public partial class ColorPickerDialog : Window
{
    public bool result = false;
    public ColorPickerDialog()
    {
        InitializeComponent();
    }
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void Confirm(object sender, RoutedEventArgs e)
    {
        result = true;
        this.Close();
    }
}