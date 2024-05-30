using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using System;
using System.IO;
using System.Reflection.Emit;

namespace TöölauaMärkmed
{
    public partial class MainWindow : Window
    {
        public int currentindex = 1;
        public string colorcode;
        bool drag = false;
        int mousex;
        int mousey;
        public bool loader = true;
        bool nodelete = true;
        bool canclose = false;
        bool initialized = false;
        App app = (App)Application.Current;
        DispatcherTimer timer = new DispatcherTimer();
        public MainWindow()
        {
            InitializeComponent();
            timer.Tick += (object? sender, EventArgs e) =>
            {
                if (File.Exists(app.masRoot + "/closenote.log"))
                {
                    if (currentindex == 1)
                    {
                        canclose = true;
                        File.Delete(app.masRoot + "/closenote.log");
                        this.Close();
                    }
                }
                if ((!initialized))
                {
                    if (currentindex == 1)
                    {
                        foreach (string filename in Directory.GetFiles(app.masRoot + "/notes"))
                        {
                            if (filename.Contains(".setting"))
                            {
                                continue;
                            }
                            if (filename.Contains("note_"))
                            {
                                int fileindex = Convert.ToInt32(filename.Split('_')[1].Replace(".txt", ""));
                                string colortype = filename.Split('_')[2].ToString().Replace(".txt", "");
                                string[] points = null;
                                if (File.Exists(app.masRoot + "/notes/.setting_note_" + fileindex.ToString() + ".meta"))
                                {
                                    points = File.ReadAllText(app.masRoot + "/notes/.setting_note_" + fileindex.ToString() + ".meta").Trim().Split(';');
                                }
                                if (fileindex == 1)
                                {
                                    colorcode = colortype;
                                    NoteBox.Text = File.ReadAllText(app.masRoot + "/notes/note_" + fileindex.ToString() + "_" + colortype + ".txt");
                                    if (points != null)
                                    {
                                        this.Position = new PixelPoint(Convert.ToInt32(points[0]), Convert.ToInt32(points[1]));
                                        this.Width = Convert.ToInt32(points[2]);
                                        this.Height = Convert.ToInt32(points[3]);
                                    }
                                }
                                else
                                {
                                    MainWindow newnote = new MainWindow();
                                    newnote.colorcode = colortype;
                                    newnote.currentindex = fileindex;
                                    newnote.loader = false;
                                    if (points != null)
                                    {
                                        newnote.Position = new PixelPoint(Convert.ToInt32(points[0]), Convert.ToInt32(points[1]));
                                        newnote.Width = Convert.ToInt32(points[2]);
                                        newnote.Width = Convert.ToInt32(points[3]);
                                    }
                                    if (fileindex > app.activeindex) { app.activeindex = fileindex; }
                                    newnote.NoteBox.Text = File.ReadAllText(app.masRoot + "/notes/note_" + fileindex.ToString() + "_" + colortype + ".txt");
                                    newnote.Show();
                                }
                            }
                        }
                    }
                    ApplyColor(colorcode ?? "y");
                    initialized = true;
                }
            };
            timer.Interval = new TimeSpan(0, 0, 0, 0, 100);
            timer.Start();
            ApplyColor(colorcode ?? "y");
            TitleBarLabel.Content = "Märkmik " + currentindex.ToString();
        }

        private void Add_Enter(object? sender, PointerEventArgs e)
        {
            AddBtn.Background = new SolidColorBrush(Colors.Lime);
            AddBtn.Foreground = new SolidColorBrush(Colors.Green);
        }

        private void Add_Leave(object? sender, PointerEventArgs e)
        {
            AddBtn.Background = new SolidColorBrush(Colors.Green);
            AddBtn.Foreground = new SolidColorBrush(Colors.White);
        }

        private void Exit_Enter(object? sender, PointerEventArgs e)
        {
            ExitBtn.Background = new SolidColorBrush(Colors.Red);
            ExitBtn.Foreground = new SolidColorBrush(Colors.Maroon);
        }

        private void Exit_Leave(object? sender, PointerEventArgs e)
        {
            ExitBtn.Background = new SolidColorBrush(Colors.Maroon);
            ExitBtn.Foreground = new SolidColorBrush(Colors.White);
        }

