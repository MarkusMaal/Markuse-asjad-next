using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.OpenGL;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using Avalonia.Threading;
using MsBox.Avalonia;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Markuse_arvuti_juhtpaneel
{
    public partial class MainWindow : Window
    {
        string masRoot = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/.mas";
        Color[] scheme;
        int rotation = 0;
        string[] locations;
        bool freezeTimer = false;
        DispatcherTimer dispatcherTimer2 = new DispatcherTimer();
        DispatcherTimer rotateLogo = new DispatcherTimer();
        readonly string whatNew = "+ Linuxi tugi\n+ Kuvatõmmise tegemine Spectacle abiga (Linux)\n- Eemaldatud kuvatõmmise nupud Windowsi versioonist";
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
                p.Start();
            } else if (OperatingSystem.IsLinux()) {
               // stop existing Python processes
                Console.WriteLine("kill Python");
                RunCommand("pkill", "python");
                RunCommand("pkill", "python3");
               // Restart integration software and maia server
                Console.WriteLine("start maia server");
                RunCommand("bash", masRoot + "/maia_autostart.sh");
                Console.WriteLine("start integration software");
                RunCommand("bash", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/markuseasjad/maswelcome.sh", false);
                // Restart KDE Plasma
                Console.WriteLine("restart plasma");
                RunCommand("bash", masRoot + "/restart_plasma.sh");
            } else if (OperatingSystem.IsMacOS()) {
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
            }
            else
            {
                // Unsupported OS, display an error message
                NotWindows();
            }
        }

        private void InfoText(object? sender, PointerEventArgs e)
        {
            string devPrefix = "Markuse arvuti asjade";
            if (Directory.Exists(masRoot + "/.masv"))
            {
                devPrefix = "Markuse virtuaalarvuti asjade";
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


        private string Verifile2()
        {
            Process p = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "java",
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
            return Verifile2() == "VERIFIED";
        }

        private void InitTimers()
        {
            DispatcherTimer dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(CheckTheme);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 5);
            dispatcherTimer.Start();
            dispatcherTimer2.Tick += new EventHandler(CollectInfo);
            dispatcherTimer2.Interval = new TimeSpan(0, 0, 0);
            dispatcherTimer2.Start();
            rotateLogo.Tick += new EventHandler(RotateLogo);
            rotateLogo.Interval = new TimeSpan(0, 0, 0, 0, 16);
        }

        private void CollectInfo(object sender, EventArgs e)
        {
            dispatcherTimer2.Stop();
            string devPrefix = "Markuse arvuti asjade";
            if (Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/.masv"))
            {
                devPrefix = "Markuse virtuaalarvuti asjade";
            }
            if (File.Exists(masRoot + "/mas.cnf"))
            {
                string[] cnfs = File.ReadAllText(masRoot + "/mas.cnf").Split(';');
                ShowMasLogoCheck.IsChecked = cnfs[0].ToString() == "true";                  // Kuva Markuse asjade logo integratsioonitarkvara käivitumisel
                AllowScheduledTasksCheck.IsChecked = cnfs[1].ToString() == "true";          // Käivita töölauamärkmed arvuti käivitumisel
                StartDesktopNotesCheck.IsChecked = cnfs[2].ToString() == "true";            // Käivita töölauamärkmed arvuti käivitumisel
            }
            else
            {
                MessageBoxShow(devPrefix + " tarkvara ei ole juurutatud. Palun juurutage seade kasutades juurutamise tööriista.", "Markuse asjade tarkvara pole paigaldatud", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error);
                this.Close();
            }
            if (File.Exists(masRoot + "/edition.txt"))
            {

                switch (Verifile2())
                {
                    case "VERIFIED":
                        break;
                    case "FOREIGN":
                        Console.WriteLine("See programm töötab ainult Markuse arvutis.\nVeakood: VF_FOREIGN");
                        this.Close();
                        return;
                    case "FAILED":
                        Console.WriteLine("Verifile püsivuskontrolli läbimine nurjus.\nVeakood: VF_FAILED");
                        this.Close();
                        return;
                    case "TAMPERED":
                        Console.WriteLine("See arvuti pole õigesti juurutatud. Seda võis põhjustada hiljutine riistvaramuudatus. Palun kasutage juurutamiseks Markuse asjade juurutamistööriista.\nVeakood: VF_TAMPERED");
                        this.Close();
                        return;
                    case "LEGACY":
                        Console.WriteLine("See arvuti on juurutatud vana juurutamistööriistaga. Palun juurutage arvuti uuesti uue juurutamistarkvaraga.\nVeakood: VF_LEGACY");
                        this.Close();
                        return;
                }
                if (Verifile())
                {
                    scheme = LoadTheme();
                    ApplyTheme();
                    this.IsVisible = true;
                    GetEditionInfo();

                    this.Title = "Markuse arvuti juhtpaneel";
                    if (Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/.masv"))
                    {
                        TopLabel.Text = "markuse virtuaalarvuti juhtpaneel";
                        this.Title = "Markuse virtuaalarvuti juhtpaneel";
                    }
                    if (File.Exists(masRoot + "/irunning.log"))
                    {
                        // Projekt ITS aktiivne
                        WindowState = WindowState.FullScreen;
                    }
                    TabsControl.IsEnabled = true;
                }
                else
                {
                    Console.WriteLine(devPrefix + " tarkvara ei ole õigesti juurutatud. Palun juurutage seade kasutades juurutamise tööriista.");
                    this.Close();
                    return;
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
            TransformGroup tg = new TransformGroup();
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
                MarkuStation_Edit mse = new MarkuStation_Edit();
                mse.NameBox.Text = SelectedGame;
                mse.LocationBox.Text = GameLocation;
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
                if ((val != "") && (val != null))
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
            string saveprog = "";
            saveprog += (bool)ShowMasLogoCheck.IsChecked ? "true;" : "false;";
            saveprog += (bool)AllowScheduledTasksCheck.IsChecked ? "true;" : "false;";
            saveprog += (bool)StartDesktopNotesCheck.IsChecked ? "true;" : "false;";
            File.WriteAllText(masRoot + "/mas.cnf", saveprog);
        }

        private void ReloadThumbs()
        {
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
            var topLevel = TopLevel.GetTopLevel(this);
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

        private async void EditBg(object sender, RoutedEventArgs e)
        {
            ColorPickerDialog cpd = new ColorPickerDialog();
            cpd.Color.Color = scheme[0];
            await cpd.ShowDialog(this);
            this.scheme[0] = cpd.Color.Color;
            if (cpd.result)
            {
                freezeTimer = true;
                File.WriteAllText(masRoot + "/scheme.cfg", cpd.Color.Color.R.ToString() + ":" + cpd.Color.Color.G.ToString() + ":" + cpd.Color.Color.B.ToString() + ":;" + this.scheme[1].R + ":" + this.scheme[1].G + ":" + this.scheme[1].B + ":;");
                freezeTimer = false;
                LoadTheme();
                ApplyTheme();
            }
        }

        private async void EditFg(object sender, RoutedEventArgs e)
        {
            ColorPickerDialog cpd = new ColorPickerDialog();
            cpd.Color.Color = scheme[1];
            await cpd.ShowDialog(this);
            this.scheme[1] = cpd.Color.Color;
            if (cpd.result)
            {
                freezeTimer = true;
                File.WriteAllText(masRoot + "/scheme.cfg", this.scheme[0].R.ToString() + ":" + this.scheme[0].G.ToString() + ":" + this.scheme[0].B.ToString() + ":;" + cpd.Color.Color.R + ":" + cpd.Color.Color.G + ":" + cpd.Color.Color.B + ":;");
                freezeTimer = false;
                LoadTheme();
                ApplyTheme();
            }
        }

        /* Teave */

        private void GetEditionInfo()
        {
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
    }
}