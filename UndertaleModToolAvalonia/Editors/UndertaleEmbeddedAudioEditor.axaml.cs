using System;
using System.IO;
using Avalonia;
using Avalonia.Interactivity;
using UndertaleModLib.Models;

namespace UndertaleModTool
{
    public partial class UndertaleEmbeddedAudioEditor : DataUserControl
    {
        private static MainWindow mainWindow => MainWindow.Instance;

        private readonly AudioPlayer player = new();
        private string fileType = "Unknown";

        // FileType is a direct property so the header binding updates; avalonia controls cannot add a plain
        // INotifyPropertyChanged.PropertyChanged event (it clashes with AvaloniaObject's own).
        public static readonly DirectProperty<UndertaleEmbeddedAudioEditor, string> FileTypeProperty =
            AvaloniaProperty.RegisterDirect<UndertaleEmbeddedAudioEditor, string>(nameof(FileType), o => o.FileType);

        public string FileType
        {
            get => fileType;
            private set => SetAndRaise(FileTypeProperty, ref fileType, value);
        }

        public UndertaleEmbeddedAudioEditor()
        {
            InitializeComponent();
            DataContextChanged += (_, _) => FileType = ComputeFileType();
            Unloaded += (_, _) => player.Stop();
        }

        private string ComputeFileType()
        {
            if (DataContext is not UndertaleEmbeddedAudio target)
                return "Unknown";
            if (AudioPlayer.IsWav(target.Data))
                return "WAV";
            if (AudioPlayer.IsOgg(target.Data))
                return "OGG";
            return "Unknown";
        }

        private void Import_Click(object sender, RoutedEventArgs e)
        {
            UndertaleEmbeddedAudio target = DataContext as UndertaleEmbeddedAudio;

            OpenFileDialog dlg = new();
            if (AudioPlayer.IsWav(target.Data))
            {
                dlg.DefaultExt = ".wav";
                dlg.Filter = "WAV files|*.wav|All files|*";
            }
            else if (AudioPlayer.IsOgg(target.Data))
            {
                dlg.DefaultExt = ".ogg";
                dlg.Filter = "OGG files|*.ogg|All files|*";
            }
            else
            {
                dlg.DefaultExt = "";
                dlg.Filter = "All files|*";
            }

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    byte[] data = File.ReadAllBytes(dlg.FileName);
                    if (!AudioPlayer.IsWav(data) && !AudioPlayer.IsOgg(data))
                    {
                        if (mainWindow.ShowQuestionWithCancel("Warning: File being imported is not a WAV or OGG. Import anyway?\r\n\r\nThis may corrupt the sound.", MessageBoxImage.Warning, "Unknown format") != MessageBoxResult.Yes)
                            return;
                    }
                    else if ((AudioPlayer.IsWav(target.Data) && AudioPlayer.IsOgg(data)) || (AudioPlayer.IsOgg(target.Data) && AudioPlayer.IsWav(data)))
                    {
                        if (mainWindow.ShowQuestionWithCancel(
                            "Warning: Filetype being imported does not match existing filetype. Import anyway?\r\n\r\n" +
                            "This may corrupt the sound, unless sound asset compression settings are adjusted as well.", MessageBoxImage.Warning, "Format mismatch") != MessageBoxResult.Yes)
                            return;
                    }
                    target.Data = data;
                    FileType = ComputeFileType();
                }
                catch (Exception ex)
                {
                    mainWindow.ShowError("Failed to import file: " + ex.Message, "Failed to import file");
                }
            }
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            UndertaleEmbeddedAudio target = DataContext as UndertaleEmbeddedAudio;

            SaveFileDialog dlg = new();
            if (AudioPlayer.IsWav(target.Data))
            {
                dlg.DefaultExt = ".wav";
                dlg.Filter = "WAV files|*.wav|All files|*";
            }
            else if (AudioPlayer.IsOgg(target.Data))
            {
                dlg.DefaultExt = ".ogg";
                dlg.Filter = "OGG files|*.ogg|All files|*";
            }
            else
            {
                dlg.DefaultExt = "";
                dlg.Filter = "All files|*";
            }

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    File.WriteAllBytes(dlg.FileName, target.Data);
                }
                catch (Exception ex)
                {
                    mainWindow.ShowError("Failed to export file: " + ex.Message, "Failed to export file");
                }
            }
        }

        private void Play_Click(object sender, RoutedEventArgs e)
        {
            UndertaleEmbeddedAudio target = DataContext as UndertaleEmbeddedAudio;

            try
            {
                player.Play(target.Data);
            }
            catch (Exception ex)
            {
                mainWindow.ShowError("Failed to play audio!\r\n" + ex.Message, "Audio failure");
            }
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            player.Stop();
        }
    }
}
