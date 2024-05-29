using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Primitives;
using Avalonia.Dialogs.Internal;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;
using System;
using System.IO;

namespace Markuse_arvuti_integratsioonitarkvara
{
    public partial class MainWindow : Window
    {
        private TrayIcon ti;
        private App app;
        Color[] scheme;
        public MainWindow()
        {
            InitializeComponent();
            app = (App)Application.Current;
            ti = app.GetTrayIcon();
            scheme = LoadTheme();
        }

        private void Window_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            ApplyTheme();
            ti.IsVisible = true;
            this.IsVisible = false;
            if (app.fmount != "")
            {
                ModifyContextText("Ühtegi mälupulka pole sisestatud", "Markuse mälupulk");
            }
            foreach (NativeMenuItemBase nmi in ti.Menu.Items)
            {
                if (((NativeMenuItem)nmi).Header == "Värviskeem")
                {
                    foreach (NativeMenuItemBase snmi in ((NativeMenuItem)nmi).Menu.Items)
                    {
                        switch (((NativeMenuItem)snmi).Header)
                        {
                            case "Valge":
                                ((NativeMenuItem)snmi).Click += SaveThemeWhite;
                                break;
                            case "Ööreþiim":
                                ((NativeMenuItem)snmi).Click += SaveThemeBlack;
                                break;
                            case "Sinine":
                                ((NativeMenuItem)snmi).Click += SaveThemeBlue;
                                break;
                            case "Jõulud":
                                ((NativeMenuItem)snmi).Click += SaveThemeXmas;
                                break;
                        }
                    }
                }
            }
        }

        private void SaveThemeWhite(object? sender, EventArgs e)
        {
            SaveTheme(Colors.White, Colors.Black);
            ApplyTheme(true);
        }

        private void SaveThemeBlack(object? sender, EventArgs e)
        {
            SaveTheme(Colors.Black, Colors.Silver);
            ApplyTheme(true);
        }

        private void SaveThemeBlue(object? sender, EventArgs e)
        {
            SaveTheme(Colors.MidnightBlue, Colors.White);
            ApplyTheme(true);
        }

        private void SaveThemeXmas(object? sender, EventArgs e)
        {
            SaveTheme(Colors.DarkRed, Colors.Lime);
            ApplyTheme(true);
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

        private void ApplyTheme(bool reopen = false)
        {
            app.Styles.Add(new Style(x => x.OfType<MenuItem>())
            {
                Setters =
                {
                    new Setter(TemplatedControl.BackgroundProperty, new SolidColorBrush(scheme[0])),
                    new Setter(TemplatedControl.ForegroundProperty, new SolidColorBrush(scheme[1]))
                }
            });
            /*app.Styles.Add(new Style(x => x.OfType<MenuItem>().Class("White"))
            {
                Setters =
                {
                    new Setter(TemplatedControl.BackgroundProperty, new SolidColorBrush(Colors.White)),
                    new Setter(TemplatedControl.ForegroundProperty, new SolidColorBrush(Colors.Black))
                }
            });*/
        }

        private void SaveTheme(Color bg, Color fg)
        {
            this.scheme[0] = bg;
            this.scheme[1] = fg;
            File.WriteAllText(app.mas_root + "/scheme.cfg", bg.R.ToString() + ":" + bg.G.ToString() + ":" + bg.B.ToString() + ":;" + fg.R.ToString() + ":" + fg.G.ToString() + ":" + fg.B.ToString() + ":;");
        }


        private void ModifyContextText(string old, string nw)
        {
            foreach (NativeMenuItemBase nmi in ti.Menu.Items)
            {
                if (((NativeMenuItem)nmi).Header == old)
                {
                    ((NativeMenuItem)nmi).Header = nw;
                }
            }
        }
    }
}