using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using UndertaleModLib.Util;

namespace UndertaleModTool
{
    /// <summary>
    /// caches decoded texture pages keyed by <see cref="GMImage"/>. holds both the raw straight-alpha bgra bytes
    /// (used to crop sprite/tile regions) and a full-page avalonia bitmap (used to display whole pages). entries
    /// are weakly referenced, mirroring the wpf bitmap-source cache so unused pages get collected.
    /// </summary>
    internal static class TextureCache
    {
        private static readonly List<(GMImage image, WeakReference<byte[]> raw, int width, int height)> rawLookup = new();
        private static readonly List<(GMImage image, WeakReference<WriteableBitmap> bitmap)> bitmapLookup = new();
        private static readonly object lookupLock = new();

        /// <summary>returns the straight-alpha bgra pixel buffer plus dimensions for a texture page image.</summary>
        public static (byte[] bgra, int width, int height) GetRaw(GMImage image)
        {
            lock (lookupLock)
            {
                for (int i = rawLookup.Count - 1; i >= 0; i--)
                {
                    var entry = rawLookup[i];
                    if (!entry.raw.TryGetTarget(out byte[] bytes))
                        rawLookup.RemoveAt(i);
                    else if (entry.image == image)
                        return (bytes, entry.width, entry.height);
                }

                if (image.Format == GMImage.ImageFormat.Unknown)
                    image = new GMImage(1, 1);

                byte[] pixelData = image.ConvertToRawBgra().ToSpan().ToArray();
                rawLookup.Add((image, new WeakReference<byte[]>(pixelData), image.Width, image.Height));
                return (pixelData, image.Width, image.Height);
            }
        }

        /// <summary>returns (and caches) a full-page avalonia bitmap for a texture page image.</summary>
        public static WriteableBitmap GetBitmap(GMImage image)
        {
            lock (lookupLock)
            {
                for (int i = bitmapLookup.Count - 1; i >= 0; i--)
                {
                    var entry = bitmapLookup[i];
                    if (!entry.bitmap.TryGetTarget(out WriteableBitmap existing))
                        bitmapLookup.RemoveAt(i);
                    else if (entry.image == image)
                        return existing;
                }

                (byte[] bgra, int width, int height) = GetRaw(image);
                WriteableBitmap bitmap = FromBgra(bgra, width, height);
                bitmapLookup.Add((image, new WeakReference<WriteableBitmap>(bitmap)));
                return bitmap;
            }
        }

        /// <summary>builds a straight-alpha bgra writeable bitmap from a tightly packed pixel buffer.</summary>
        public static WriteableBitmap FromBgra(byte[] bgra, int width, int height)
        {
            var bitmap = new WriteableBitmap(new PixelSize(width, height), new Vector(96, 96),
                                             PixelFormat.Bgra8888, AlphaFormat.Unpremul);
            using (var frame = bitmap.Lock())
            {
                int rowBytes = width * 4;
                for (int y = 0; y < height; y++)
                    Marshal.Copy(bgra, y * rowBytes, frame.Address + y * frame.RowBytes, rowBytes);
            }
            return bitmap;
        }
    }
}
