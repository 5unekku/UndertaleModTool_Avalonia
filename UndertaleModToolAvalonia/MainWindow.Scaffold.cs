using System.Threading.Tasks;
using UndertaleModLib;
using UndertaleModLib.Models;
using UndertaleModLib.Project;

namespace UndertaleModTool
{
    // the public api surface of the main window that editors and controls bind to / call into. the navigation
    // and save plumbing is stubbed here and gets its real implementation when the main window is ported (phase 9).
    // note: change notification for the main window's own properties is handled in phase 9 (avalonia's
    // AvaloniaObject already owns a PropertyChanged event, so INotifyPropertyChanged needs care here).
    public partial class MainWindow
    {
        /// <summary>the application version string.</summary>
        public const string Version = "0.9.1.1";

        /// <summary>path of the currently open data file.</summary>
        public string FilePath { get; set; }

        /// <summary>the active project context, or null when no project is open.</summary>
        public ProjectContext Project { get; set; } = null;

        /// <summary>the resource currently highlighted in the tree (drives the right-hand editor).</summary>
        public object Highlighted { get; set; }

        /// <summary>whether the selected resource can be exported to the open project.</summary>
        public bool IsSelectedProjectExportable { get; set; }

        /// <summary>whether a save is currently in progress.</summary>
        public bool IsSaving { get; set; }

        /// <summary>true when the loaded data is GameMaker Studio 2 or newer (was a Visibility in wpf).</summary>
        public bool IsGMS2 => (Data?.GeneralInfo?.Major ?? 0) >= 2;

        /// <summary>true when the data version supports extension product IDs (drives the extension editor field).</summary>
        public bool IsExtProductIDEligible =>
            Data?.GeneralInfo is UndertaleGeneralInfo info
            && (info.Major >= 2 || (info.Major == 1 && (info.Build >= 1773 || info.Build == 1539)));

        /// <summary>
        /// navigates the tab host to the given resource (new tab when requested). the matching editor is chosen
        /// by the window data templates. (OpenInTab lives in MainWindow.axaml.cs with the tab state.)
        /// </summary>
        public void ChangeSelection(object newsel, bool inNewTab = false)
        {
            OpenInTab(newsel, inNewTab);
        }

        /// <summary>prompts to save the current data file (project save-file or a save dialog). 1:1 with wpf.</summary>
        public async Task<bool> DoSaveDialog(bool suppressDebug = false)
        {
            if (Data is null)
                return false;

            // if a project is open, save to its designated data file
            if (Project is not null)
            {
                if (this.ShowQuestionWithCancel("Save to the project's designated data file for saving?") == MessageBoxResult.Yes)
                {
                    await SaveFile(Project.SaveDataPath, suppressDebug);
                    return true;
                }
                return false;
            }

            var dlg = new SaveFileDialog
            {
                DefaultExt = "win",
                Filter = "GameMaker data files (.win, .unx, .ios, .droid)|*.win;*.unx;*.ios;*.droid|All files|*",
                FileName = FilePath
            };
            if (dlg.ShowDialog(this) == true)
            {
                await SaveFile(dlg.FileName, suppressDebug);
                return true;
            }
            return false;
        }

        /// <summary>opens a child data file at a specific chunk/item. (full implementation: phase 9)</summary>
        public void OpenChildFile(string filename, string chunkName, int itemIndex)
        {
            // TODO phase 9: real child-file open
        }

        // DeleteItem + CopyItemName are implemented in MainWindow.TreeOps.cs
    }
}
