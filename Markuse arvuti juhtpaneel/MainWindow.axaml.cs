using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using Avalonia.Threading;
using MsBox.Avalonia;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls.Presenters;
using Avalonia.Layout;
using MasCommon;
using MsBox.Avalonia.Enums;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.VisualTree;

namespace Markuse_arvuti_juhtpaneel
{
    public partial class MainWindow : Window
    {
        Verifile vf = new();
        string masRoot = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/.mas";
        Color[] scheme;
        int rotation = 0;
        string[] locations;
        bool freezeTimer = false;
        private bool preventWrites = true;
        readonly string whatNew = "+ Markuse arvuti asjad kasutavad nüüd MasCommon teeki\n+ Lisanduvad Verifile kontrollid, et vältida ootamatuid kokkujooksmisi";
        private readonly JsonSerializerOptions _serializerOptions = new() { WriteIndented = true, TypeInfoResolver = DesktopLayoutSourceGenerationContext.Default};
        private readonly JsonSerializerOptions _cmdSerializerOptions = new() { WriteIndented = true, TypeInfoResolver = MasConfigSourceGenerationContext.Default };
        private List<string> desktopIcons = [];
        DesktopLayout? desktopLayout;
        CommonConfig config = new();
        private bool LaunchError = false;
        private double angle = 0.0;
        private int eggLevel = 0;
        private List<Key> pressedKeys = [];
        public MainWindow()
        {
            InitializeComponent();
            InitResources();
            InitButtons();
            GetTopLevel(this).KeyDown += InputElement_OnKeyDown;
            GetTopLevel(this).KeyUp += OnKeyUp;
        }

        private void OnKeyUp(object? sender, KeyEventArgs e)
        {
            if (e.KeyModifiers.HasFlag(KeyModifiers.Alt) && e.Key == Key.H)
            {
                _ = MessageBoxShow(
                    "Alt - Kuva klaviatuuri otseteed\n" +
                    (OperatingSystem.IsMacOS() ? "Cmd + Q" :"Alt + F4") + " - Sulge rakendus\n" +
                    "Alt + H - Kuva kõik kiirklahvid\n" +
                    "Alt + A - Navigeeri avalehele\n" +
                    "Alt + M - Navigeeri MarkuStationi vahekaardile\n" +
                    "Alt + K - Ava konfiguratsiooni vahekaart\n" +
                    "Alt + D - Ava töölaua vahekaart\n" +
                    "Alt + T - Ava teabe vahekaart\n" +
                    "CTRL + TAB - Järgmine vahekaart\n" +
                    "CTRL + SHIFT + TAB - Eelmine vahekaart\n", "Kiirklahvid", ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Question
                    );
            }
            if (!e.KeyModifiers.HasFlag(KeyModifiers.Control) || (e.Key != Key.Tab)) return;
            if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
            {
                if (TabsControl.SelectedIndex != 0)
                {
                    TabsControl.SelectedIndex--;
                }
                else
                {
                    TabsControl.SelectedIndex = TabsControl.Items.Count - 1;
                }
            }
            else
            {
                if (TabsControl.SelectedIndex != TabsControl.Items.Count - 1)
                {
                    TabsControl.SelectedIndex += 1;
                }
                else
                {
                    TabsControl.SelectedIndex = 0;
                }
            }
        }

        /* Avaleht */
        private void StartTaskMgr(object sender, RoutedEventArgs e)
        {
            if (OperatingSystem.IsWindows())
            {
                StartWin32Process("taskmgr.exe");
            }
            else if (OperatingSystem.IsLinux())
            {
                Process.Start("konsole", "-e htop");
            } else if (OperatingSystem.IsMacOS()) {
                Process.Start("open", "\"/System/Applications/Utilities/Activity Monitor.app\"");
            }
        }

        private void StartCmd(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (OperatingSystem.IsWindows())
            {
                StartWin32Process("cmd");
            } else if (OperatingSystem.IsLinux())
            {
                Process.Start("konsole");
            } else if (OperatingSystem.IsMacOS()) {
                Process.Start("open", "/System/Applications/Utilities/Terminal.app");

            }
        }

        private void StartDevmgmt(object sender, RoutedEventArgs e)
        {
            if (OperatingSystem.IsWindows())
            {
                StartWin32Process("devmgmt.msc");
            }
            else if (OperatingSystem.IsLinux())
            {
                Process.Start("hardinfo2");
            } else if (OperatingSystem.IsMacOS())
            {
                Process.Start("open", "\"/System/Applications/Utilities/System Information.app\"");
            }
        }

        private void StartRegedit(object sender, RoutedEventArgs e)
        {
            StartWin32Process("regedit");
        }

        private void StartCompmgmt(object sender, RoutedEventArgs e)
        {
            if (OperatingSystem.IsWindows())
            {
                StartWin32Process("compmgmt.msc");
            } else if (OperatingSystem.IsLinux())
            {
                Process.Start("konsole", "-e ~/scripts/Tools.sh");
            }
        }


        private void FindFlash(object? sender, RoutedEventArgs e)
        {
            string[] drives = Directory.GetLogicalDrives();
            string drv = "bad";
            foreach (string str in drives)
            {
                if (File.Exists(str + "/E_INFO/edition.txt"))
                {
                    drv = str; break;
                }
            }
            if (drv != "bad")
            {
                try
                {
                    File.Copy(drv + "/Markuse mälupulk/Markuse mälupulk/bin/Debug/Markuse mälupulk.exe", masRoot + "/Mälupulk.exe", true);
                } catch
                {
                    MessageBoxShow("Ei saanud kopeerida ajakohast versiooni Markuse mälupulgalt.\nOlge kindlad, et sisestatud seade oleks kindlasti Markuse mälupulk.\nProgramm käivitub viimase salvestatud mälupulga tarkvara versiooniga...", "Viga", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error);
                }
            } else
            {
                MessageBoxShow("Ei saanud kopeerida ajakohast versiooni Markuse mälupulgalt", "Teade", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Warning);
            }
            if (File.Exists(masRoot + "/Mälupulk.exe"))
            {
                StartWin32Process(masRoot + "/Mälupulk.exe");
            } else
            {
                MessageBoxShow("Programmi käivitumine ebaõnnestus.\nPõhjus: Faili ei leitud", "Tõrge", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error);
            }
        }

        private void QuickNotes(object? sender, RoutedEventArgs e)
        {
            if (File.Exists(masRoot + "/sharpNotepad.exe"))
            {
                StartWin32Process(masRoot + "/sharpNotepad.exe");
            }
        }

        private void RestartMas(object? sender, RoutedEventArgs e)
        {
            if (OperatingSystem.IsWindows())
            {
                Process p = new();
                p.StartInfo.FileName = masRoot + "/remas.bat"; // here be dragons
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                p.StartInfo.UseShellExecute = true;
                p.Start();
            } else if (OperatingSystem.IsLinux()) {
                StopIcons();
               // stop existing Python processes
                Console.WriteLine("kill Python");
                RunCommand("pkill", "python");
                RunCommand("pkill", "python3");
                foreach (var p in Process.GetProcessesByName("Markuse arvuti integratsioonitarkvara"))
                {
                    p.Kill();
                }
                // Restart integration software and maia server
                Console.WriteLine("start maia server");
                RunCommand("bash", masRoot + "/maia_autostart.sh");
                Console.WriteLine("start integration software");
                StartIntegrationSoftware();
                RunCommand("bash", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/markuseasjad/maswelcome.sh", false);
                // Restart KDE Plasma
                Console.WriteLine("restart plasma");
                RunCommand("bash", masRoot + "/restart_plasma.sh");
                ShowIcons();
            } else if (OperatingSystem.IsMacOS()) {
                StopIcons();
                string packageName = "Markuse arvuti integratsioonitarkvara"; // TODO: some way of using dynamic naming in case it's ever renamed in the future ;)
                RunCommand("killall", "\"" + packageName + "\"");
                string appPath = "/Applications/" + packageName + ".app";
                if (!Directory.Exists(appPath)) {
                    // fallback in case integration app is not installed to /Applications
                    appPath = masRoot + "/Markuse asjad/" + packageName;
                    RunCommand("open", "\"" + appPath + "\"");
                } else {
                    RunCommand("open", "-a \"" + packageName + "\"");
                }
                // maia and native shell restart currently unsupported :(
                ShowIcons();
            }
            else
            {
                // Unsupported OS, display an error message
                NotWindows();
            }
        }

        private bool IsAndroid()
        {
            return File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "/Android.lnk")
                || File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + "/sta/sta.exe");
        }
        