        private void ExitNote()
        {
            if (currentindex != 1)
            {
                if (File.Exists(app.masRoot + "/notes/note_" + currentindex.ToString() + "_" + colorcode + ".txt"))
                {
                    File.Delete(app.masRoot + "/notes/note_" + currentindex.ToString() + "_" + colorcode + ".txt");
                    File.Delete(app.masRoot + "/notes/.setting_note_" + currentindex.ToString() + ".meta");
                }
            }
            else
            {
                if (File.Exists(app.masRoot + "/closenote.log"))
                {
                    File.Delete(app.masRoot + "/closenote.log");
                }
                Environment.Exit(0);
            }
            this.Close();
        }

        private void NewNote()
        {
            MainWindow mw = new MainWindow();
            app.activeindex++;
            mw.currentindex = app.activeindex;
            mw.loader = false;
            mw.Show();
        }

        private void Exit_Click(object? sender, PointerPressedEventArgs e)
        {
            ExitNote();
        }

        private void Add_Click(object? sender, Avalonia.Input.PointerPressedEventArgs e)
        {
            NewNote();
        }

        private void TitleBar_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            int screenX = (int)(this.Position.X + e.GetPosition(null).X);
            int screenY = (int)(this.Position.Y + e.GetPosition(null).Y);
            mousex = (int)(screenX - this.Position.X);
            mousey = (int)(screenY - this.Position.Y);
            drag = true;
        }

        private void TitleBar_PointerMoved(object? sender, PointerEventArgs e)
        {
            if (drag)
            {
                int screenX = (int)(this.Position.X + e.GetPosition(null).X);
                int screenY = (int)(this.Position.Y + e.GetPosition(null).Y);
                this.Position = new Avalonia.PixelPoint((int)(screenX - mousex), (int)(screenY - mousey));
            }
        }

        private void TitleBar_PointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            drag = false;
        }

        private void ApplyColor(string cc)
        {
            Color bg;
            switch (cc)
            {
                case "r":
                    bg = Colors.LightCoral;
                    break;
                case "g":
                    bg = Colors.PaleGreen;
                    break;
                case "b":
                    bg = Colors.LightBlue;
                    break;
                case "w":
                    bg = Colors.White;
                    break;
                case "gr":
                    bg = Colors.LightGray;
                    break;
                case "br":
                    bg = Colors.Tan;
                    break;
                case "l":
                    bg = Colors.Thistle;
                    break;
                case "y":
                default:
                    bg = Colors.PaleGoldenrod;
                    break;
            }
            this.colorcode = cc;
            this.Background = new SolidColorBrush(bg);
            TitleBar.Background = new SolidColorBrush(Color.FromRgb((byte)(255 - bg.R), (byte)(255 - bg.G), (byte)(255 - bg.B)));
        }

        private void ApplyColorObj(object? sender)
        {
            string cc;
            switch (((MenuItem)sender).Header)
            {
                case "Punane":
                    cc = "r";
                    break;
                case "Roheline":
                    cc = "g";
                    break;
                case "Sinine":
                    cc = "b";
                    break;
                case "Valge":
                    cc = "w";
                    break;
                case "Hall":
                    cc = "gr";
                    break;
                case "Pruun":
                    cc = "br";
                    break;
                case "Lilla":
                    cc = "l";
                    break;
                case "Kollane":
                default:
                    cc = "y";
                    break;
            }
            ApplyColor(cc);
        }

        private void Color_Click(object? sender, RoutedEventArgs e)
        {
            ApplyColorObj(sender);
        }

        private void New_Click(object? sender, RoutedEventArgs e)
        {
            NewNote();
        }

        private void Del_Click(object? sender, RoutedEventArgs e)
        {
            ExitNote();
        }

        private void NoteBox_TextChanged(object? sender, TextChangedEventArgs e)
        {
            if (NoteBox.Text?.Length > 0)
            {
                TitleBarLabel.Content = "Märkmik " + currentindex.ToString() + " - " + NoteBox.Text.Split('\n')[0].ToString();
            }
            else
            {
                TitleBarLabel.Content = "Märkmik " + currentindex.ToString();
            }
            string[] allcodes = { "y", "r", "g", "b", "gr", "br", "l" };
            foreach (string code in allcodes)
            {
                if (File.Exists(app.masRoot + "/notes/note_" + currentindex.ToString() + "_" + code + ".txt"))
                {
                    try
                    {
                        File.Delete(app.masRoot + "/notes/note_" + currentindex.ToString() + "_" + code + ".txt");
                    }
                    catch { }
                }
            }
            File.WriteAllText(app.masRoot + "/notes/note_" + currentindex.ToString() + "_" + colorcode + ".txt", NoteBox.Text);
        }
    }
}