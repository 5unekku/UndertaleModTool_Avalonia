using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Interactivity;
using Avalonia.Threading;
using UndertaleModLib;
using UndertaleModLib.Project;

namespace UndertaleModTool
{
    // project menu handlers (new / open / save / view unexported / close) plus the bottom-bar object-id label
    // and marked-for-export checkbox. 1:1 port of the wpf project commands and UpdateObjectLabel.
    public partial class MainWindow
    {
        private ProjectAssetsWindow _projectAssetsWindow = null;

        // guards the marked-for-export checkbox against the change event we cause by setting IsChecked ourselves
        private bool _updatingObjectLabel = false;

        private const string ProjectDataFileFilter = "GameMaker data files (.win, .unx, .ios, .droid, audiogroup*.dat)|*.win;*.unx;*.ios;*.droid;audiogroup*.dat|All files|*";
        private const string ProjectMainDataFileFilter = "GameMaker main data files (.win, .unx, .ios, .droid)|*.win;*.unx;*.ios;*.droid|All files|*";

        private void UnloadProject()
        {
            Project = null;
            _projectAssetsWindow?.Close();
            _projectAssetsWindow = null;
        }

        private void AssignNewProject(ProjectContext project)
        {
            UnloadProject();

            Project = project;
            project.UnexportedAssetsChanged += (_, _) => UpdateObjectLabel(Selected);
            UpdateObjectLabel(Selected);
            UpdateMenuStates();
        }

        /// <summary>picks a destination data file to save the project into, warning on same-directory / empty-directory.</summary>
        private string ChooseProjectSaveFile(string sourceFilePath)
        {
            var saveDataDialog = new SaveFileDialog
            {
                DefaultExt = "win",
                Filter = ProjectMainDataFileFilter,
                Title = "Choose destination data file"
            };
            if (saveDataDialog.ShowDialog(this) != true)
                return null;

            string saveFilePath = saveDataDialog.FileName;
            if (sourceFilePath is not null && Path.GetFullPath(Path.GetDirectoryName(saveFilePath)).Equals(
                    Path.GetFullPath(Path.GetDirectoryName(sourceFilePath)), StringComparison.OrdinalIgnoreCase))
            {
                if (this.ShowQuestionWithCancel("The destination data file is in the same directory as the source data file. This may permanently overwrite external data files. Proceed?", MessageBoxImage.Warning, "Destination file in same directory as source file") != MessageBoxResult.Yes)
                    return null;
            }

            try
            {
                if (!Directory.EnumerateFileSystemEntries(Path.GetDirectoryName(saveFilePath)).Any())
                    this.ShowWarning("Currently, the destination data file's directory is empty. You will likely want to copy all other game files to the destination directory, so that external assets can be loaded correctly (both in-game and in this tool), and so the game can be started.");
            }
            catch (Exception)
            {
                // ignore filesystem errors on the above check; we don't really care
            }

            return saveFilePath;
        }

        private async void NewProject_Click(object sender, RoutedEventArgs e)
        {
            if (Project is not null && Project.HasUnexportedAssets)
            {
                if (this.ShowQuestionWithCancel("There are assets marked to be exported in the current project - create a new project and discard all unexported changes?", MessageBoxImage.Warning, "Project already open") != MessageBoxResult.Yes)
                    return;
            }

            // if necessary, ask for a source data file
            if (Data is null || FilePath is null)
            {
                var sourceDialog = new OpenFileDialog { DefaultExt = "win", Filter = ProjectDataFileFilter, Title = "Choose source data file" };
                if (sourceDialog.ShowDialog(this) != true)
                    return;
                if (!await LoadFile(sourceDialog.FileName) || Data is null || FilePath is null)
                    return;
            }

            string projectName = SimpleTextInput("Choose project name", "Choose a name for the new project", $"{Data.GeneralInfo?.DisplayName?.Content ?? "New"} Mod", false, true);
            if (projectName is null)
            {
                SetUMTConsoleText("Cancelled new project creation.");
                return;
            }
            projectName = projectName.Trim();

            string directory = PromptChooseDirectory();
            if (directory is null)
            {
                SetUMTConsoleText("Cancelled new project creation.");
                return;
            }

            string saveFilePath = ChooseProjectSaveFile(FilePath);
            if (saveFilePath is null)
                return;

            ProjectContext newProjectContext;
            try
            {
                newProjectContext = new(Data, FilePath, saveFilePath, Path.Join(directory, "project.json"), projectName, (f) => Dispatcher.UIThread.Invoke(f));
            }
            catch (ProjectException ex)
            {
                this.ShowError(ex.Message, "Failed to create new project");
                SetUMTConsoleText("Project creation failed.");
                return;
            }
            catch (Exception ex)
            {
                this.ShowError($"Error occurred when creating new project:\n{ex}");
                SetUMTConsoleText("Project creation failed.");
                return;
            }

            AssignNewProject(newProjectContext);
            SetUMTConsoleText($"Project \"{projectName}\" created successfully!");
        }

