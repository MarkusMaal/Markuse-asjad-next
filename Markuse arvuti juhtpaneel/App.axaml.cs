using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace Markuse_arvuti_juhtpaneel
{
    public partial class App : Application
    {
        public static string root = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/.mas";
        readonly public static string[] whitelistedHashes = { "B881FBAB5E73D3984F2914FAEA743334D1B94DFFE98E8E1C4C8C412088D2C9C2", "A0B93B23301FC596789F83249A99F507A9DA5CBA9D636E4D4F88676F530224CB", "B08AABB1ED294D8292FDCB2626D4B77C0A53CB4754F3234D8E761E413289057F", "8076CF7C156D44472420C1225B9F6ADB661E3B095E29E52E3D4E8598BB399A8F" };

        public override void Initialize()
        {
            if (!CheckVerifileTamper())
            {
                Program.Launcherror = true;
            }
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow();
            }

            base.OnFrameworkInitializationCompleted();
        }


        private static bool CheckVerifileTamper()
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
    }
}