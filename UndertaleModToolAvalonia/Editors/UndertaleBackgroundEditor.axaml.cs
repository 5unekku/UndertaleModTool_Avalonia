using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using UndertaleModLib.Models;

namespace UndertaleModTool
{
    public partial class UndertaleBackgroundEditor : DataUserControl
    {
        private static MainWindow mainWindow => MainWindow.Instance;

        private UndertaleBackground subscribed;

        public UndertaleBackgroundEditor()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
            Unloaded += OnUnloaded;
        }

        private void OnUnloaded(object sender, RoutedEventArgs e) => Detach();

        private void Detach()
        {
            if (subscribed is not null)
            {
                subscribed.PropertyChanged -= OnPropertyChanged;
                if (subscribed.GMS2TileIds is INotifyCollectionChanged tileIds)
                    tileIds.CollectionChanged -= DataGrid_CollectionChanged;
                subscribed = null;
            }
        }

        private void OnDataContextChanged(object sender, EventArgs e)
        {
            Detach();

            subscribed = DataContext as UndertaleBackground;
            if (subscribed is not null)
            {
                subscribed.PropertyChanged += OnPropertyChanged;
                if (subscribed.GMS2TileIds is INotifyCollectionChanged tileIds)
                    tileIds.CollectionChanged += DataGrid_CollectionChanged;
            }
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e) => OnAssetUpdated();
        private void DataGrid_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) => OnAssetUpdated();

        private void OnAssetUpdated()
        {
            if (mainWindow.Project is null || !mainWindow.IsSelectedProjectExportable)
                return;
            Dispatcher.UIThread.Post(() =>
            {
                if (DataContext is UndertaleBackground obj)
                    mainWindow.Project?.MarkAssetForExport(obj);
            });
        }

        private void AddTileId_Click(object sender, RoutedEventArgs e) => TileIdList.AddNew();

        private void FindAllTileReferencesItem_Click(object sender, RoutedEventArgs e)
        {
            // TODO phase 8: open FindReferencesResults for the selected tile
            mainWindow.ShowMessage("Find all references is not yet available in the Avalonia port.");
        }

        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((sender as DataGrid)?.SelectedItem is UndertaleBackground.TileID tileID
                && DataContext is UndertaleBackground bg && bg.GMS2TileColumns != 0)
            {
                uint x = tileID.ID % bg.GMS2TileColumns;
                uint y = tileID.ID / bg.GMS2TileColumns;

                Canvas.SetLeft(TileRectangle, ((x + 1) * bg.GMS2OutputBorderX) + (x * (bg.GMS2TileWidth + bg.GMS2OutputBorderX)));
                Canvas.SetTop(TileRectangle, ((y + 1) * bg.GMS2OutputBorderY) + (y * (bg.GMS2TileHeight + bg.GMS2OutputBorderY)));
            }
        }

        private bool SelectTileRegion(object sender, PointerPressedEventArgs e)
        {
            if (!TileRectangle.IsVisible) // MainWindow.IsGMS2
                return false;

            Point pos = e.GetPosition(sender as Visual);
            UndertaleBackground bg = DataContext as UndertaleBackground;
            int x = (int)((int)pos.X / (bg.GMS2TileWidth + (2 * bg.GMS2OutputBorderX)));
            int y = (int)((int)pos.Y / (bg.GMS2TileHeight + (2 * bg.GMS2OutputBorderY)));
            int tileID = (int)((bg.GMS2TileColumns * y) + x);
            if (tileID > bg.GMS2TileCount - 1)
                return false;

            e.Handled = true;

            int tileIndex = bg.GMS2TileIds.FindIndex(t => t.ID == tileID);
            if (tileIndex == -1)
                return false;

            TileIdList.SelectedIndex = tileIndex;
            TileIdList.ScrollIntoView(TileIdList.SelectedItem, null);
            return true;
        }

        private void BGTexture_PointerPressed(object sender, PointerPressedEventArgs e)
        {
            var props = e.GetCurrentPoint(this).Properties;
            if (props.IsLeftButtonPressed)
            {
                SelectTileRegion(sender, e);
            }
            else if (props.IsRightButtonPressed)
            {
                if (SelectTileRegion(sender, e))
                    FindAllTileReferencesItem_Click(sender, null);
            }
        }
    }
}