        private void InfoText(object? sender, PointerEventArgs e)
        {
            string devPrefix = "Markuse arvuti asjade";
            if (Directory.Exists(masRoot + "/.masv"))
            {
                devPrefix = "Markuse virtuaalarvuti asjade";
            }
            else if (IsAndroid()) // check for Android shortcut on the desktop to determine that it's actually a tablet
            {
                devPrefix = "Markuse tahvelarvuti asjade";
            }
            Dictionary<string, string> hints = new Dictionary<string, string>();
            hints["Loo kuvatõmmis ja salvesta nim."] = "Teeb praegusest ekraanist kuvatõmmise ja kuvab salvestamise dialoogi, kus saate valida kausta kuhu salvestada, faili nime ja formaadi.";
            hints["Loo kuvatõmmis ja salvesta"] = "Teeb praegusest ekraanist kuvatõmmise ja salvestab selle piltide teeki.";
            hints["Markuse mälupulk"] = "Käivitab Markuse mälupulga juhtpaneeli (mälupulk).";
            hints["Kiirmärkmik"] = "Käivitab lihtsa tekstitöötluse programmi (sharpNotepad.exe).";
            if (OperatingSystem.IsWindows()) {
                hints["Taaskäivita Markuse asjad"] = "Taaskäivitab " + devPrefix + " integratsiooniprogrammi ja Windows Explorer'i.";
            } else if (OperatingSystem.IsLinux()) {
                hints["Taaskäivita Markuse asjad"] = "Taaskäivitab " + devPrefix + " integratsiooniprogrammi ja KDE Plasma töölaua.";
            } else {
                hints["Taaskäivita Markuse asjad"] = "Taaskäivitab " + devPrefix + " integratsiooniprogrammi.";
            }
            hints["Tegumihaldur"] = "Käivitab Windowsi tegumihalduri, millega saate vaadata avatud tegumeid, neid sulgeda, muuta nende prioriteeti ja resursside kasutust, samuti saate vaadata aktiivseid teenuseid (taustaprogramme) (taskmgr, valikuline administraator).";
            hints["Tegevuse monitor"] = "Võimaldab peatada mittereageerivaid rakendusi ja jälgida riistvara koormust.";
            hints["Käsuviip"] = "Käivitab tarviku käsuviip (cmd, administraator).";
            hints["Seadmehaldur"] = "Käivitab seadmehalduri, millega saate vaadata seadmesse paigaldatud riistvara ning installida/uuendada draivereid (devmgmt.msc, valikuline administraator).";
            hints["Start menüü"] = "Avab Windowsi start menüü (Ctrl + Esc).";
            hints["Registrite redigeerija"] = "Käivitab registrite redigeerija (regedit, administraator).";
            hints["Arvuti haldamine"] = "Käivitab seadme haldamise utilliidid (compmgmt.msc, administraator).";
            hints["Konsool"] = "Käivitab käsurea konsooli (konsole).";
            hints["Protsessid"] = "Käivitab protsesside ülevaate (htop).";
            hints["Riistvara info"] = "Kuvab info riistvara kohta (hardinfo2).";
            hints["Käsurea utilliidid"] = "Käivitab käsurea põhise juhtpaneeli (" + Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/scripts/Tools.sh).";
            hints["Terminal"] = "Käivitab terminali (terminal)";
            hints["Ooterežiimi seaded"] = "Avab akna, kus saate kohandada ooterežiimi seadeid";
            hints["Laadi andmed uuesti"] = "Laadib seadistused uuesti, juhul, kui need peaksid olema muutunud";
            foreach (var um in GetUniverse())
            {
                hints[um] = "Markuse asjad universum!";
            }
            this.InfoTextBlock.Text = hints[(string?)((Button)e.Source).Content];
        }

        private void DefaultInfo(object? sender, PointerEventArgs e)
        {
            this.InfoTextBlock.Text = "Siin kuvatakse teave, kui liigutate kursori teatud nupu peale.";
        }

        private void ScreenshotNow(object? sender, RoutedEventArgs e) {
            var year = DateTime.Now.Year.ToString();
            var month = DateTime.Now.Month.ToString();
            var day = DateTime.Now.Day.ToString();
            var currentDate = year + month.PadLeft(2, '0') + day.PadLeft(2, '0');
            var currentTime = DateTime.Now.ToLongTimeString().Replace(":", "");
            var filename = "Screenshot_" + currentDate + "_" + currentTime + ".png";
            if (OperatingSystem.IsLinux())
            {
                // Must have spectacle installed on KDE Plasma, other DEs and WMs not supported
                RunCommand("spectacle", "-f -b -o \"" + Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/Pildid/Screenshots/" + filename + "\"", false);
            } else if (OperatingSystem.IsWindows())
            {
                filename = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures) + "/" + filename;
                var p = new Process();
                p.StartInfo.FileName = "powershell";
                p.StartInfo.Arguments = masRoot.Replace("/", "\\") + "\\ScreenShot.ps1 -FileName \"" + filename.Replace("/", "\\") + "\"";
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                p.Start();
                p.WaitForExit();
                MessageBoxShow("Kuvatõmmis salvestati edukalt", "Markuse arvuti juhtpaneel", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Success);
            } else if (OperatingSystem.IsMacOS()) {
                // 1-second delay, because we want to wait for the button to exit the "pressed" state
                RunCommand("screencapture", "-T 1 \"" + Environment.GetFolderPath(Environment.SpecialFolder.MyPictures) + "/" + filename + "\"", false);
            }
        }

        private async void ScreenshotAs(object? sender, RoutedEventArgs e) {
            var year = DateTime.Now.Year.ToString();
            var month = DateTime.Now.Month.ToString();
            var day = DateTime.Now.Day.ToString();
            var currentDate = year + month.PadLeft(2, '0') + day.PadLeft(2, '0');
            var currentTime = DateTime.Now.ToLongTimeString().Replace(":", "");
            var filename = "Screenshot_" + currentDate + "_" + currentTime + ".png";
        
            // Get top level from the current control. Alternatively, you can use Window reference instead.
            var topLevel = TopLevel.GetTopLevel(this);

            // Start async operation to open the dialog.
            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Salvesta kuvatõmmis nimega",
                SuggestedFileName = filename,
            });

            if (file is null) return;
            filename = file.Path.AbsolutePath;
            Thread.Sleep(2000);
            if (OperatingSystem.IsWindows())
            {
                var p = new Process();
                p.StartInfo.FileName = "powershell";
                p.StartInfo.Arguments = masRoot.Replace("/", "\\") + "\\ScreenShot.ps1 -FileName \"" + filename.Replace("/", "\\") + "\"";
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                p.Start();
                await p.WaitForExitAsync();
                _ = MessageBoxShow("Kuvatõmmis salvestati edukalt", "Markuse arvuti juhtpaneel", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Success);
            }
            else if (OperatingSystem.IsLinux())
            {
                RunCommand("spectacle", "-f -b -o \"" + filename + "\"", false);
            } else if (OperatingSystem.IsMacOS()) {
                // 1-second delay to wait for the save as dialog to close
                RunCommand("screencapture", "-T 1 \"" + filename + "\"", false);
            }

        }


        /* Misc functions */

        // Reimplementation of WinForms MessageBox.Show
        private Task MessageBoxShow(string message, string caption = "Markuse arvuti juhtpaneel", MsBox.Avalonia.Enums.ButtonEnum buttons = MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon icon = MsBox.Avalonia.Enums.Icon.None)
        {
            var box = MessageBoxManager.GetMessageBoxStandard(caption, message, buttons, icon, WindowStartupLocation.CenterOwner);
            var result = box.ShowWindowDialogAsync(this);
            return result;
        }

        private void NotWindows()
        {
            MessageBoxShow("Seda funktsiooni selles operatsioonsüsteemis veel ei toetata", "Markuse arvuti juhtpaneel", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error);
        }

        
        
        private void RunCommand(string command, string args, bool waitForExit = true) {
            var p = new Process();
            p.StartInfo.FileName = command;
            p.StartInfo.Arguments = args;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            p.Start();
            if (waitForExit) {
                p.WaitForExit();
            }
        }

        // Windows-only, will not work on other operating systems
        private void StartWin32Process(string filename)
        {
            if (OperatingSystem.IsWindows())
            {
                var p = new Process();
                p.StartInfo.FileName = "cmd";
                p.StartInfo.Arguments = "/c start " + filename;
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                p.Start();
            }
            else
            {
                NotWindows();
            }
        }

        /* Loads mas theme */
        Color[] LoadTheme()
        {
            if (!File.Exists(masRoot + "/scheme.cfg"))
            {
                return [Colors.Black, Colors.White];
            }
            var bgfg = File.ReadAllText(masRoot + "/scheme.cfg").Split(';');
            var bgs = bgfg[0].ToString().Split(':');
            var fgs = bgfg[1].ToString().Split(':');
            Color[] cols = [Color.FromArgb(255, byte.Parse(bgs[0]), byte.Parse(bgs[1]), byte.Parse(bgs[2])), Color.FromArgb(255, byte.Parse(fgs[0]), byte.Parse(fgs[1]), byte.Parse(fgs[2]))
            ];
            return cols;
        }

        private void ApplyColors()
        {
            this.Background = new SolidColorBrush(scheme[0]);
            this.Foreground = new SolidColorBrush(scheme[1]);
            Program.BgCol = this.Background;
            Program.FgCol = this.Foreground;
        }

        private void ApplyTheme()
        {
            this.Background = new SolidColorBrush(scheme[0]);
            this.Foreground = new SolidColorBrush(scheme[1]);
            Program.BgCol = this.Background;
            Program.FgCol = this.Foreground;
            var IB = new ImageBrush(new Bitmap(masRoot + "/bg_common.png"))
            {
                Stretch = Stretch.Fill
            };
            this.Styles.Add(new Style(x => x.OfType<Button>())
            {
                Setters = { new Setter(ForegroundProperty, new SolidColorBrush(scheme[1])) },
                Children =
                {
                    new Style(x => x.Nesting().Class(":disabled"))
                    {
                        Setters = {
                            new Setter(ForegroundProperty, new SolidColorBrush(scheme[0])),
                        }
                    },
                }
            });
            this.Styles.Add(new Style(x => x.OfType<CheckBox>())
            {
                Setters = { new Setter(ForegroundProperty, new SolidColorBrush(scheme[1])) }
            });
            this.Styles.Add(new Style(x => x.OfType<TabItem>())
            {
                Setters = { new Setter(ForegroundProperty, new SolidColorBrush(scheme[1])) },
                Children =
                {
                    new Style(x => x.Nesting().Class(":pointerover"))
                    {
                        Setters = { new Setter(ForegroundProperty, new SolidColorBrush(scheme[1])) }
                    },
                    new Style(x => x.Nesting().Class(":selected"))
                    {
                        Setters = {
                            new Setter(ForegroundProperty, new SolidColorBrush(scheme[1])),
                            new Setter(FontWeightProperty, FontWeight.SemiBold)
                        }
                    }
                }
            });
            this.ErtGrid.Background = IB;
        }

        private void CheckTheme(object sender, EventArgs e)
        {
            try
            {
                if (freezeTimer)
                {
                    return;
                }
                var tempScheme = LoadTheme();
                if (tempScheme == scheme) return;
                scheme = LoadTheme();
                ApplyTheme();
            }
            catch
            {
                // ignored
            }
        }

        private static void StopThread(Thread th)
        {
            th.Interrupt();
            th.Join();
        }

        private void InitTimers()
        {
            LaunchError = Program.Launcherror;
            if (LaunchError)
            {
                string devPrefix = "Markuse arvuti";
                if (Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/.masv")) { devPrefix = "Markuse virtuaalarvuti"; }
                else if (IsAndroid()) { devPrefix = "Markuse tahvelarvuti"; }
                SetCollectProgress(-2, "Püsivuskontroll ei ole usaldusväärne, sest Verifile 2.0 räsi ei ole sobiv. Palun uuendage " + devPrefix  + " juhtpaneeli ja/või Verifile 2.0 tarkvara kataloogis\n\"" + masRoot + "\". Täpsem info standardväljundis.");
                this.Title = devPrefix + " juhtpaneel";
                return;
            }
            ThreadStart ts = CollectInfo;
            var t = new Thread(ts)
            {
                IsBackground = true
            };
            t.Start();
        }
        
        private void SetCollectProgress(int value, String status)
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (value == -2)
                { 
                    CollectProgress.Value = 0;
                    LoaderLogo.IsVisible = false;
                    FailGif.IsVisible = true;
                    ProgressStatusLabel.HorizontalAlignment = HorizontalAlignment.Center;
                    ProgressStatusLabel.TextWrapping = TextWrapping.Wrap;
                    ProgressStatusLabel.MaxWidth = this.Width / 2;
                    CollectProgress.IsVisible = false;
                    InfoCollectLabel.Content = "Programmi laadimine nurjus";
                    this.Title = "Markuse asjad";
                    if (status.Contains("VF_BYPASS"))
                    {
                        ErrorExitButton.Content = "Ignoreeri";
                        ErrorExitButton.Click -= ErrorExitButton_OnClick;
                        ErrorExitButton.Click += (_, _) =>
                        {
                            Header1.IsVisible = true;
                            CheckSysLabel.IsVisible = false;
                            TabsControl.IsVisible = true;
                            TabsControl.IsEnabled = true;
                        };
                    }

                    ErrorExitButton.IsVisible = true;
                }
                if (value != -1)
                {
                    CollectProgress.Value = value;
                }
                else
                {
                    CollectProgress.Value += 1;
                }
                ProgressStatusLabel.Text = status;
            });
        }

        private void CollectInfo()
        {
            SetCollectProgress(0, "Seadme tüübi tuvastamine...");
            string devPrefix = "Markuse arvuti asjade";
            if (Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/.masv"))
            {
                devPrefix = "Markuse virtuaalarvuti asjade";
            }
            else if (IsAndroid())
            {
                devPrefix = "Markuse tahvelarvuti asjade";
            }
            SetCollectProgress(1, "Teema laadimine...");
            scheme = LoadTheme();
            Dispatcher.UIThread.Post(ApplyColors);
            SetCollectProgress(20, "Konfiguratsiooni laadimine...");
            if (File.Exists(masRoot + "/Config.json"))
            {
                config.Load(masRoot);
                Dispatcher.UIThread.Post(() =>
                {
                    ShowMasLogoCheck.IsChecked = config.ShowLogo;
                    AllowScheduledTasksCheck.IsChecked = config.AllowScheduledTasks;
                    StartDesktopNotesCheck.IsChecked = config.AutostartNotes;
                    IntegrationPollrate.Text = config.PollRate.ToString();
                });
            }
            else if (File.Exists(masRoot + "/mas.cnf"))
            {
                string[] cnfs = File.ReadAllText(masRoot + "/mas.cnf").Split(';');
                Dispatcher.UIThread.Post(() =>
                {
                    ShowMasLogoCheck.IsChecked = cnfs[0] == "true";                  // Kuva Markuse asjade logo integratsioonitarkvara käivitumisel
                    AllowScheduledTasksCheck.IsChecked = cnfs[1] == "true";          // Käivita töölauamärkmed arvuti käivitumisel
                    StartDesktopNotesCheck.IsChecked = cnfs[2] == "true";            // Käivita töölauamärkmed arvuti käivitumisel 
                    IntegrationPollrate.Text = "5000";                               // Pollimise sagedus (ms)
                    config = new()
                    {
                        ShowLogo = cnfs[0] == "true",
                        AllowScheduledTasks = cnfs[1] == "true",
                        AutostartNotes = cnfs[2] == "true",
                        PollRate = 5000
                    };
                });
            }
            else
            {
                SetCollectProgress(-2, devPrefix + " tarkvara ei ole juurutatud. Palun juurutage seade kasutades juurutamise tööriista.");
                return;
            }
            SetCollectProgress(30, "Väljaande info kogumine...");
            if (File.Exists(masRoot + "/edition.txt"))
            {

                switch (vf.MakeAttestation())
                {
                    case "VERIFIED":
                        break;
                    case "FOREIGN":
                        SetCollectProgress(-2, "See programm töötab ainult Markuse arvutis.\nVeakood: VF_FOREIGN");
                        return;
                    case "FAILED":
                        SetCollectProgress(-2, "Verifile püsivuskontrolli läbimine nurjus.\nVeakood: VF_FAILED");
                        return;
                    case "TAMPERED":
                        SetCollectProgress(-2, "See arvuti pole õigesti juurutatud. Seda võis põhjustada hiljutine riistvaramuudatus. Palun kasutage juurutamiseks Markuse asjade juurutamistööriista.\nVeakood: VF_TAMPERED");
                        return;
                    case "LEGACY":
                        SetCollectProgress(-2, "See arvuti on juurutatud vana juurutamistööriistaga. Palun juurutage arvuti uuesti uue juurutamistarkvaraga.\nVeakood: VF_LEGACY.");
                        return;
                    case "BYPASS":
                        SetCollectProgress(-2, "Veakood: VF_BYPASS");
                        Program.Launcherror = true;
                        return;
                }
                SetCollectProgress(35, "Verifile OK");
                if (Verifile.CheckFiles(Verifile.FileScope.ControlPanel))
                {
                    SetCollectProgress(40, "Töölauaikooni seadete laadimine...");
                    LoadDesktopSettings();
                    SetCollectProgress(55, "Kogun infot töölauaikoonide kohta...");

                    var proc = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = masRoot + "/Markuse asjad/DesktopIcons" + (OperatingSystem.IsMacOS() ? ".app/Contents/MacOS/DesktopIcons" : "") +
                                       (OperatingSystem.IsWindows() ? ".exe" : ""),
                            Arguments = "--icons",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            CreateNoWindow = true,
                        }
                    };
                    proc.Start();
                    desktopIcons.Clear();
                    while (!proc.StandardOutput.EndOfStream)
                    {
                        var line = proc.StandardOutput.ReadLine();
                        if (string.IsNullOrEmpty(line)) continue;
                        desktopIcons.Add(line);
                        SetCollectProgress(-1, "Tuvastatud ikoon: " + line);
                    }

                    SetCollectProgress(75, "Oleku kontrollimine...");
                    var VF_STATUS = vf.MakeAttestation();
                    SetCollectProgress(80, "UI ettevalmistamine...");
                    Dispatcher.UIThread.Post(() =>
                    {
                        ApplyTheme();
                        this.IsVisible = true;
                        GetEditionInfo();
                        this.Title = "Markuse arvuti juhtpaneel";
                        if (Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/.masv"))
                        {
                            TopLabel.Text = "markuse virtuaalarvuti juhtpaneel";
                            this.Title = "Markuse virtuaalarvuti juhtpaneel";
                        }
                        else if (IsAndroid())
                        {
                            TopLabel.Text = "markuse tahvelarvuti juhtpaneel";
                            this.Title = "Markuse tahvelarvuti juhtpaneel";
                        }
                        DeviceCpanelLabel.Content = this.Title;
                        MasName.Content = devPrefix[..^1];
                        if (File.Exists(masRoot + "/irunning.log"))
                        {
                            // Projekt ITS aktiivne
                            WindowState = WindowState.FullScreen;
                            CloseButton.IsVisible = true;
                        }
                        CollectProgress.Value = 100;
                        if (VF_STATUS != "BYPASS")
                        {
                            CheckSysLabel.IsVisible = false;
                            TabsControl.IsEnabled = true;
                            TabsControl.IsVisible = true;
                            Header1.IsVisible = true;
                        }
                        else
                        {
                            SetCollectProgress(-2, "Veakood: VF_BYPASS");
                            Program.Launcherror = true;
                        }
                    });
                    SetCollectProgress(100, "Valmis!");
                    var me = Thread.CurrentThread;
                    Dispatcher.UIThread.Post(() =>
                    {
                        if (LaunchError)
                        {
                            MessageBoxShow("Programmi laadimisel ilmnes vigu. Programm ei pruugi õigesti toimida.", "Markuse arvuti juhtpaneel", ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error);
                        }
                        StopThread(me);
                    });
                }
                else
                {
                    SetCollectProgress(-2, devPrefix + " tarkvara ei ole õigesti juurutatud. Palun juurutage seade kasutades juurutamise tööriista.");
                }
            }
        }

        private void InitResources()
        {
            Bitmap icon;
            using (var ms = new MemoryStream(Properties.Resources.mas_web))
            {
                icon = new Bitmap(ms);
            }
            this.Icon = new WindowIcon(icon);
            this.Logo.Source = icon;
            ReloadThumbs();
        }

        private void InitButtons() {
            if (OperatingSystem.IsLinux()) {
                //this.FlashDriveButton.IsVisible = false;
                this.QuickNotesButton.IsVisible = false;
                this.RegeditButton.IsVisible = false;
                this.CommandButton.Content = "Konsool";
                this.TaskmgrButton.Content = "Protsessid";
                this.DevmgmtButton.Content = "Riistvara info";
                this.CompManButton.Content = "Käsurea utilliidid";
                WinUtilsLabel.Content = "Linuxi tarvikud";
            } else if (OperatingSystem.IsWindows()) {
                return;
            } else if (OperatingSystem.IsMacOS()) {
                this.RegeditButton.IsVisible = false;
                this.CommandButton.Content = "Terminal";
                this.CompManButton.IsVisible = false;
                WinUtilsLabel.Content = "macOS tarvikud";
                Console.WriteLine("Hoiatus: Kõik funktsioonid ei ole saadaval macOS-is");
            } else {
                Console.WriteLine("Hoiatus: Operatsioonsüsteemi ei toetata!");
            }
        }


        private void Image_DoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e)
        {
            RotateObject([Logo, LoaderLogo, FailGif]);
            if (GameNameBox.Text == null) return;
            if (!GameNameBox.Text.Equals(Encoding.UTF8.GetString([0x4F, 0x6C, 0x67, 0x65, 0x20,
                    0x76, 0x61, 0x6C, 0x6D, 0x69, 0x73, 0x20, 0x6D, 0x69, 0x6C, 0x6C, 0x65, 0x67,
                    0x69, 0x20, 0x75, 0x73, 0x6B, 0x75, 0x6D, 0x61, 0x74, 0x75, 0x20, 0x6A, 0x61,
                    0x6F, 0x6B, 0x73, 0x21])) && (eggLevel == 0)) return;
            eggLevel++;
            if (!GameNameBox.Text.Equals(char.ToUpper(TopLabel.Text[0]) + $"{TopLabel.Text[1..]} on lahe!")) return;
            if (eggLevel % 4 != 0) return;
            var allControls = this.GetVisualDescendants();
            List<Control> controls = [];
            controls.AddRange(allControls.Cast<Control>());
            new Thread(() =>
            {
                Random r = new();
                foreach (var v in allControls)
                {
                    Thread.Sleep(r.Next(10));
                    Dispatcher.UIThread.Post(() =>
                    {
                        ShakeObject((Control)v); 
                    });
                }
            }).Start();
        }

        private void ShakeObject(Control senders)
        {
            if (senders.Name is "Logo" or "Header1" or "TopLabel") return;
            const int duration = 500;
            const double strength = 3.0;
            Animation animation = new()
            {
                Duration = TimeSpan.FromMilliseconds(duration),
                IterationCount = new IterationCount(9999),
                PlaybackDirection = PlaybackDirection.Alternate,
                FillMode = FillMode.Both,
                Easing = new SineEaseInOut()
            };
            KeyFrame key1 = new()
            {
                KeyTime = TimeSpan.FromMilliseconds(0)
            };
            KeyFrame key2 = new()
            {
                KeyTime = TimeSpan.FromMilliseconds(duration / 2)
            };
            KeyFrame key3 = new()
            {
                KeyTime = TimeSpan.FromMilliseconds(duration)
            };
            if (!senders.GetVisualDescendants().Any())
            {
                key1.Setters.Add(new Setter(RotateTransform.AngleProperty, 0.0));
                key2.Setters.Add(new Setter(RotateTransform.AngleProperty, -strength));
                key3.Setters.Add(new Setter(RotateTransform.AngleProperty, strength));
            }
            else
            {
                key1.Setters.Add(new Setter(ScaleTransform.ScaleXProperty, 1.0));
                key1.Setters.Add(new Setter(ScaleTransform.ScaleYProperty, 1.0));
                key2.Setters.Add(new Setter(ScaleTransform.ScaleXProperty, 1.025));
                key2.Setters.Add(new Setter(ScaleTransform.ScaleYProperty, 1.025));
                key3.Setters.Add(new Setter(ScaleTransform.ScaleXProperty, 1.0));
                key3.Setters.Add(new Setter(ScaleTransform.ScaleYProperty, 1.0));
            }

            animation.Children.Add(key1);
            animation.Children.Add(key2);
            animation.Children.Add(key3);
            _ = animation.RunAsync(senders);
        }

        private void RotateObject(Control[] senders)
        {
            const int duration = 500;
            angle += 90;
            Animation animation = new()
            {
                Duration = TimeSpan.FromMilliseconds(duration),
                IterationCount = new IterationCount(1),
                PlaybackDirection = PlaybackDirection.Normal,
                FillMode = FillMode.Forward,
                Easing = new SineEaseInOut()
            };
            KeyFrame key1 = new()
            {
                KeyTime = TimeSpan.FromMilliseconds(0)
            };
            KeyFrame key2 = new()
            {
                KeyTime = TimeSpan.FromMilliseconds(duration)
            };
            key1.Setters.Add(new Setter(RotateTransform.AngleProperty, angle - 90.0));
            key2.Setters.Add(new Setter(RotateTransform.AngleProperty, angle));
            animation.Children.Add(key1);
            animation.Children.Add(key2);
            foreach (var c in senders)
            {
                _ = animation.RunAsync(c);
            }
        }

        private void Grid_SizeChanged(object? sender, Avalonia.Controls.SizeChangedEventArgs e)
        {
            InfoTextBlock.Width = this.Width - 20;
        }

        /* MarkuStation */
        private void MarkuStationTabLoad(object? sender, RoutedEventArgs e)
        {
            MsLoadSettings(sender, e);
        }

        private void MsLoadSettings(object? sender, RoutedEventArgs e)
        {

            string file = File.ReadAllLines(masRoot + "/ms_display.txt")[0];
            switch (file)
            {
                case "internal":
                    this.MonMode.SelectedIndex = 1;
                    break;
                case "external":
                    this.MonMode.SelectedIndex = 2;
                    break;
                case "extend":
                    this.MonMode.SelectedIndex = 0;
                    break;
                case "clone":
                    this.MonMode.SelectedIndex = 3;
                    break;
            }
            file = File.ReadAllText(masRoot + "/setting.txt");
            string[] lines = file.Split('\n');
            creepCheck.IsChecked = lines[0] == "true";
            specialCheck.IsChecked = lines[1] == "true";
            introCheck.IsChecked = lines[2] == "true";
            legacyIntroCheck.IsChecked = (lines.Length > 3) && (lines[3] == "true");
            file = File.ReadAllText(masRoot + "/ms_games.txt");
            file = file.Substring(0, file.Length - 2);
            GameList.Items.Clear();
            foreach (string line in file.Split('\n').Skip(1))
            {
                ListBoxItem lbi = new ListBoxItem();
                lbi.Content = line;
                GameList.Items.Add(lbi);
            }
            file = File.ReadAllText(masRoot + "/ms_exec.txt");
            file = file.Substring(0, file.Length - 2);
            locations = file.Split('\n').Skip(1).ToArray();
        }

        private void RunMs(object? sender, RoutedEventArgs e)
        {
            // käivita MarkuStation 2 kui see eksisteerib
            if (File.Exists(masRoot + "/Markuse asjad/MarkuStation2") || File.Exists(masRoot + "/Markuse asjad/MarkuStation2.exe"))
            {
                if (OperatingSystem.IsWindows())
                {
                    StartWin32Process(masRoot + "/Markuse asjad/MarkuStation2.exe");
                } else
                {
                    Process p = new Process();
                    p.StartInfo.FileName = masRoot + "/Markuse asjad/MarkuStation2";
                    p.StartInfo.UseShellExecute = true;
                    p.Start();
                }
            }
            else
            { // käivita MarkuStation 1 fallback-ina
                StartWin32Process(masRoot + "/MarkuStation.exe");
            }
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

        private void AddButton(object? sender, RoutedEventArgs e)
        {
            string[] new_locations = new string[locations.Length + 1];
            for (int i = 0; i < locations.Length; i++)
            {
                new_locations[i] = locations[i];
            }
            new_locations[locations.Length] = (LocationBox.Text ?? "") + ";";
            ListBoxItem lbi = new ListBoxItem();
            lbi.Content = GameNameBox.Text ?? "";
            if ((lbi.Content == "") || (new_locations[locations.Length] == ""))
            {
                MessageBoxShow("Palun täitke kõik väljad!", "Mängu lisamine", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error);
                return;
            }
            locations = new_locations;
            GameList.Items.Add(lbi);
            LocationBox.Text = "";
            GameNameBox.Text = "";
        }

        private async void MsGameEdit(object? sender, TappedEventArgs e)
        {
            if (GameList.SelectedItems?.Count > 0)
            {
                string? SelectedGame;
                if (GameList.Selection.SelectedItems[0] is string)
                {
                    SelectedGame = GameList.Selection.SelectedItems[0] as string;
                } else
                {
                    SelectedGame = ((ListBoxItem)GameList.Selection.SelectedItems[0]).Content.ToString();
                }
                string GameLocation = locations[GameList.SelectedIndex];
                GameLocation = GameLocation.Substring(0, GameLocation.Length - 1);
                var mse = new MarkuStation_Edit
                {
                    NameBox =
                    {
                        Text = SelectedGame
                    },
                    LocationBox =
                    {
                        Text = GameLocation
                    },
                    Background = this.Background,
                    Foreground = this.Foreground
                };
                await mse.ShowDialog(this).WaitAsync(new CancellationToken(false));
                if (mse.DialogResult && (mse.LocationBox.Text == ";") && (mse.NameBox.Text == ";"))
                {
                    int Deletable = GameList.SelectedIndex;
                    string[] new_games = new string[locations.Length - 1];
                    int i = 0;
                    foreach (string location in locations)
                    {
                        if (location == GameLocation + ";")
                        {
                            continue;
                        }
                        new_games[i] = location;
                        i++;
                    }
                    locations = new_games;
                    GameList.Items.RemoveAt(Deletable);
                    _ = MessageBoxShow("Üksus eemaldati edukalt! Vajutage \"Salvesta muudatused\", et list rakendada.", "Markuse arvuti juhtpaneel", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Success);
                }
                else if (mse.DialogResult)
                {
                    int Modifiable = GameList.SelectedIndex;
                    locations[Modifiable] = mse.LocationBox.Text + ";";
                    GameList.Items[Modifiable] = mse.NameBox.Text;
                    _ = MessageBoxShow("Üksus muudeti edukalt! Vajutage \"Salvesta muudatused\", et list rakendada.", "Markuse arvuti juhtpaneel", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Success);
                }
            }
        }

        private void MsSaveConfig(object? sender, RoutedEventArgs e)
        {
            string scrntype = "extend";
            switch (MonMode.SelectedIndex)
            {
                case 1:
                    scrntype = "internal";
                    break;
                case 2:
                    scrntype = "external";
                    break;
                case 3:
                    scrntype = "clone";
                    break;
                default:
                    break;
            }
            File.WriteAllText(masRoot + @"/ms_display.txt", scrntype + Environment.NewLine);
            StringBuilder? builder = new StringBuilder();
            builder.Append("* MarkuStation mängude loetelu *").Append("\n");
            foreach (var lvi in GameList.Items)
            {
                string? val;
                if (lvi is ListBoxItem)
                {
                    val = ((ListBoxItem)lvi).Content?.ToString();
                }
                else
                {
                    val = lvi.ToString();
                }
                if (!string.IsNullOrEmpty(val))
                {
                    builder.Append(val);
                    builder.Append("\n");
                }
            }
            string temp = builder.ToString();
            temp += "\n";
            File.WriteAllText(masRoot + "/ms_games.txt", temp, Encoding.UTF8);
            temp = "";
            builder = null;
            builder = new StringBuilder();
            builder.Append("* MarkuStation käivitaja loetelu (ärge eemaldage/lisage semikooloneid)*;").Append("\n");
            foreach (string val in locations)
            {
                if (val != "")
                {
                    builder.Append(val);
                    builder.Append("\n");
                }
            }
            temp = builder.ToString();
            temp = temp + "\n";
            File.WriteAllText(masRoot + "/ms_exec.txt", temp, Encoding.UTF8);
            temp = "";
            if (creepCheck.IsChecked == true) { temp = "true"; }
            if (creepCheck.IsChecked == false) { temp = "false"; }
            if (specialCheck.IsChecked == true) { temp += "\ntrue"; }
            if (specialCheck.IsChecked == false) { temp += "\nfalse"; }
            if (introCheck.IsChecked == true) { temp += "\ntrue"; }
            if (introCheck.IsChecked == false) { temp += "\nfalse"; }
            if (legacyIntroCheck.IsChecked == true) { temp += "\ntrue"; }
            if (legacyIntroCheck.IsChecked == false) { temp += "\nfalse"; }
            File.WriteAllText(masRoot + "/setting.txt", temp);
            temp = "";
        }

        /* Konfiguratsioon */
        private void ConfigCheck(object? sender, RoutedEventArgs e)
        {
            // check if poll rate is numeric
            if (!int.TryParse(IntegrationPollrate.Text ?? "", out int pollRate)) return;

            // backwards compatibility
            string saveprog = "";
            saveprog += (bool)ShowMasLogoCheck.IsChecked! ? "true;" : "false;";
            saveprog += (bool)AllowScheduledTasksCheck.IsChecked! ? "true;" : "false;";
            saveprog += (bool)StartDesktopNotesCheck.IsChecked! ? "true;" : "false;";
            try
            {
                File.WriteAllText(masRoot + "/mas.cnf", saveprog);

                // new method
                config = new()
                {
                    AllowScheduledTasks = AllowScheduledTasksCheck.IsChecked ?? false,
                    AutostartNotes = StartDesktopNotesCheck.IsChecked ?? false,
                    ShowLogo = ShowMasLogoCheck.IsChecked ?? false,
                    PollRate = pollRate,
                };
                config.Save(masRoot);
            }
            catch
            {
                AllowScheduledTasksCheck.IsEnabled = false;
                StartDesktopNotesCheck.IsEnabled = false;
                ShowMasLogoCheck.IsEnabled = false;
                IntegrationPollrate.IsEnabled = false;
                ConfigNoticeLabel.Content = "Neid sätteid ei saa hetkel muuta. Olge kindlad, et kirjutamise ligipääs failidele mas.cnf ja Config.json oleks saadaval.";
                LaunchError = true;
                return;
            }
        }

        private void ReloadThumbs()
        {
            if (Program.Launcherror) return; // avoid further errors in case something went wrong
            ThumbDesktop.Source = new Bitmap(masRoot + "/bg_desktop.png");
            ThumbLockscreen.Source = new Bitmap(masRoot + "/bg_login.png");
            ThumbMiniversion.Source = new Bitmap(masRoot + "/bg_uncommon.png");
        }

        private void ChangeDesktop(object? sender, TappedEventArgs e)
        {
            DesktopFunction();
        }

        private void ChangeMini(object? sender, TappedEventArgs e)
        {
            MiniFunction();
        }

        private void ChangeLogin(object? sender, TappedEventArgs e)
        {
            LoginFunction();
        }

        private async void DesktopFunction()
        {
            var topLevel = GetTopLevel(this);
            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Markuse arvuti juhtpaneel",
                AllowMultiple = false
            });

            if (files.Count < 1)
            {
                return;
            }
            string filename = files[0].Path.AbsolutePath;
            if (OperatingSystem.IsWindows())
            {
                ThumbDesktop.Source = null;
                foreach (Process p in Process.GetProcesses())
                {
                    if ((p.ProcessName == "Markuse arvuti integratsioonitarkvara.exe") || (p.ProcessName == "Markuse arvuti integratsioonitarkvara.EXE") || (p.ProcessName == "Markuse arvuti integratsioonitarkvara"))
                    {
                        p.Kill();
                    }
                }
                var pr = new Process();
                pr.StartInfo.FileName = masRoot + "/ChangeWallpaper.exe";
                pr.StartInfo.UseShellExecute = false;
                pr.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                pr.StartInfo.CreateNoWindow = true;
                pr.StartInfo.Arguments = masRoot.Replace("/", "\\") + "\\bg_login.png";
                pr.Start();
                pr.StartInfo.FileName = "cmd.exe";
                pr.StartInfo.Arguments = "/k move " + masRoot + "\\bg_desktop.png " + masRoot + "\\bg_desktop.temp";
                pr.Start();
                while (!File.Exists(masRoot + "/bg_desktop.temp")) { }
                File.Copy(filename, masRoot + "/bg_desktop.png");
                File.Delete(masRoot + "/bg_desktop.temp");
                pr.StartInfo.Arguments = "";
                pr.StartInfo.WindowStyle = ProcessWindowStyle.Minimized;
                pr.StartInfo.CreateNoWindow = false;
                pr.StartInfo.FileName = masRoot + "/Markuse asjad/Markuse arvuti integratsioonitarkvara.exe";
                pr.Start();
                if (File.Exists(masRoot + "/bg_desktop.png"))
                {
                    ThumbDesktop.Source = new Bitmap(masRoot + "/bg_desktop.png");
                }
                foreach (var p in Process.GetProcesses())
                {
                    if (p.ProcessName is "cmd.exe" or "cmd.EXE" or "cmd" or "conhost.exe" or "conhost.EXE" or "conhost")
                    {
                        p.Kill();
                    }
                }
            } else if (OperatingSystem.IsLinux()) {
                ThumbDesktop.Source = null;
                // move existing background image
                RunCommand("mv", "\"" + masRoot + "/bg_desktop.png\" \"" + masRoot + "/bg_desktop.temp\"");
                // replace background image
                while (!File.Exists(masRoot + "/bg_desktop.temp")) { }
                RunCommand("cp", "\"" + filename + "\" \"" + masRoot + "/bg_desktop_l.png\"");
                // delete temporary background
                File.Delete(masRoot + "/bg_desktop.temp");
                // remove cropped background images
                RunCommand("rm", "\"" + masRoot + "/bg_desktop_l.png\"");
                RunCommand("rm", "\"" + masRoot + "/bg_desktop_r.png\"");
                // crop left/right backgrounds
                RunCommand("magick", "\"" + masRoot + "/bg_desktop.png\" -crop 1280x1024+0+0 \"" + masRoot + "/bg_desktop_l.png\"");
                RunCommand("magick", "\"" + masRoot + "/bg_desktop.png\" -crop 1280x1024+3200+0 \"" + masRoot + "/bg_desktop_r.png\"");
                // apply desktop background
                RunCommand("sh", masRoot + "/change_bg.sh");
                if (File.Exists(masRoot + "/bg_desktop.png"))
                {
                    ThumbDesktop.Source = new Bitmap(masRoot + "/bg_desktop.png");
                }
            } else
            {
                NotWindows();
            }
        }

        private async void LoginFunction()
        {
            var topLevel = GetTopLevel(this);
            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Markuse arvuti juhtpaneel",
                AllowMultiple = false
            });

            if (files.Count < 1)
            {
                return;
            }
            var filename = files[0].Path.AbsolutePath;
            ThumbLockscreen.Source = null;
            File.Delete(masRoot + "/bg_login.png");
            File.Copy(filename, masRoot + "/bg_login.png");
            if (File.Exists(masRoot + "/bg_desktop.png"))
            {
                ThumbLockscreen.Source = new Bitmap(masRoot + "/bg_login.png");
            }
        }

        private async void MiniFunction()
        {
            var topLevel = GetTopLevel(this);
            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Markuse arvuti juhtpaneel",
                AllowMultiple = false
            });

            if (files.Count < 1)
            {
                return;
            }
            var filename = files[0].Path.AbsolutePath;
            ThumbMiniversion.Source = null;
            File.Delete(masRoot + "/bg_uncommon.png");
            File.Copy(filename, masRoot + "/bg_uncommon.png");
            if (File.Exists(masRoot + "/bg_uncommon.png"))
            {
                ThumbMiniversion.Source = new Bitmap(masRoot + "/bg_uncommon.png");
            }
        }

        private void SwapBgs(object sender, RoutedEventArgs e)
        {
            if (OperatingSystem.IsWindows())
            {
                bool rng;
                rng = false;
                foreach (Process p in Process.GetProcesses())
                {
                    if ((p.ProcessName == "Markuse arvuti integratsioonitarkvara.exe") || (p.ProcessName == "Markuse arvuti integratsioonitarkvara.EXE") || (p.ProcessName == "Markuse arvuti integratsioonitarkvara"))
                    {
                        rng = true;
                        p.Kill();
                    }
                }
                ThumbDesktop.Source = null;
                ThumbLockscreen.Source = null;
                ThumbMiniversion.Source = null;
                string rootBackSlash = masRoot.Replace("/", "\\");
                //võta kasutusele ajutine taustapilt
                var pr = new Process();
                pr.StartInfo.FileName = masRoot + "/ChangeWallpaper.exe";
                pr.StartInfo.UseShellExecute = false;
                pr.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                pr.StartInfo.CreateNoWindow = true;
                pr.StartInfo.Arguments = rootBackSlash + "\\bg_login.png";
                pr.Start();
                pr.StartInfo.FileName = "cmd.exe";
                pr.StartInfo.Arguments = "/k move " + rootBackSlash + "\\bg_desktop.png " + rootBackSlash + "\\bg_desktop.temp";
                pr.Start();
                while (!File.Exists(masRoot + "/bg_desktop.temp")) { }
                pr.StartInfo.Arguments = "/k move " + rootBackSlash + "\\bg_uncommon.png " + rootBackSlash + "\\bg_desktop.png";
                pr.Start();
                while (!File.Exists(masRoot + "/bg_desktop.png")) { }
                pr.StartInfo.Arguments = "/k move " + rootBackSlash + "\\bg_desktop.temp " + rootBackSlash + "\\bg_uncommon.png";
                pr.Start();
                while (!File.Exists(masRoot + "/bg_uncommon.png")) { }
                pr.StartInfo.Arguments = "";
                pr.StartInfo.WindowStyle = ProcessWindowStyle.Minimized;
                pr.StartInfo.CreateNoWindow = false;
                pr.StartInfo.FileName = masRoot + "/Markuse asjad/Markuse arvuti integratsioonitarkvara.exe";
                pr.Start();
                ReloadThumbs();
                foreach (var p in Process.GetProcesses())
                {
                    try
                    {
                        if ((p.ProcessName == "cmd.exe") || (p.ProcessName == "cmd.EXE") || (p.ProcessName == "cmd"))
                        {
                            p.Kill();
                        }
                        else if ((p.ProcessName == "conhost.exe") || (p.ProcessName == "conhost.EXE") || (p.ProcessName == "conhost"))
                        {
                            p.Kill();
                        }
                    } catch
                    {

                    }
                }
            } else if (OperatingSystem.IsLinux()) {
                // remove cropped background images
                RunCommand("rm", "\"" + masRoot + "/bg_desktop_l.png\"");
                RunCommand("rm", "\"" + masRoot + "/bg_desktop_r.png\"");
                // swap bg_desktop and bg_uncommon (only use when swapping backgrounds)
                RunCommand("mv", "\"" + masRoot + "/bg_desktop.png\" \"" + masRoot + "/temp.png\"");
                RunCommand("mv", "\"" + masRoot + "/bg_uncommon.png\" \"" + masRoot + "/bg_desktop.png\"");
                RunCommand("mv", "\"" + masRoot + "/temp.png\" \"" + masRoot + "/bg_uncommon.png\"");
                // crop left/right backgrounds
                RunCommand("magick", "\"" + masRoot + "/bg_desktop.png\" -crop 1280x1024+0+0 \"" + masRoot + "/bg_desktop_l.png\"");
                RunCommand("magick", "\"" + masRoot + "/bg_desktop.png\" -crop 1280x1024+3200+0 \"" + masRoot + "/bg_desktop_r.png\"");
                // apply desktop background
                RunCommand("sh", masRoot + "/change_bg.sh");
                // update thumbnails
                ReloadThumbs();
            } else
            {
                NotWindows();
            }
        }

        private void EditScheds(object sender, RoutedEventArgs e)
        {
            if (AllowScheduledTasksCheck.IsChecked ?? false)
            {
                if (File.Exists(masRoot + "/events.txt"))
                {
                    var p = new Process();
                    p.StartInfo = new ProcessStartInfo(masRoot + "/events.txt")
                    {
                        UseShellExecute = true,
                    };
                    p.Start();

                } else
                {
                    MessageBoxShow("Sündmuste faili ei eksisteeri", "Probleem", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error);
                }
            } else
            {
                MessageBoxShow("Ajastatud sündmused on keelatud", "Probleem", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error);
            }
        }

        private void SendDesktopIconCommand(string type, string args = "")
        {
            if (preventWrites)
            {
                return;
            }
            DesktopCommand cmd = new()
            {
                Arguments = args,
                Type = type
            };
            cmd.Send(masRoot);
        }

        private async void EditBg(object sender, RoutedEventArgs e)
        {
            if (Program.Launcherror) return;
            var cpd = new ColorPickerDialog
            {
                Color =
                {
                    Color = scheme[0]
                },
                Background = this.Background,
                Foreground = this.Foreground
            };
            await cpd.ShowDialog(this);
            this.scheme[0] = cpd.Color.Color;
            if (cpd.result)
            {
                freezeTimer = true;
                await File.WriteAllTextAsync(masRoot + "/scheme.cfg", cpd.Color.Color.R.ToString() + ":" + cpd.Color.Color.G.ToString() + ":" + cpd.Color.Color.B.ToString() + ":;" + this.scheme[1].R + ":" + this.scheme[1].G + ":" + this.scheme[1].B + ":;");
                freezeTimer = false;
                LoadTheme();
                ApplyTheme();
                SendDesktopIconCommand("ReloadTheme");
            }
        }

        private async void EditFg(object sender, RoutedEventArgs e)
        {
            if (Program.Launcherror) return;
            var cpd = new ColorPickerDialog
            {
                Color =
                {
                    Color = scheme[1]
                },
                Background = this.Background,
                Foreground = this.Foreground
            };
            await cpd.ShowDialog(this);
            this.scheme[1] = cpd.Color.Color;
            if (cpd.result)
            {
                freezeTimer = true;
                await File.WriteAllTextAsync(masRoot + "/scheme.cfg", this.scheme[0].R.ToString() + ":" + this.scheme[0].G.ToString() + ":" + this.scheme[0].B.ToString() + ":;" + cpd.Color.Color.R + ":" + cpd.Color.Color.G + ":" + cpd.Color.Color.B + ":;");
                freezeTimer = false;
                LoadTheme();
                ApplyTheme();
                SendDesktopIconCommand("ReloadTheme");
            }
        }

        /* Teave */

        private void GetEditionInfo()
        {
            if (!vf.IsVerified())
            {
                return;
            }
            Bitmap cross;
            Bitmap check;
            using (var ms = new MemoryStream(Properties.Resources.failure))
            {
                cross = new Bitmap(ms);
            }
            using (var ms = new MemoryStream(Properties.Resources.success))
            {
                check = new Bitmap(ms);
            }
            string[] masVer = File.ReadAllLines(masRoot + "/edition.txt");
            string edition = masVer[1];
            FileInfo fi = new FileInfo(masRoot + "/edition.txt");
            MasEditionLabel.Content = edition;
            switch (edition)
            {
                case "Basic":
                case "Basic+":
                    EditionBox.Fill = new SolidColorBrush(Colors.Yellow);
                    break;
                case "Starter":
                    EditionBox.Fill = new SolidColorBrush(Colors.Lime);
                    break;
                case "Premium":
                    EditionBox.Fill = new SolidColorBrush(Colors.DarkRed);
                    break;
                case "Pro":
                    EditionBox.Fill = new SolidColorBrush(Colors.DeepSkyBlue);
                    break;
                case "Ultimate":
                    EditionBox.Fill = new SolidColorBrush(Colors.BlueViolet);
                    break;
            }
            var editionDetails = new StringBuilder();
            editionDetails.AppendLine("Versioon: " + masVer[2]);
            editionDetails.AppendLine("Järk: " + masVer[3]);
            editionDetails.AppendLine("Nimi: " + masVer[10]);
            editionDetails.AppendLine("Keel: " + masVer[6]);
            editionDetails.AppendLine("Juurutatud?: " + (masVer[4] == "Yes" ? "Jah" : "Ei"));
            editionDetails.Append("Muutmisaeg: ")
                          .Append(fi.LastWriteTime.ToShortDateString())
                          .Append(" ")
                          .Append(fi.LastWriteTime.ToShortTimeString());
            editionDetails.AppendLine();
            editionDetails.AppendLine("Kinnituskood: " + masVer[9]);
            editionDetails.AppendLine("Olek: " + vf.MakeAttestation());
            EditionDetails.Text = editionDetails.ToString();
            var features = masVer[8].Split('-');
            FeatTS.Source = cross;
            FeatRM.Source = cross;
            FeatIP.Source = cross;
            FeatCS.Source = cross;
            FeatMM.Source = cross;
            FeatRD.Source = cross;
            FeatWX.Source = cross;
            FeatLT.Source = cross;
            FeatGP.Source = cross;
            foreach (var feature in features)
            {
                switch (feature)
                {
                    case "MM":
                        FeatMM.Source = check;
                        break;
                    case "TS":
                        FeatTS.Source = check;
                        break;
                    case "RM":
                        FeatRM.Source = check;
                        break;
                    case "IP":
                        FeatIP.Source = check;
                        break;
                    case "CS":
                        FeatCS.Source = check;
                        break;
                    case "WX":
                        FeatWX.Source = check;
                        break;
                    case "RD":
                        FeatRD.Source = check;
                        break;
                    case "LT":
                        FeatLT.Source = check;
                        break;
                    case "GP":
                        FeatGP.Source = check;
                        break;
                }
            }
            WhatNewLabel.Text += "\n" + whatNew;
            var fullVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString().Split(".");
            CpanelVersionLabel.Content = fullVersion[2] != "0" ? $"versioon {fullVersion[0]}.{fullVersion[1]}.{fullVersion[2]}" : $"versioon {fullVersion[0]}.{fullVersion[1]}";
        }

        private void ComputerInfoClicked(object sender, RoutedEventArgs e)
        {
            if (OperatingSystem.IsWindows())
            {
                Process.Start("msinfo32");
            } else if (OperatingSystem.IsLinux())
            {
                Process.Start("kinfocenter");
            } else if (OperatingSystem.IsMacOS()) {
                RunCommand("open", "-a \"About This Mac\"");
            }
        }

        private void OpenMasRootClicked(object sender, RoutedEventArgs e)
        {
            var p = new Process();
            p.StartInfo = new ProcessStartInfo(masRoot)
            {
                UseShellExecute = true,
            };
            p.Start();
        }

        private void ReRootClicked(object sender, RoutedEventArgs e)
        {
            var fName = masRoot + "/Markuse asjade juurutamise tööriist" +
                           (OperatingSystem.IsWindows() ? ".exe" : "");
            if (!File.Exists(fName))
            {
                MessageBoxShow("Palun kopeerige Markuse asjade juurutamise tööriist " +
                               (OperatingSystem.IsWindows() ? "kausta" : "kataloogi") + $" \"{masRoot}\"");
                return;
            }
            var ps = WindowState;
            new Thread(() =>
            {
                Process p = new()
                {
                    StartInfo =
                    {
                        FileName = fName,
                        UseShellExecute = true,
                    }
                };
                p.Start();
                Dispatcher.UIThread.Post(() =>
                {
                    ErtGrid.IsEnabled = false;
                    WindowState = WindowState.Minimized;
                    IsVisible = false;
                });
                p.WaitForExit();
                Dispatcher.UIThread.Post(() =>
                {
                    ErtGrid.Background = null;
                    ErtGrid.IsEnabled = true;
                    WindowState = ps;
                    IsVisible = true;
                    WhatNewLabel.Text = "Mis on uut?";
                    TabsControl.IsEnabled = false;
                    TabsControl.IsVisible = false;
                    Header1.IsVisible = false;
                    CheckSysLabel.IsVisible = true;
                });
                CollectInfo();
            }).Start();
        }

        private static bool IsAppleSilicon() {
            return OperatingSystem.IsMacOS() && (System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture == System.Runtime.InteropServices.Architecture.Arm64);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            InitTimers();
            //TabMarkuStation.IsVisible = !IsAppleSilicon(); // LibVLC is currently unsupported on Apple Silicon, so any Markus' stuff that leverages it (incl. MarkuStation 2) will not work.
        }

        private void Locked_IsCheckedChanged(object? sender, RoutedEventArgs e)
        {
            SendDesktopIconCommand("Lock", ((CheckBox?)e.Source).IsChecked ?? false ? "true" : "false");
        }

        private void ShowIcons_IsCheckedChanged(object? sender, RoutedEventArgs e)
        {
            SendDesktopIconCommand("IsIconVisible", ((CheckBox?)e.Source).IsChecked ?? false ? "true" : "false");
        }

        private void ShowLogo_IsCheckedChanged(object? sender, RoutedEventArgs e)
        {
            SendDesktopIconCommand("IsLogoVisible", ((CheckBox?)e.Source).IsChecked ?? false ? "true" : "false");
        }

        private void ShowActions_IsCheckedChanged(object? sender, RoutedEventArgs e)
        {
            SendDesktopIconCommand("IsActionVisible", ((CheckBox?)e.Source).IsChecked ?? false ? "true" : "false");
        }

        private void DesktopIconCountX_OnSelectionChanged(object? sender, TextChangedEventArgs e)
        {
            if (preventWrites) return;
            var cb = (TextBox?)e.Source;
            if (!int.TryParse(cb.Text, out var n)) return;
            LoadDesktopSettings();
            desktopLayout.IconCountX = n;
            SaveDesktopSettings();
            SendDesktopIconCommand("Restart", "true");
        }

        private void DesktopIconCountY_OnSelectionChanged(object? sender, TextChangedEventArgs e)
        {
            if (preventWrites) return;
            var cb = (TextBox?)e.Source;
            if (!int.TryParse(cb.Text, out var n)) return;
            LoadDesktopSettings();
            desktopLayout.IconCountY = Convert.ToInt32(n);
            SaveDesktopSettings();
            SendDesktopIconCommand("Restart", "true");
        }

        private void SaveDesktopSettings()
        {
            if (preventWrites) return;
            var jsonData = JsonSerializer.Serialize(desktopLayout, _serializerOptions);
            try
            {
                File.WriteAllText(masRoot + "/DesktopIcons.json", jsonData, encoding: Encoding.UTF8);
            }
            catch
            {
                DesktopTab.IsEnabled = false;
                DesktopTab.IsSelected = false;
                TabPrimary.IsSelected = true;
                MessageBoxShow("Sätete salvestamine nurjus. Olge kindlad, et teil oleks kirjutamise ligipääs failile \"DesktopIcons.json\".", "Markuse arvuti juhtpaneel", ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error);
            }
        }

        private void LoadDesktopSettings()
        {
            desktopLayout = JsonSerializer.Deserialize<DesktopLayout>(File.ReadAllText(masRoot + "/DesktopIcons.json"));
        }

        private void DesktopTab_Loaded(object? sender, RoutedEventArgs e)
        {
            RefreshDesktopIcons();
        }

        private void RefreshDesktopIcons()
        {
            preventWrites = true;
            LoadDesktopSettings();
            DesktopIconCountX.Text = desktopLayout.IconCountX.ToString();
            DesktopIconCountY.Text = desktopLayout.IconCountY.ToString();
            DesktopLockedCheck.IsChecked = desktopLayout.LockIcons;
            DesktopActionCheck.IsChecked = desktopLayout.ShowActions;
            DesktopIconPadding.Text = desktopLayout.IconPadding.ToString();
            DesktopIconSize.Text = desktopLayout.IconSize.ToString();
            DesktopLogoCheck.IsChecked = desktopLayout.ShowLogo;
            DesktopApps.Items.Clear();
            foreach (var di in desktopLayout.Children)
            {
                DesktopApps.Items.Add(di.Icon + ": " + di.Executable);
            }
            new Thread(() =>
            {
                Thread.Sleep(200);
                Dispatcher.UIThread.Post(() => { DesktopTab.IsEnabled = true;});
                preventWrites = false;
            }) {IsBackground = true}.Start();   
        }

        private async void DesktopApps_OnPointerPressed(object? sender, TappedEventArgs tappedEventArgs)
        {
            var li = tappedEventArgs.Source switch
            {
                TextBlock textBlock => textBlock.Parent as ContentPresenter,
                ContentPresenter cp => cp,
                _ => null
            };
            if (li is null)
            {
                return;
            }
            var dEdit = new DesktopIcon_Edit();
            var icon = li.Content?.ToString().Split(": ")[0];
            var uri = li.Content?.ToString().Split(": ")[1];
            foreach (var s in desktopIcons)
            {
                dEdit.NameBox.Items.Add(s);   
            }
            dEdit.NameBox.SelectedItem = dEdit.NameBox.Items[desktopIcons.IndexOf(icon)];
            dEdit.LocationBox.Text = uri;
            dEdit.Background = this.Background;
            dEdit.Foreground = this.Foreground;
            await dEdit.ShowDialog(this).WaitAsync(new CancellationToken(false));
            if (!dEdit.DialogResult) return;
            if (dEdit.NameBox.SelectedIndex != -1)
            {
                desktopLayout.Children[DesktopApps.SelectedIndex].Icon = dEdit.NameBox.SelectedItem.ToString()!;
                desktopLayout.Children[DesktopApps.SelectedIndex].Executable = dEdit.LocationBox.Text;
            }
            else
            {
                desktopLayout.Children = desktopLayout.Children.Where(w => w != desktopLayout.Children[DesktopApps.SelectedIndex]).ToArray();
            }
            SaveDesktopSettings();
            SendDesktopIconCommand("Reset", "true");
            RefreshDesktopIcons();
        }

        private void DesktopJSONEditButton_OnClick(object? sender, RoutedEventArgs e)
        {
            var p = new Process
            {
                StartInfo =
                {
                    UseShellExecute = true,
                    FileName = masRoot + "/DesktopIcons.json",
                }
            };
            p.Start();
        }

        private void DesktopIconsRestart_OnClick(object? sender, RoutedEventArgs e)
        {
            StopIcons();
            ShowIcons();
        }

        private void StopIcons()
        {
            foreach (var process in Process.GetProcesses())
            {
                if (process.ProcessName.StartsWith("DesktopIcons"))
                {
                    process.Kill();
                }
            }
        }

        private void ShowIcons()
        {
            var p = new Process
            {
                StartInfo = {
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    FileName = masRoot + "/Markuse asjad/DesktopIcons" + (OperatingSystem.IsWindows() ? ".exe" : ""),
                    RedirectStandardOutput = false,
                    RedirectStandardError = false,
                    RedirectStandardInput = false,
                    
                }
            };
            // some additional nonsense is required if we're not in Windows 
            if (!OperatingSystem.IsWindows())
            {
                p.StartInfo.Arguments = "-c \"nohup '" + p.StartInfo.FileName + "' > /dev/null 2>&1 &\"";
                p.StartInfo.FileName = "bash";
            }
            p.Start();
        }

        private void StartIntegrationSoftware()
        {
            var p = new Process
            {
                StartInfo = {
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    FileName = masRoot + "/Markuse asjad/Markuse arvuti integratsioonitarkvara" + (OperatingSystem.IsWindows() ? ".exe" : ""),
                    RedirectStandardOutput = false,
                    RedirectStandardError = false,
                    RedirectStandardInput = false,
                    
                }
            };
            // some additional nonsense is required if we're not in Windows 
            if (!OperatingSystem.IsWindows())
            {
                p.StartInfo.Arguments = "-c \"nohup '" + p.StartInfo.FileName + "' > /dev/null 2>&1 &\"";
                p.StartInfo.FileName = "bash";
            }
            p.Start();
        }

        private void DesktopIconsResetDefaults_OnClick(object? sender, RoutedEventArgs e)
        {
            if (File.Exists(masRoot + "/DesktopIcons.json"))
            {
                File.Delete(masRoot + "/DesktopIcons.json");
            }
            if (File.Exists(masRoot + "/DesktopIconsCommand.json"))
            {
                File.Delete(masRoot + "/DesktopIconsCommand.json");
            }

            DesktopIconsRestart_OnClick(sender, e);
        }

        private async void Window_OnClosing(object? sender, WindowClosingEventArgs e)
        {
            if (Debugger.IsAttached)
            {
                await MessageBoxShow("Lõpetan siluri", "Markuse arvuti juhtpaneel", ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Warning);
            }
            Environment.Exit(Environment.ExitCode);
        }

        private void ReloadInfo_OnClick(object? sender, RoutedEventArgs e)
        {
            WhatNewLabel.Text = "Mis on uut?";
            TabsControl.IsEnabled = false;
            TabsControl.IsVisible = false;
            Header1.IsVisible = false;
            CheckSysLabel.IsVisible = true;
            this.ErtGrid.Background = null;
            ThreadStart ts = CollectInfo;
            var t = new Thread(ts)
            {
                IsBackground = true
            };
            t.Start();
        }

        private void ErrorExitButton_OnClick(object? sender, RoutedEventArgs e)
        {
            Environment.Exit(Environment.ExitCode);
        }

        private async void DesktopIconsAddButton_OnClick(object? sender, RoutedEventArgs e)
        {
            var dEdit = new DesktopIcon_Edit();
            foreach (var s in desktopIcons)
            {
                dEdit.NameBox.Items.Add(s);   
            }

            dEdit.Background = this.Background;
            dEdit.Foreground = this.Foreground;
            dEdit.DeleteButton.IsVisible = false;
            await dEdit.ShowDialog(this).WaitAsync(new CancellationToken(false));
            if (!dEdit.DialogResult) return;
            try
            {
                desktopLayout.Children = desktopLayout.Children.Append(new DesktopIcon
                {
                    Icon = dEdit.NameBox.SelectedItem.ToString()!,
                    Executable = dEdit.LocationBox.Text!,
                    LocationX = -1,
                    LocationY = -1
                }).ToArray();
                SaveDesktopSettings();
                SendDesktopIconCommand("Reset", "true");
                RefreshDesktopIcons();
            }
            catch (Exception ex)
            {
                MessageBoxShow(ex.Message, "Viga", ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error);
            }
        }

        private void DesktopIconPadding_TextChanged(object? sender, TextChangedEventArgs e)
        {
            if (preventWrites) return;
            var cb = (TextBox?)e.Source;
            if (!int.TryParse(cb.Text, out var n)) return;
            LoadDesktopSettings();
            desktopLayout.IconPadding = n;
            SaveDesktopSettings();
            StopIcons();
            ShowIcons();
        }

        private void DesktopIconSize_TextChanged(object? sender, TextChangedEventArgs e)
        {
            if (preventWrites) return;
            var cb = (TextBox?)e.Source;
            if (!int.TryParse(cb.Text, out var n)) return;
            LoadDesktopSettings();
            desktopLayout.IconSize = n;
            SaveDesktopSettings();
            StopIcons();
            ShowIcons();
        }

        private void IntegrationPollrate_TextChanged(object? sender, TextChangedEventArgs e)
        {
            ConfigCheck(sender, e);
        }

        private void ScreensaverSettingsButton_OnClick(object? sender, RoutedEventArgs e)
        {
            if (!File.Exists(masRoot + "/Markuse asjad/Markuse arvuti ooterežiim" +
                             (OperatingSystem.IsWindows() ? ".scr" : "")))
            {
                MessageBoxShow("Ooterežiimi rakendust pole paigaldatud", Title, ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Warning);
                return;
            }

            var ps = WindowState;
            new Thread(() =>
            {
                Process p = new()
                {
                    StartInfo =
                    {
                        FileName = masRoot + "/Markuse asjad/Markuse arvuti ooterežiim" +
                                   (OperatingSystem.IsWindows() ? ".scr" : ""),
                        Arguments = "/c",
                        UseShellExecute = true,
                    }
                };
                p.Start();
                Dispatcher.UIThread.Post(() =>
                {
                    ErtGrid.IsEnabled = false;
                    IsVisible = false;
                    WindowState = WindowState.Minimized;
                });
                p.WaitForExit();
                Dispatcher.UIThread.Post(() =>
                {
                    ErtGrid.IsEnabled = true;
                    WindowState = ps;
                    IsVisible = true;
                });
            }).Start();
        }

        private void Image_DoubleTapped_1(object? sender, Avalonia.Input.TappedEventArgs e)
        {
        }

        private static string[] GetUniverse()
        {
            return Encoding.UTF8.GetString(Program.Universe.Reverse().ToArray()).Split(',');
        }
        
        private void InputElement_OnKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key is Key.LeftAlt or Key.RightAlt)
            {
                TipLabel.IsVisible = true;
                CloseButton.IsVisible = false;
            }
            pressedKeys.Add(e.Key);
            if (pressedKeys.Count <= 10) return;
            pressedKeys.RemoveAt(0);
            List<Key> anokim = [Key.Up, Key.Up, Key.Down, Key.Down, Key.Left, Key.Right, Key.Left, Key.Right, Key.B, Key.A];
            if (pressedKeys.Where((t, i) => t != anokim[i]).Any()) return;
            var r = new Random();
            var universeMembers = GetUniverse();
            foreach (var control in this.GetVisualDescendants())
            {
                var um = universeMembers[r.Next(0, universeMembers.Length - 1)]; 
                switch (control)
                {
                    case TextBox tb:
                        tb.Text = um;
                        break;
                    case Label lb:
                        lb.Content = um;
                        break;
                    case TextBlock tbl:
                        tbl.Text = um;
                        break;
                    case CheckBox cb:
                        cb.Content = um;
                        break;
                    case Button b:
                        b.Content = um;
                        break;
                    case ListBox lb:
                        for (var i = 0; i < lb.Items.Count; i++)
                        {
                            um = universeMembers[r.Next(0, universeMembers.Length - 1)];
                            lb.Items[i] = um;
                        }
                        break;
                }
            }
        }

        private void InputElement_OnPointerMoved(object? sender, PointerEventArgs e)
        {
            TipLabel.IsVisible = false;
            CloseButton.IsVisible = File.Exists(masRoot + "/irunning.log");
        }

        private void CloseButton_OnClick(object? sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}