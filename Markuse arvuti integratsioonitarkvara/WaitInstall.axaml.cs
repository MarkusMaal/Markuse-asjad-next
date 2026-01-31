using Avalonia;
using Avalonia.Controls;
using System.Diagnostics;
using Avalonia.Markup.Xaml;

namespace Markuse_arvuti_integratsioonitarkvara
{
    public partial class WaitInstall : Window
    {
        internal bool redo = false;
        private App app = (App) Application.Current;
        public WaitInstall()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void Button_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            Process p = new();
            p.StartInfo.FileName = app.mas_root + @"/finalize_install.bat";
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            p.Start();
            p.WaitForExit();
            if (redo)
            {
                p = new Process();
                p.StartInfo.FileName = app.mas_root + "/organize_desktop.bat";
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                p.Start();
            }
            this.Close();
        }
    }
}
