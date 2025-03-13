using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;

namespace Markuse_arvuti_integratsioonitarkvara;

public partial class VerifileFail : Window
{
    public VerifileFail()
    {
        InitializeComponent();
    }

    private void Button_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        this.Close();
    }

    private void Window_Loaded_1(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        string att = ((App)Application.Current).attestation;
        string rd = ((App)App.Current).mas_root;
        if (OperatingSystem.IsWindows())
        {
            rd = rd.Replace("/", "\\");
        }
        if (att != "CHECK_TAMPER")
        {
            InfoTextBlock.Text += "\n\nVeakood: VF_" + att;
        } else
        {
            InfoTextBlock.Text = "Püsivuskontroll ei ole usaldusväärne, sest Verifile 2.0 räsi ei ole sobiv.\nPalun uuendage Markuse arvuti integratsioonitarkvara ja/või Verifile 2.0 tarkvara kataloogis\n\"" + rd + "\". Täpsem info standardväljundis.";
        }
    }
}