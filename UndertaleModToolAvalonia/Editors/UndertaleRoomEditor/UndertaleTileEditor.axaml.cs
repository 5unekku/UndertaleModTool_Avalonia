using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using UndertaleModLib.Models;
using static UndertaleModLib.Models.UndertaleRoom;

namespace UndertaleModTool
{
    /// <summary>
    /// tile-layer painting editor: shows the layer (left) and the tileset palette (right). click a palette tile to
    /// pick a brush, then click/drag on the layer to paint (right-click erases). this is a functional port of the
    /// wpf editor's core usage; its advanced features (multi-tile brushes, brush tiling, flip/rotate transforms,
    /// undo/redo, room preview overlay, grid overlay) are simplifications. see avalonia-port memory.
    /// </summary>
    public partial class UndertaleTileEditor : Window
    {
        private readonly Layer editingLayer;
        private readonly Layer.LayerTilesData tilesData;
        private readonly Layer.LayerTilesData paletteData;
        private readonly uint[][] oldTileData;
        private readonly int tileWidth;
        private readonly int tileHeight;
        private uint brushTileId;
        private bool applied;

        private readonly TileEditorViewModel viewModel = new();

        public UndertaleTileEditor(Layer layer)
        {
            editingLayer = layer;
            tilesData = layer.TilesData;
            oldTileData = CloneTileData(tilesData.TileData);

            UndertaleBackground background = tilesData.Background;
            tileWidth = (int)background.GMS2TileWidth;
            tileHeight = (int)background.GMS2TileHeight;

            paletteData = BuildPalette(background);

            InitializeComponent();
            DataContext = viewModel;

            viewModel.EditWidth = (double)tilesData.TilesX * tileWidth;
            viewModel.EditHeight = (double)tilesData.TilesY * tileHeight;
            viewModel.PaletteWidth = (double)paletteData.TilesX * tileWidth;
            viewModel.PaletteHeight = (double)paletteData.TilesY * tileHeight;
            RefreshLayer();
            viewModel.PaletteBitmap = CachedTileDataLoader.BuildLayerBitmap(paletteData, useCache: false);
        }

        private static uint[][] CloneTileData(uint[][] source)
        {
            if (source is null)
                return null;
            var clone = new uint[source.Length][];
            for (int y = 0; y < source.Length; y++)
                clone[y] = (uint[])source[y].Clone();
            return clone;
        }

        // builds a palette tile layer containing every tileset tile id laid out in a grid
        private static Layer.LayerTilesData BuildPalette(UndertaleBackground background)
        {
            uint cols = background.GMS2TileColumns == 0 ? 1 : background.GMS2TileColumns;
            uint rows = (uint)Math.Ceiling(background.GMS2TileCount / (double)cols);
            if (rows == 0)
                rows = 1;

            var palette = new Layer.LayerTilesData { Background = background, TilesX = cols, TilesY = rows };
            var grid = new uint[rows][];
            for (int y = 0; y < rows; y++)
                grid[y] = new uint[cols];
            palette.TileData = grid;

            int itemsPerTile = Math.Max(1, (int)background.GMS2ItemsPerTileCount);
            int count = (int)background.GMS2TileCount * itemsPerTile;
            int i = 0;
            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < cols; x++)
                {
                    grid[y][x] = i >= count ? 0 : background.GMS2TileIds[i].ID;
                    i += itemsPerTile;
                }
            }
            return palette;
        }

        private void RefreshLayer()
        {
            viewModel.LayerBitmap = CachedTileDataLoader.BuildLayerBitmap(tilesData, useCache: false);
        }

        private void Layer_PointerPressed(object sender, PointerPressedEventArgs e)
        {
            var point = e.GetCurrentPoint(LayerImage);
            int cellX = (int)(point.Position.X / tileWidth);
            int cellY = (int)(point.Position.Y / tileHeight);
            if (cellX < 0 || cellY < 0 || cellY >= tilesData.TileData.Length || cellX >= tilesData.TileData[cellY].Length)
                return;

            uint value = point.Properties.IsRightButtonPressed ? 0 : brushTileId;
            tilesData.TileData[cellY][cellX] = value;
            RefreshLayer();
        }

        private void Palette_PointerPressed(object sender, PointerPressedEventArgs e)
        {
            var point = e.GetCurrentPoint(PaletteImage);
            int cellX = (int)(point.Position.X / tileWidth);
            int cellY = (int)(point.Position.Y / tileHeight);
            if (cellX < 0 || cellY < 0 || cellY >= paletteData.TileData.Length || cellX >= paletteData.TileData[cellY].Length)
                return;

            brushTileId = paletteData.TileData[cellY][cellX];
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            applied = true;
            tilesData.TileDataUpdated();
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            if (!applied)
                tilesData.TileData = oldTileData; // discard edits
            CachedTileDataLoader.Reset();
            base.OnClosed(e);
        }
    }

    public class TileEditorViewModel : INotifyPropertyChanged
    {
        private Bitmap layerBitmap;
        private Bitmap paletteBitmap;
        private double editWidth, editHeight, paletteWidth, paletteHeight;

        public Bitmap LayerBitmap { get => layerBitmap; set { layerBitmap = value; OnPropertyChanged(); } }
        public Bitmap PaletteBitmap { get => paletteBitmap; set { paletteBitmap = value; OnPropertyChanged(); } }
        public double EditWidth { get => editWidth; set { editWidth = value; OnPropertyChanged(); } }
        public double EditHeight { get => editHeight; set { editHeight = value; OnPropertyChanged(); } }
        public double PaletteWidth { get => paletteWidth; set { paletteWidth = value; OnPropertyChanged(); } }
        public double PaletteHeight { get => paletteHeight; set { paletteHeight = value; OnPropertyChanged(); } }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
