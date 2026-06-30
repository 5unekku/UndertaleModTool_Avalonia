using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using UndertaleModLib.Models;
using static UndertaleModLib.Models.UndertaleRoom;

namespace UndertaleModTool
{
    /// <summary>
    /// converts a <see cref="UndertaleTexturePageItem"/> (or a room <see cref="Tile"/>) into a cropped avalonia
    /// bitmap for display, caching results. the gms2 tile-layer batch paths (ProcessTileSet / CachedTileDataLoader)
    /// are ported with the room editor; here only the sprite/texture and single-tile display paths are implemented.
    /// </summary>
    public class UndertaleCachedImageLoader : IValueConverter
    {
        private static readonly ConcurrentDictionary<string, Bitmap> imageCache = new();
        private static readonly ConcurrentDictionary<Tuple<string, Tuple<int, int, uint, uint>>, Bitmap> tileCache = new();

        // kept for settings/room-editor compatibility; only affects the (deferred) batch tile generation path
        public static bool ReuseTileBuffer { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is null)
                return null;

            bool isTile = false;
            bool cacheEnabled = true;
            bool generate = false;

            List<Tuple<int, int, uint, uint>> tileRectList = null;
            if (parameter is string par)
            {
                isTile = par.Contains("tile");
                cacheEnabled = !par.Contains("nocache");
                generate = par.Contains("generate");
            }
            else if (parameter is List<Tuple<int, int, uint, uint>> list)
            {
                generate = true;
                tileRectList = list;
            }

            Tile tile = null;
            if (isTile)
                tile = value as Tile;

            UndertaleTexturePageItem texture = isTile ? tile.Tpag : value as UndertaleTexturePageItem;
            if (texture is null || texture.TexturePage is null)
                return null;

            string texName = texture.Name?.Content;
            if (texName is null || texName == "PageItem Unknown Index")
            {
                texName = (MainWindow.Instance.Data.TexturePageItems.IndexOf(texture) + 1).ToString();
                if (texName == "0")
                    return null;
            }

            if (texture.SourceWidth == 0 || texture.SourceHeight == 0)
                return null;

            if (tileRectList is not null)
            {
                // batch gms2 tileset pre-generation is ported with the room editor (phase 7)
                return null;
            }

            Bitmap spriteSrc;
            if (isTile)
            {
                if (tileCache.TryGetValue(new(texName, new(tile.SourceX, tile.SourceY, tile.Width, tile.Height)), out spriteSrc))
                    return spriteSrc;
            }

            if (!imageCache.ContainsKey(texName) || !cacheEnabled)
            {
                int rectX, rectY, rectW, rectH;

                // how many pixels are out of bounds of tile texture page
                int diffW = 0;
                int diffH = 0;

                if (isTile)
                {
                    int actualTileSourceX = (int)(tile.SourceX - texture.TargetX);
                    int actualTileSourceY = (int)(tile.SourceY - texture.TargetY);
                    diffW = (int)(actualTileSourceX + tile.Width - texture.SourceWidth);
                    diffH = (int)(actualTileSourceY + tile.Height - texture.SourceHeight);
                    rectX = texture.SourceX + actualTileSourceX;
                    rectY = texture.SourceY + actualTileSourceY;
                    rectW = (int)tile.Width;
                    rectH = (int)tile.Height;
                }
                else
                {
                    rectX = texture.SourceX;
                    rectY = texture.SourceY;
                    rectW = texture.SourceWidth;
                    rectH = texture.SourceHeight;
                }

                spriteSrc = CreateSpriteBitmap(rectX, rectY, rectW, rectH, texture, diffW, diffH);

                if (cacheEnabled)
                {
                    if (isTile)
                        tileCache.TryAdd(new(texName, new(tile.SourceX, tile.SourceY, tile.Width, tile.Height)), spriteSrc);
                    else
                        imageCache.TryAdd(texName, spriteSrc);
                }

                if (generate)
                    return null;
                else
                    return spriteSrc;
            }

