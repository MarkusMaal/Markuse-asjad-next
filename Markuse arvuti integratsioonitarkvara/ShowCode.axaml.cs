using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Avalonia.Markup.Xaml;

namespace Markuse_arvuti_integratsioonitarkvara
{
    public partial class ShowCode : Window
    {
        internal Color bg = Colors.Black;
        internal Color fg = Colors.White;
        private App app = (App)Application.Current;
        private string devType = null;
        private string devIP = null;
        private int timeLeft = 80;
        private bool initialized = false;
        DispatcherTimer waitForClose = new DispatcherTimer();

        public ShowCode()
        {
            Program.CodeOpen = true;
            InitializeComponent();
            if (app.dev)
            {
                return;
            }
            string[] log_content;
            Thread.Sleep(1000); // wait for server to finish writing the request_permission.maia file before continuing
            string fileName = app.mas_root + "/maia/request_permission.maia";
            if (File.Exists(app.mas_root + "/maia/request_permission.mai"))
            {
                fileName = app.mas_root + "/maia/request_permission.mai";
            }

            if (!File.Exists(fileName))
            {
                waitForClose.Tick += new EventHandler(WaitForClose);
                waitForClose.Interval = new TimeSpan(0, 0, 1);
                waitForClose.Start();
                return;
            }
            log_content = File.ReadAllText(fileName).Split(';');
            devType = log_content[0];
            devIP = log_content[1];
            string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            Random r = new();
            string code = "";
            for (int i = 0; i < 8; i++)
            {
                code += chars[r.Next(0, chars.Length)];
            }
            this.GetControl<Label>("CodeText").Content = code;
            File.WriteAllText(string.Format(app.mas_root + "/maia/{0}.{1}.maia", devType, devIP.Replace(".", "_")), GetHashString(devType + "__" + code));
            File.Delete(fileName);
            waitForClose.Tick += new EventHandler(WaitForClose);
            waitForClose.Interval = new TimeSpan(0, 0, 1);
            waitForClose.Start();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void WaitForClose(object? sender, EventArgs e)
        {
            if (ReferenceEquals(this.GetControl<Label>("CodeText").Content, "AAAAAAAA")) this.Close(); // no valid code, so close immediately
            if (!initialized)
            {
                this.Background = new SolidColorBrush(bg);
                this.Foreground = new SolidColorBrush(fg);
            }
            initialized = true;
            if (File.Exists(app.mas_root + "/maia/close_popup.maia"))
            {
                File.Delete(string.Format(app.mas_root + "/maia/{0}.{1}.maia", devType, devIP.Replace(".", "_")));
                File.Delete(app.mas_root + "/maia/close_popup.maia");
                waitForClose.Stop();
                Program.CodeOpen = false;
                this.Close();
            }
            else
            {
                timeLeft -= 1;
                this.GetControl<Label>("TimerLabel").Content = timeLeft.ToString();
                if (timeLeft == 0)
                {
                    File.Delete(string.Format(app.mas_root + "/maia/{0}.{1}.maia", devType, devIP.Replace(".", "_")));
                    waitForClose.Stop();
                    Program.CodeOpen = false;
                    this.Close();
                }
            }
        }

        public static byte[] GetHash(string inputString)
        {
            using (HashAlgorithm algorithm = SHA256.Create())
                return algorithm.ComputeHash(Encoding.UTF8.GetBytes(inputString));
        }

        public static string GetHashString(string inputString)
        {
            StringBuilder sb = new StringBuilder();
            foreach (byte b in GetHash(inputString))
                sb.Append(b.ToString("X2"));

            return sb.ToString();
        }

        private void Button_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            File.Delete(string.Format(app.mas_root + "/maia/{0}.{1}.maia", devType, devIP.Replace(".", "_")));
            waitForClose.Stop();
            Program.CodeOpen = false;
            this.Close();
        }
    }
}
