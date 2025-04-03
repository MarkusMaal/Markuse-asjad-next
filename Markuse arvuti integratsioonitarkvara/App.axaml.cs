using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Avalonia.Controls.Notifications;
using DesktopNotifications;
using DesktopNotifications.Avalonia;
using INotificationManager = DesktopNotifications.INotificationManager;
using Notification = DesktopNotifications.Notification;

namespace Markuse_arvuti_integratsioonitarkvara
{
    public class App : Application
    {
        public string mas_root = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/.mas";
        public static string static_mas_root = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/.mas";
        readonly public static string[] whitelistedHashes = { "B881FBAB5E73D3984F2914FAEA743334D1B94DFFE98E8E1C4C8C412088D2C9C2", "A0B93B23301FC596789F83249A99F507A9DA5CBA9D636E4D4F88676F530224CB", "B08AABB1ED294D8292FDCB2626D4B77C0A53CB4754F3234D8E761E413289057F", "8076CF7C156D44472420C1225B9F6ADB661E3B095E29E52E3D4E8598BB399A8F" };
        public bool croot = false;
        public bool dev = false;
        public string fmount = "";
        public string FlashEnabled { get; set; } = "False";
        public bool showsplash = true;
        public string[]? specialevents = new string[1];
        internal string attestation = "BYPASS";
        static bool bad = false;
        private TrayIcon ti;
        private readonly INotificationManager _notificationManage = Program.notificationManager ??
                                                                    throw new InvalidOperationException("Missing notification manager");
        
        public override void Initialize()
        {
            try
            {
                AvaloniaXamlLoader.Load(this);
                this.DataContext = this;
                fmount = GetMount();
            }
            catch (Exception ex) when (!Debugger.IsAttached)
            {
                Program.CatchErrors(ex, "App.Initialize");
            }
        }

        public override void OnFrameworkInitializationCompleted()
        {
            try {
                if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                {
                    if (!dev || (desktop.Args!.Contains("/debug")))
                    {
                        dev = false;
                        desktop.MainWindow = new MainWindow();
                    }
                    else
                    {
                        desktop.MainWindow = new InterfaceTest();
                    }

                    if (!CheckVerifileTamper())
                    {
                        bad = true;
                        attestation = "CHECK_TAMPER";
                        desktop.MainWindow = null;
                    } else if (!File.Exists(mas_root + "/edition.txt"))
                    {
                        attestation = "FOREIGN";
                        desktop.MainWindow = null;
                    }
                    if (desktop?.Args?.Length > 0)
                    {
                        string[] args = desktop.Args;
                        if (args.Contains("/root"))
                        {
                            Console.WriteLine("Selle programmiga ei saa arvutit nullist juurutada. Palun kasutage /reroot parameetrit, et arvuti uuesti juurutada.");
                        }
                        else if (args.Contains("/reroot"))
                        {
                            foreach (Process p in Process.GetProcesses())
                            {
                                if (p.ProcessName == "JTR.exe" || p.ProcessName == "JTR.EXE" || p.ProcessName == "JTR") // veendume, et JTR oleks kindlasti avatud, vastasel juhul rikume Verifile sertifikaadi (mis pole hea btw)
                                {
                                    croot = true;
                                }
                            }
                            if (!croot)
                            {
                                Console.WriteLine("Juurutamiseks kasutage taasjuurutamise tööriista. Ilma taasjuurutamise tööriistata juurutamine võib põhjustada Verifile sertifikaadi riknemist.");
                            }
                        }
                    }
                    else
                    {
                        if (File.Exists(mas_root + "/edition.txt") && !bad)
                        {
                            attestation = Verifile2();
                            switch (attestation)
                            {
                                case "VERIFIED":
                                    break;
                                case "FOREIGN":
                                    Console.WriteLine("See programm töötab ainult Markuse arvutis.\nVeakood: VF_FOREIGN");
                                    break;
                                case "FAILED":
                                    Console.WriteLine("Verifile püsivuskontrolli läbimine nurjus.\nVeakood: VF_FAILED");
                                    break;
                                case "TAMPERED":
                                    Console.WriteLine("See arvuti pole õigesti juurutatud. Seda võis põhjustada hiljutine riistvaramuudatus. Palun kasutage juurutamiseks Markuse asjade juurutamistööriista.\nVeakood: VF_TAMPERED");
                                    break;
                                case "LEGACY":
                                    Console.WriteLine("See arvuti on juurutatud vana juurutamistööriistaga. Palun juurutage arvuti uuesti uue juurutamistarkvaraga.\nVeakood: VF_LEGACY");
                                    break;
                            }
                            if (!Verifile())
                            {
                                Console.WriteLine("Markuse asjad tarkvara ei ole õigesti juurutatud. Palun juurutage seade kasutades juurutamise tööriista.");
                                desktop.MainWindow = null;
                            }
                        }
                    }
                }


                base.OnFrameworkInitializationCompleted();
            }
            catch (Exception ex) when (!Debugger.IsAttached)
            {
                Program.CatchErrors(ex, "App.OnFrameworkInitializationCompleted");
            }
        }
        
