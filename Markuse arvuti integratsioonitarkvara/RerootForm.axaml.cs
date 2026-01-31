using Avalonia.Controls;
using System.Text;
using System.Xml.Linq;
using System;
using System.Diagnostics;
using Avalonia;
using System.IO;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace Markuse_arvuti_integratsioonitarkvara
{
    public partial class RerootForm : Window
    {
        App app = (App)Application.Current;
        string edition = "Pro";
        string version = "0.0";
        string build = "A00000a";
        string name = "Alpha";
        static Random rnd = new Random();
        string pin = rnd.Next(0, 9).ToString() + rnd.Next(0, 9).ToString() + rnd.Next(0, 9).ToString() + rnd.Next(0, 9).ToString();
        string[] features = { "IT", "TS", "MM" };

        public RerootForm()
        {
            InitializeComponent();
            Reloadvalues();
            Color[] scheme = LoadTheme();
            this.Background = new SolidColorBrush(scheme[0]);
            this.Foreground = new SolidColorBrush(scheme[1]);
            
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
        
        /* Loads mas theme */
        Color[] LoadTheme()
        {
            string[] bgfg = File.ReadAllText(app.mas_root + "/scheme.cfg").Split(';');
            string[] bgs = bgfg[0].ToString().Split(':');
            string[] fgs = bgfg[1].ToString().Split(':');
            Color[] cols = { Color.FromArgb(255, byte.Parse(bgs[0]), byte.Parse(bgs[1]), byte.Parse(bgs[2])), Color.FromArgb(255, byte.Parse(fgs[0]), byte.Parse(fgs[1]), byte.Parse(fgs[2])) };
            return cols;
        }

        private void Button_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            string s;
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            s = File.ReadAllText(app.mas_root + "/edition.txt", Encoding.GetEncoding(1252));
            string[] attribs = s.Split('\n');
            edition = attribs[1].ToString();
            version = attribs[2].ToString();
            build = attribs[3].ToString();
            pin = attribs[9].ToString();
            features = attribs[8].Split('-');
            name = attribs[10].ToString();
            Reloadvalues();
        }


        private void Reloadvalues()
        {
            string s;
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            s = File.ReadAllText(app.mas_root + "/edition.txt", Encoding.GetEncoding(1252));
            string[] attribs = s.Split('\n');
            edition = attribs[1].ToString();
            version = attribs[2].ToString();
            build = attribs[3].ToString();
            pin = attribs[9].ToString();
            features = attribs[8].Split('-');
            name = attribs[10].ToString();
            if (edition == "Pro")
            {
                this.GetControl<ComboBox>("EditionBox").SelectedIndex = 1;
            }
            else if (edition == "Ultimate")
            {
                this.GetControl<ComboBox>("EditionBox").SelectedIndex = 0;
            }
            else if (edition == "Premium")
            {
                this.GetControl<ComboBox>("EditionBox").SelectedIndex = 2;
            }
            else
            {
                this.GetControl<ComboBox>("EditionBox").SelectedIndex = 3;
            }
            this.GetControl<TextBox>("VersionBox").Text = version;
            this.GetControl<TextBox>("BuildBox").Text = build;
            this.GetControl<TextBox>("NameBox").Text = name;
            this.GetControl<Label>("label11").Content = "PIN kood: " + pin + " (automaatne väli)";
            this.GetControl<Label>("label7").Content = "Juurutaja: " + Environment.UserName + " (automaatne väli)";
            this.GetControl<Label>("label9").Content = "Tuuma versioon: " + Environment.OSVersion.Version.Major.ToString() + "." + Environment.OSVersion.Version.Minor.ToString() + " (automaatne väli)";
            this.GetControl<CheckBox>("DXBox").IsChecked = false;
            this.GetControl<CheckBox>("ITBox").IsChecked = false;
            this.GetControl<CheckBox>("WXBox").IsChecked = false;
            this.GetControl<CheckBox>("GPBox").IsChecked = false;
            this.GetControl<CheckBox>("CSBox").IsChecked = false;
            this.GetControl<CheckBox>("RDBox").IsChecked = false;
            this.GetControl<CheckBox>("LTBox").IsChecked = false;
            foreach (string element in features)
            {
                if ((element == "DX") || (element == "RM"))
                {
                    this.GetControl<CheckBox>("DXBox").IsChecked = true;
                }
                else if (element == "IT")
                {
                    this.GetControl<CheckBox>("ITBox").IsChecked = true;
                }
                else if (element == "WX")
                {
                    this.GetControl<CheckBox>("WXBox").IsChecked = true;
                }
                else if (element == "GP")
                {
                    this.GetControl<CheckBox>("GPBox").IsChecked = true;
                }
                else if (element == "CS")
                {
                    this.GetControl<CheckBox>("CSBox").IsChecked = true;
                }
                else if (element == "RD")
                {
                    this.GetControl<CheckBox>("RDBox").IsChecked = true;
                }
                else if (element == "LT")
                {
                    this.GetControl<CheckBox>("LTBox").IsChecked = true;
                }
            }

        }

        private void RootClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            try
            {
                string feats = "IP-";
                this.GetControl<TextBox>("NameBox").Text = this.GetControl<TextBox>("NameBox").Text.Replace("ä", "2").Replace("õ", "?").Replace("ü", "_y_").Replace("ö", "9");
                if (this.GetControl<CheckBox>("ITBox").IsChecked == true) { feats += "IT-"; }
                if (this.GetControl<CheckBox>("WXBox").IsChecked == true) { feats += "WX-"; }
                if (this.GetControl<CheckBox>("GPBox").IsChecked == true) { feats += "GP-"; }
                if (this.GetControl<CheckBox>("DXBox").IsChecked == true) { feats += "RM-"; }
                if (this.GetControl<CheckBox>("CSBox").IsChecked == true) { feats += "CS-"; }
                if (this.GetControl<CheckBox>("RDBox").IsChecked == true) { feats += "RD-"; }
                if (this.GetControl<CheckBox>("LTBox").IsChecked == true) { feats += "LT-"; }
                if (this.GetControl<CheckBox>("TSMMBox").IsChecked == true) { feats += "TS-MM"; }
                string rooter = Environment.UserName;
                string win = Environment.OSVersion.Version.Major.ToString() + "." + Environment.OSVersion.Version.Minor.ToString();
                //if (File.Exists("\\mas\\edition.bak")) { File.Delete("\\mas\\edition.bak"); }
                //File.Move("\\mas\\edition.txt", "\\mas\\edition.bak");
                
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                File.WriteAllText(app.mas_root + "/edition.txt", "[Edition_info]\n" + edition + "\n" + version + "\n" + build + "\nYes\n" + rooter + "\net-EE\n" + win + "\n" + feats + "\n" + pin + "\n" + name + "\n<null>", Encoding.GetEncoding(1252));
                File.WriteAllText(app.mas_root + "/edition_1.txt", "[Edition_info];" + edition + ";" + version + ";" + build + ";Yes;" + rooter + ";et-EE;" + win + ";" + feats + ";" + pin + ";" + name + "\n;[this file is for backwards compatibility with legacy programs]", Encoding.GetEncoding(1252));
                Environment.Exit(0);
            }
            catch (Exception ex) when (!Debugger.IsAttached)
            {
                Console.WriteLine(string.Format("Juurutusteabe salvestamine nurjus\n\n{0}\n\n{1}", ex.Message, ex.StackTrace));
            }
        }
    }
}
