using System;
using System.Windows;
using System.IO;
using System.Collections.ObjectModel;
using System.Windows.Media;
using System.Windows.Threading;
using System.Windows.Controls;
using Microsoft.Win32;
using NAudio.Wave;
using MusicPlayer.Properties;
using System.Windows.Input;
using System.Windows.Data;
using System.Collections.Generic;

namespace MusicPlayer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Fields

        private MediaPlayer player = new MediaPlayer();
        private DispatcherTimer timer;
        private ObservableCollection<MusicFile> MusicQueue { get; set; }

        private string currentDirectory;
        private int currentSong = 0;

        private bool muted = false;
        private bool nightMode = false;
        private bool shuffle = false;
        private bool repeat = false;
        private bool paused = true;
        private bool firstOpen = true;

        #endregion

        #region Main Windows

        public MainWindow()
        {
            InitializeComponent();

            AddHandler(Keyboard.KeyDownEvent, (KeyEventHandler)HandleKeyDownEvent);

            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            player.MediaEnded += playerMediaEnded;
            player.Volume = DefaultSettings.DefaultVolume;

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick;

            MusicQueue = new ObservableCollection<MusicFile>();
            AddSymbols();

            if (TimeBetween(DateTime.Now, DefaultSettings.StartNightMode, DefaultSettings.StopNightMode))
            {
                nightMode = true;
                NightModeInterface();
            }
            else
                NormalModeInterface();

        }

        #endregion

        #region Play/Last/Next Click Events

        private void btnPlayPause_Click(object sender, RoutedEventArgs e)
        {
            if (MusicQueue.Count == 0)
                return;

            paused = !paused;
            lblCurrentlyPlaying.Content = "Currently Playing: " + MusicQueue[currentSong].Title;

            // paused = true -> paused
            if (paused)
            {
                player.Pause();
                btnPlayPause.Content = DefaultSettings.PlaySymbol;
            }
            // paused = false -> playing
            else
            {
                timer.Start();
                player.Play();
                btnPlayPause.Content = DefaultSettings.PauseSymbol;
            }
        }

        private void btnNextTrack_Click(object sender, RoutedEventArgs e)
        {
            if (MusicQueue.Count == 0)
                return;

            if (currentSong + 1 == MusicQueue.Count)
                currentSong = -1;

            if (shuffle)
            {
                currentSong = new Random().Next(0, MusicQueue.Count + 1);
                PlaySong(currentSong);
            }

            else
                PlaySong(currentSong += 1);
        }

        private void btnLastTrack_Click(object sender, RoutedEventArgs e)
        {
            if (MusicQueue.Count == 0)
                return;

            if (currentSong == 0)
                currentSong = MusicQueue.Count;

            if (shuffle)
            {
                currentSong = new Random().Next(0, MusicQueue.Count + 1);
                PlaySong(currentSong);
            }

            else
                PlaySong(currentSong -= 1);
        }

        #endregion

        #region Repeat/Shuffle Click Events

        private void btnRepeat_Click(object sender, RoutedEventArgs e)
        {
            repeat = !repeat;

            if (repeat)
                btnRepeat.Foreground = DefaultSettings.GreenForeground;
            else
                CheckNightMode(btnRepeat);

        }

        private void btnShuffle_Click(object sender, RoutedEventArgs e)
        {
            shuffle = !shuffle;

            if (shuffle)
                btnShuffle.Foreground = DefaultSettings.GreenForeground;

            else
                CheckNightMode(btnShuffle);

        }

        #endregion

        #region Mute/NM Click Events

        private void btnMute_Click(object sender, RoutedEventArgs e)
        {
            muted = !muted;

            if (muted)
            {
                player.Volume = 0;
                btnMute.Content = DefaultSettings.MuteSymbol;
            }
            else
            {
                player.Volume = DefaultSettings.DefaultVolume;
                btnMute.Content = DefaultSettings.SpeakerSymbol;
                CheckNightMode(btnMute);
            }
        }

        private void btnNightMode_Click(object sender, RoutedEventArgs e)
        {
            if (nightMode)
                NormalModeInterface();
            else
                NightModeInterface();

            nightMode = !nightMode;
        }

        #endregion

        #region Textbox Text Changed

        private void txtFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (txtFilter.Text == "")
                dataGrid.ItemsSource = MusicQueue;

            try
            {
                ListCollectionView collectionView = new ListCollectionView(MusicQueue);
                collectionView.Filter = (ex) =>
                {
                    MusicFile file = ex as MusicFile;
                    if (file.Title.IsMatch(txtFilter.Text))
                    {
                        dataGrid.ItemsSource = collectionView;
                        return true;
                    }
                    return false;
                };
            }
            catch (Exception ee) { MessageBox.Show(ee.Message); }
        }

        #endregion

        #region Add Track

        private void btnAddTrack_Click(object sender, RoutedEventArgs e)
        {
            string fileName = "";
            string[] fileDirectories;
            TimeSpan duration = new TimeSpan();

            if (Settings.Default.PreloadOnClick && firstOpen)
            {
                try
                {
                    PreloadSongs(Settings.Default.PreloadDirectory);
                    musicProgress.Maximum = MusicQueue[currentSong].Duration.TotalSeconds;
                    lblMaxTime.Content = MusicQueue[currentSong].Duration.ToString(DefaultSettings.TimeSpanFormat);
                    dataGrid.ItemsSource = MusicQueue;
                    player.Open(new Uri(MusicQueue[currentSong].FilePath));
                    firstOpen = false;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error Loading Files", "File Loading Error");
                }
            }

            else
            {
                try
                {
                    OpenFileDialog ofd = new OpenFileDialog();
                    ofd.Multiselect = true;
                    ofd.Filter = "MP3 Files (*.mp3)|*.mp3";
                    ofd.InitialDirectory = Settings.Default.OpenDirecory;
                    if (ofd.ShowDialog() == true)
                    {
                        fileDirectories = ofd.FileNames;
                        foreach (string fileDir in fileDirectories)
                        {
                            currentDirectory = fileDir;
                            fileName = FormatFileName(fileDir);
                            duration = GetMP3Time(fileDir);

                            MusicFile currentNew = new MusicFile(fileName, currentDirectory, duration);
                            MusicQueue.Add(currentNew);
                            musicProgress.Maximum = currentNew.Duration.TotalSeconds + DefaultSettings.Buffer;
                            lblMaxTime.Content = currentNew.Duration.TotalSeconds.ToString(DefaultSettings.TimeSpanFormat);
                        }

                        dataGrid.SelectedIndex = currentSong;

                        if (firstOpen)
                            player.Open(new Uri(MusicQueue[currentSong].FilePath));
                        firstOpen = false;
                    }
                    dataGrid.ItemsSource = MusicQueue;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        #endregion

        #region Created Events

        private void HandleKeyDownEvent(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.S && (Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Shift)) == (ModifierKeys.Control | ModifierKeys.Shift))
            {
                SettingsWindow settingsWindow = new SettingsWindow();
                settingsWindow.ShowDialog();
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (!paused)
            {
                musicProgress.Value = player.Position.TotalSeconds;
                lblCurrentProgress.Content = player.Position.ToString(DefaultSettings.TimeSpanFormat);
            }
        }

        private void playerMediaEnded(object sender, EventArgs e)
        {
            try
            {
                if (repeat)
                {
                    player.Position = TimeSpan.Zero;
                    player.Play();
                }

                else if (shuffle)
                {
                    currentSong = new Random().Next(0, MusicQueue.Count + 1);
                    if (MusicQueue[currentSong].BeenPlayed)
                        currentSong = new Random().Next(0, MusicQueue.Count + 1);
                    PlaySong(currentSong);
                }
                else
                {
                    if (currentSong + 1 == MusicQueue.Count)
                        currentSong = -1;

                    currentSong++;
                    PlaySong(currentSong);
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        #endregion

        #region Interfaces/Setup

        private void AddSymbols()
        {
            btnNextTrack.Content = DefaultSettings.NextTrack;
            btnPlayPause.Content = DefaultSettings.PlaySymbol;
            btnLastTrack.Content = DefaultSettings.LastTrack;
            btnShuffle.Content = DefaultSettings.Shuffle;
            btnRepeat.Content = DefaultSettings.RepeatOnce;
        }

        private void CheckNightMode(Control c1)
        {
            if (!(c1 is Button))
                return;

            if (nightMode)
                c1.Foreground = DefaultSettings.ButtonForegroundNight;
            else
                c1.Foreground = DefaultSettings.ButtonForeground;
        }

        private void NormalModeInterface()
        {
            Background = DefaultSettings.DefaultBackround;

            btnAddTrack.Background = DefaultSettings.DefaultBackround;
            btnAddTrack.Foreground = DefaultSettings.ButtonForeground;

            btnNextTrack.Background = DefaultSettings.DefaultBackround;
            btnNextTrack.Foreground = DefaultSettings.ButtonForeground;

            btnPlayPause.Background = DefaultSettings.DefaultBackround;
            btnPlayPause.Foreground = DefaultSettings.ButtonForeground;

            btnLastTrack.Background = DefaultSettings.DefaultBackround;
            btnLastTrack.Foreground = DefaultSettings.ButtonForeground;

            btnNightMode.Background = DefaultSettings.DefaultBackround;
            btnNightMode.Foreground = DefaultSettings.ButtonForeground;

            btnShuffle.Background = DefaultSettings.DefaultBackround;
            btnRepeat.Background = DefaultSettings.DefaultBackround;
            btnMute.Background = DefaultSettings.DefaultBackround;

            dataGrid.Background = DefaultSettings.DefaultBackround;

            if (!shuffle)
                btnShuffle.Foreground = DefaultSettings.ButtonForeground;


            if (!repeat)
                btnRepeat.Foreground = DefaultSettings.ButtonForeground;


            if (!muted)
                btnMute.Foreground = DefaultSettings.ButtonForeground;


            lblCurrentlyPlaying.Foreground = Brushes.Black;
            lblCurrentProgress.Foreground = Brushes.Black;
            lblMaxTime.Foreground = Brushes.Black;
            lblVolumeTxt.Foreground = Brushes.Black;
        }

        private void NightModeInterface()
        {
            Background = DefaultSettings.DefaultBackroundNight;

            btnAddTrack.Background = DefaultSettings.DefaultBackroundNight;
            btnAddTrack.Foreground = DefaultSettings.ButtonForegroundNight;

            btnNextTrack.Background = DefaultSettings.DefaultBackroundNight;
            btnNextTrack.Foreground = DefaultSettings.ButtonForegroundNight;

            btnPlayPause.Background = DefaultSettings.DefaultBackroundNight;
            btnPlayPause.Foreground = DefaultSettings.ButtonForegroundNight;

            btnLastTrack.Background = DefaultSettings.DefaultBackroundNight;
            btnLastTrack.Foreground = DefaultSettings.ButtonForegroundNight;

            btnNightMode.Background = DefaultSettings.DefaultBackroundNight;
            btnNightMode.Foreground = DefaultSettings.ButtonForegroundNight;

            btnShuffle.Background = DefaultSettings.DefaultBackroundNight;
            btnRepeat.Background = DefaultSettings.DefaultBackroundNight;
            btnMute.Background = DefaultSettings.DefaultBackroundNight;

            dataGrid.Background = DefaultSettings.DefaultGridBackroundNight;

            if (!shuffle)
                btnShuffle.Foreground = DefaultSettings.ButtonForegroundNight;


            if (!repeat)
                btnRepeat.Foreground = DefaultSettings.ButtonForegroundNight;


            if (!muted)
                btnMute.Foreground = DefaultSettings.ButtonForegroundNight;


            lblCurrentlyPlaying.Foreground = Brushes.White;
            lblCurrentProgress.Foreground = Brushes.White;
            lblMaxTime.Foreground = Brushes.White;
            lblVolumeTxt.Foreground = Brushes.White;
        }

        #endregion

        #region Methods

        private void PlaySong(int index)
        {
            timer.Start();
            MusicQueue[index].BeenPlayed = true;
            dataGrid.SelectedIndex = index;
            object view = dataGrid.Items[index];
            dataGrid.ScrollIntoView(view);
            lblMaxTime.Content = MusicQueue[index].Duration.ToString(DefaultSettings.TimeSpanFormat);
            lblCurrentlyPlaying.Content = "Currently Playing: " + MusicQueue[index].Title;
            musicProgress.Maximum = MusicQueue[index].Duration.TotalSeconds + DefaultSettings.Buffer;
            player.Open(new Uri(MusicQueue[index].FilePath));
            player.Play();
        }

        private void PreloadSongs(string directory)
        {
            TimeSpan duration = new TimeSpan();
            string fileName = "";
            var fileDirectory = Directory.EnumerateFiles(directory);

            foreach (string fileDir in fileDirectory)
            {
                currentDirectory = fileDir;
                fileName = FormatFileName(fileDir);
                duration = GetMP3Time(fileDir);

                MusicQueue.Add(new MusicFile(fileName, currentDirectory, duration));
            }
            dataGrid.SelectedIndex = currentSong;
        }

        private static bool TimeBetween(DateTime dt, TimeSpan start, TimeSpan end)
        {
            TimeSpan now = dt.TimeOfDay;
            if (start < end)
                return start <= now && now <= end;
            return !(end < now && now < start);
        }

        private bool IsStartEndTrack()
        {
            return currentSong == 0 || currentSong + 1 == MusicQueue.Count;
        }

        private static TimeSpan GetMP3Time(string path)
        {
            Mp3FileReader reader = new Mp3FileReader(path);
            TimeSpan duration = reader.TotalTime;
            return duration.StripMilliseconds();
        }

        private string FormatFileName(string directory)
        {
            int lastIndexOf = directory.LastIndexOf('\\');
            string title = directory.Substring(lastIndexOf + 1);
            int index = title.LastIndexOf('.');
            return title.Remove(index);
        }

        #endregion

        #region Slider/DataGrid Events

        private double SetProgressBarValue(double x)
        {
            double ratio = x / musicProgress.ActualWidth;
            double value = ratio * musicProgress.Maximum;
            return value;
        }

        private void musicProgress_LeftMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (MusicQueue.Count == 0 || paused)
                return;

            double mousePosition = e.GetPosition(musicProgress).X;
            musicProgress.Value = SetProgressBarValue(mousePosition);
            player.Position = new TimeSpan(0, 0, (int)musicProgress.Value);
        }

        private void slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            player.Volume = slider.Value / 100;
        }

        private void dataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            DataGridRow row = ItemsControl.ContainerFromElement((DataGrid)sender, e.OriginalSource as DependencyObject) as DataGridRow;
            if (row == null)
            {
                e.Handled = true;
                return;
            }

            paused = false;
            btnPlayPause.Content = DefaultSettings.PauseSymbol;
            currentSong = row.GetIndex();
            PlaySong(currentSong);
        }

        #endregion
    }
}