        private async void OpenProject_Click(object sender, RoutedEventArgs e)
        {
            if (Project is not null && Project.HasUnexportedAssets)
            {
                if (this.ShowQuestionWithCancel("There are assets marked to be exported in the current project - open another new project and discard all unexported changes?", MessageBoxImage.Warning, "Project already open") != MessageBoxResult.Yes)
                    return;
            }

            var openProjectDialog = new OpenFileDialog
            {
                DefaultExt = "json",
                Filter = "UndertaleModTool project files (.json)|*.json|All files|*",
                Title = "Open project file"
            };
            if (openProjectDialog.ShowDialog(this) != true)
                return;

            // if necessary, ask for a source data file
            string dataFilePathToLoad = null;
            if (Data is null || FilePath is null)
            {
                var sourceDialog = new OpenFileDialog { DefaultExt = "win", Filter = ProjectDataFileFilter, Title = "Choose source data file" };
                if (sourceDialog.ShowDialog(this) != true)
                    return;
                dataFilePathToLoad = sourceDialog.FileName;
            }

            string saveFilePath = ChooseProjectSaveFile(dataFilePathToLoad ?? FilePath);
            if (saveFilePath is null)
                return;

            if (dataFilePathToLoad is not null)
            {
                if (!await LoadFile(dataFilePathToLoad) || Data is null || FilePath is null)
                    return;
            }

            // change main file path to the save data file path
            string loadFilePath = FilePath;
            FilePath = saveFilePath;

            ProjectContext newProjectContext = null;
            IsEnabled = false;
            await Task.Run(() =>
            {
                try
                {
                    newProjectContext = ProjectContext.CreateWithDataFilePaths(loadFilePath, saveFilePath, openProjectDialog.FileName);
                    newProjectContext.Import(Data, null, (f) => Dispatcher.UIThread.Invoke(f));
                }
                catch (ProjectException ex)
                {
                    newProjectContext = null;
                    this.ShowError(ex.Message, "Failed to load project");
                }
                catch (Exception ex)
                {
                    newProjectContext = null;
                    this.ShowError($"Error occurred when loading project:\n{ex}");
                }
            });
            IsEnabled = true;

            if (newProjectContext is null)
            {
                SetUMTConsoleText("Project failed to open.");
                return;
            }

            AssignNewProject(newProjectContext);
            SetUMTConsoleText($"Opened project \"{newProjectContext.Name}\".");
        }

        private async void SaveProject_Click(object sender, RoutedEventArgs e)
        {
            if (Data is null || FilePath is null || Project is null)
                return;

            IsEnabled = false;
            bool success = false;
            await Task.Run(() =>
            {
                try
                {
                    Project.Export(true);
                    success = true;
                }
                catch (ProjectException ex)
                {
                    this.ShowError(ex.Message, "Failed to save project");
                }
                catch (Exception ex)
                {
                    this.ShowError($"Error occurred when saving project:\n{ex}");
                }
            });
            IsEnabled = true;
            SetUMTConsoleText(success ? "Saved project successfully." : "Project failed to save.");
        }

        private void ViewProjectAssets_Click(object sender, RoutedEventArgs e)
        {
            if (Data is null || FilePath is null || Project is null)
                return;

            if (_projectAssetsWindow is not null)
            {
                _projectAssetsWindow.Activate();
                return;
            }

            _projectAssetsWindow = new ProjectAssetsWindow();
            _projectAssetsWindow.Closed += (_, _) => _projectAssetsWindow = null;
            _projectAssetsWindow.Show(this);
        }

        private void CloseProject_Click(object sender, RoutedEventArgs e)
        {
            if (Data is null || FilePath is null || Project is null)
                return;

            if (Project.HasUnexportedAssets
                && this.ShowQuestionWithCancel("There are assets marked to be exported in the current project. Really close?") != MessageBoxResult.Yes)
                return;

            UnloadProject();
            UpdateObjectLabel(Selected);
            UpdateMenuStates();
            SetUMTConsoleText("Project closed.");
        }

        /// <summary>updates the bottom-bar id label and marked-for-export checkbox for the given object (1:1 with wpf).</summary>
        public void UpdateObjectLabel(object obj)
        {
            if (ObjectLabel is null)
                return; // called before the view is built

            int foundIndex = (Data is not null && obj is UndertaleResource res) ? Data.IndexOf(res, false) : -1;
            string idString = foundIndex switch
            {
                -2 => "None",
                -1 => "N/A",
                _ => foundIndex.ToString()
            };
            ObjectLabel.Text = $"ID: {idString}";

            _updatingObjectLabel = true;
            if (Project is not null && foundIndex >= 0 && obj is IProjectAsset { ProjectExportable: true } projectAsset)
            {
                IsSelectedProjectExportable = true;
                MarkedForExportGroup.IsVisible = true;
                MarkedForExportCheckBox.IsChecked = Project.IsAssetMarkedForExport(projectAsset);
            }
            else
            {
                IsSelectedProjectExportable = false;
                MarkedForExportGroup.IsVisible = false;
            }
            _updatingObjectLabel = false;
        }

        private void MarkedForExport_CheckBoxChanged(object sender, RoutedEventArgs e)
        {
            if (_updatingObjectLabel || Project is null)
                return;
            if (MarkedForExportCheckBox.IsChecked is not bool isChecked)
                return;
            if (Selected is not IProjectAsset projectAsset)
                return;

            if (isChecked)
                Project.MarkAssetForExport(projectAsset);
            else
                Project.UnmarkAssetForExport(projectAsset);
        }
    }
}
