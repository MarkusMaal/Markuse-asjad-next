using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.Interactivity;
using System;
using System.IO;
using System.Text;
using System.Threading;
using LibVLCSharp.Shared;
using Avalonia.Platform.Storage;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Logging;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using MsBox.Avalonia;

namespace Pidu_
{
    public partial class MainWindow : Window
    {
        // VLC stuff
        public LibVLC _libVLC;
        public MediaPlayer mp;
        public Media? media;
        public bool isPlaying = false;
        string[]? playlist;
        int track = 0;
        readonly DispatcherTimer timer = new();
        readonly DispatcherTimer volume = new();
        bool custMessage = false;
        private readonly string idleString = "Hetkel ei mängi ühtegi pala";

        public MainWindow()
        {
            InitializeComponent();
            _libVLC = new LibVLC();
            mp = new MediaPlayer(_libVLC);
            timer.Interval = new TimeSpan(0, 0, 0, 0, 100);
            volume.Interval = new TimeSpan(0, 0, 0, 0, 20);
            this.GetControl<Slider>("VolumeControl").Value = mp.Volume;
            mp.EncounteredError += Mp_EncounteredError;
            SizeCheck();
            // prevent window borders from being rendered if not in a Linux DE
            if (!OperatingSystem.IsLinux())
            {
                //Console.WriteLine("Windows/Mac paranduste aktiveerimine...");
                this.ExtendClientAreaToDecorationsHint = false;
                this.SystemDecorations = SystemDecorations.None;
            }
            timer.Tick += (object? sender, EventArgs e) =>
            {
                /*if ((this.WindowState != WindowState.Minimized) && (this.WindowState == WindowState.Normal))
                {
                    this.WindowState = WindowState.FullScreen;
                }*/

                if (mp.Media?.State == VLCState.Ended)
                {
                    track++;
                    custMessage = false;
                    if (this.GetControl<ListBox>("Playlist").IsVisible)
                    {
                        this.GetControl<ListBox>("Playlist").SelectedIndex = track;
                    }
                    if (track >= playlist?.Length)
                    {
                        isPlaying = false;
                        this.GetControl<Label>("BigTitle").Content = idleString;
                    } else
                    {
                        PlayTrack();
                    }
                }
                if (mp.IsPlaying && !custMessage)
                {
                    if ((mp.Media?.Meta(MetadataType.Album) != null) && (mp.Media?.Meta(MetadataType.Artist) != null))
                    {
                        this.GetControl<Label>("BigTitle").Content = "Hetkel esitusel: " + mp.Media.Meta(MetadataType.Artist) + " - " + mp.Media.Meta(MetadataType.Title) + " (" + mp.Media.Meta(MetadataType.Album) + ")";
                    }
                    else if (mp.Media?.Meta(MetadataType.Album) != null)
                    {
                        this.GetControl<Label>("BigTitle").Content = "Hetkel esitusel: " + mp.Media.Meta(MetadataType.Title) + " (" + mp.Media.Meta(MetadataType.Album) + ")";
                    }
                    else if (mp.Media?.Meta(MetadataType.Artist) != null)
                    {
                        this.GetControl<Label>("BigTitle").Content = "Hetkel esitusel: " + mp.Media.Meta(MetadataType.Artist) + " - " + mp.Media.Meta(MetadataType.Title);
                    }
                    else if (mp.Media != null)
                    {
                        this.GetControl<Label>("BigTitle").Content = "Hetkel esitusel: " + mp.Media.Meta(MetadataType.Title);
                    }
                    this.GetControl<Button>("PausePlay").Content = "||";
                    this.GetControl<Button>("PausePlay").Background = new SolidColorBrush(Colors.DeepSkyBlue);
                } else if (!mp.IsPlaying)
                {
                    this.GetControl<Button>("PausePlay").Content = ">";
                    this.GetControl<Button>("PausePlay").Background = new SolidColorBrush(Colors.Lime);
                }
                this.GetControl<Label>("StatusLabel").Content = string.Format("{0}  |  {1}%  |  {2} / {3}", DateTime.Now, mp.Volume, NiceTime(mp.Time), NiceTime(mp.Length));
                switch (mp.Media?.State)
                {
                    case VLCState.Paused:
                        this.GetControl<Label>("StatusLabel").Content += " (paus)";
                        break;
                    case VLCState.Buffering:
                        this.GetControl<Label>("StatusLabel").Content += " (puhverdamine)";
                        break;
                    default:
                        break;
                }
            };
            volume.Tick += (object? sender, EventArgs e) =>
            {
                if (mp.Length < 30000)
                {
                    mp.Volume = 100;
                    this.GetControl<Slider>("VolumeControl").Value = mp.Volume;
                    this.GetControl<Slider>("VolumeControl").IsEnabled = true;
                    return;
                }
                if (mp.Time < 10000L)
                {
                    mp.Volume = (int)mp.Time / 100;
                    this.GetControl<Slider>("VolumeControl").Value = mp.Volume;
                    this.GetControl<Slider>("VolumeControl").IsEnabled = false;
                }
                if ((mp.Time >= 10000L) && (mp.Time < 10500L))
                {
                    mp.Volume = 100;
                    this.GetControl<Slider>("VolumeControl").Value = mp.Volume;
                    this.GetControl<Slider>("VolumeControl").IsEnabled = true;
                }
                else if (mp.Time > mp.Length - 11000L)
                {
                    int level = ((int)(mp.Length - 1000 - mp.Time) / 100);
                    if (level < 0)
                    {
                        level = 0;
                    }
                    mp.Volume = level;
                    this.GetControl<Slider>("VolumeControl").Value = mp.Volume;
                    this.GetControl<Slider>("VolumeControl").IsEnabled = false;
                }
            };
            timer.Start();
            volume.Start();
            if (!Program.streamMode) return;
            this.WindowState = WindowState.Normal;
            this.ExtendClientAreaToDecorationsHint = false;
            this.ExtendClientAreaChromeHints = ExtendClientAreaChromeHints.Default;
            this.SystemDecorations = SystemDecorations.Full;
            this.GetControl<Button>("MinimizeButton").Content = "";
            this.GetControl<Button>("MinimizeButton").Padding = new Thickness(0,5);
            this.GetControl<Button>("MinimizeButton").BorderThickness = new Thickness(0);
            Width = 1280;
            Height = 1024;
            this.Title = "Pidu!";
            this.GetControl<Button>("CloseButton").IsVisible = false;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void Mp_EncounteredError(object? sender, EventArgs e)
        {
            _ = MessageBoxShow(mp.Media?.Statistics.ToString() ?? "Tundmatu rike", "Viga esitamisel", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error);
        }

        private void SizeCheck()
        {
            if (Program.streamMode) return;
            if ((double)this.Width / (double)this.Height > 3.0)
            {
                var padding = this.Width / 4;
                this.GetControl<Grid>("MainGrid").Margin = new Thickness(padding, 0, padding, 0);
            }
            else
            {
                this.GetControl<Grid>("MainGrid").Margin = new Thickness(0, 0, 0, 0);
            }
        }
        
        private static string NiceTime(long millis)
        {
            int totalSecs = (int)millis / 1000;
            if (totalSecs < 0 )
            {
                totalSecs = 0;
            }
            int totalMins = (int)(totalSecs / 60);
            int subsetSecs = totalSecs - totalMins * 60;
            return string.Format("{0}:{1}", totalMins.ToString().PadLeft(2, '0'), subsetSecs.ToString().PadLeft(2, '0'));
        }

        private void X_Click(object? sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Underscore_Click(object? sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void Slider_ValueChanged(object? sender, Avalonia.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            mp.Volume = (int)e.NewValue;
            if ((this.GetControl<Label>("StatusLabel").Content != null) && (this.GetControl<Label>("StatusLabel") != null))
            {
                this.GetControl<Label>("StatusLabel").Content = (this.GetControl<Label>("StatusLabel").Content.ToString() ?? "").Replace((int)e.OldValue + "%", (int)e.NewValue + "%");
            }
        }

        private void ShowPlaylist_PointerReleased(object? sender, Avalonia.Input.PointerReleasedEventArgs e)
        {
            if (sender == null) { return; }
            if (e.InitialPressMouseButton == Avalonia.Input.MouseButton.Left)
            {
                this.GetControl<ListBox>("Playlist").IsVisible = !this.GetControl<ListBox>("Playlist").IsVisible;
                if ((playlist != null) && (playlist.Length > 0))
                {
                    this.GetControl<ListBox>("Playlist").Items.Clear();
                    foreach (string song in playlist)
                    {
                        this.GetControl<ListBox>("Playlist").Items.Add(song);
                    }
                    this.GetControl<ListBox>("Playlist").SelectedIndex = track;
                }
            }
            ((TextBlock)sender).Foreground = new SolidColorBrush(Color.FromRgb(191, 191, 255));
        }

        private void ShowPlaylist_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
        {
            if (sender == null) { return; }
            ((TextBlock)sender).Foreground = new SolidColorBrush(Color.FromRgb(128, 255, 255));
        }

        private void AddMusic_Click(object? sender, RoutedEventArgs e)
        {
            AddMusic();
        }

        private async void AddMusic()
        {
            // source: https://docs.avaloniaui.net/docs/basics/user-interface/file-dialogs
            // Get top level from the current control. Alternatively, you can use Window reference instead.
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) { return; }
            // Start async operation to open the dialog.
            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Vali muusikapala",
                AllowMultiple = true
            });

            if (files.Count >= 1)
            {
                if (playlist != null)
                {
                    string[] oldSongs = playlist;
                    playlist = new string[playlist.Length + files.Count];
                    for (int i = 0; i < oldSongs.Length; i++)
                    {
                        playlist[i] = oldSongs[i];
                    }
                    for (int i = 0; i < files.Count; i++)
                    {
                        playlist[oldSongs.Length + i] = Uri.UnescapeDataString(files[i].Path.AbsolutePath);
                    }
                }
                else
                {
                    playlist = new string[files.Count];
                    for (int i = 0; i < files.Count; i++)
                    {
                        playlist[i] = Uri.UnescapeDataString(files[i].Path.AbsolutePath);
                    }
                    track = 0;
                    PlayTrack();
                }
                if (this.GetControl<ListBox>("Playlist").IsVisible)
                {
                    this.GetControl<ListBox>("Playlist").Items.Clear();
                    foreach (string song in playlist)
                    {
                        this.GetControl<ListBox>("Playlist").Items.Add(song);
                    }
                    this.GetControl<ListBox>("Playlist").SelectedIndex = track;
                }
            }
        }

