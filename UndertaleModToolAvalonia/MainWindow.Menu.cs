using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Interactivity;
using UndertaleModLib;

namespace UndertaleModTool
{
    // top menu handlers ported 1:1 from the wpf MainWindow (File/Find/Help). run-game and project menus
    // live alongside their heavier handlers in other partials.
    public partial class MainWindow
    {
        private void New_Click(object sender, RoutedEventArgs e) => MakeNewDataFile();

        private void GitHub_Click(object sender, RoutedEventArgs e)
            => OpenBrowser("https://github.com/UnderminersTeam/UndertaleModTool");

        private void About_Click(object sender, RoutedEventArgs e)
            => this.ShowMessage("UndertaleModTool by krzys_h and the Underminers team\nVersion " + Version, "About");

        private void FindUnreferencedAssets_Click(object sender, RoutedEventArgs e)
        {
            if (Data is null)
                return;
            Windows.FindReferencesTypesDialog dialog = null;
            try
            {
                dialog = new Windows.FindReferencesTypesDialog(Data);
                dialog.ShowDialogSync(this);
            }
            catch (Exception ex)
            {
                this.ShowError("An error occurred in the object references related window.\n" +
                               $"Please report this on GitHub.\n\n{ex}");
            }
            finally
            {
                dialog?.Close();
            }
        }

        // dumps the byte-offset -> object map of a chosen data file to a text file (debug aid). 1:1 with wpf.
        private async void OffsetMap_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog { DefaultExt = "win", Filter = "GameMaker main data files (.win, .unx, .ios, .droid)|*.win;*.unx;*.ios;*.droid|All files|*" };
            if (dlg.ShowDialog(this) != true)
                return;

            var dlgout = new SaveFileDialog
            {
                DefaultExt = "txt",
                Filter = "Text files (.txt)|*.txt|All files|*",
                FileName = dlg.FileName + ".offsetmap.txt"
            };
            if (dlgout.ShowDialog(this) != true)
                return;

            var dialog = new LoaderDialog("Generating", "Loading, please wait...");
            Task t = Task.Run(() =>
            {
                try
                {
                    using var stream = new FileStream(dlg.FileName, FileMode.Open, FileAccess.Read);
                    var offsets = UndertaleIO.GenerateOffsetMap(stream);
                    using var writer = File.CreateText(dlgout.FileName);
                    foreach (var off in offsets.OrderBy(x => x.Key))
                        writer.WriteLine(off.Key.ToString("X8") + " " + off.Value.ToString().Replace("\n", "\\\n"));
                }
                catch (Exception ex)
                {
                    this.ShowError("An error occurred while trying to load:\n" + ex.Message, "Load error");
                }
                dialog.TryClose();
            });
            dialog.ShowDialogSync(this);
            await t;
        }

        /// <summary>opens a url in the system browser (cross-platform; 1:1 with the wpf helper).</summary>
        public static void OpenBrowser(string url)
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    using var process = Process.Start(new ProcessStartInfo
                    {
                        FileName = "/bin/sh",
                        Arguments = $"-c \"{$"xdg-open {url}".Replace("\"", "\\\"")}\"",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden
                    });
                }
                else
                {
                    using var process = Process.Start(new ProcessStartInfo
                    {
                        FileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? url : "open",
                        Arguments = RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? $"{url}" : "",
                        CreateNoWindow = true,
                        UseShellExecute = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                    });
                }
            }
            catch (Exception e)
            {
                Instance?.ShowError("Failed to open browser!\n" + e);
            }
        }

        /// <summary>opens a folder in the system file manager (cross-platform; 1:1 with the wpf helper).</summary>
        public static void OpenFolder(string folder)
        {
            if (!folder.EndsWith(Path.DirectorySeparatorChar))
                folder += Path.DirectorySeparatorChar;
            try
            {
                Process.Start(new ProcessStartInfo { FileName = folder, UseShellExecute = true, Verb = "Open" });
            }
            catch (Exception e)
            {
                Instance?.ShowError("Failed to open folder!\n" + e);
            }
        }
    }
}
