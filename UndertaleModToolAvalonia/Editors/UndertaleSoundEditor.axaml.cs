using System;
using System.ComponentModel;
using System.IO;
using Avalonia.Interactivity;
using Avalonia.Threading;
using UndertaleModLib;
using UndertaleModLib.Models;
using UndertaleModLib.Util;

namespace UndertaleModTool
{
    public partial class UndertaleSoundEditor : DataUserControl
    {
        private static MainWindow mainWindow => MainWindow.Instance;

        private readonly AudioPlayer player = new();
        private UndertaleData audioGroupData;
        private string loadedPath;
        private UndertaleSound subscribed;

        public UndertaleSoundEditor()
        {
            InitializeComponent();
            Unloaded += OnUnloaded;
            DataContextChanged += OnDataContextChanged;
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            player.Stop();
            if (subscribed is not null)
            {
                subscribed.PropertyChanged -= OnPropertyChanged;
                subscribed = null;
            }
        }

        private void OnDataContextChanged(object sender, EventArgs e)
        {
            if (subscribed is not null)
                subscribed.PropertyChanged -= OnPropertyChanged;

            subscribed = DataContext as UndertaleSound;

            if (subscribed is not null)
                subscribed.PropertyChanged += OnPropertyChanged;
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            OnAssetUpdated();
        }

        private void OnAssetUpdated()
        {
            if (mainWindow.Project is null || !mainWindow.IsSelectedProjectExportable)
                return;

            Dispatcher.UIThread.Post(() =>
            {
                if (DataContext is UndertaleSound obj)
                    mainWindow.Project?.MarkAssetForExport(obj);
            });
        }

        private void Play_Click(object sender, RoutedEventArgs e)
        {
            UndertaleSound sound = DataContext as UndertaleSound;

            if ((sound.Flags & UndertaleSound.AudioEntryFlags.IsEmbedded) != UndertaleSound.AudioEntryFlags.IsEmbedded &&
                (sound.Flags & UndertaleSound.AudioEntryFlags.IsCompressed) != UndertaleSound.AudioEntryFlags.IsCompressed)
            {
                try
                {
                    string filename;
                    if (!sound.File.Content.Contains("."))
                        filename = sound.File.Content + ".ogg";
                    else
                        filename = sound.File.Content;
                    string audioPath = Paths.JoinVerifyWithinDirectory(Path.GetDirectoryName(mainWindow.FilePath), filename);
                    if (File.Exists(audioPath))
                        player.PlayFile(audioPath);
                    else
                        throw new Exception("Failed to find audio file.");
                }
                catch (Exception ex)
                {
                    mainWindow.ShowError("Failed to play audio!\r\n" + ex.Message, "Audio failure");
                }
                return;
            }

            UndertaleEmbeddedAudio target;

            if (sound.GroupID != 0 && sound.AudioID != -1)
            {
                try
                {
                    string relativePath;
                    if (sound.AudioGroup is UndertaleAudioGroup { Path.Content: string customRelativePath })
                        relativePath = customRelativePath;
                    else
                        relativePath = $"audiogroup{sound.GroupID}.dat";
                    string path = Paths.JoinVerifyWithinDirectory(Path.GetDirectoryName(mainWindow.FilePath), relativePath);
                    if (File.Exists(path))
                    {
                        if (loadedPath != path)
                        {
                            loadedPath = path;

                            using FileStream stream = new(path, FileMode.Open, FileAccess.Read);
                            audioGroupData = UndertaleIO.Read(stream, (warning, _) =>
                            {
                                throw new Exception(warning);
                            });
                        }

                        target = audioGroupData.EmbeddedAudio[sound.AudioID];
                    }
                    else
                        throw new Exception("Failed to find audio group file.");
                }
                catch (Exception ex)
                {
                    mainWindow.ShowError("Failed to play audio!\r\n" + ex.Message, "Audio failure");
                    return;
                }
            }
            else
                target = sound.AudioFile;

            if (target != null)
            {
                if (target.Data.Length > 4)
                {
                    try
                    {
                        player.Play(target.Data);
                    }
                    catch (Exception ex)
                    {
                        mainWindow.ShowError("Failed to play audio!\r\n" + ex.Message, "Audio failure");
                    }
                }
            }
            else
                mainWindow.ShowError("Failed to play audio!\r\nNo options for playback worked.", "Audio failure");
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            player.Stop();
        }
    }
}
