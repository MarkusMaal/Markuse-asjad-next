using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.VisualTree;


namespace DesktopIcons;

public partial class MainWindow : Window
{
    private static readonly string[] VERIFILE_FLAGS = ["VERIFIED", "LEGACY", "TAMPERED", "FAILED", "FOREIGN", "BYPASS", "MISSING", "CHECK_TAMPER"]; // never ever modify this during runtime
    private static readonly string[] WHITELISTED_HASHES = [
        "B881FBAB5E73D3984F2914FAEA743334D1B94DFFE98E8E1C4C8C412088D2C9C2",
        "A0B93B23301FC596789F83249A99F507A9DA5CBA9D636E4D4F88676F530224CB",
        "B08AABB1ED294D8292FDCB2626D4B77C0A53CB4754F3234D8E761E413289057F",
        "8076CF7C156D44472420C1225B9F6ADB661E3B095E29E52E3D4E8598BB399A8F"];
    string ATTESTATION_STATE = "BYPASS";
    private string mas_root = "C:\\mas";
    private string cd = "";
    private bool SomethingSelected = false;
    private Window[] iconWindows = [];
    private Color theme;
    public bool locked = true;
    private const int grid_items_x = 3;
    private const int grid_items_y = 3;
    private const int grid_padding = 25;
    private const int icon_size = 200;
    public MainWindow()
    {
        DataContext = new MainWindowModel();
        GetMasRoot();
        ATTESTATION_STATE = Verifile2();
        CheckVerifileTamper();
        if (ATTESTATION_STATE != "VERIFIED")
        {
            Console.Error.WriteLine("Verifile kontroll nurjus!");
            Environment.Exit(Array.IndexOf(VERIFILE_FLAGS, ATTESTATION_STATE));
            return;
        }
        else
        {
            Console.WriteLine("INFO: Verifile korras");
        }

        theme = LoadTheme()[0];
        this.Background = Brush.Parse("#a0" + theme.R.ToString("X").PadLeft(2, '0') + theme.G.ToString("X").PadLeft(2, '0') + theme.B.ToString("X").PadLeft(2, '0'));

        GenerateChildren();
        GenerateSpecialChildren();
        InitializeComponent();
    }

    private void GenerateSpecialChildren()
    {
        const int special_count = 3;
        string[] special_icons = ["EyeB", "LockB", "Reset"];
        // ReSharper disable once StringLiteralTypo
        string[] special_actions = ["showhide", "lockunlock", "reset"];
        int width = icon_size / 3;
        int height = icon_size / 3;
        int offset_left = Screens.Primary.Bounds.Width / 2 -  (width + grid_padding / 4) * special_count / 2;
        int offset_top = Screens.Primary.Bounds.Height - height * 2;
        for (int i = 0; i < special_count; i++)
        {
            TopIcon tI = new TopIcon();
            tI.BgCol.Background = new SolidColorBrush(Color.Parse("#a0" + theme.R.ToString("X").PadLeft(2, '0') +
                                                                  theme.G.ToString("X").PadLeft(2, '0') +
                                                                  theme.B.ToString("X").PadLeft(2, '0')))
                { Opacity = 0.5 };
            tI.WindowStartupLocation = WindowStartupLocation.Manual;
            tI.Position = new PixelPoint(offset_left, offset_top);
            tI.Width = width;
            tI.Height = height;
            tI.icon = special_icons[i];
            tI.action = "special:" + special_actions[i];
            tI.myparent = this;
            tI.Pic.Opacity = 0.75;
            tI.Show();
            offset_left += width + (grid_padding / 2);
        }

    }

    // generate icons
    private void GenerateChildren()
    {
        iconWindows = new Window[grid_items_x * grid_items_y];
        
        int grid_width = (grid_items_x - 1) * grid_padding + icon_size * grid_items_x;
        int grid_height = (grid_items_y - 1) * grid_padding + icon_size * grid_items_y;

        int offset_left = Screens.Primary.Bounds.Width / 2 - grid_width / 2;
        int offset_top = Screens.Primary.Bounds.Height / 2 - grid_height / 2;

        string[] icons = ["Games", "Word", "Www", "Player", "Excel", "Folder", "Maia", "PowerPoint", "Apps"];
        string[] actions =
        [
            mas_root + "/Markuse asjad/MarkuStation2", "winword", "https://start.me", mas_root + "/Markuse asjad/Pidu!", "excel", "explorer",
            "http://localhost:14414", "powerpnt", "special:apps"
        ];
        if (OperatingSystem.IsWindows())
        {
            actions[0] += ".exe";
            actions[3] += ".exe";
        } else if (OperatingSystem.IsLinux())
        {
            actions[1] = "onlyoffice-desktopeditors --new=doc";
            actions[4] = "onlyoffice-desktopeditors --new=cell";
            actions[5] = "dolphin";
            actions[7] = "onlyoffice-desktopeditors --new=slide";
        }
        int i = 0;
        for (int y = 0; y < grid_items_y; y++)
        {
            for (int x = 0; x < grid_items_x; x++)
            {
                
                TopIcon tI = new TopIcon();
                tI.BgCol.Background = new SolidColorBrush(Color.Parse("#a0" + theme.R.ToString("X").PadLeft(2, '0') + theme.G.ToString("X").PadLeft(2, '0') + theme.B.ToString("X").PadLeft(2, '0'))) { Opacity = 0.5 };
                tI.WindowStartupLocation = WindowStartupLocation.Manual;
                tI.Position = new PixelPoint(offset_left, offset_top);
                tI.Width = icon_size;
                tI.Height = icon_size;
                tI.icon = icons[i];
                tI.action = actions[i];
                tI.myparent = this;
                iconWindows[i] = tI;
                iconWindows[i].Show();
                offset_left += icon_size + grid_padding;
                i++;
            }
            offset_left = Screens.Primary.Bounds.Width / 2 - grid_width / 2;
            offset_top += icon_size + grid_padding;
        }
    }

