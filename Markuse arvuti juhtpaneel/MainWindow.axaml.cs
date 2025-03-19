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
using DesktopIcons;
using MsBox.Avalonia.Enums;
using Markuse_arvuti_juhtpaneel.IntegrationSoftware;
using System.Globalization;

namespace Markuse_arvuti_juhtpaneel
{
    public partial class MainWindow : Window
    {
        string masRoot = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/.mas";
        Color[] scheme;
        int rotation = 0;
        string[] locations;
        bool freezeTimer = false;
        private bool preventWrites = true;
        DispatcherTimer dispatcherTimer2 = new();
        DispatcherTimer rotateLogo = new();
        readonly string whatNew = "+ Töölauaikoonide kohandamine\n+ Laadimisekraan info kogumise ajal";
        private readonly JsonSerializerOptions _serializerOptions = new() { WriteIndented = true, TypeInfoResolver = DesktopLayoutSourceGenerationContext.Default};
        private readonly JsonSerializerOptions _cmdSerializerOptions = new() { WriteIndented = true, TypeInfoResolver = CommandSourceGenerationContext.Default };
        private List<string> desktopIcons = [];
        DesktopLayout? desktopLayout;
        MasConfig config = new();
        private bool LaunchError = false;
        public MainWindow()
        {
            InitializeComponent();
            InitResources();
            InitButtons();
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
            hints["Laadi andmed uuesti"] = "Laadib seadistused uuesti, juhul, kui need peaksid olema muutunud";
            this.InfoTextBlock.Text = hints[(string?)((Button)e.Source).Content];
        }

        private void DefaultInfo(object? sender, PointerEventArgs e)
        {
            this.InfoTextBlock.Text = "Siin kuvatakse teave, kui liigutate kursori teatud nupu peale.";
        }

        private void ScreenshotNow(object? sender, RoutedEventArgs e) {
            string year = DateTime.Now.Year.ToString();
            string month = DateTime.Now.Month.ToString();
            string day = DateTime.Now.Day.ToString();
            string currentDate = year + month.PadLeft(2, '0') + day.PadLeft(2, '0');
            string currentTime = DateTime.Now.ToLongTimeString().Replace(":", "");
            string filename = "Screenshot_" + currentDate + "_" + currentTime + ".png";
            if (OperatingSystem.IsLinux())
            {
                // Must have spectacle installed on KDE Plasma, other DEs and WMs not supported
                RunCommand("spectacle", "-f -b -o \"" + Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/Pildid/Screenshots/" + filename + "\"", false);
            } else if (OperatingSystem.IsWindows())
            {
                filename = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures) + "/" + filename;
                Process p = new Process();
                p.StartInfo.FileName = "powershell";
                p.StartInfo.Arguments = masRoot.Replace("/", "\\") + "\\ScreenShot.ps1 -FileName \"" + filename.Replace("/", "\\") + "\"";
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                p.Start();
                p.WaitForExit();
                MessageBoxShow("Kuvatõmmis salvestati edukalt", "Markuse arvuti juhtpaneel", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Success);
            } else if (OperatingSystem.IsMacOS()) {
                // 1 second delay, because we want to wait for the button to exit the "pressed" state
                RunCommand("screencapture", "-T 1 \"" + Environment.GetFolderPath(Environment.SpecialFolder.MyPictures) + "/" + filename + "\"", false);
            }
        }