            return imageCache[texName];
        }

        public static void Reset()
        {
            imageCache.Clear();
            tileCache.Clear();
            ReuseTileBuffer = false;
        }

        /// <summary>
        /// crops the rectangle out of the texture page into a fresh transparent bitmap, clamping to page bounds
        /// (diffW/diffH say how far the requested rectangle extends past the page, as for room tiles).
        /// </summary>
        public static WriteableBitmap CreateSpriteBitmap(int rectX, int rectY, int rectW, int rectH,
                                                         UndertaleTexturePageItem texture, int diffW = 0, int diffH = 0)
        {
            (byte[] src, int srcW, int srcH) = TextureCache.GetRaw(texture.TexturePage.TextureData.Image);

            int targetW = rectW;
            int targetH = rectH;
            var target = new WriteableBitmap(new PixelSize(Math.Max(1, targetW), Math.Max(1, targetH)),
                                             new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Unpremul);

            // clamp width/height in bounds (diffW/diffH represent how out of bounds they are)
            rectW -= (diffW > 0) ? diffW : 0;
            rectH -= (diffH > 0) ? diffH : 0;

            // clamp x/y in bounds
            int offsetX = 0, offsetY = 0;
            if (rectX < texture.SourceX)
            {
                offsetX = texture.SourceX - rectX;
                rectW -= offsetX;
                rectX = texture.SourceX;
            }
            if (rectY < texture.SourceY)
            {
                offsetY = texture.SourceY - rectY;
                rectH -= offsetY;
                rectY = texture.SourceY;
            }

            // abort (leaving the bitmap transparent) if the rect is out of bounds of the texture item or page
            if (rectX >= texture.SourceX + texture.SourceWidth || rectY >= texture.SourceY + texture.SourceHeight)
                return target;
            if (rectW <= 0 || rectH <= 0)
                return target;
            if (rectX < 0 || rectX >= srcW || rectY < 0 || rectY >= srcH
                || (rectX + rectW) > srcW || (rectY + rectH) > srcH)
                return target;

            // copy the in-bounds region from the page into a zeroed destination buffer, then upload it
            byte[] dest = new byte[targetW * targetH * 4];
            for (int row = 0; row < rectH; row++)
            {
                int srcOffset = ((rectY + row) * srcW + rectX) * 4;
                int destOffset = ((offsetY + row) * targetW + offsetX) * 4;
                Buffer.BlockCopy(src, srcOffset, dest, destOffset, rectW * 4);
            }

            using (var frame = target.Lock())
            {
                int rowBytes = targetW * 4;
                for (int y = 0; y < targetH; y++)
                    Marshal.Copy(dest, y * rowBytes, frame.Address + y * frame.RowBytes, rowBytes);
            }

            return target;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // UndertaleCachedImageLoader wrappers
    public class CachedTileImageLoader : IMultiValueConverter
    {
        private static readonly UndertaleCachedImageLoader loader = new();

        public object Convert(IList<object> values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values[0] is null) // tile
                return null;

            if ((uint)values[1] == 0 || (uint)values[2] == 0) // width, height
                return null;

            return loader.Convert(values[0], null, "tile", null);
        }
    }

    public class CachedImageLoaderWithIndex : IMultiValueConverter
    {
        private static readonly UndertaleCachedImageLoader loader = new();

        public object Convert(IList<object> values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Any(x => x is null))
                return null;

            IList<UndertaleSprite.TextureEntry> textures = values[0] as IList<UndertaleSprite.TextureEntry>;
            if (textures is null)
                return null;

            int index = -1;
            if (values[1] is int indexInt)
                index = indexInt;
            else if (values[1] is float indexFloat)
                index = (int)indexFloat;

            if (index > textures.Count - 1 || index < 0)
                return null;
            else
                return loader.Convert(textures[index].Texture, null, null, null);
        }
    }
}
