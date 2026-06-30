using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace UndertaleModTool
{
    /// <summary>
    /// builds a collision-mask image (1 bit per pixel, msb first) into an avalonia bitmap. a set bit is white,
    /// an unset bit is black, matching the wpf <c>PixelFormats.BlackWhite</c> source.
    /// </summary>
    public class MaskImageConverter : IMultiValueConverter
    {
        public object Convert(IList<object> values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Any(e => e is null || e == AvaloniaProperty.UnsetValue))
            {
                return null;
            }

            int width = (int)values[0];
            int height = (int)values[1];
            byte[] data = (byte[])values[2];
            int stride = (width + 7) / 8;
            if (data == null || data.Length != stride * height || width <= 0 || height <= 0)
                return null;

            var bitmap = new WriteableBitmap(new PixelSize(width, height), new Vector(96, 96),
                                             PixelFormat.Bgra8888, AlphaFormat.Opaque);
            using (var frame = bitmap.Lock())
            {
                byte[] row = new byte[width * 4];
                for (int y = 0; y < height; y++)
                {
                    int rowStart = y * stride;
                    for (int x = 0; x < width; x++)
                    {
                        bool on = (data[rowStart + (x >> 3)] & (0x80 >> (x & 7))) != 0;
                        byte v = on ? (byte)0xFF : (byte)0x00;
                        int p = x * 4;
                        row[p] = v;       // b
                        row[p + 1] = v;   // g
                        row[p + 2] = v;   // r
                        row[p + 3] = 0xFF; // a
                    }
                    Marshal.Copy(row, 0, frame.Address + y * frame.RowBytes, row.Length);
                }
            }
            return bitmap;
        }
    }
}