    // show/hide icons
    public void ToggleChildren(TopIcon invoker)
    {
        bool isVisible = false;
        foreach (var w in iconWindows)
        {
            if (w.IsVisible)
            {
                isVisible = true;
                w.Hide();
            }
            else
            {
                w.Show();
                w.ShowInTaskbar = true;
                w.ShowInTaskbar = false;
            }
        }
        invoker.ReplaceIcon(isVisible ? "EyeA" : "EyeB");
    }

    // toggles movement lock
    public void ToggleChildLock(TopIcon invoker)
    {
        locked = !locked;
        invoker.ReplaceIcon(locked ? "LockB" : "LockA");
    }

    // reset positions of icons
    public void ResetChildren()
    {
        foreach (var w in iconWindows)
        {
            ((TopIcon)w).canClose = true;
            w.Close();
        }
        GenerateChildren();
    }
    
    private void CheckVerifileTamper()
    {
        if (!File.Exists(mas_root + BackForwardSlash(@"\verifile2.jar")))
        {
            ATTESTATION_STATE = "MISSING";
        }
        var hash = "";
        using (var sha256 = SHA256.Create())
        {
            using (var stream = File.OpenRead(mas_root + BackForwardSlash(@"\verifile2.jar")))
            {
                hash = BitConverter.ToString(sha256.ComputeHash(stream));
            }
        }
        if (!WHITELISTED_HASHES.Contains(hash.Replace("-", "")))
        {
            ATTESTATION_STATE = "CHECK_TAMPER";
        }
    }
    
    private void GetMasRoot()
    {
        if (File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) +
                                   BackForwardSlash(@"\.mas\edition.txt")))
        {
            mas_root = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + BackForwardSlash(@"\.mas"); // in home directory
        } else if (File.Exists(BackForwardSlash(Environment.GetEnvironmentVariable("HOMEDRIVE") + @"\mas\edition.txt")))
        {
            mas_root = BackForwardSlash(Environment.GetEnvironmentVariable("HOMEDRIVE") + @"\mas"); // at drive root
        }
        else if (!File.Exists(BackForwardSlash(mas_root + @"\edition.txt")))
        {
            Console.WriteLine("Arvuti ei vasta Markuse arvuti asjad nõuetele");
            Close();
        }
        else
        {
            mas_root = BackForwardSlash(mas_root);
        }
    }

    // change backslashes to forward slashes in case we're not in Windows
    private string BackForwardSlash(string path)
    {
        if (!OperatingSystem.IsWindows())
        {
            path = path.Replace("\\", "/");
        }
        return path;
    }
    
    
    
    private string Verifile2()
    {
        Process p = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "java",
                Arguments = "-jar " + mas_root + BackForwardSlash(@"\verifile2.jar"),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
            }
        };
        p.Start();
        while (!p.StandardOutput.EndOfStream)
        {
            string line = p.StandardOutput.ReadLine() ?? "";
            return line.Split('\n')[0];
        }
        return "FAILED";
    }

    public void NavigateDirectory(string path, bool opendir = false, string selfile = null)
    {
        SomethingSelected = false;
        SomethingSel.Content = DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss");
        if (opendir && Directory.Exists(path))
        {
            Process p = new();
            p.StartInfo.UseShellExecute = true;
            p.StartInfo.FileName = path;
            p.Start();
            return;
        }
        MainWindowModel mwm = new();
        mwm.Navigate(path, selfile);
        this.DataContext = mwm;
    }

    private void Control_OnLoaded(object? sender, RoutedEventArgs e)
    {
        cd = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        NavigateDirectory(cd);

        this.Hide();
    }
    

    private void InputElement_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        SomethingSelected = !SomethingSelected;
        if (((Grid?)sender).Background?.ToString().ToLower() == "#ff0000ff") return;
        var selFolder = (((StackPanel)(((Grid)((Grid?)sender).Children.First()).Children.First())).Children.Skip(1).First() as TextBlock)
            .Text;
        var mwm = this.DataContext as MainWindowModel;
        if (selFolder != null) mwm.Resel(selFolder);
        this.DataContext = mwm;
    }
    
    
    private void InputElement_OnDoubleTapped(object? sender, TappedEventArgs? e)
    {
        string? selFolder = (((StackPanel)(((Grid)((Grid?)sender).Children.First()).Children.First())).Children.Skip(1).First() as TextBlock)
            .Text;
        MainWindowModel? ctx = ((MainWindowModel?)DataContext);
        if ((ctx == null) || (selFolder == null))
        {
            return;
        }
        string currentDir = ctx.url;
        NavigateDirectory(currentDir + "/" + selFolder, true);
    }

    
    Color[] LoadTheme()
    {
        string[] bgfg = File.ReadAllText(mas_root + "/scheme.cfg").Split(';');
        string[] bgs = bgfg[0].ToString().Split(':');
        string[] fgs = bgfg[1].ToString().Split(':');
        Color[] cols = { Color.FromArgb(255, byte.Parse(bgs[0]), byte.Parse(bgs[1]), byte.Parse(bgs[2])), Color.FromArgb(255, byte.Parse(fgs[0]), byte.Parse(fgs[1]), byte.Parse(fgs[2])) };
        return cols;
    }
    
    private void Form_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
    }

    private void Window_OnClosing(object? sender, WindowClosingEventArgs e)
    {
        bool cancel = e.CloseReason is not (WindowCloseReason.ApplicationShutdown or WindowCloseReason.OSShutdown);
        if (cancel)
        {
            this.Hide();
        }
        e.Cancel = cancel;
    }
}