        private void Folders_Click(object? sender, System.EventArgs e)
        {
            CookToast("Kodukasta avamine");
            Process p = new Process();
            p.StartInfo.FileName = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            p.StartInfo.UseShellExecute = true;
            p.Start();
        }

        public Bitmap GetResource(byte[] resource) {
            Bitmap icon;
            using (var ms = new MemoryStream(resource))
            {
                icon = new Bitmap(ms);
            }
            return icon;
        }

        public void RestartMainWindow() {
            
        }

        private void FlashUnlock_Click(object? sender, System.EventArgs e)
        {
            if (OperatingSystem.IsWindows())
            {
                bool none = true;
                foreach (Process p in Process.GetProcesses())
                {
                    if (p.ProcessName == "FlashUnlock")
                    {
                        none = false;
                        File.WriteAllText(mas_root + "/stop_authenticate", "now");
                    }
                }
                if (none)
                {
                    try
                    {
                        Process p = new Process();
                        p.StartInfo.FileName = mas_root + "/Markuse asjad/FlashUnlock.exe";
                        p.StartInfo.UseShellExecute = true;
                        p.StartInfo.Verb = "runas";
                        p.Start();
                    } catch (Exception) when (!Debugger.IsAttached) {
                        return;
                    }
                }
            }
        }

        private void FlashPanel_Click(object? sender, System.EventArgs e)
        {
            if (OperatingSystem.IsWindows())
            {
                if (File.Exists(mas_root + "/running.log"))
                {
                    File.Delete(mas_root + "/running.log");
                }
                if (fmount != "")
                {
                    Process p = new Process();
                    p.StartInfo.FileName = fmount + "/Markuse mälupulk/Markuse mälupulk/bin/Debug/Markuse mälupulk.exe";
                    p.StartInfo.WorkingDirectory = fmount + "/Markuse mälupulk/Markuse mälupulk/bin/Debug";
                    p.StartInfo.UseShellExecute = false;
                    p.Start();
                }
            } else if (OperatingSystem.IsLinux()) {
                if (fmount != "")
                {
                    Process p = new Process();
                    p.StartInfo.FileName = "java";
                    p.StartInfo.Arguments = "-jar " + fmount + "/.fdpanel/fdpanel.jar";
                    p.StartInfo.WorkingDirectory = fmount + "/.fdpanel";
                    p.StartInfo.UseShellExecute = false;
                    p.Start();
                }
            }
        }

        public void InitSettings()
        {
            if (File.Exists(mas_root + "/events.txt"))
            {
                string fcont = File.ReadAllText(mas_root + "/events.txt");
                specialevents = fcont.Split(';');
            }
            if (File.Exists(mas_root + "/Config.json"))
            {
                Program.config.Load(mas_root);
            }
            else if (File.Exists(mas_root + "/mas.cnf"))
            {
                // teisenda uueks formaadiks
                string[] incontent = File.ReadAllText(mas_root + "/mas.cnf").Split(';');
                Program.config.ShowLogo = incontent[0] == "true";
                Program.config.AllowScheduledTasks = incontent[1] == "true";
                Program.config.AutostartNotes = incontent[2] == "true";
                Program.config.PollRate = 5000;
                Program.config.Save(mas_root);
            }
            //kas lubada avateade
            showsplash = Program.config.ShowLogo;
            //kas lubada erisündmused
            if (!Program.config.AllowScheduledTasks)
            {
                specialevents = null;
            }
            if (OperatingSystem.IsWindows())
            {
                Process p = new();
                p.StartInfo.WorkingDirectory = mas_root;
                p.StartInfo.FileName = mas_root + "\\ChangeWallpaper.exe";
                p.StartInfo.Arguments = mas_root + "\\bg_desktop.png";
                p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.CreateNoWindow = true;
                p.Start();
            }
        }

