using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using UndertaleModLib;
using UndertaleModLib.Project;

namespace UndertaleModTool
{
    public partial class ProjectAssetsWindow : Window
    {
        private static MainWindow mainWindow => MainWindow.Instance;
        private bool preventUpdateList = false;

        public readonly record struct UnexportedAsset(string Name, string AssetType, IProjectAsset ProjectAsset);

        public ProjectAssetsWindow()
        {
            InitializeComponent();

            DragDrop.SetAllowDrop(RootGrid, true);
            RootGrid.AddHandler(DragDrop.DragOverEvent, Grid_DragOver);
            RootGrid.AddHandler(DragDrop.DropEvent, Grid_Drop);

            if (mainWindow.Project is ProjectContext project)
            {
                UpdateList(project, EventArgs.Empty);
                project.UnexportedAssetsChanged += UpdateList;
            }
        }

        private void UpdateList(object sender, EventArgs e)
        {
            if (preventUpdateList)
                return;

            List<UnexportedAsset> assets = ((ProjectContext)sender)
                .EnumerateUnexportedAssets()
                .Select(asset => new UnexportedAsset(asset.ProjectName, asset.ProjectAssetType.ToInterfaceName(), asset))
                .ToList();

            assets.Sort((a, b) =>
            {
                if (a.AssetType.CompareTo(b.AssetType) is int i && i != 0)
                    return i;
                if (a.Name.CompareTo(b.Name) is int j && j != 0)
                    return j;
                return 0;
            });

            AssetsListView.ItemsSource = null;
            AssetsListView.ItemsSource = assets;
        }

        protected override void OnClosing(WindowClosingEventArgs e)
        {
            if (mainWindow.Project is ProjectContext project)
                project.UnexportedAssetsChanged -= UpdateList;
            base.OnClosing(e);
        }

        private void OpenSelectedListViewItem(bool inNewTab = false)
        {
            if (AssetsListView.SelectedItems.Cast<UnexportedAsset>().FirstOrDefault() is { ProjectAsset: UndertaleObject obj })
            {
                if (!mainWindow.HasEditorForAsset(obj))
                {
                    this.ShowError("The type of this object doesn't have an editor/viewer.");
                    return;
                }
                mainWindow.Activate();
                mainWindow.ChangeSelection(obj, inNewTab);
            }
        }

        private void UnmarkSelectedListViewItemsForExport()
        {
            if (mainWindow.Project is ProjectContext projectContext)
            {
                preventUpdateList = true;
                foreach (UnexportedAsset asset in AssetsListView.SelectedItems.Cast<UnexportedAsset>().ToList())
                    projectContext.UnmarkAssetForExport(asset.ProjectAsset);
                preventUpdateList = false;
                UpdateList(projectContext, EventArgs.Empty);
            }
        }

        private void AssetsListView_DoubleTapped(object sender, TappedEventArgs e) => OpenSelectedListViewItem();

        private void AssetsListView_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                OpenSelectedListViewItem();
                e.Handled = true;
            }
            else if (e.Key == Key.Delete)
            {
                UnmarkSelectedListViewItemsForExport();
                e.Handled = true;
            }
        }

        private void MenuItemOpen_Click(object sender, RoutedEventArgs e) => OpenSelectedListViewItem();
        private void MenuItemOpenInNewTab_Click(object sender, RoutedEventArgs e) => OpenSelectedListViewItem(true);
        private void MenuItemUnmarkForExport_Click(object sender, RoutedEventArgs e) => UnmarkSelectedListViewItemsForExport();

        // ponytail: obsolete IDataObject retained on purpose, see UndertaleObjectReference.GetDragObject
        // (new DataTransfer custom formats are byte[]/string only; this is an in-process live-object drag)
#pragma warning disable CS0618
        private static IProjectAsset GetDraggedAsset(DragEventArgs e)
        {
            var formats = e.Data.GetDataFormats().ToArray();
            if (formats.Length == 0)
                return null;
            return e.Data.Get(formats[^1]) as IProjectAsset;
        }
#pragma warning restore CS0618

        private void Grid_DragOver(object sender, DragEventArgs e)
        {
            e.DragEffects = GetDraggedAsset(e) is { ProjectExportable: true } ? DragDropEffects.Move : DragDropEffects.None;
            e.Handled = true;
        }

        private void Grid_Drop(object sender, DragEventArgs e)
        {
            if (GetDraggedAsset(e) is IProjectAsset projectAsset
                && mainWindow.Project is ProjectContext project && project.MarkAssetForExport(projectAsset))
            {
                e.Handled = true;
            }
        }
    }
}
