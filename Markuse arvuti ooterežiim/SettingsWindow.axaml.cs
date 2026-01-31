using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;

namespace Markuse_arvuti_ooterežiim;

public partial class SettingsWindow : Window
{
    private ScreensaverOptions screensaverOptions = new();
    private string masRoot;
    public SettingsWindow()
    {
        masRoot = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/.mas";
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void Button_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        this.Close();
    }

    private void Window_Loaded_1(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (Program.settingsid != 0)
        {
            //ContentBox.Text += $"\n\nVäljakutse ID: {Program.settingsid}";
        }

        if (File.Exists(masRoot + "/ScreensaverConfig.json"))
        {
            screensaverOptions.Load(masRoot);
        }
        else
        {
            screensaverOptions = new ScreensaverOptions()
            {
                AnimationEasing = ScreensaverOptions.Easing.ExponentialEaseInOut,
                AnimationInterval = 3,
                BackgroundPath = masRoot + "/bg_common.png",
                CustomImage = false,
                DisplayBackground = false,
                ImagePath = "",
                ImageWidth = 128
            };
        }
        this.GetControl<ComboBox>("AnimationEasingBox").SelectedIndex = (int)screensaverOptions.AnimationEasing;
        this.GetControl<NumericUpDown>("AnimationIntervalBox").Value = (decimal?)screensaverOptions.AnimationInterval;
        this.GetControl<TextBox>("BackgroundLocation").Text = screensaverOptions.BackgroundPath;
        this.GetControl<CheckBox>("BackgroundCheck").IsChecked = screensaverOptions.DisplayBackground;
        this.GetControl<TextBox>("IconLocation").Text = screensaverOptions.ImagePath;
        this.GetControl<CheckBox>("ImageCheck").IsChecked = screensaverOptions.CustomImage;
        this.GetControl<NumericUpDown>("ImageWidthBox").Text = screensaverOptions.ImageWidth.ToString();
    }

    private void OKButton_OnClick(object? sender, RoutedEventArgs e)
    {
        screensaverOptions = new ScreensaverOptions
        {
            AnimationEasing = (ScreensaverOptions.Easing)this.GetControl<ComboBox>("AnimationEasingBox").SelectedIndex,
            AnimationInterval = (double)this.GetControl<NumericUpDown>("AnimationIntervalBox").Value!,
            BackgroundPath = this.GetControl<TextBox>("BackgroundLocation").Text ?? "",
            ImagePath = this.GetControl<TextBox>("IconLocation").Text ?? "",
            DisplayBackground = this.GetControl<CheckBox>("BackgroundCheck").IsChecked ?? false,
            CustomImage = this.GetControl<CheckBox>("ImageCheck").IsChecked ?? false,
            ImageWidth = int.Parse(this.GetControl<NumericUpDown>("ImageWidthBox").Text ?? "128")
        };
        screensaverOptions.Save(masRoot);
        this.Close();
    }

    private void ImageCheck_OnIsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        this.GetControl<Grid>("CustomIconGrid").IsEnabled = (bool)this.GetControl<CheckBox>("ImageCheck").IsChecked!;
    }

    private void BackgroundCheck_OnIsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        this.GetControl<Grid>("BackgroundLocationGrid").IsEnabled = (bool)this.GetControl<CheckBox>("BackgroundCheck").IsChecked!;
    }

    private async void FileBrowserShow(string currentLocation, TextBox output, string windowTitle = "Markuse arvuti ooterežiim")
    {
        
        var filename = currentLocation;
        
        // Get top level from the current control. Alternatively, you can use Window reference instead.
        var topLevel = TopLevel.GetTopLevel(this);

        // Start async operation to open the dialog.
        var file = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions()
        {
            Title = windowTitle,
            AllowMultiple = false,
            FileTypeFilter = [FilePickerFileTypes.ImageAll],
            SuggestedStartLocation = await StorageProvider.TryGetFolderFromPathAsync(Path.GetDirectoryName(filename) ?? "")
        });

        if (file.Count == 0) return;
        filename = file[0].Path.AbsolutePath;
        output.Text = filename;
    }

    private void BrowseBgButton_OnClick(object? sender, RoutedEventArgs e)
    {
        FileBrowserShow(this.GetControl<TextBox>("BackgroundLocation").Text ?? "", this.GetControl<TextBox>("BackgroundLocation"), "Vali taustapilt..");
    }

    private void BrowseIconButton_OnClick(object? sender, RoutedEventArgs e)
    {
        FileBrowserShow(this.GetControl<TextBox>("IconLocation").Text ?? "", this.GetControl<TextBox>("IconLocation"), "Vali ikoon..");
    }
}