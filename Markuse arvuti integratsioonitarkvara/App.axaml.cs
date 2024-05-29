using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System;
using System.Diagnostics;
using System.IO;

namespace Markuse_arvuti_integratsioonitarkvara
{
    public class App : Application
    {
        public string mas_root = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/.mas";
        public string fmount = "";
        public string FlashEnabled { get; set; } = "False";
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
            this.DataContext = this;
            fmount = GetMount();
            
            
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow();
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
                    p.StartInfo.FileName = fmount + "/Markuse m�lupulk/Markuse m�lupulk/bin/Debug/Markuse m�lupulk.exe";
                    p.StartInfo.WorkingDirectory = fmount + "/Markuse m�lupulk/Markuse m�lupulk/bin/Debug";
                    p.StartInfo.UseShellExecute = false;
                    p.Start();
                }
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
                Process p = new Process();
                p.StartInfo.UseShellExecute = true;
                p.StartInfo.FileName = mas_root + "/Markuse asjad/Markuse arvuti juhtpaneel/Markuse arvuti juhtpaneel";
                p.Start();
            }
        }
    }
}