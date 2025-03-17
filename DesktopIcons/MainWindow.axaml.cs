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
using Avalonia.Threading;
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
    private readonly JsonSerializerOptions _cmdSerializerOptions;
    private FileSystemWatcher watcher;

    private DesktopLayout? desktopLayout;
    public MainWindow()
    {
        _serializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            TypeInfoResolver = DesktopLayoutSourceGenerationContext.Default
        };
        _cmdSerializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            TypeInfoResolver = CommandSourceGenerationContext.Default
        };
        DataContext = new MainWindowModel();
        GetMasRoot();
        ATTESTATION_STATE = Verifile2();
        CheckVerifileTamper();
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
        new Thread(() =>
        {
            while (true)
            {
                Dispatcher.UIThread.Post(() => { this.ZIndex -= 1; });
                Thread.Sleep(500);
            }
        }).Start();
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
        if (e.FullPath.EndsWith(BackForwardSlash("\\DesktopIconsCommand.json")))
        {
            Console.WriteLine("Found command file!");
            Command? command = null;
            if (e.ChangeType is WatcherChangeTypes.Changed or WatcherChangeTypes.Created) // ignore if deleted
            {
                if (!File.Exists(e.FullPath)) { return; }
                command = JsonSerializer.Deserialize<Command>(File.ReadAllText(e.FullPath), _cmdSerializerOptions);
                Console.WriteLine("Serialize OK");

                if (command != null)
                {
                    Console.WriteLine("Recieve command: " + command.Type +
                                      (command.Arguments != "" ? " (args: " + command.Arguments + ")" : " (no args)"));
                    switch (command.Type)
                    {
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
                                        { Opacity = 0.5 };
                                }

                                foreach (var window in specialWindows)
                                {
                                    window.BgCol.Background = new SolidColorBrush(Color.Parse("#a0" +
                                            theme.R.ToString("X").PadLeft(2, '0') +
                                            theme.G.ToString("X").PadLeft(2, '0') +
                                            theme.B.ToString("X").PadLeft(2, '0')))
                                        { Opacity = 0.5 };
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
            mas_root + "/Markuse asjad/MarkuStation2", "winword", "https://start.me", mas_root + "/Markuse asjad/Pidu!", "excel", "explorer",
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
            Position = new PixelPoint((me.LocationX > 0) ? me.LocationX : (int)(0.25 * size), me.LocationY > 0 ? me.LocationY : Screens.Primary.Bounds.Height - (int)(size * 1.5) - (int)(size * 0.25)),
            Width = size,
            Height = size,
            icon = me.IconA,
            action = me.Executable,
            myparent = this,
            Pic =
            {
                Opacity = 0.25
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
        int offset_left = Screens.Primary.Bounds.Width / 2 -  (width + grid_padding / 4) * special_count / 2;
        int offset_top = Screens.Primary.Bounds.Height - height * 2;
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
                        { Opacity = 0.5 }
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
                    Opacity = 0.75
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

        int offset_left = Screens.Primary.Bounds.Width / 2 - grid_width / 2;
        int offset_top = Screens.Primary.Bounds.Height / 2 - grid_height / 2;
        
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
                tI.BgCol.Background = new SolidColorBrush(Color.Parse("#a0" + theme.R.ToString("X").PadLeft(2, '0') + theme.G.ToString("X").PadLeft(2, '0') + theme.B.ToString("X").PadLeft(2, '0'))) { Opacity = 0.5 };
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
            offset_left = Screens.Primary.Bounds.Width / 2 - grid_width / 2;
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
    
    /// <summary>
    /// Builds a script that displays all Java binaries and versions for your system and marks it executable (Unix-like systems)
    /// </summary>
    private void BuildJavaFinder()
    {
        if (!File.Exists(mas_root + "/find_java" + (OperatingSystem.IsWindows() ? ".bat" : ".sh")))
        {

            var builder = new StringBuilder();
            using var javaFinder = new StringWriter(builder)
            {
                NewLine = OperatingSystem.IsWindows() ? "\r\n" : "\n"
            };
            if (OperatingSystem.IsWindows())
            {
                javaFinder.WriteLine("@echo off");
                javaFinder.WriteLine("setlocal EnableDelayedExpansion");
                javaFinder.WriteLine("for /f \"delims=\" %%a in ('where java') do (");
                javaFinder.WriteLine("\tset \"javaPath=\"%%a\"\"");
                javaFinder.WriteLine("\tfor /f \"tokens=3\" %%V in ('%%javaPath%% -version 2^>^&1 ^| findstr /i \"version\"') do (");
                javaFinder.WriteLine("\t\tset \"version=%%V\"");
                javaFinder.WriteLine("\t\tset \"version=!version:\"=!\"");
                javaFinder.WriteLine("\t\techo !javaPath:\"=!:!version!");
                javaFinder.WriteLine("\t)");
                javaFinder.WriteLine(")");
                javaFinder.WriteLine("endlocal");
                javaFinder.WriteLine("exit/b");
            }
            else if (OperatingSystem.IsLinux())
            {
                javaFinder.WriteLine("#!/usr/bin/bash");
            }
            else if (OperatingSystem.IsMacOS())
            {
                javaFinder.WriteLine("#!/bin/bash");
            }
            if (!OperatingSystem.IsWindows())
            {
                javaFinder.WriteLine("OLDIFS=$IFS");
                javaFinder.WriteLine("IFS=:");
                javaFinder.WriteLine("for dir in $PATH; do");
                javaFinder.WriteLine("    if [[ -x \"$dir/java\" ]]; then  # Check if java exists and is executable");
                javaFinder.WriteLine("        javaPath=\"$dir/java\"");
                javaFinder.WriteLine("        version=$(\"$javaPath\" -version 2>&1 | awk -F '\"' '/version/ {print $2}')");
                javaFinder.WriteLine("        echo \"$javaPath:$version\"");
                javaFinder.WriteLine("    fi");
                javaFinder.WriteLine("done");
                javaFinder.WriteLine("IFS=$OLDIFS");
            }
            File.WriteAllText(mas_root + "/find_java" + (OperatingSystem.IsWindows() ? ".bat" : ".sh"), builder.ToString(), Encoding.ASCII);
            if (!OperatingSystem.IsWindows())
            {
                File.SetUnixFileMode(mas_root + "/find_java.sh", UnixFileMode.UserRead | UnixFileMode.UserExecute | UnixFileMode.UserWrite);
            }
        }
    }

    /// <summary>
    /// Finds the latest version of Java installed on your system, since if you install the Java SE version, Verifile may not work with it.
    /// </summary>
    /// <returns>Path to the latest Java binary found on your system</returns>
    private string FindJava()
    {
        CultureInfo culture = CultureInfo.CurrentCulture;
        string p = culture.NumberFormat.NumberDecimalSeparator;
        string latest_version = $"0{p}0";
        string latest_path = "";
        string interpreter = OperatingSystem.IsWindows() ? "cmd" : "bash";
        if (OperatingSystem.IsWindows())
        {
            mas_root = mas_root.Replace("/", "\\");
        }
        Process pr = new()
        {
            StartInfo = new ProcessStartInfo()
            {
                FileName = interpreter,
                Arguments = (OperatingSystem.IsWindows() ? "/c " : "") + "\"" + mas_root + (OperatingSystem.IsWindows() ? "\\" : "/") + "find_java." + (OperatingSystem.IsWindows() ? "bat" : "sh") + "\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
            }
        };
        pr.Start();
        while (!pr.StandardOutput.EndOfStream) {
            string[] path_version = (pr.StandardOutput.ReadLine() ?? ":").Replace(":\\", "_WINDRIVE\\").Split(':');
            string path = path_version[0].Replace("_WINDRIVE\\", ":\\");
            string version = path_version[1].Split('_')[0];
            version = version.Split('.')[0] + p + version.Split('.')[1];
            if (double.Parse(version, NumberStyles.Any) > double.Parse(latest_version, NumberStyles.Any))
            {
                latest_path = path;
                latest_version = version;
            }
        }
        return latest_path;
    }
    
    private string Verifile2()
    {
        BuildJavaFinder();
        Process p = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = FindJava(),
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
        if (ATTESTATION_STATE != "VERIFIED")
        {
            this.Close();
            return;
        }
        cd = desktopLayout.DesktopDir;
        NavigateDirectory(cd);

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
}