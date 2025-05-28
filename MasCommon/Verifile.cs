using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;

namespace MasCommon;

/// <summary>
/// Verifile integrity checking system
/// </summary>
public class Verifile
{
    private static string root = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/.mas";
    private static readonly string[] whitelistedHashes = { "B881FBAB5E73D3984F2914FAEA743334D1B94DFFE98E8E1C4C8C412088D2C9C2", "A0B93B23301FC596789F83249A99F507A9DA5CBA9D636E4D4F88676F530224CB", "B08AABB1ED294D8292FDCB2626D4B77C0A53CB4754F3234D8E761E413289057F", "8076CF7C156D44472420C1225B9F6ADB661E3B095E29E52E3D4E8598BB399A8F" };
    
    private DateTime LastCheck = DateTime.MinValue;
    private string AttestationState = "BYPASS"; // save attestation state for performance reasons
    
    public enum FileScope
    {
        IntegrationSoftware,
        ControlPanel,
        MarkuStation,
        Screensaver,
        InteractiveDesktop,
        DesktopIcons,
        DesktopNotes
    }
    /// <summary>
    /// Create new verifile instance
    /// </summary>
    public Verifile() {}
    
    /// <summary>
    /// Check for tampering of verifile2.jar file
    /// </summary>
    /// <returns>true if the file has the correct signature, otherwise false</returns>
    public static bool CheckVerifileTamper()
    {
        if (!File.Exists(root + "/verifile2.jar"))
        {
            Console.WriteLine("Verifile 2.0 tarkvara (verifile2.jar) ei ole Markuse asjade juurkaustas.\nVeakood: VF_MISSING");
            return false;
        }
        string hash = "";
        using (var sha256 = SHA256.Create())
        {
            using (var stream = File.OpenRead(root + "/verifile2.jar"))
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
    /// Performs Verifile 2.0 attestation
    /// </summary>
    /// <returns>Attestation result as string specified by the Verifile 2.0 standard</returns>
    public string MakeAttestation()
    {
        if (DateTime.Now - LastCheck <= new TimeSpan(0, 1, 0)) return AttestationState;
        if (Debugger.IsAttached) { Console.Write("Making attestation...\r");}
        LastCheck = DateTime.Now;
        BuildJavaFinder();
        var p = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = FindJava(),
                Arguments = "-jar " + root + "/verifile2.jar",
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
            AttestationState = line.Split('\n')[0];
            if (Debugger.IsAttached) { Console.WriteLine("Attestation result: " + AttestationState); }
            return AttestationState;
        }
        AttestationState = "FAILED";
        if (Debugger.IsAttached) { Console.WriteLine("Attestation result: " + AttestationState); }
        return AttestationState;
    }

    /// <summary>
    /// Makes a verifile attestation
    /// </summary>
    /// <returns>Boolean indicating if the computer complies with Verifile 2.0 requirements</returns>
    public bool IsVerified()
    {
        return MakeAttestation() == "VERIFIED" ||  MakeAttestation() == "BYPASS";
    }

    /// <summary>
    /// Finds the latest version of Java installed on your system, since if you install the Java SE version, Verifile may not work with it.
    /// </summary>
    /// <returns>Path to the latest Java binary found on your system</returns>
    private string FindJava()
    {
        var culture = CultureInfo.CurrentCulture;
        var p = culture.NumberFormat.NumberDecimalSeparator;
        var latest_version = $"0{p}0";
        var latest_path = "";
        var interpreter = OperatingSystem.IsWindows() ? "cmd" : "bash";
        if (OperatingSystem.IsWindows())
        {
            root = root.Replace("/", "\\");
        }
        Process pr = new()
        {
            StartInfo = new ProcessStartInfo()
            {
                FileName = interpreter,
                Arguments = (OperatingSystem.IsWindows() ? "/c " : "") + "\"" + root + (OperatingSystem.IsWindows() ? "\\" : "/") + "find_java." + (OperatingSystem.IsWindows() ? "bat" : "sh") + "\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
            }
        };
        pr.Start();
        while (!pr.StandardOutput.EndOfStream)
        {
            var path_version = (pr.StandardOutput.ReadLine() ?? ":").Replace(":\\", "_WINDRIVE\\").Split(':');
            var path = path_version[0].Replace("_WINDRIVE\\", ":\\");
            var version = path_version[1].Split('_')[0];
            version = version.Split('.')[0] + p + version.Split('.')[1];
            if (!(double.Parse(version, NumberStyles.Any) > double.Parse(latest_version, NumberStyles.Any))) continue;
            latest_path = path;
            latest_version = version;
        }
        return latest_path;
    }
     
    /// <summary>
    /// Builds a script that displays all Java binaries and versions for your system and marks it executable (Unix-like systems)
    /// </summary>
    private void BuildJavaFinder()
    {
        if (File.Exists(root + "/find_java" + (OperatingSystem.IsWindows() ? ".bat" : ".sh"))) return;
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
        File.WriteAllText(root + "/find_java" + (OperatingSystem.IsWindows() ? ".bat" : ".sh"), builder.ToString(), Encoding.ASCII);
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(root + "/find_java.sh", UnixFileMode.UserRead | UnixFileMode.UserExecute | UnixFileMode.UserWrite);
        }
    }

    /// <summary>
    /// Check for missing files for specified scope
    /// </summary>
    /// <param name="Scope">Specific application to check files for</param>
    /// <returns>Boolean value, true when all files are present</returns>
    public static bool CheckFiles(FileScope Scope)
    {
        var r = false;
        string[] filesToCheck = [];
        switch (Scope)
        {
            case FileScope.ControlPanel:
                filesToCheck = ["edition.txt", "edition_1.txt", "events.txt", "scheme.cfg", "mas.cnf", "bg_common.png", "bg_desktop.png", "bg_login.png", "bg_uncommon.png"];
                break;
            case FileScope.MarkuStation:
                filesToCheck = ["ms_games.txt", "ms_exec.txt", "ms_display.txt", "setting.txt"];
                break;
            case FileScope.IntegrationSoftware:
                filesToCheck = ["edition.txt", "edition_1.txt", "events.txt", "scheme.cfg", "mas.cnf", "bg_common.png", "bg_desktop.png", "bg_login.png", "bg_uncommon.png"];
                if (OperatingSystem.IsWindows())
                {
                    filesToCheck = filesToCheck.Concat(["ChangeWallpaper.exe", "CD_Close.ps1", "CD_open.ps1", "refresh.vbs"]).ToArray();
                }
                break;
            case FileScope.InteractiveDesktop:
                filesToCheck = ["edition.txt", "edition_1.txt"];
                if (OperatingSystem.IsWindows())
                {
                    filesToCheck = filesToCheck.Concat(["remas.bat", 
    "itstart.bat", "redoexp.cmd"]).ToArray();
                }
                break;
            case FileScope.DesktopIcons:
                filesToCheck = ["edition.txt", "edition_1.txt", "scheme.cfg"];
                break;
            case FileScope.Screensaver:
                filesToCheck = ["bg_common.png"];
                break;
            case FileScope.DesktopNotes:
                filesToCheck = ["edition.txt", "verifile2.dat"];
                break;
        }

        // check to make sure every file exists
        return Directory.Exists(root) && filesToCheck.All(file => File.Exists(root + "/" + file));
    }
}