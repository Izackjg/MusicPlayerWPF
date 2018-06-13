using System;
using System.Windows;
using System.IO;
using System.Collections.ObjectModel;
using System.Windows.Media;
using System.Windows.Threading;
using System.Windows.Controls;
using Microsoft.Win32;
using NAudio.Wave;

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
        private bool loopSong = false;
        private bool paused = true;
        private bool firstOpen = true;

        private bool preload = true;

        #endregion

        #region Main Windows

        public MainWindow()
        {
            InitializeComponent();

            player.MediaEnded += playerMediaEnded;
            player.Volume = 0.1;

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick;

            MusicQueue = new ObservableCollection<MusicFile>();
            AddSymbols();
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
                btnPlayPause.Content = Settings.PlaySymbol;
            }
            // paused = false -> playing
            else
            {
                timer.Start();
                player.Play();
                btnPlayPause.Content = Settings.PauseSymbol;
            }
        }

        private void btnNextTrack_Click(object sender, RoutedEventArgs e)
        {
            if (currentSong + 1 == MusicQueue.Count || MusicQueue.Count == 0)
                return;
            PlaySong(currentSong += 1);

            //currentSong++;
            //dataGrid.SelectedIndex = currentSong;
            //lblMaxTime.Content = MusicQueue[currentSong].Duration.ToString(Settings.TimeSpanFormat);
            //lblCurrentlyPlaying.Content = "Currently Playing: " + MusicQueue[currentSong].Title;
            //musicProgress.Maximum = MusicQueue[currentSong].Duration.TotalSeconds;
            //player.Open(new Uri(MusicQueue[currentSong].FilePath));
            //player.Play();
        }

        private void btnLastTrack_Click(object sender, RoutedEventArgs e)
        {
            if (currentSong == 0)
                return;
            PlaySong(currentSong -= 1);

            //currentSong--;
            //dataGrid.SelectedIndex = currentSong;
            //lblMaxTime.Content = MusicQueue[currentSong].Duration.ToString(Settings.TimeSpanFormat);
            //lblCurrentlyPlaying.Content = "Currently Playing: " + MusicQueue[currentSong].Title;
            //musicProgress.Maximum = MusicQueue[currentSong].Duration.TotalSeconds;
            //player.Open(new Uri(MusicQueue[currentSong].FilePath));
            //player.Play();
        }

        #endregion

        #region Repeat/Shuffle Click Events

        private void btnRepeat_Click(object sender, RoutedEventArgs e)
        {
            loopSong = !loopSong;

            if (loopSong)
                btnRepeat.Foreground = Settings.GreenForeground;
            else
                CheckNightMode(btnRepeat);

        }

        private void btnShuffle_Click(object sender, RoutedEventArgs e)
        {
            shuffle = !shuffle;

            if (shuffle)
                btnShuffle.Foreground = Settings.GreenForeground;

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
                btnMute.Foreground = Settings.MutedForeground;
            }
            else
            {
                player.Volume = 0.2;
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

        #region Add Track

        private void btnAddTrack_Click(object sender, RoutedEventArgs e)
        {
            string fileName = "";
            string[] fileDirectories;
            TimeSpan duration = new TimeSpan();

            if (preload)
            {
                PreloadSongs(Settings.DefaultFileDir);
                musicProgress.Maximum = MusicQueue[currentSong].Duration.TotalSeconds;
                lblMaxTime.Content = MusicQueue[currentSong].Duration.ToString(Settings.TimeSpanFormat);
                dataGrid.ItemsSource = MusicQueue;
                player.Open(new Uri(MusicQueue[currentSong].FilePath));
                firstOpen = false;
                preload = false;
            }
            else
            {
                try
                {
                    OpenFileDialog ofd = new OpenFileDialog();
                    ofd.Multiselect = true;
                    ofd.Filter = "MP3 Files (*.mp3)|*.mp3";
                    ofd.InitialDirectory = Settings.DefaultFileDir;
                    if (ofd.ShowDialog() == true)
                    {
                        fileDirectories = ofd.FileNames;

                        foreach (string fileDir in fileDirectories)
                        {
                            currentDirectory = fileDir;
                            fileName = FormatFileName(fileDir);
                            duration = GetMP3Time(fileDir);

                            MusicQueue.Add(new MusicFile(fileName, currentDirectory, duration));
                            musicProgress.Maximum = MusicQueue[currentSong].Duration.TotalSeconds;
                            lblMaxTime.Content = MusicQueue[currentSong].Duration.ToString(Settings.TimeSpanFormat);
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

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (!paused)
            {
                musicProgress.Value = player.Position.TotalSeconds;
                lblCurrentProgress.Content = player.Position.ToString(Settings.TimeSpanFormat);
            }
        }

        private void playerMediaEnded(object sender, EventArgs e)
        {
            try
            {
                if (loopSong)
                {
                    player.Position = TimeSpan.Zero;
                    player.Play();
                    //return;
                }

                else if (shuffle)
                {
                    //currentSong = new Random().Next(0, MusicQueue.Count + 1);
                    PlaySong(new Random().Next(0, MusicQueue.Count + 1));
                    //return;
                }

                else
                {
                    currentSong++;
                    PlaySong(currentSong);
                    //dataGrid.SelectedIndex = currentSong;
                    //lblMaxTime.Content = MusicQueue[currentSong].Duration.ToString(Settings.TimeSpanFormat);
                    //lblCurrentlyPlaying.Content = "Currently Playing: " + MusicQueue[currentSong].Title;
                    //musicProgress.Maximum = MusicQueue[currentSong].Duration.TotalSeconds;
                    //player.Open(new Uri(MusicQueue[currentSong].FilePath));
                    //player.Play();               
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        #endregion

        #region Interfaces/Setup

        private void AddSymbols()
        {
            btnNextTrack.Content = Settings.NextTrack;
            btnPlayPause.Content = Settings.PlaySymbol;
            btnLastTrack.Content = Settings.LastTrack;
            btnShuffle.Content = Settings.Shuffle;
            btnRepeat.Content = Settings.RepeatOnce;
        }

        private void CheckNightMode(Control c1)
        {
            if (!(c1 is Button))
                return;

            if (nightMode)
                c1.Foreground = Settings.ButtonForegroundNight;
            else
                c1.Foreground = Settings.ButtonForeground;
        }

        private void NormalModeInterface()
        {
            Background = Settings.DefaultBackround;

            btnAddTrack.Background = Settings.DefaultBackround;
            btnAddTrack.Foreground = Settings.ButtonForeground;

            btnNextTrack.Background = Settings.DefaultBackround;
            btnNextTrack.Foreground = Settings.ButtonForeground;

            btnPlayPause.Background = Settings.DefaultBackround;
            btnPlayPause.Foreground = Settings.ButtonForeground;

            btnLastTrack.Background = Settings.DefaultBackround;
            btnLastTrack.Foreground = Settings.ButtonForeground;

            btnShuffle.Background = Settings.DefaultBackround;
            btnShuffle.Foreground = Settings.ButtonForeground;

            btnRepeat.Background = Settings.DefaultBackround;
            btnRepeat.Foreground = Settings.ButtonForeground;

            btnNightMode.Background = Settings.DefaultBackround;
            btnNightMode.Foreground = Settings.ButtonForeground;

            btnMute.Background = Settings.DefaultBackround;
            btnMute.Foreground = Settings.ButtonForeground;

            dataGrid.Background = Settings.DefaultBackround;

            lblCurrentlyPlaying.Foreground = Brushes.Black;
            lblCurrentProgress.Foreground = Brushes.Black;
            lblMaxTime.Foreground = Brushes.Black;
            lblVolumeTxt.Foreground = Brushes.Black;
        }

        private void NightModeInterface()
        {
            Background = Settings.DefaultBackroundNight;

            btnAddTrack.Background = Settings.DefaultBackroundNight;
            btnAddTrack.Foreground = Settings.ButtonForegroundNight;

            btnNextTrack.Background = Settings.DefaultBackroundNight;
            btnNextTrack.Foreground = Settings.ButtonForegroundNight;

            btnPlayPause.Background = Settings.DefaultBackroundNight;
            btnPlayPause.Foreground = Settings.ButtonForegroundNight;

            btnLastTrack.Background = Settings.DefaultBackroundNight;
            btnLastTrack.Foreground = Settings.ButtonForegroundNight;

            btnShuffle.Background = Settings.DefaultBackroundNight;
            btnShuffle.Foreground = Settings.ButtonForegroundNight;

            btnRepeat.Background = Settings.DefaultBackroundNight;
            btnRepeat.Foreground = Settings.ButtonForegroundNight;

            btnNightMode.Background = Settings.DefaultBackroundNight;
            btnNightMode.Foreground = Settings.ButtonForegroundNight;

            btnMute.Background = Settings.DefaultBackroundNight;
            btnMute.Foreground = Settings.ButtonForegroundNight;

            dataGrid.Background = Settings.DefaultGridBackroundNight;

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
            dataGrid.SelectedIndex = index;
            lblMaxTime.Content = MusicQueue[index].Duration.ToString(Settings.TimeSpanFormat);
            lblCurrentlyPlaying.Content = "Currently Playing: " + MusicQueue[index].Title;
            musicProgress.Maximum = MusicQueue[index].Duration.TotalSeconds;
            player.Open(new Uri(MusicQueue[index].FilePath));
            player.Play();
        }

        private void PreloadSongs(string directory)
        {
            TimeSpan duration = new TimeSpan();
            string fileName = "";
            string[] fileDirectory = Directory.GetFiles(directory);

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

        private void slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            player.Volume = slider.Value / 100;
        }

        private void slider_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            double mousePosition = e.GetPosition(musicProgress).X;
            musicProgress.Value = SetProgressBarValue(mousePosition);
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
            btnPlayPause.Content = Settings.PauseSymbol;
            currentSong = row.GetIndex();
            PlaySong(currentSong);
        }

        #endregion
    }
}
