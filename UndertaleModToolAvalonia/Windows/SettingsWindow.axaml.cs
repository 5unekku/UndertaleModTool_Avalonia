using System;
using System.Diagnostics;
using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace UndertaleModTool
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
            DataContext = Settings.Instance;
        }

        protected override void OnClosing(WindowClosingEventArgs e)
        {
            Settings.Save();
            base.OnClosing(e);
        }

        private void AppDataButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Directory.CreateDirectory(Settings.AppDataFolder);
                Process.Start(new ProcessStartInfo { FileName = Settings.AppDataFolder, UseShellExecute = true });
            }
            catch (Exception ex)
            {
                this.ShowError("Could not open the application data folder:\n" + ex.Message);
            }
        }

        private void GMLSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var settings = new GMLSettingsWindow(Settings.Instance);
            settings.ShowDialogSync(this);
            Settings.Save();
        }
    }
}
