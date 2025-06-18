using Avalonia.Controls;
using Avalonia.Threading;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace Markuse_arvuti_integratsioonitarkvara
{
    internal class Watchers
    {
        private MainWindow uiForm;
        private FileSystemWatcher checkMasTrigger;
        private FileSystemWatcher checkMaiaTrigger;
        private string mas_root;

        // Constructor
        public Watchers(MainWindow mw, string mas_root)
        {
            this.uiForm = mw;
            this.mas_root = BackForwardSlash(mas_root.Replace("/", "\\"));
            this.checkMasTrigger = new FileSystemWatcher(this.mas_root);
            this.checkMaiaTrigger = new FileSystemWatcher(this.mas_root + BackForwardSlash("\\maia"));
            InitializeWatcher("*.*", this.checkMasTrigger, new FileSystemEventHandler(CheckMasFiles));
            InitializeWatcher("*.*", this.checkMaiaTrigger, new FileSystemEventHandler(CheckMaiaFiles));
        }

        // Create a new watcher
        private void InitializeWatcher(string filename, FileSystemWatcher watcher, FileSystemEventHandler fn)
        {
            watcher.NotifyFilter = /*NotifyFilters.Attributes
                                   | NotifyFilters.CreationTime
                                   | NotifyFilters.DirectoryName
                                   | */NotifyFilters.FileName
                                   /*| NotifyFilters.LastAccess*/
                                   | NotifyFilters.LastWrite
                                   /*| NotifyFilters.Security*/
                                   | NotifyFilters.Size;
            watcher.Changed += fn;
            watcher.Error += new ErrorEventHandler(OnError);
            watcher.Filter = filename;
            watcher.IncludeSubdirectories = false;
            watcher.EnableRaisingEvents = true;
        }

        // File system event handlers
        private void CheckMasFiles(object sender, FileSystemEventArgs e)
        {
            try
            {
                switch (e.Name)
                {
                    case "showabout.txt":
                        CheckAboutFiles(sender, e);
                        break;
                    case "scheme.cfg":
                        CheckScheme(sender, e);
                        break;
                    case "Config.json":
                        ReloadConfig(sender, e);
                        break;
                    case "edition.txt":
                    case "verifile2.dat":
                        CheckVerifile(sender, e);
                        break;
                    default:
                        Dispatcher.UIThread.Post(() => uiForm.ReloadMenu());
                        break;
                }
            } catch (Exception ex) when (!Debugger.IsAttached)
            {
                Program.CatchErrors(ex, "Watchers.CheckMasFiles");
            }
        }

        private void CheckVerifile(object sender, FileSystemEventArgs e)
        {
            uiForm.CheckVerifile();
        }

        private void ReloadConfig(object sender, FileSystemEventArgs e)
        {
            Program.config.Load(mas_root);
            Dispatcher.UIThread.Post(() =>
            {
                uiForm.IsVisible = false;
                if (OperatingSystem.IsMacOS())
                {
                    uiForm.WindowState = WindowState.Minimized;
                    uiForm.Width = 0;
                    uiForm.Height = 0;
                }
                if (Program.config.AllowScheduledTasks)
                {
                    uiForm.dispatcherTimer.Start();
                }
                else
                {
                    uiForm.dispatcherTimer.Stop();
                }
                uiForm.ReloadMenu();
            });
        }

        private void CheckScheme(object sender, FileSystemEventArgs e)
        {
            if (!e.FullPath.EndsWith(BackForwardSlash("\\scheme.cfg"))) return;
            uiForm.scheme = uiForm.LoadTheme();
            uiForm.ApplyTheme();
        }

        private void CheckAboutFiles(object sender, FileSystemEventArgs e)
        {
            if (!e.FullPath.EndsWith(BackForwardSlash("\\showabout.txt"))) return;
            if (File.Exists(mas_root + "/showabout.txt"))
            {
                File.Delete(mas_root + "/showabout.txt");
                //teaveMarkuseAsjadeKohtaToolStripMenuItem.PerformClick();
            }
        }

        private void CheckMaiaFiles(object sender, FileSystemEventArgs e)
        {
            try
            {
                if (!e.FullPath.EndsWith(BackForwardSlash("\\request_permission.maia")) || (e.ChangeType == WatcherChangeTypes.Deleted)) return;
                // M.A.I.A. ligipääsu taotlemine
                if (File.Exists(mas_root + @"/maia/request_permission.maia") || File.Exists(mas_root + "/maia/request_permission.mai"))
                {
                    if (uiForm.allowCode)
                    {
                        if (Program.CodeOpen) return;
                        Dispatcher.UIThread.Post(() =>
                        {
                            ShowCode sc = new()
                            {
                                bg = uiForm.scheme[0],
                                fg = uiForm.scheme[1]
                            };
                            sc.Show(); 
                        });
                    }
                    else
                    {
                        try { File.Delete(mas_root + "/maia/request_permission.maia"); } catch (Exception) when (!Debugger.IsAttached) { File.Delete(mas_root + "/maia/request_permission.mai"); }
                    }
                }
            }
            catch (Exception ex) when (!Debugger.IsAttached)
            {
                Program.CatchErrors(ex, "Watchers.CheckMaiaFiles");
            }
        }

        // Error handlers
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
            NotAccessibleError((FileSystemWatcher)source, e);
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
                    Thread.Sleep(iTimeOut);
                }
            }

        }

        // change backslashes to forward slashes in case we're not in Windows
        private static string BackForwardSlash(string path)
        {
            if (!OperatingSystem.IsWindows())
            {
                path = path.Replace("\\", "/");
            }
            return path;
        }
    }
}