        private async void ScreenshotAs(object? sender, RoutedEventArgs e) {
            string year = DateTime.Now.Year.ToString();
            string month = DateTime.Now.Month.ToString();
            string day = DateTime.Now.Day.ToString();
            string currentDate = year + month.PadLeft(2, '0') + day.PadLeft(2, '0');
            string currentTime = DateTime.Now.ToLongTimeString().Replace(":", "");
            string filename = "Screenshot_" + currentDate + "_" + currentTime + ".png";
        
            // Get top level from the current control. Alternatively, you can use Window reference instead.
            var topLevel = TopLevel.GetTopLevel(this);

            // Start async operation to open the dialog.
            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Salvesta kuvatõmmis nimega",
                SuggestedFileName = filename,
            });

            if (file is not null)
            {
                filename = file.Path.AbsolutePath;
                Thread.Sleep(2000);
                if (OperatingSystem.IsWindows())
                {
                    Process p = new Process();
                    p.StartInfo.FileName = "powershell";
                    p.StartInfo.Arguments = masRoot.Replace("/", "\\") + "\\ScreenShot.ps1 -FileName \"" + filename.Replace("/", "\\") + "\"";
                    p.StartInfo.CreateNoWindow = true;
                    p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    p.Start();
                    p.WaitForExit();
                    _ = MessageBoxShow("Kuvatõmmis salvestati edukalt", "Markuse arvuti juhtpaneel", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Success);
                }
                else if (OperatingSystem.IsLinux())
                {
                    RunCommand("spectacle", "-f -b -o \"" + filename + "\"", false);
                } else if (OperatingSystem.IsMacOS()) {
                    // 1 second delay to wait for the save as dialog to close
                    RunCommand("screencapture", "-T 1 \"" + filename + "\"", false);
                }
        
            }

        }


        /* Misc functions */

        // Reimplementation of WinForms MessageBox.Show
        private Task MessageBoxShow(string message, string caption = "Markuse arvuti juhtpaneel", MsBox.Avalonia.Enums.ButtonEnum buttons = MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon icon = MsBox.Avalonia.Enums.Icon.None)
        {
            var box = MessageBoxManager.GetMessageBoxStandard(caption, message, buttons, icon);
            var result = box.ShowWindowDialogAsync(this);
            return result;
        }

        private void NotWindows()
        {
            MessageBoxShow("Seda funktsiooni selles operatsioonsüsteemis veel ei toetata", "Markuse arvuti juhtpaneel", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error);
        }

        
        
        private void RunCommand(string command, string args, bool waitForExit = true) {
            Process p = new Process();
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
                Process p = new Process();
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
            string[] bgfg = File.ReadAllText(masRoot + "/scheme.cfg").Split(';');
            string[] bgs = bgfg[0].ToString().Split(':');
            string[] fgs = bgfg[1].ToString().Split(':');
            Color[] cols = { Color.FromArgb(255, byte.Parse(bgs[0]), byte.Parse(bgs[1]), byte.Parse(bgs[2])), Color.FromArgb(255, byte.Parse(fgs[0]), byte.Parse(fgs[1]), byte.Parse(fgs[2])) };
            return cols;
        }

        void ApplyTheme()
        {
            this.Background = new SolidColorBrush(scheme[0]);
            this.Foreground = new SolidColorBrush(scheme[1]);
            ImageBrush IB = new ImageBrush(new Bitmap(masRoot + "/bg_common.png"));
            IB.Stretch = Stretch.Fill;
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
                Color[] tempScheme = LoadTheme();
                if (tempScheme != scheme)
                {
                    scheme = LoadTheme();
                    ApplyTheme();
                }
            }
            catch
            {

            }
        }

        private void StopThread(Thread th)
        {
            th.Interrupt();
            th.Join();
        }

        /// <summary>
        /// Builds a script that displays all Java binaries and versions for your system and marks it executable (Unix-like systems)
        /// </summary>
        private void BuildJavaFinder()
        {
            if (!File.Exists(masRoot + "/find_java" + (OperatingSystem.IsWindows() ? ".bat" : ".sh")))
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
                File.WriteAllText(masRoot + "/find_java" + (OperatingSystem.IsWindows() ? ".bat" : ".sh"), builder.ToString(), Encoding.ASCII);
                if (!OperatingSystem.IsWindows())
                {
                    File.SetUnixFileMode(masRoot + "/find_java.sh", UnixFileMode.UserRead | UnixFileMode.UserExecute | UnixFileMode.UserWrite);
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
                masRoot = masRoot.Replace("/", "\\");
            }
            Process pr = new()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = interpreter,
                    Arguments = (OperatingSystem.IsWindows() ? "/c " : "") + "\"" + masRoot + (OperatingSystem.IsWindows() ? "\\" : "/") + "find_java." + (OperatingSystem.IsWindows() ? "bat" : "sh") + "\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                }
            };
            pr.Start();
            while (!pr.StandardOutput.EndOfStream)
            {
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
                    Arguments = "-jar " + masRoot + "/verifile2.jar",
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


        private bool Verifile()
        {
            return Verifile2() == "VERIFIED" ||  Verifile2() == "BYPASS";
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
            rotateLogo.Tick += new EventHandler(RotateLogo);
            rotateLogo.Interval = new TimeSpan(0, 0, 0, 0, 16);
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
            SetCollectProgress(1, "Konfiguratsiooni laadimine...");
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
            SetCollectProgress(10, "Väljaande info kogumine...");
            if (File.Exists(masRoot + "/edition.txt"))
            {

                switch (Verifile2())
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
                SetCollectProgress(25, "Verifile OK");
                if (Verifile())
                {
                    SetCollectProgress(30, "Teema laadimine...");
                    scheme = LoadTheme();
                    SetCollectProgress(50, "Töölauaikooni seadete laadimine...");
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
                    var VF_STATUS = Verifile2();
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
            if (rotateLogo.IsEnabled)
            {
                rotateLogo.Stop();
            } else
            {
                rotateLogo.Start();
            }
        }

        private void RotateLogo(object sender, EventArgs e)
        {
            rotation += 3;
            if (rotation % 90 == 0)
            {
                rotateLogo.Stop();
            }
            var tg = new TransformGroup();
            tg.Children.Add(new RotateTransform(rotation));
            Logo.RenderTransform = tg;
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
                this.LocationBox.Text = files[0].Path.AbsolutePath;
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
                    }
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
                Process pr = new Process();
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
                foreach (Process p in Process.GetProcesses())
                {
                    if ((p.ProcessName == "cmd.exe") || (p.ProcessName == "cmd.EXE") || (p.ProcessName == "cmd"))
                    {
                        p.Kill();
                    }
                    else if ((p.ProcessName == "conhost.exe") || (p.ProcessName == "conhost.EXE") || (p.ProcessName == "conhost"))
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
            string filename = files[0].Path.AbsolutePath;
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
            string filename = files[0].Path.AbsolutePath;
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
                Process pr = new Process();
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
                foreach (Process p in Process.GetProcesses())
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
                    Process p = new Process();
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
            Command cmd = new()
            {
                Arguments = args,
                Type = type
            };
            var jsonData = JsonSerializer.Serialize(cmd, _cmdSerializerOptions);
            File.WriteAllText(masRoot + "/DesktopIconsCommand.json", jsonData);
        }

        private async void EditBg(object sender, RoutedEventArgs e)
        {
            if (Program.Launcherror) return;
            ColorPickerDialog cpd = new ColorPickerDialog
            {
                Color =
                {
                    Color = scheme[0]
                }
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
            ColorPickerDialog cpd = new ColorPickerDialog();
            cpd.Color.Color = scheme[1];
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
            if (!Verifile())
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
            StringBuilder editionDetails = new StringBuilder();
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
            editionDetails.AppendLine("Olek: " + Verifile2());
            EditionDetails.Text = editionDetails.ToString();
            string[] features = masVer[8].Split('-');
            FeatTS.Source = cross;
            FeatRM.Source = cross;
            FeatIP.Source = cross;
            FeatCS.Source = cross;
            FeatMM.Source = cross;
            FeatRD.Source = cross;
            FeatWX.Source = cross;
            FeatLT.Source = cross;
            FeatGP.Source = cross;
            foreach (string feature in features)
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
            string[] fullVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString().Split(".");
            CpanelVersionLabel.Content = "versioon " + fullVersion[0] + "." + fullVersion[1];
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
            Process p = new Process();
            p.StartInfo = new ProcessStartInfo(masRoot)
            {
                UseShellExecute = true,
            };
            p.Start();
        }

        private void ReRootClicked(object sender, RoutedEventArgs e)
        {
            if (OperatingSystem.IsWindows())
            {
                Process.Start(masRoot + "/Markuse asjad/JTR.exe");
                this.Close();
            }
            else
            {
                MessageBoxShow("Juurutamine selles Markuse arvuti asjade versioonis on võimalik ainult Windowsis", "Markuse arvuti juhtpaneel", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error);
            }
        }

        private static bool IsAppleSilicon() {
            return OperatingSystem.IsMacOS() && (System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture == System.Runtime.InteropServices.Architecture.Arm64);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            InitTimers();
            TabMarkuStation.IsVisible = !IsAppleSilicon(); // LibVLC is currently unsupported on Apple Silicon, so any Markus' stuff that leverages it (incl. MarkuStation 2) will not work.
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
            File.WriteAllText(masRoot + "/DesktopIcons.json", jsonData, encoding: Encoding.UTF8);
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
    }
}