        private void PausePlay_Click(object? sender,  RoutedEventArgs e)
        {
            if (!isPlaying)
            {
                track = 0;
                PlayTrack();
                isPlaying = true;
            } else if (mp.IsPlaying && (mp.Media?.State == VLCState.Playing))
            {
                mp.Pause();
            }
            else if (mp.Media?.State == VLCState.Paused)
            {
                mp.Play();
            }
        }

        private void ClearPlaylist_Click(object? sender, RoutedEventArgs e)
        {
            mp.Stop();
            mp = new MediaPlayer(_libVLC);
            isPlaying = false;
            this.GetControl<Label>("BigTitle").Content = idleString;
            playlist = [];
            this.GetControl<ListBox>("Playlist").Items.Clear();
            track = 0;
        }

        private void NextTrack_Click(object? sender, RoutedEventArgs e)
        {
            mp.Stop();
            track++;
            PlayTrack();
        }

        private void LastTrack_Click(object? sender, RoutedEventArgs e)
        {
            mp.Stop();
            track--;
            PlayTrack();
        }


        private void SeekBack_Click(object? sender, RoutedEventArgs e)
        {
            if (mp.Time > 10000L)
            {
                mp.Time -= 10000L;
            } else
            {
                mp.SeekTo(new TimeSpan(0, 0, 0));
            }
        }

