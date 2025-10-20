using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
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
using Avalonia.Platform;
using Avalonia.Threading;
using Avalonia.VisualTree;
using MasCommon;


namespace DesktopIcons;

public partial class MainWindow : Window
{
    private Verifile vf = new();
    public string ATTESTATION_STATE = "BYPASS";
    public string mas_root = "C:\\mas";
    private string cd = "";
    private bool SomethingSelected;
    private Window?[] iconWindows = [];
    private TopIcon[] specialWindows = [];
    private TopIcon logoWindow;
    public Color theme;
    public bool locked = true;
    private int grid_items_x = 3;
    private int grid_items_y = 3;
    private int grid_padding = 25;
    private int icon_size = 200;
    private bool generate_hidden_children;
    private readonly JsonSerializerOptions _serializerOptions;
    private FileSystemWatcher watcher;

    private DesktopLayout? desktopLayout;
    public MainWindow()
    {
        _serializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            TypeInfoResolver = DesktopLayoutSourceGenerationContext.Default
        };
        DataContext = new MainWindowModel();
        GetMasRoot();
        ATTESTATION_STATE = vf.MakeAttestation();
        if (!Verifile.CheckVerifileTamper()) ATTESTATION_STATE = "CHECK_TAMPER";
        if (!Verifile.CheckFiles(Verifile.FileScope.DesktopIcons)) ATTESTATION_STATE = "MISSING_FILES";
        if ((ATTESTATION_STATE != "VERIFIED") && (ATTESTATION_STATE != "BYPASS"))
        {
            Console.Error.WriteLine("Verifile kontroll nurjus!");
            new VerifileFail().Show();
            InitializeComponent();
            return;
        }

        Console.WriteLine("INFO: Verifile korras");

