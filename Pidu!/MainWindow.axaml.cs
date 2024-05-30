using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.Interactivity;
using System;
using LibVLCSharp.Shared;
using Avalonia.Platform.Storage;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia;
using System.IO;
using MsBox.Avalonia;

namespace Pidu_
{
    public partial class MainWindow : Window
    {
        // VLC stuff
        public LibVLC _libVLC;
        public MediaPlayer mp;
        public Media media;
        public bool isPlaying = false;
        string[] playlist;
        int track = 0;
        DispatcherTimer timer = new DispatcherTimer();
        DispatcherTimer volume = new DispatcherTimer();
        string imageSource = null;

        public MainWindow()
        {
            InitializeComponent();
            _libVLC = new LibVLC();
            mp = new MediaPlayer(_libVLC);
            timer.Interval = new TimeSpan(0, 0, 0, 0, 100);
            volume.Interval = new TimeSpan(0, 0, 0, 0, 20);
            VolumeControl.Value = mp.Volume;
            timer.Tick += (object? sender, EventArgs e) =>
            {
                if (this.WindowState != WindowState.Minimized)
                {
                    this.WindowState = WindowState.FullScreen;
                }
                if (mp.Media?.State == VLCState.Ended)
                {
                    track++;
                    if (Playlist.IsVisible)
                    {
                        Playlist.SelectedIndex = track;
                    }
                    if (track >= playlist.Length)
                    {
                        isPlaying = false;
                        BigTitle.Content = "Hetkel ei mängi ühtegi pala";
                    } else
                    {
                        PlayTrack();
                    }
                }
                if (mp.IsPlaying)
                {
                    if ((mp.Media?.Meta(MetadataType.Album) != null) && (mp.Media?.Meta(MetadataType.Artist) != null))
                    {
                        BigTitle.Content = "Hetkel esitusel: " + mp.Media.Meta(MetadataType.Artist) + " - " + mp.Media.Meta(MetadataType.Title) + " (" + mp.Media.Meta(MetadataType.Album) + ")";
                    }
                    else if (mp.Media?.Meta(MetadataType.Album) != null)
                    {
                        BigTitle.Content = "Hetkel esitusel: " + mp.Media.Meta(MetadataType.Title) + " (" + mp.Media.Meta(MetadataType.Album) + ")";
                    }
                    else if (mp.Media?.Meta(MetadataType.Artist) != null)
                    {
                        BigTitle.Content = "Hetkel esitusel: " + mp.Media.Meta(MetadataType.Artist) + " - " + mp.Media.Meta(MetadataType.Title);
                    }
                    else
                    {
                        BigTitle.Content = "Hetkel esitusel: " + mp.Media.Meta(MetadataType.Title);
                    }
                    PausePlay.Content = "||";
                    PausePlay.Background = new SolidColorBrush(Colors.DeepSkyBlue);
                } else if (!mp.IsPlaying)
                {
                    PausePlay.Content = ">";
                    PausePlay.Background = new SolidColorBrush(Colors.Lime);
                }
                StatusLabel.Content = string.Format("{0}  |  {1}%  |  {2} / {3}", DateTime.Now, mp.Volume, NiceTime(mp.Time), NiceTime(mp.Length));
                switch (mp.Media?.State)
                {
                    case VLCState.Paused:
                        StatusLabel.Content += " (paus)";
                        break;
                    case VLCState.Buffering:
                        StatusLabel.Content += " (puhverdamine)";
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
                    VolumeControl.Value = mp.Volume;
                    VolumeControl.IsEnabled = true;
                    return;
                }
                if (mp.Time < 10000L)
                {
                    mp.Volume = (int)mp.Time / 100;
                    VolumeControl.Value = mp.Volume;
                    VolumeControl.IsEnabled = false;
                }
                if ((mp.Time >= 10000L) && (mp.Time < 10500L))
                {
                    mp.Volume = 100;
                    VolumeControl.Value = mp.Volume;
                    VolumeControl.IsEnabled = true;
                }
                else if (mp.Time > mp.Length - 10000L)
                {
                    mp.Volume = ((int)(mp.Length - mp.Time) / 100);
                    VolumeControl.Value = mp.Volume;
                    VolumeControl.IsEnabled = false;
                }
            };
            timer.Start();
            volume.Start();
        }

        private string NiceTime(long millis)
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
            StatusLabel.Content = StatusLabel.Content.ToString().Replace((int)e.OldValue + "%", (int)e.NewValue + "%");
        }

        private void ShowPlaylist_PointerReleased(object? sender, Avalonia.Input.PointerReleasedEventArgs e)
        {
            if (e.InitialPressMouseButton == Avalonia.Input.MouseButton.Left)
            {
                Playlist.IsVisible = !Playlist.IsVisible;
                if ((playlist != null) && (playlist.Length > 0))
                {
                    Playlist.Items.Clear();
                    foreach (string song in playlist)
                    {
                        Playlist.Items.Add(song);
                    }
                    Playlist.SelectedIndex = track;
                }
            }
            ((TextBlock)sender).Foreground = new SolidColorBrush(Color.FromRgb(191, 191, 255));
        }

        private void ShowPlaylist_PointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
        {
            ((TextBlock)sender).Foreground = new SolidColorBrush(Color.FromRgb(128, 255, 255));
        }

