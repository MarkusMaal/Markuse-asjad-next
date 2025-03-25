using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Markuse_arvuti_ooterežiim;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
    }

    private void Button_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        this.Close();
    }

    private void Window_Loaded_1(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (Program.settingsid != 0)
        {
            ContentBox.Text += $"\n\nVäljakutse ID: {Program.settingsid}";
        }
    }
}