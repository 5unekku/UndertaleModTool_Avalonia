using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using UndertaleModLib.Models;
using static UndertaleModLib.Models.UndertaleRoom;

namespace UndertaleModTool
{
    /// <summary>
    /// composites a GMS2 tile layer into a single bitmap, drawing each non-empty tile from the tileset at its grid
    /// position (with output borders and flip flags). this is the avalonia reimplementation of the wpf
    /// System.Drawing/GDI tile compositor: it blits straight-alpha BGRA pixels via <see cref="TextureCache"/>.
    /// rotation flags (4..7) are drawn unrotated (a known simplification); flips (1..3) are handled.
    /// </summary>
    public class CachedTileDataLoader : IMultiValueConverter
    {
        private static readonly ConcurrentDictionary<Layer.LayerTilesData, WeakReference<Bitmap>> layerCache = new();

        public static void Reset() => layerCache.Clear();

        public object Convert(IList<object> values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Count == 0 || values[0] is not Layer.LayerTilesData tilesData)
                return null;
            return BuildLayerBitmap(tilesData);
        }

        public static Bitmap BuildLayerBitmap(Layer.LayerTilesData tilesData)
        {
            if (tilesData?.Background is not UndertaleBackground tilesBG || tilesBG.Texture is null)
                return null;
            if (tilesData.TileData is null || tilesData.TilesX == 0 || tilesData.TilesY == 0)
                return null;

            if (layerCache.TryGetValue(tilesData, out var weak) && weak.TryGetTarget(out Bitmap cached))
                return cached;

            try
            {
                (byte[] page, int pageW, int pageH) = TextureCache.GetRaw(tilesBG.Texture.TexturePage.TextureData.Image);

                int w = (int)tilesBG.GMS2TileWidth;
                int h = (int)tilesBG.GMS2TileHeight;
                if (w <= 0 || h <= 0)
                    return null;
                int outX = (int)tilesBG.GMS2OutputBorderX;
                int outY = (int)tilesBG.GMS2OutputBorderY;
                int cols = (int)tilesBG.GMS2TileColumns;
                if (cols <= 0)
                    return null;
                int baseX = tilesBG.Texture.SourceX;
                int baseY = tilesBG.Texture.SourceY;
                uint maxID = tilesBG.GMS2TileIds.Count > 0 ? tilesBG.GMS2TileIds.Select(x => x.ID).Max() : 0;

                int tilesX = (int)tilesData.TilesX;
                int tilesY = (int)tilesData.TilesY;
                int compW = w * tilesX;
                int compH = h * tilesY;
                byte[] composite = new byte[compW * compH * 4];

                for (int ty = 0; ty < tilesY; ty++)
                {
                    uint[] row = tilesData.TileData[ty];
                    for (int tx = 0; tx < tilesX; tx++)
                    {
                        uint id = row[tx];
                        if (id == 0)
                            continue;
                        uint realID = id & 0x0FFFFFFF;
                        if (realID > maxID && maxID > 0)
                            continue;
                        uint flag = id >> 28;

                        int col = (int)(realID % (uint)cols);
                        int srcRow = (int)(realID / (uint)cols);
                        int srcX = baseX + ((col + 1) * outX) + (col * (w + outX));
                        int srcY = baseY + ((srcRow + 1) * outY) + (srcRow * (h + outY));

                        BlitTile(page, pageW, pageH, srcX, srcY, composite, compW, tx * w, ty * h, w, h, flag);
                    }
                }

                Bitmap result = TextureCache.FromBgra(composite, compW, compH);
                layerCache[tilesData] = new WeakReference<Bitmap>(result);
                return result;
            }
            catch
            {
                return null;
            }
        }

        // copies a w x h tile from the page buffer into the composite, applying horizontal/vertical flip flags
        private static void BlitTile(byte[] page, int pageW, int pageH, int srcX, int srcY,
                                     byte[] dest, int destW, int destX, int destY, int w, int h, uint flag)
        {
            bool flipX = flag is 1 or 3;
            bool flipY = flag is 2 or 3;

            for (int py = 0; py < h; py++)
            {
                int sy = srcY + (flipY ? h - 1 - py : py);
                if (sy < 0 || sy >= pageH)
                    continue;
                int dy = destY + py;
                for (int px = 0; px < w; px++)
                {
                    int sx = srcX + (flipX ? w - 1 - px : px);
                    if (sx < 0 || sx >= pageW)
                        continue;
                    int dx = destX + px;
                    int srcIndex = (sy * pageW + sx) * 4;
                    int dstIndex = (dy * destW + dx) * 4;
                    dest[dstIndex] = page[srcIndex];
                    dest[dstIndex + 1] = page[srcIndex + 1];
                    dest[dstIndex + 2] = page[srcIndex + 2];
                    dest[dstIndex + 3] = page[srcIndex + 3];
                }
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