        private void SeekForward_Click(object? sender, RoutedEventArgs e)
        {
            if (mp.Time < mp.Length - 10000L)
            {
                mp.Time += 10000L;
            }
            else
            {
                mp.Time = mp.Length;
            }
        }

        private void PlayTrack()
        {
            if ((track > -1) && (track < playlist?.Length))
            {
                custMessage = false;
                _libVLC.Dispose();
                mp.Dispose();
                _libVLC = new LibVLC();
                Media vlcMed = new(_libVLC, playlist[track]);   // removes URL encodings, such as %20, etc.
                mp = new MediaPlayer(vlcMed);
                mp.EncounteredError += Mp_EncounteredError;
                mp.Play();
                if (this.GetControl<ListBox>("Playlist").IsVisible)
                {
                    this.GetControl<ListBox>("Playlist").SelectedIndex = track;
                }
            }
        }

        private void Window_Closing(object? sender, WindowClosingEventArgs e)
        {
            _libVLC.Dispose(); // we're disposing everything properly, because we don't leave litter on the streets
            mp.Dispose();
        }

        private void Window_PointerPressed_1(object? sender, Avalonia.Input.PointerPressedEventArgs e)
        {
            if (e.Pointer.IsPrimary && (e.ClickCount == 2))
            {
                this.GetControl<StackPanel>("TopButtons").IsVisible = !this.GetControl<StackPanel>("TopButtons").IsVisible;
                this.GetControl<StackPanel>("PlayControls").IsVisible = !this.GetControl<StackPanel>("PlayControls").IsVisible;
                this.GetControl<Panel>("Overlay").Opacity = (!this.GetControl<StackPanel>("TopButtons").IsVisible) ? 0 : 0.25;
                new Thread(() =>
                {
                    Thread.Sleep(500);
                    Dispatcher.UIThread.Post(() =>
                    {
                        if (!this.GetControl<StackPanel>("TopButtons").IsVisible && this.GetControl<ListBox>("Playlist").IsVisible)
                        {
                            this.GetControl<ListBox>("Playlist").IsVisible = false;
                        }
                    });
                }).Start();
            }
        }

