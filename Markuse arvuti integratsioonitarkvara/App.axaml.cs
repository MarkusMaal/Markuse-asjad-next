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
using System.IO;
using System.Linq;
using System.Security.Cryptography;

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
        public string[] specialevents = new string[1];
        public override void Initialize()
        {
            if (!CheckVerifileTamper())
            {
                return;
            }
            AvaloniaXamlLoader.Load(this);
            this.DataContext = this;
            fmount = GetMount();
            
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow();
                if (desktop?.Args?.Length > 0)
                {
                    string[] args = desktop.Args;
                    if (args.Contains("/root"))
                    {
                        Console.WriteLine("Selle programmiga ei saa arvutit nullist juurutada. Palun kasutage /reroot parameetrit, et arvuti uuesti juurutada.");
                    } else if (args.Contains("/reroot"))
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
                } else
                {
                    if (File.Exists(mas_root + "/edition.txt"))
                    {

                        switch (Verifile2())
                        {
                            case "VERIFIED":
                                break;
                            case "FOREIGN":
                                Console.WriteLine("See programm töötab ainult Markuse arvutis.\nVeakood: VF_FOREIGN");
                                return;
                            case "FAILED":
                                Console.WriteLine("Verifile püsivuskontrolli läbimine nurjus.\nVeakood: VF_FAILED");
                                return;
                            case "TAMPERED":
                                Console.WriteLine("See arvuti pole õigesti juurutatud. Seda võis põhjustada hiljutine riistvaramuudatus. Palun kasutage juurutamiseks Markuse asjade juurutamistööriista.\nVeakood: VF_TAMPERED");
                                return;
                            case "LEGACY":
                                Console.WriteLine("See arvuti on juurutatud vana juurutamistööriistaga. Palun juurutage arvuti uuesti uue juurutamistarkvaraga.\nVeakood: VF_LEGACY");
                                return;
                        }
                        if (!Verifile())
                        {
                            Console.WriteLine("Markuse asjad tarkvara ei ole õigesti juurutatud. Palun juurutage seade kasutades juurutamise tööriista.");
                            return;
                        }
                    }
                }
            }


            base.OnFrameworkInitializationCompleted();
        }

        private void Folders_Click(object? sender, System.EventArgs e)
        {
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
                    } catch {
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
            if (File.Exists(mas_root + "\\events.txt"))
            {
                string fcont = File.ReadAllText(mas_root + "\\events.txt");
                specialevents = fcont.Split(';');
            }
            if (File.Exists(mas_root + "/mas.cnf"))
            {
                string[] incontent = File.ReadAllText(mas_root + "/mas.cnf").Split(';');
                //kas lubada avateade
                showsplash = Convert.ToBoolean(incontent[0].ToString());
                //kas lubada erisündmused
                if (incontent[1].ToString() == "false")
                {
                    specialevents = null;
                }
            }
            if (OperatingSystem.IsWindows())
            {
                Process p = new Process();
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
        
        public TrayIcon GetTrayIcon()
        {
            return (TrayIcon)this.Resources["TrayIcon1"];
        }

        private void Cpanel_Clicked(object? sender, System.EventArgs e)
        {
            if (OperatingSystem.IsWindows())
            {
                Process.Start(mas_root + "/Markuse asjad/Markuse arvuti juhtpaneel.exe");
            } else
            {
                var p = new Process();
                p.StartInfo.UseShellExecute = true;
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


        private string Verifile2()
        {
            Process p = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "java",
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

            if (File.Exists(mas_root + "/Markuse asjad/MarkuStation2") || File.Exists(mas_root + "/Markuse asjad/MarkuStation2.exe"))
            {
                Process p = new Process();
                p.StartInfo.FileName = mas_root + "/Markuse asjad/MarkuStation2";
                p.StartInfo.UseShellExecute = true;
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
                    p.StartInfo.UseShellExecute = true;
                    p.StartInfo.Verb = "runas";
                    p.Start();
                }
            }
        }

        private void LockWorkstation_Click(object? sender, EventArgs e)
        {
            Process p = new Process();
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
            }
        }

        private void StickyNotes_Click(object? sender, EventArgs e)
        {
            if (OperatingSystem.IsWindows()) {
                if (!File.Exists(mas_root + "/noteopen.txt"))
                {
                    File.WriteAllText(mas_root + "/noteopen.txt", "See fail sisaldab informatsiooni töölauamärkmetega töötamiseks.");
                    Process.Start(mas_root + "/Markuse asjad/TöölauaMärkmed.exe");
                    ((NativeMenuItem)sender).Header = "Sulge töölauamärkmed";
                }
                else if (File.Exists(mas_root + @"\noteopen.txt"))
                {
                    File.Delete(mas_root + "/noteopen.txt");
                    File.WriteAllText(mas_root + "/closenote.log", "See fail saadab töölauamärkmete rakendusele käskluse sulgeda. Kui te näete seda teksti, palun kustutage see fail.");
                    ((NativeMenuItem)sender).Header = "Ava töölauamärkmed";
                }
            } else {
                if (!File.Exists(mas_root + "/noteopen.txt"))
                {
                    File.WriteAllText(mas_root + "/noteopen.txt", "See fail sisaldab informatsiooni töölauamärkmetega töötamiseks.");
                    Process.Start(mas_root + "/Markuse asjad/TöölauaMärkmed");
                    ((NativeMenuItem)sender).Header = "Sulge töölauamärkmed";
                }
                else if (File.Exists(mas_root + @"\noteopen.txt"))
                {
                    File.Delete(mas_root + "/noteopen.txt");
                    File.WriteAllText(mas_root + "/closenote.log", "See fail saadab töölauamärkmete rakendusele käskluse sulgeda. Kui te näete seda teksti, palun kustutage see fail.");
                    ((NativeMenuItem)sender).Header = "Ava töölauamärkmed";
                }
            }
        }

        private void NativeMenuItem_Click_1(object? sender, System.EventArgs e)
        {
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
            } else {
                ((NativeMenuItem)sender).IsEnabled = false;
            }
        }
    }
}