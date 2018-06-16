using System.Windows;
using System.IO;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace MusicPlayer
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            if (Properties.Settings.Default.PreloadOnClick)
                cbPreloadOnClick.IsChecked = true;
        }

        private void cbPreloadOnClick_Click(object sender, RoutedEventArgs e)
        {
            if (cbPreloadOnClick.IsChecked == true)
                Properties.Settings.Default.PreloadOnClick = true;
            else
                Properties.Settings.Default.PreloadOnClick = false;
        }

        private void btnPreloadDir_Click(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.InitialDirectory = Properties.Settings.Default.PreloadDirectory;
            dialog.IsFolderPicker = true;
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                Properties.Settings.Default.PreloadDirectory = dialog.FileName;
        }

        private void btnDefaultDir_Click(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.InitialDirectory = Properties.Settings.Default.OpenDirecory;
            dialog.IsFolderPicker = true;
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                Properties.Settings.Default.OpenDirecory = dialog.FileName;
        }

        private void Window_Closed(object sender, System.EventArgs e)
        {
            Properties.Settings.Default.Save();
        }
    }
}