        private static string GetMount()
        {
            string[] drives = Directory.GetLogicalDrives();
            string drv = "";
            foreach (string str in drives)
            {
                if (File.Exists(str + "/E_INFO/edition.txt"))
                {
                    drv = str; break;
                }
            }
            return drv;
        }

        public void ResetTrayIcon()
        {
            TrayIcon.SetIcons(this, TrayIcon.GetIcons(this));
        }
        
        public TrayIcon GetTrayIcon()
        {
            return TrayIcon.GetIcons(this)!.First();;
        }

        private void Cpanel_Clicked(object? sender, System.EventArgs e)
        {
            if (OperatingSystem.IsWindows())
            {
                Process.Start(mas_root + "/Markuse asjad/Markuse arvuti juhtpaneel.exe");
            } else
            {
                var p = new Process();
                p.StartInfo.UseShellExecute = !OperatingSystem.IsMacOS() ;
                p.StartInfo.FileName = mas_root + "/Markuse asjad/Markuse arvuti juhtpaneel";
                p.Start();
            }
        }


        private static bool CheckVerifileTamper()
        {
            if (!File.Exists(static_mas_root + "/verifile2.jar"))
            {
                Console.WriteLine("Verifile 2.0 tarkvara (verifile2.jar) ei ole Markuse asjade juurkaustas.\nVeakood: VF_MISSING");
                return false;
            }
            string hash = "";
            using (var sha256 = SHA256.Create())
            {
                using (var stream = File.OpenRead(static_mas_root + "/verifile2.jar"))
                {
                    hash = BitConverter.ToString(sha256.ComputeHash(stream));
                }
            }
            if (!whitelistedHashes.Contains(hash.Replace("-", "")))
            {
                Console.WriteLine("Arvuti püsivuskontrolli käivitamine nurjus. Põhjus: Verifile 2.0 räsi ei ole sobiv.");
                return false;
            }
            return true;
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
                    Arguments = "-jar " + mas_root + "/verifile2.jar",
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


        internal bool Verifile()
        {
            return Verifile2() == "VERIFIED";
        }

        private void PITS_Click(object? sender, System.EventArgs e)
        {
            CookToast("Interaktiivse töölaua laadimine...");
            if (OperatingSystem.IsWindows())
            {
                if (!File.Exists(mas_root + "/irunning.log"))
                {
                    Process.Start(mas_root + "/itstart.bat");
                }
                else
                {
                    Process p = new Process();
                    p.StartInfo.FileName = mas_root + "\\redoexp.cmd";
                    p.StartInfo.CreateNoWindow = true;
                    p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    p.Start();
                    File.WriteAllText(mas_root + "/closing.log", "");
                    if (File.Exists(mas_root + "/noteopen.txt"))
                    {
                        File.Delete(mas_root + "/noteopen.txt");
                        File.WriteAllText(mas_root + "/closenote.log", "See fail saadab töölauamärkmete rakendusele käskluse sulgeda. Kui te näete seda teksti, palun kustutage see fail.");
                    }
                }
            } else {
                Process.Start(mas_root + "/Markuse asjad/Interaktiivne töölaud");
            }
        }
        private void MS_Click(object? sender, System.EventArgs e)
        {
            CookToast("MarkuStation 2 käivitamine...");
            if (File.Exists(mas_root + "/Markuse asjad/MarkuStation2") || File.Exists(mas_root + "/Markuse asjad/MarkuStation2.exe"))
            {
                Process p = new Process();
                p.StartInfo.FileName = mas_root + "/Markuse asjad/MarkuStation2";
                p.StartInfo.UseShellExecute = !OperatingSystem.IsMacOS();
                if (OperatingSystem.IsWindows())
                {
                    p.StartInfo.FileName += ".exe";
                }
                p.Start();
            }
            else
            {
                if (OperatingSystem.IsWindows())
                {

                    Process p = new Process();
                    p.StartInfo.FileName = mas_root + "/MarkuStation.exe";
                    p.StartInfo.UseShellExecute = !OperatingSystem.IsMacOS();
                    p.StartInfo.Verb = "runas";
                    p.Start();
                }
            }
        }

        private void LockWorkstation_Click(object? sender, EventArgs e)
        {
            CookToast("Ekraani lukustamine...");
            var p = new Process();
            if (OperatingSystem.IsWindows()) {
                p.StartInfo.FileName = mas_root + "/Markuse asjad/Markuse arvuti lukustamissüsteem";
                if (OperatingSystem.IsWindows())
                {
                    p.StartInfo.FileName += ".exe";
                }
                p.StartInfo.UseShellExecute = false;
                p.Start();
            } else if (OperatingSystem.IsLinux()) {
                p.StartInfo.FileName = "loginctl";
                p.StartInfo.Arguments = "lock-session";
                p.Start();
            } else if (OperatingSystem.IsMacOS())
            {
                p.StartInfo.FileName = "pmset";
                p.StartInfo.Arguments = "displaysleepnow";
                p.Start();
            }
        }

        private void StickyNotes_Click(object? sender, EventArgs e)
        {
            if (OperatingSystem.IsWindows()) {
                if (!File.Exists(mas_root + "/noteopen.txt"))
                {
                    CookToast("Töölauamärkmete avamine...");
                    File.WriteAllText(mas_root + "/noteopen.txt", "See fail sisaldab informatsiooni töölauamärkmetega töötamiseks.");
                    Process.Start(mas_root + "/Markuse asjad/TöölauaMärkmed.exe");
                    ((NativeMenuItem)sender).Header = "Sulge töölauamärkmed";
                }
                else if (File.Exists(mas_root + @"\noteopen.txt"))
                {
                    CookToast("Töölauamärkmete sulgemine...");
                    File.Delete(mas_root + "/noteopen.txt");
                    File.WriteAllText(mas_root + "/closenote.log", "See fail saadab töölauamärkmete rakendusele käskluse sulgeda. Kui te näete seda teksti, palun kustutage see fail.");
                    ((NativeMenuItem)sender).Header = "Ava töölauamärkmed";
                }
            } else {
                if (!File.Exists(mas_root + "/noteopen.txt"))
                {
                    CookToast("Töölauamärkmete avamine...");
                    File.WriteAllText(mas_root + "/noteopen.txt", "See fail sisaldab informatsiooni töölauamärkmetega töötamiseks.");
                    Process.Start(mas_root + "/Markuse asjad/TöölauaMärkmed");
                    ((NativeMenuItem)sender).Header = "Sulge töölauamärkmed";
                }
                else if (File.Exists(mas_root + @"\noteopen.txt"))
                {
                    CookToast("Töölauamärkmete sulgemine...");
                    File.Delete(mas_root + "/noteopen.txt");
                    File.WriteAllText(mas_root + "/closenote.log", "See fail saadab töölauamärkmete rakendusele käskluse sulgeda. Kui te näete seda teksti, palun kustutage see fail.");
                    ((NativeMenuItem)sender).Header = "Ava töölauamärkmed";
                }
            }
        }

        private void NativeMenuItem_Click_1(object? sender, System.EventArgs e)
        {
            CookToast("Virtuaalarvuti käivitamine...");
            if (OperatingSystem.IsWindows())
            {
                Process p = new Process();
                string sdir = mas_root + "/vpc";
                string hypervisor = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) + "/VMware/VMware Player/vmware-kvm.exe";
                p.StartInfo.FileName = hypervisor;
                p.StartInfo.WorkingDirectory = sdir;
                p.StartInfo.Arguments = "\"Windows 11.vmx\"";
                p.Start();
                /*StartingVM svm = new StartingVM();
                svm.BackColor = avaMarkuseKaustadToolStripMenuItem.BackColor;
                svm.ForeColor = avaMarkuseKaustadToolStripMenuItem.ForeColor;
                svm.ShowDialog();
                svm.Dispose();*/
            } else if (OperatingSystem.IsLinux())
            {
                new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "sudo",
                        Arguments = "/usr/bin/virsh start win10",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden
                    }
                }.Start();
            } else {

                ((NativeMenuItem)sender).IsEnabled = false;
            }
        }

        public async void CookToast(string text)
        {
            try
            {
                var nf = new Notification
                {
                    Title = "Markuse arvuti integratsioonitarkvara",
                    Body = text
                };
                await _notificationManage.ShowNotification(nf);
                new Thread(() =>
                {
                    Thread.Sleep(10000);
                    _notificationManage.HideNotification(nf);
                }).Start();
            }
            catch (Exception) when (!Debugger.IsAttached)
            {
                // ignored
            }
        }

        private void Pidu_OnClick(object? sender, EventArgs e)
        {
            CookToast("Pidu algab nüüd!");
            new Process
            {
                StartInfo =
                {
                    FileName = mas_root + "/Markuse asjad/Pidu!",
                    UseShellExecute = false,
                }
            }.Start();
        }

        private void CloseMenu_Click(object? sender, EventArgs e)
        {
            
        }
    }
}