        private async void AddMusic_Click(object? sender, RoutedEventArgs e)
        {
            // source: https://docs.avaloniaui.net/docs/basics/user-interface/file-dialogs
            // Get top level from the current control. Alternatively, you can use Window reference instead.
            var topLevel = TopLevel.GetTopLevel(this);

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
                        playlist[oldSongs.Length+i] = Uri.UnescapeDataString(files[i].Path.AbsolutePath);
                    }
                } else
                {
                    playlist = new string[files.Count];
                    for (int i = 0; i < files.Count; i++)
                    {
                        playlist[i] = Uri.UnescapeDataString(files[i].Path.AbsolutePath);
                    }
                }
                if (Playlist.IsVisible)
                {
                    Playlist.Items.Clear();
                    foreach (string song in playlist)
                    {
                        Playlist.Items.Add(song);
                    }
                    Playlist.SelectedIndex = track;
                }
            }
        }

        private async void PausePlay_Click(object? sender,  RoutedEventArgs e)
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
            BigTitle.Content = "Hetkel ei mängi ühtegi pala";
            playlist = Array.Empty<string>();
            Playlist.Items.Clear();
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
            if (track < playlist?.Length)
            {
                _libVLC.Dispose();
                mp.Dispose();
                _libVLC = new LibVLC();
                Media vlcMed = new Media(_libVLC, playlist[track]);   // removes URL encodings, such as %20, etc.
                mp = new MediaPlayer(vlcMed);
                mp.Play();
                if (Playlist.IsVisible)
                {
                    Playlist.SelectedIndex = track;
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
                TopButtons.IsVisible = !TopButtons.IsVisible;
                PlayControls.IsVisible = !PlayControls.IsVisible;
                Overlay.Opacity = (!TopButtons.IsVisible) ? 0 : 0.25;
                if (!TopButtons.IsVisible && Playlist.IsVisible)
                {
                    Playlist.IsVisible = false;
                }
            }
        }

        private void Playlist_DoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e)
        {
            if (Playlist?.SelectedItems?.Count > 0)
            {
                mp.Stop();
                track = Playlist.SelectedIndex;
                PlayTrack();
            }
        }

        private void ToggleFade(object? sender, RoutedEventArgs e)
        {
            volume.IsEnabled = !volume.IsEnabled;
            VolumeControl.IsEnabled = true;
            Button? me = (Button?)sender;
            if (me != null)
            {
                me.Content = volume.IsEnabled ? "Hajumine välja" : "Hajumine sisse";
            }
        }

        private async void Background_Click(object? sender, RoutedEventArgs e)
        {
            var topLevel = TopLevel.GetTopLevel(this);

            // Start async operation to open the dialog.
            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Vali taustapilt",
                AllowMultiple = false
            });
            if (files.Count > 0)
            {
                ImageBrush ib = new ImageBrush(new Bitmap(Uri.UnescapeDataString(files[0].Path.AbsolutePath)));
                ib.Stretch = Stretch.Fill;
                MainGrid.Background = ib;
                this.imageSource = files[0].Path.AbsolutePath;
            }
        }

        private async void Message_Click(object? sender, RoutedEventArgs e)
        {
            MessageEntry me = new MessageEntry();
            await me.ShowDialog(this);
            this.BigTitle.Content = me.MessageField.Text ?? "";
        }
        private void Help_Click(object? sender, RoutedEventArgs e)
        {
            _ = MessageBoxShow("Selle programmiga saate kuulata muusikat ja\ntunda ennast nagu päris DJ. Üleval vasakus nurgas on\nerinevad nupud. Hajumine välja/sisse võmaldab\nheli automaatselt hajutada palade alguses ja lõpus\nVasakul pool ekraani on esitusloend ehk\nPlaylist. Seal on mängiv muusika ja tulevased palad.\nVajutage nupul Lisa muusikat, et lisada Playlisti\nuusi palasid. Tühjenda esitusloend nupp kustutab\nkõik palad playlistist. Teil on võimalik muuta\ntaustapilti playlisti ja nuppude taga. Vaiketausta-\npilt on rahvast pidutsemas, kuid te võite panna\nselle asemel enda pildi. Teil on võimalik ka kuulata\nYouTube-ist muusikat. See avab brauseri, millest saate\nvalida video ja minna muusikapala nime valimise juurde.\nTe saate valida värve ja fonte, et muuta loo pealkirja\nisikupärasemaks. Teave kuvab täpsema info programmi\nkohta ning spikker avab praeguse dialoogi.", "Spikker", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Question);
        }

        private void About_Click(object? sender, RoutedEventArgs e)
        {
            _ = MessageBoxShow("Teeme pidu!\nNullist uuesti kirjutatud Avalonia UI ja .NET Core 8.0 raamistikes\nC# keeles.\n\nKasutab LibVLCSharp teeki\nVersioon 2.0 / 31.05.2024\nTegi: Markus Maal\nKogu muusika kuulub nende respektiivsetele omanikele.\nAutoriõiguste rikkumine ei ole lubatud!\n\nMis on uut?\n+ Üleminek Avalonia UI raamistikule\n- Eemaldati klaviatuuritulede nupp\n- Eemaldati visualiseeringu kuvamise nupp\n+ Tausta tumendamine", "Pidu!", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Info);
        }

        // Reimplementation of WinForms MessageBox.Show
        private Task MessageBoxShow(string message, string caption = "Pidu!", MsBox.Avalonia.Enums.ButtonEnum buttons = MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon icon = MsBox.Avalonia.Enums.Icon.None)
        {
            var box = MessageBoxManager.GetMessageBoxStandard(caption, message, buttons, icon);
            var result = box.ShowWindowDialogAsync(this);
            return result;
        }
    }
}