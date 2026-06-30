using Avalonia;
using Avalonia.Controls;
using static UndertaleModLib.Models.UndertaleRoom;

namespace UndertaleModTool
{
    /// <summary>
    /// an image that displays a composited GMS2 tile layer. the wpf version overrode <c>HitTestCore</c> to ignore
    /// empty/transparent tiles when picking; avalonia's hit-testing model differs, so tile picking is left to
    /// coordinate math in the editor and this is a plain image carrying the tile-layer data.
    /// </summary>
    public class TileLayerImage : Image
    {
        public static readonly StyledProperty<Layer.LayerTilesData> LayerTilesDataProperty =
            AvaloniaProperty.Register<TileLayerImage, Layer.LayerTilesData>(nameof(LayerTilesData));

        public static readonly StyledProperty<bool> CheckTransparencyProperty =
            AvaloniaProperty.Register<TileLayerImage, bool>(nameof(CheckTransparency));

        public Layer.LayerTilesData LayerTilesData
        {
            get => GetValue(LayerTilesDataProperty);
            set => SetValue(LayerTilesDataProperty, value);
        }

        public bool CheckTransparency
        {
            get => GetValue(CheckTransparencyProperty);
            set => SetValue(CheckTransparencyProperty, value);
        }
    }
}
