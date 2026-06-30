using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Threading;

namespace UndertaleModTool
{
    /// <summary>
    /// shared helpers for the wpf-style file dialog shims: synchronous execution (nested dispatcher frame),
    /// owner resolution, and parsing of the wpf <c>Filter</c> string into avalonia file types.
    /// </summary>
    internal static class FileDialogSupport
    {
        public static T RunSync<T>(Func<Task<T>> op)
        {
            if (!Dispatcher.UIThread.CheckAccess())
                return Dispatcher.UIThread.Invoke(() => RunSync(op));

            var frame = new DispatcherFrame();
            Task<T> task = op();
            task.ContinueWith(_ => frame.Continue = false, TaskScheduler.FromCurrentSynchronizationContext());
            Dispatcher.UIThread.PushFrame(frame);
            return task.IsCompletedSuccessfully ? task.Result : default;
        }

        public static IStorageProvider StorageFor(Window owner)
        {
            Window window = owner ?? MessageBox.MainDesktopWindow;
            TopLevel top = window is not null ? TopLevel.GetTopLevel(window) : null;
            return top?.StorageProvider;
        }

        /// <summary>parses a wpf filter ("Desc|*.a;*.b|Desc2|*.c") into avalonia file types.</summary>
        public static List<FilePickerFileType> ParseFilter(string filter)
        {
            var types = new List<FilePickerFileType>();
            if (string.IsNullOrEmpty(filter))
                return types;

            string[] parts = filter.Split('|');
            for (int i = 0; i + 1 < parts.Length; i += 2)
            {
                string name = parts[i];
                string[] patterns = parts[i + 1].Split(';', StringSplitOptions.RemoveEmptyEntries)
                                                .Select(p => p.Trim())
                                                .ToArray();
                types.Add(new FilePickerFileType(name) { Patterns = patterns });
            }
            return types;
        }

        public static async Task<IStorageFolder> StartFolder(IStorageProvider provider, string initialDirectory)
        {
            if (string.IsNullOrEmpty(initialDirectory))
                return null;
            try
            {
                return await provider.TryGetFolderFromPathAsync(initialDirectory);
            }
            catch
            {
                return null;
            }
        }
    }

    /// <summary>
    /// a drop-in replacement for wpf's <c>Microsoft.Win32.OpenFileDialog</c>, backed by avalonia's storage provider.
    /// </summary>
    public sealed class OpenFileDialog
    {
        public string Filter { get; set; }
        public string Title { get; set; }
        public string FileName { get; set; }
        public string[] FileNames { get; set; } = Array.Empty<string>();
        public bool Multiselect { get; set; }
        public string InitialDirectory { get; set; }
        public string DefaultExt { get; set; }

        public bool? ShowDialog() => ShowDialog(null);

        public bool? ShowDialog(Window owner)
        {
            IStorageProvider provider = FileDialogSupport.StorageFor(owner);
            if (provider is null)
                return false;

            return FileDialogSupport.RunSync(async () =>
            {
                var options = new FilePickerOpenOptions
                {
                    Title = Title,
                    AllowMultiple = Multiselect,
                    FileTypeFilter = FileDialogSupport.ParseFilter(Filter),
                    SuggestedStartLocation = await FileDialogSupport.StartFolder(provider, InitialDirectory)
                };

                IReadOnlyList<IStorageFile> files = await provider.OpenFilePickerAsync(options);
                if (files is null || files.Count == 0)
                    return (bool?)false;

                FileNames = files.Select(f => f.Path.LocalPath).ToArray();
                FileName = FileNames[0];
                return true;
            });
        }
    }

    /// <summary>
    /// a drop-in replacement for wpf's <c>Microsoft.Win32.SaveFileDialog</c>, backed by avalonia's storage provider.
    /// </summary>
    public sealed class SaveFileDialog
    {
        public string Filter { get; set; }
        public string Title { get; set; }
        public string FileName { get; set; }
        public string InitialDirectory { get; set; }
        public string DefaultExt { get; set; }

        public bool? ShowDialog() => ShowDialog(null);

        public bool? ShowDialog(Window owner)
        {
            IStorageProvider provider = FileDialogSupport.StorageFor(owner);
            if (provider is null)
                return false;

            return FileDialogSupport.RunSync(async () =>
            {
                var options = new FilePickerSaveOptions
                {
                    Title = Title,
                    SuggestedFileName = string.IsNullOrEmpty(FileName) ? null : Path.GetFileName(FileName),
                    DefaultExtension = DefaultExt,
                    FileTypeChoices = FileDialogSupport.ParseFilter(Filter),
                    SuggestedStartLocation = await FileDialogSupport.StartFolder(provider, InitialDirectory)
                };

                IStorageFile file = await provider.SaveFilePickerAsync(options);
                if (file is null)
                    return (bool?)false;

                FileName = file.Path.LocalPath;
                return true;
            });
        }
    }

    /// <summary>
    /// a drop-in replacement for Ookii's <c>VistaFolderBrowserDialog</c>, backed by avalonia's storage provider.
    /// </summary>
    public sealed class VistaFolderBrowserDialog
    {
        public string SelectedPath { get; set; }
        public string Description { get; set; }
        public bool UseDescriptionForTitle { get; set; }

        public bool? ShowDialog() => ShowDialog(null);

        public bool? ShowDialog(Window owner)
        {
            IStorageProvider provider = FileDialogSupport.StorageFor(owner);
            if (provider is null)
                return false;

            return FileDialogSupport.RunSync(async () =>
            {
                var options = new FolderPickerOpenOptions
                {
                    Title = UseDescriptionForTitle ? Description : null,
                    AllowMultiple = false,
                    SuggestedStartLocation = await FileDialogSupport.StartFolder(provider, SelectedPath)
                };

                IReadOnlyList<IStorageFolder> folders = await provider.OpenFolderPickerAsync(options);
                if (folders is null || folders.Count == 0)
                    return (bool?)false;

                SelectedPath = folders[0].Path.LocalPath;
                return true;
            });
        }
    }
}
