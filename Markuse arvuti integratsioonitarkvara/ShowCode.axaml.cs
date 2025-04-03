using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

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
            InitializeComponent();
            if (app.dev)
            {
                return;
            }
            string[] log_content;
            string fileName = app.mas_root + "/maia/request_permission.maia";
            if (File.Exists(app.mas_root + "/maia/request_permission.mai"))
            {
                fileName = app.mas_root + "/maia/request_permission.mai";
            }
            log_content = File.ReadAllText(fileName).Split(';');
            devType = log_content[0];
            devIP = log_content[1];
            string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            Random r = new Random();
            string code = "";
            for (int i = 0; i < 8; i++)
            {
                code += chars[r.Next(0, chars.Length)];
            }
            CodeText.Content = code;
            File.WriteAllText(string.Format(app.mas_root + "/maia/{0}.{1}.maia", devType, devIP.Replace(".", "_")), GetHashString(devType + "__" + code));
            File.Delete(fileName);
            waitForClose.Tick += new EventHandler(WaitForClose);
            waitForClose.Interval = new TimeSpan(0, 0, 1);
            waitForClose.Start();
        }

        private void WaitForClose(object? sender, EventArgs e)
        {
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
                this.Close();
            }
            else
            {
                timeLeft -= 1;
                TimerLabel.Content = timeLeft.ToString();
                if (timeLeft == 0)
                {
                    File.Delete(string.Format(app.mas_root + "/maia/{0}.{1}.maia", devType, devIP.Replace(".", "_")));
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
            this.Close();
        }
    }
}
