﻿using System.Diagnostics;

namespace MineGeneraator
{
    internal static class Program
    {
        private static List<DirectoryInfo> CheckDirs = [];
        private static string[] BlackListedDirs = ["proc", "usr", "sys", "boot", "dev", "etc", "opt", "root", "var", "home/timeshift"];
        private static string MineRoot = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/Mine/";
        public static void Main(string[] args)
        {
            PadFill("Otsin .mine.json faile...");
            if (args.Length == 0)
            {
                _ = OperatingSystem.IsWindows() ? RecurseFolders(Environment.GetEnvironmentVariable("HOMEDRIVE") ?? string.Empty) : RecurseFolders("/");
            } else
            {
                _ = RecurseFolders(args[0]);
            }
            PadFill("Kustutan olemasoleva Mine kataloogi...");
            if (OperatingSystem.IsWindows()) MineRoot = MineRoot.Replace("/", "\\");
            try
            {
                if (Directory.Exists(MineRoot)) Directory.Delete(MineRoot, true);
                Directory.CreateDirectory(MineRoot);
            } catch
            {
                // ignored
            }
            foreach (var dir in CheckDirs)
            {
                var fa = new FolderAlias();
                fa.Load(dir.FullName + "/.mine.json");
                MakeSymlinks("", fa, dir);
            }
        }

        private static void PadFill(string str)
        {
            var outText = $"{str}";
            var spaces = "";
            for (var i = 0; i < Console.WindowWidth - outText.Length; i++) spaces += " ";
            if (outText.Length > Console.WindowWidth - 3)
            {
                outText = $"{str[..(Console.WindowWidth - 5)]}...";
            }
            Console.Write($"\r{outText}{spaces}");
        }

        private static void MakeSymlinks(string prefix, FolderAlias folder, DirectoryInfo root)
        {
            var FinalName = (folder.alias != "" ? folder.alias : folder.name).Replace("$PWD", root.Name);
            foreach (var fa in folder.subdirs)
            {
                MakeSymlinks(prefix + FinalName + " - ", fa, new DirectoryInfo(root.FullName+ "/" + fa.name));
            }
            PadFill($"{root.FullName} => {MineRoot}{prefix}{FinalName}");
            if (OperatingSystem.IsWindows())
            {
                var psi = new ProcessStartInfo("cmd.exe", "/C mklink /J \"" + MineRoot.Replace("/", "\\") + prefix.Replace("/", "\\") + FinalName + "\" \"" + root.FullName.Replace("/", "\\") + "\"")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                Process.Start(psi);
                return;
            }
            File.CreateSymbolicLink(MineRoot + prefix + FinalName, root.FullName);
        }

        private static int RecurseFolders(string path)
        {
            try
            {
                PadFill(path);
                foreach (var dir in Directory.GetDirectories(path))
                {
                    var cloop = false;
                    foreach (var s in BlackListedDirs)
                    {
                        if (dir.StartsWith("/" + s, StringComparison.InvariantCultureIgnoreCase)) cloop = true;
                    }
                    if (cloop) continue;
                    if (dir.Contains("/.")) continue;
                    if (dir.Contains("/markuse asjad/markuse asjad/")) continue;
                    var directoryInfo = new DirectoryInfo(dir);
                    if (directoryInfo.Attributes.HasFlag(FileAttributes.ReparsePoint))
                    {
                        continue;
                    }
                    if (File.Exists(dir + "/.mine.json"))
                    {
                        CheckDirs.Add(directoryInfo);
                    }
                    RecurseFolders(dir);
                }

                return 0;
            }
            catch (IOException)
            {
                return 2;
            }
            catch (UnauthorizedAccessException)
            {
                return 1;
            }
        }
    }
}