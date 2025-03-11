using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;

namespace Markuse_arvuti_juhtpaneel;

public partial class DesktopIcon_Edit : Window
{
    public bool DialogResult { get; set; }
    
    public DesktopIcon_Edit()
    {
        InitializeComponent();
    }
    
    private async void BrowseButtonAsync(object? sender, RoutedEventArgs e)
    {
        // source: https://docs.avaloniaui.net/docs/basics/user-interface/file-dialogs
        // Get top level from the current control. Alternatively, you can use Window reference instead.
        var topLevel = TopLevel.GetTopLevel(this);

        // Start async operation to open the dialog.
        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Markuse arvuti juhtpaneel",
            AllowMultiple = false
        });

        if (files.Count >= 1)
        {
            this.LocationBox.Text = Uri.UnescapeDataString(files[0].Path.AbsolutePath);
        }
    }

    private void Cancel_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        DialogResult = false;
        this.Close();
    }

    private void Delete_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        NameBox.SelectedIndex = -1;
        LocationBox.Text = "";
        DialogResult = true;
        this.Close();
    }

    private void OK_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        DialogResult = true;
        this.Close();
    }
}