using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Markuse_arvuti_integratsioonitarkvara;

public partial class InterfaceTest : Window
{
    public InterfaceTest()
    {
        InitializeComponent();
    }

    private void Button_OnClick(object? sender, RoutedEventArgs e)
    {
        var app = (App?)Application.Current;
        switch ((sender as Button)?.Content)
        {
            case "Tegumiriba menüü":
                app.GetTrayIcon().IsVisible = true;
                break;
            case "Käivitusaken":
                new MainWindow().Show();
                break;
            case "Juurutamise vorm":
                new RerootForm().Show();
                break;
            case "M.A.I.A. kood":
                new ShowCode().Show();
                break;
            case "Verifile kontrolli nurjumine":
                new VerifileFail().Show();
                break;
            case "Paigalduse lõpetamise aken":
                new WaitInstall().Show();
                break;
            case "Silu rakendus":
                if (app.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime appLifetime)
                {
                    appLifetime.MainWindow = new MainWindow();
                    appLifetime.MainWindow.Show();
                }
                this.Close();
                break;
        }
    }

    private void CheckBox_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is CheckBox checkBox)
        {
            var app = (App?)Application.Current;
            if (app == null) return;
            app.dev = checkBox.IsChecked!.Value;
        }
    }
}