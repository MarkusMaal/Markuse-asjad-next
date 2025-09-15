using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using MasCommon;

namespace Markuse_arvuti_juhtpaneel
{
    
    public class App : Application
    {
        public static string root = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/.mas";
        readonly public static string[] whitelistedHashes = { "B881FBAB5E73D3984F2914FAEA743334D1B94DFFE98E8E1C4C8C412088D2C9C2", "A0B93B23301FC596789F83249A99F507A9DA5CBA9D636E4D4F88676F530224CB", "B08AABB1ED294D8292FDCB2626D4B77C0A53CB4754F3234D8E761E413289057F", "8076CF7C156D44472420C1225B9F6ADB661E3B095E29E52E3D4E8598BB399A8F" };
        private MainWindow? MainWindow { get; set; }
        public override void Initialize()
        {
            if (!Verifile.CheckVerifileTamper())
            {
                Program.Launcherror = true;
            }
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            MainWindow = new MainWindow();
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = MainWindow;
            }

            base.OnFrameworkInitializationCompleted();
        }

        private void NativeMenuItem_OnClick(object? sender, EventArgs e)
        {
            MainWindow?.Close();
        }
    }
}