        Restart(false);
        if (desktopLayout.AcceptCommands)
        {
            Console.WriteLine("Accepting commands");
            watcher = new FileSystemWatcher(mas_root);
            Rewatch();
        }
        InitializeComponent();
        if (OperatingSystem.IsWindows())
        {
            // prevents maximizing the window, because the dumb Windows tablet mode tries to do that no matter what
            var handle = Win32Interop.GetWindowHandle(this);
            Win32Interop.SetWindowLongPtr(handle, Win32Interop.GWL_STYLE, Win32Interop.GetWindowLongPtr(handle, Win32Interop.GWL_STYLE) & ~Win32Interop.WS_MAXIMIZEBOX);
        }
    }

    public void ZOrderFix()
    {
        foreach (var window in iconWindows)
        {
            window.IsVisible = !window.IsVisible;
            window.IsVisible = !window.IsVisible;
        }

        foreach (var window in specialWindows)
        {
            window.IsVisible = !window.IsVisible;
            window.IsVisible = !window.IsVisible;
        }
        logoWindow.IsVisible = !logoWindow.IsVisible;
        logoWindow.IsVisible = !logoWindow.IsVisible;
    }

    private void Restart(bool logo)
    {
        
        theme = LoadTheme()[0];
        this.Background = Brush.Parse("#a0" + theme.R.ToString("X").PadLeft(2, '0') + theme.G.ToString("X").PadLeft(2, '0') + theme.B.ToString("X").PadLeft(2, '0'));

        if (logo)
        {
            logoWindow.Close();
        }

        foreach (var window in iconWindows)
        {
            window.Hide();
            window.Close();
        }

        foreach (var window in specialWindows)
        {
            window.Hide();
            window.Close();
        }

        iconWindows = [];
        specialWindows = [];
        if (!File.Exists(mas_root + "/DesktopIcons.json"))
        {
            InitSettings();
        }
        ReloadSettings();
        GenerateChildren();

        GenerateSpecialChildren();
        GenerateMasLogo();
    }

    private void CommandRecieveEvent(object sender, FileSystemEventArgs e)
    {
        if (e.FullPath.EndsWith(BackForwardSlash("\\DesktopIconsCommand.json")) && vf.IsVerified())
        {
            Console.WriteLine("Found command file!");
            DesktopCommand? command = new DesktopCommand();
            if (e.ChangeType is WatcherChangeTypes.Changed or WatcherChangeTypes.Created) // ignore if deleted
            {
                if (!File.Exists(e.FullPath)) { return; }

                command.Load(mas_root);
                Console.WriteLine("Serialize OK");

                if (command != null)
                {
                    Console.WriteLine("Recieve command: " + command.Type +
                                      (command.Arguments != "" ? " (args: " + command.Arguments + ")" : " (no args)"));
                    switch (command.Type)
                    {
                        case "IsDesktopIconsVisible":
                            Dispatcher.UIThread.Post(() =>
                            {
                                Position = new PixelPoint(0, 0);
                                IsVisible = command.Arguments.Equals("true", StringComparison.CurrentCultureIgnoreCase);
                            });
                            break;
                        case "IsIconVisible":
                            desktopLayout.ShowIcons =
                                command.Arguments.Equals("true", StringComparison.CurrentCultureIgnoreCase);
                            SaveSettings();
                            Dispatcher.UIThread.Post(() =>
                            {
                                foreach (var window in iconWindows)
                                {
                                    window.IsVisible = desktopLayout.ShowIcons;
                                }

                                specialWindows[0].ReplaceIcon(desktopLayout.ShowIcons ? "EyeB" : "EyeA");
                            });
                            break;
                        case "IsActionVisible":
                            desktopLayout.ShowActions =
                                command.Arguments.Equals("true", StringComparison.CurrentCultureIgnoreCase);
                            SaveSettings();
                            Dispatcher.UIThread.Post(() =>
                            {
                                foreach (var window in specialWindows)
                                {
                                    window.IsVisible = desktopLayout.ShowActions;
                                }
                            });
                            break;
                        case "IsLogoVisible":
                            desktopLayout.ShowLogo =
                                command.Arguments.Equals("true", StringComparison.CurrentCultureIgnoreCase);
                            SaveSettings();
                            Dispatcher.UIThread.Post(() => { logoWindow.IsVisible = desktopLayout.ShowLogo; });
                            break;
                        case "Restart":
                            if (!File.Exists(mas_root + "/DesktopIcons.json"))
                            {
                                InitSettings();
                            }
                            ReloadSettings();
                            Dispatcher.UIThread.Post(ResetChildren);
                            //Dispatcher.UIThread.Post(() => { Restart(command.Arguments == "true"); });
                            break;
                        case "Reset":
                            Dispatcher.UIThread.Post(ResetChildren);
                            break;
                        case "Lock":
                            desktopLayout.LockIcons =
                                command.Arguments.Equals("true", StringComparison.CurrentCultureIgnoreCase);
                            locked = desktopLayout.LockIcons;
                            Dispatcher.UIThread.Post(() =>
                            {
                                specialWindows[1].ReplaceIcon(locked ? "LockB" : "LockA");
                            });
                            break;
                        case "ReloadTheme":
                            theme = LoadTheme()[0];
                            Dispatcher.UIThread.Post(() =>
                            {
                                this.Background = Brush.Parse("#a0" + theme.R.ToString("X").PadLeft(2, '0') +
                                                              theme.G.ToString("X").PadLeft(2, '0') +
                                                              theme.B.ToString("X").PadLeft(2, '0'));
                                foreach (var window in iconWindows)
                                {
                                    ((TopIcon)window).BgCol.Background = new SolidColorBrush(Color.Parse("#a0" +
                                            theme.R.ToString("X").PadLeft(2, '0') +
                                            theme.G.ToString("X").PadLeft(2, '0') +
                                            theme.B.ToString("X").PadLeft(2, '0')))
                                        { Opacity = 0.85 };
                                }

                                foreach (var window in specialWindows)
                                {
                                    window.BgCol.Background = new SolidColorBrush(Color.Parse("#a0" +
                                            theme.R.ToString("X").PadLeft(2, '0') +
                                            theme.G.ToString("X").PadLeft(2, '0') +
                                            theme.B.ToString("X").PadLeft(2, '0')))
                                        { Opacity = 0.85 };
                                }
                            });

                            break;
                        default:
                            Console.WriteLine(command.Type + " is not recognized as a valid command.");
                            break;
                    }

                }
            }

            if (File.Exists(e.FullPath))
            {
                File.Delete(e.FullPath);
            }
        }
        else
        {
            Console.WriteLine("File not found");
        }
    }

    public void UpdatePositions()
    {
        int i = 0;
        foreach (var window in iconWindows)
        {
            desktopLayout.Children[i].LocationX = window.Position.X;
            desktopLayout.Children[i].LocationY = window.Position.Y;
            i++;
        }
        SaveSettings();
    }

    private void Rewatch()
    {
        watcher.NotifyFilter = NotifyFilters.Attributes
                               | NotifyFilters.CreationTime
                               | NotifyFilters.DirectoryName
                               | NotifyFilters.FileName
                               | NotifyFilters.LastAccess
                               | NotifyFilters.LastWrite
                               | NotifyFilters.Security
                               | NotifyFilters.Size;
        watcher.Created += CommandRecieveEvent;
        watcher.Changed += CommandRecieveEvent;
        watcher.Error += new ErrorEventHandler(OnError);
        watcher.Filter = "DesktopIconsCommand.json";
        watcher.IncludeSubdirectories = true;
        watcher.EnableRaisingEvents = true;
    }
    private void OnError(object source, ErrorEventArgs e)
    {
        if (e.GetException().GetType() == typeof(InternalBufferOverflowException))
        {
            Console.WriteLine("Error: File System Watcher internal buffer overflow at " + DateTime.Now + "\r\n");
        }
        else
        {
            Console.WriteLine("Error: Watched directory not accessible at " + DateTime.Now + "\r\n");
        }
        NotAccessibleError(watcher ,e);
    }

    static void NotAccessibleError(FileSystemWatcher source, ErrorEventArgs e)
    {
        source.EnableRaisingEvents = false;
        int iMaxAttempts = 120;
        int iTimeOut = 30000;
        int i = 0;
        while (source.EnableRaisingEvents == false && i < iMaxAttempts)
        {
            i += 1;
            try
            {
                source.EnableRaisingEvents = true;
            }
            catch
            {
                source.EnableRaisingEvents = false;
                System.Threading.Thread.Sleep(iTimeOut);
            }
        }

    }
    private void ReloadSettings()
    {
        desktopLayout = JsonSerializer.Deserialize<DesktopLayout>(File.ReadAllText(mas_root + "/DesktopIcons.json"), _serializerOptions);
        grid_padding = desktopLayout.IconPadding;
        grid_items_x = desktopLayout.IconCountX;
        grid_items_y = desktopLayout.IconCountY;
        icon_size = desktopLayout.IconSize;
    }
    
    private void SaveSettings()
    {
        var jsonData = JsonSerializer.Serialize(desktopLayout, _serializerOptions);
        File.WriteAllText(mas_root + "/DesktopIcons.json", jsonData, encoding: System.Text.Encoding.UTF8);
    }

    private void InitSettings()
    {
        string[] icons = ["Games", "Word", "Www", "Player", "Excel", "Folder", "Maia", "PowerPoint", "Apps"];
        string[] actions =
        [
            "\"" + mas_root + "/Markuse asjad/MarkuStation2\"", "winword", "https://start.me", "\"" + mas_root + "/Markuse asjad/Pidu!\"", "excel", "explorer",
            "http://localhost:14414", "powerpnt", "special:apps"
        ];
        string[] special_icons = ["EyeB", "LockB", "Reset"];
        // ReSharper disable once StringLiteralTypo
        string[] special_actions = ["showhide", "lockunlock", "reset"];
        int width = icon_size / 3;
        int height = icon_size / 3;
        desktopLayout = new DesktopLayout
        {
            Children = new DesktopIcon[9],
            IconCountX = 3,
            IconCountY = 3,
            IconSize = icon_size,
            IconPadding = grid_padding,
            LockIcons = true,
            Logo = new SpecialIcon()
            {
                Executable = "special:mas",
                IconA = "Mas",
                IconB = "Mas",
                LocationX = -1,
                LocationY = -1,
            },
            ShowActions = true,
            ShowIcons = true,
            ShowLogo = true,
            SpecialIcons = new SpecialIcon[3],
            AcceptCommands = true,
            DesktopDir = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
        };
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
        } else if (OperatingSystem.IsMacOS())
        {
            actions[1] = "open -na ONLYOFFICE.app --args --new:word";
            actions[4] = "open -na ONLYOFFICE.app --args --new:cell";
            actions[5] = "open " + Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            actions[7] = "open -na ONLYOFFICE.app --args --new:slide";
        }
        for (int i = 0; i < 9; i++)
        {
            desktopLayout.Children[i] = new DesktopIcon()
            {
                Icon = icons[i],
                Executable = actions[i],
                LocationX = -1,
                LocationY = -1
            };
        }

        for (int i = 0; i < special_icons.Length; i++)
        {
            desktopLayout.SpecialIcons[i] = new SpecialIcon()
            {
                IconA = special_icons[i].Replace("B", "A"),
                IconB = special_icons[i],
                Executable = "special:" + special_actions[i],
                LocationX = -1,
                LocationY = -1
            };
        }
        SaveSettings();
    }

    private void GenerateMasLogo()
    {
        var size = icon_size / 2;
        var me = desktopLayout.Logo; 
        var primaryScreen = Screens.Primary ?? Screens.All[0]; 
        var aspect = (double)primaryScreen.Bounds.Width / (double)primaryScreen.Bounds.Height;
        var padx = (int)(0.25 * size) ;
        if (aspect > 3.0) // super ultra-wide
        {
            padx = primaryScreen.Bounds.Width / 4;
        }
        var tI = new TopIcon
        {
            BgCol =
            {
                Background = new SolidColorBrush(Color.Parse("#a0" + theme.R.ToString("X").PadLeft(2, '0') +
                                                             theme.G.ToString("X").PadLeft(2, '0') +
                                                             theme.B.ToString("X").PadLeft(2, '0')))
                    { Opacity = 0 }
            },
            WindowStartupLocation = WindowStartupLocation.Manual,
            Position = new PixelPoint((me.LocationX > 0) ? me.LocationX : padx, me.LocationY > 0 ? me.LocationY : primaryScreen.Bounds.Height - (int)(size * 1.5) - (int)(size * 0.25)),
            Width = size,
            Height = size,
            icon = me.IconA,
            action = me.Executable,
            myparent = this,
            Pic =
            {
                Opacity = 0.75
            }
        };
        logoWindow = tI;
        if (!desktopLayout.ShowLogo) return;
        logoWindow.Show();
    }
    
    private void GenerateSpecialChildren()
    {
        SpecialIcon[] special_icons = desktopLayout.SpecialIcons;
        const int special_count = 3;
        int width = icon_size / 3;
        int height = icon_size / 3;
        var primaryScreen = Screens.Primary ?? Screens.All[0]; 
        int offset_left = primaryScreen.Bounds.Width / 2 -  (width + grid_padding / 4) * special_count / 2;
        int offset_top = primaryScreen.Bounds.Height - height * 2;
        int i = 0;
        specialWindows = new TopIcon[special_count];
        foreach (SpecialIcon me in special_icons) {
            var tI = new TopIcon
            {
                BgCol =
                {
                    Background = new SolidColorBrush(Color.Parse("#a0" + theme.R.ToString("X").PadLeft(2, '0') +
                                                                 theme.G.ToString("X").PadLeft(2, '0') +
                                                                 theme.B.ToString("X").PadLeft(2, '0')))
                        { Opacity = 0.75 }
                },
                WindowStartupLocation = WindowStartupLocation.Manual,
                Position = new PixelPoint(me.LocationX > 0 ? me.LocationX : offset_left, me.LocationY > 0 ? me.LocationY : offset_top),
                Width = width,
                Height = height,
                icon = me.IconB,
                alt_icon = me.IconA,
                action = me.Executable,
                myparent = this,
                Pic =
                {
                    Opacity = 0.85
                }
            };
            specialWindows[i] = tI;
            if (desktopLayout.ShowActions)
            {
                specialWindows[i].Show();
            }
            offset_left += width + (grid_padding / 2);
            i++;
        }

    }

    // generate icons
    private void GenerateChildren()
    {
        iconWindows = new Window[grid_items_x * grid_items_y];
        
        int grid_width = (grid_items_x - 1) * grid_padding + icon_size * grid_items_x;
        int grid_height = (grid_items_y - 1) * grid_padding + icon_size * grid_items_y;

        var primaryScreen = Screens.Primary ?? Screens.All[0]; 
        
        int offset_left = primaryScreen.Bounds.Width / 2 - grid_width / 2;
        int offset_top = primaryScreen.Bounds.Height / 2 - grid_height / 2;
        
        DesktopIcon[] children = desktopLayout.Children;
        
        string[] icons = new string[children.Length];
        string[] actions = new string[children.Length];
        PixelPoint[] locations = new PixelPoint[children.Length];

        int i = 0;
        foreach (DesktopIcon child in children)
        {
            icons[i] = child.Icon;
            actions[i] = child.Executable;
            locations[i] = new PixelPoint(child.LocationX, child.LocationY);
            i++;
        }
        
        i = 0;
        for (int y = 0; y < grid_items_y; y++)
        {
            for (int x = 0; x < grid_items_x; x++)
            {
                if (i > locations.Length - 1)
                {
                    break;
                }
                TopIcon tI = new TopIcon();
                tI.BgCol.Background = new SolidColorBrush(Color.Parse("#a0" + theme.R.ToString("X").PadLeft(2, '0') + theme.G.ToString("X").PadLeft(2, '0') + theme.B.ToString("X").PadLeft(2, '0'))) { Opacity = 0.75 };
                tI.WindowStartupLocation = WindowStartupLocation.Manual;
                tI.Position = new PixelPoint(locations[i].X > 0 ? locations[i].X : offset_left, locations[i].Y > 0 ? locations[i].Y : offset_top);
                tI.Width = icon_size;
                tI.Height = icon_size;
                tI.icon = icons[i];
                tI.action = actions[i];
                tI.myparent = this;
                iconWindows[i] = tI;
                if (!generate_hidden_children && desktopLayout.ShowIcons)
                {
                    iconWindows[i].Show();
                }

                offset_left += icon_size + grid_padding;
                i++;
            }
            offset_left = primaryScreen.Bounds.Width / 2 - grid_width / 2;
            offset_top += icon_size + grid_padding;
        }
    }

    // show/hide icons
    public void ToggleChildren(TopIcon invoker, bool? visible = null)
    {
        if (visible != null && visible != iconWindows[iconWindows.Length - 1].IsVisible) {
            return;
        }
        bool isVisible = false;
        foreach (var w in iconWindows)
        {
            if (w is null)
            {
                continue;
            }
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
        invoker.ReplaceIcon(isVisible ? invoker.alt_icon : invoker.icon);
    }

    // toggles movement lock
    public void ToggleChildLock(TopIcon invoker)
    {
        locked = !locked;
        invoker.ReplaceIcon(locked ? invoker.alt_icon : invoker.icon);
    }

    // reset positions of icons
    public void ResetChildren()
    {
        generate_hidden_children = !iconWindows[0].IsVisible;
        foreach (var w in iconWindows)
        {
            try
            {
                ((TopIcon?)w).canClose = true;
                w.Close();
            }
            catch (NullReferenceException ex)
            {
                Console.WriteLine("ERROR: " + ex.Message);
            }
        }
        foreach (var di in desktopLayout.Children)
        {
            di.LocationX = -1;
            di.LocationY = -1;
        }

        SaveSettings();
        GenerateChildren();
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
        if (ATTESTATION_STATE != "VERIFIED")
        {
            this.Close();
            return;
        }
        cd = desktopLayout.DesktopDir;
        NavigateDirectory(cd);
        this.Glass.Points = [new Point(0,0), new Point(Width, 0), new Point(0, Height)];
        this.Glass.Fill = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(new Point(Width / 2, 0), RelativeUnit.Absolute),
            EndPoint = new RelativePoint(new Point(Width / 2, Height), RelativeUnit.Absolute),
            GradientStops = [new GradientStop(Color.FromArgb(60, 255, 255, 255), -0.3), new GradientStop(Color.FromArgb(5, 255, 255, 255), 0.4),
                new GradientStop(Color.FromArgb(0, 255, 255, 255), 0.5)]
        };

        if (!OperatingSystem.IsLinux())
        {
            //Console.WriteLine("Windows/Mac paranduste aktiveerimine...");
            this.ExtendClientAreaToDecorationsHint = false;
            this.SystemDecorations = SystemDecorations.None;
        }
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

    private void Window_Opened(object? sender, System.EventArgs e)
    {
        if (OperatingSystem.IsMacOS()) MacOSInterop.SetWindowProperties(this); // Specific for Mac users, uses native macOS APIs                        
        else if (OperatingSystem.IsWindows()) new Win32Interop().KeepWindowAtBottom(this); // Specific for Windows
    }
}