        private void Playlist_DoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e)
        {
            if (this.GetControl<ListBox>("Playlist")?.SelectedItems?.Count > 0)
            {
                mp.Stop();
                track = this.GetControl<ListBox>("Playlist").SelectedIndex;
                PlayTrack();
            }
        }

        private void ToggleFade(object? sender, RoutedEventArgs e)
        {
            volume.IsEnabled = !volume.IsEnabled;
            this.GetControl<Slider>("VolumeControl").IsEnabled = true;
            Button? me = (Button?)sender;
            if (me != null)
            {
                me.Content = volume.IsEnabled ? "Hajumine välja" : "Hajumine sisse";
            }
        }

        private async void Background_Click(object? sender, RoutedEventArgs e)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) { return; }

            // Start async operation to open the dialog.
            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Vali taustapilt",
                AllowMultiple = false
            });
            if (files.Count > 0)
            {
                this.GetControl<Image>("BackgroundImage").Source = new Bitmap(Uri.UnescapeDataString(files[0].Path.AbsolutePath));
            }
        }

        private async void Message_Click(object? sender, RoutedEventArgs e)
        {
            if (sender == null) { return; }
            if (!custMessage)
            {
                MessageEntry me = new();
                await me.ShowDialog(this);
                if (me.GetControl<TextBox>("MessageField").Text != null)
                {
                    ((Button)sender).Content = "Pala pealkiri";
                    custMessage = true;
                    this.GetControl<Label>("BigTitle").Content = me.GetControl<TextBox>("MessageField").Text;
                }
            } else
            {
                ((Button)sender).Content = "Isikupärastatud teade";
                custMessage = false;
            }
        }
        private void Help_Click(object? sender, RoutedEventArgs e)
        {
            _ = MessageBoxShow("Selle programmiga saate kuulata muusikat ja\ntunda ennast nagu päris DJ. Üleval vasakus nurgas on\nerinevad nupud. Hajumine välja/sisse võmaldab\nheli automaatselt hajutada palade alguses ja lõpus\nVasakul pool ekraani on esitusloend ehk\nPlaylist). Seal on mängiv muusika ja tulevased palad.\nVajutage nupul Lisa muusikat, et lisada Playlisti\nuusi palasid. Tühjenda esitusloend nupp kustutab\nkõik palad playlistist. Teil on võimalik muuta\ntaustapilti playlisti ja nuppude taga. Vaiketausta-\npilt on rahvast pidutsemas, kuid te võite panna\nselle asemel enda pildi. Teil on võimalik ka kuulata\nYouTube-ist muusikat. See avab brauseri, millest saate\nvalida video ja minna muusikapala nime valimise juurde.\nTe saate valida värve ja fonte, et muuta loo pealkirja\nisikupärasemaks. Teave kuvab täpsema info programmi\nkohta ning spikker avab praeguse dialoogi.", "Spikker", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Question);
        }

        private void About_Click(object? sender, RoutedEventArgs e)
        {
            _ = MessageBoxShow("Teeme pidu!\nNullist uuesti kirjutatud Avalonia UI ja .NET 10.0 raamistikes\nC# keeles.\n\nKasutab LibVLCSharp teeki\nVersioon 2.1.3 / 31.01.2026\nTegi: Markus Maal\nKogu muusika kuulub nende respektiivsetele omanikele.\nAutoriõiguste rikkumine ei ole lubatud!\n\nMis on uut?\n* Uuendatud .NET 10 raamistikule", "Pidu!", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Info);
        }

        // Reimplementation of WinForms MessageBox.Show
        private Task<MsBox.Avalonia.Enums.ButtonResult> MessageBoxShow(string message, string caption = "Pidu!", MsBox.Avalonia.Enums.ButtonEnum buttons = MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon icon = MsBox.Avalonia.Enums.Icon.None)
        {
            var box = MessageBoxManager.GetMessageBoxStandard(caption, message, buttons, icon);
            var result = box.ShowWindowDialogAsync(this);
            return result;
        }

        private void Window_KeyDown(object? sender, Avalonia.Input.KeyEventArgs e)
        {
            if (e.Key == Avalonia.Input.Key.F4)
            {
                mp.Stop();
                playlist = [];
                track = 0;
                this.GetControl<ListBox>("Playlist").Items.Clear();
                this.GetControl<Label>("BigTitle").Content = idleString;
                return;
            }
            if (e.KeyModifiers == Avalonia.Input.KeyModifiers.Control)
            {
                switch (e.Key)
                {
                    case Avalonia.Input.Key.P:
                        if (mp.IsPlaying) { mp.Pause(); } else { mp.Play(); };
                        break;
                    case Avalonia.Input.Key.O:
                        AddMusic();
                        break;
                    case Avalonia.Input.Key.Left:
                        mp.Stop();
                        track--;
                        PlayTrack();
                        break;
                    case Avalonia.Input.Key.Right:
                        mp.Stop();
                        track++;
                        PlayTrack();
                        break;
                }
            } else if (e.KeyModifiers == Avalonia.Input.KeyModifiers.Alt)
            {
                switch (e.Key)
                {
                    case Avalonia.Input.Key.Left:
                        mp.Time -= 10000;
                        break;
                    case Avalonia.Input.Key.Right:
                        mp.Time += 10000;
                        break;
                }
            }
        }

        private void Control_OnSizeChanged(object? sender, SizeChangedEventArgs e)
        {
            SizeCheck();
        }

        private void BigTitle_OnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (!Program.streamMode) return;
            using var fs = new FileStream(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/.mas/songname.txt", FileMode.Create, FileAccess.Write);
            fs.Write(Encoding.UTF8.GetBytes(this.GetControl<Label>("BigTitle")?.Content?.ToString().Replace("Hetkel esitusel: ", "") ?? ""));
            fs.Close();
        }

        private void Control_OnLoaded(object? sender, RoutedEventArgs e)
        {
            if (!OperatingSystem.IsMacOS() || Program.streamMode) return;
			new Thread(() => {
				Thread.Sleep(1000);
				Dispatcher.UIThread.Post(() => {
		            this.WindowState = WindowState.Normal;
		            this.WindowState = WindowState.Maximized;
    		        this.WindowState = WindowState.FullScreen;
				});
			}).Start();
        }
    }
}