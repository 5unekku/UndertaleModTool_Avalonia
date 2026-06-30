using System;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Interactivity;
using UndertaleModLib.Models;

namespace UndertaleModTool
{
    public partial class UndertaleFontEditor : DataUserControl
    {
        private static MainWindow mainWindow => MainWindow.Instance;

        public UndertaleFontEditor()
        {
            InitializeComponent();
        }

        private void Button_Sort_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not UndertaleFont font || font.Glyphs.Count == 0)
                return;

            var copy = font.Glyphs.ToList();
            copy.Sort((x, y) => x.Character.CompareTo(y.Character));
            font.Glyphs.Clear();
            foreach (var glyph in copy)
                font.Glyphs.Add(glyph);

            mainWindow.ShowMessage("The glyphs were sorted successfully.");
        }

        private void Button_UpdateRange_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not UndertaleFont font || font.Glyphs.Count == 0)
                return;

            var characters = font.Glyphs.Select(x => x.Character);
            font.RangeStart = characters.Min();
            font.RangeEnd = characters.Max();

            mainWindow.ShowMessage("The range was updated successfully.");
        }

        private void Grid_MouseDown(object sender, PointerPressedEventArgs e)
        {
            if (DataContext is not UndertaleFont font)
                return;

            var pos = e.GetPosition(sender as Visual);
            for (int i = 0; i < font.Glyphs.Count; i++)
            {
                var glyph = font.Glyphs[i];
                if (pos.X > glyph.SourceX && pos.X < glyph.SourceX + glyph.SourceWidth
                    && pos.Y > glyph.SourceY && pos.Y < glyph.SourceY + glyph.SourceHeight)
                {
                    GlyphsGrid.SelectedItem = glyph;
                    GlyphsGrid.ScrollIntoView(glyph, null);
                    break;
                }
            }
        }

        private void EditRectangleButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO phase 8: port the interactive EditGlyphRectangleWindow (pan/zoom rectangle selection)
            mainWindow.ShowMessage("The visual glyph-rectangle editor is not yet available in the Avalonia port; edit the source rectangle fields in the glyphs table directly.");
        }

        private void CreateGlyphButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not UndertaleFont font)
                return;

            int index = font.Glyphs.Count - 1;
            if (index >= 0)
            {
                if (font.Glyphs[index].SourceWidth == 0 || font.Glyphs[index].SourceHeight == 0)
                {
                    mainWindow.ShowWarning("The last glyph has zero size.\n" +
                                           "You can use the button on the left to fix that.");
                    return;
                }
            }

            font.Glyphs.Add(new());
            index++;

            GlyphsGrid.SelectedIndex = index;
            GlyphsGrid.ScrollIntoView(GlyphsGrid.SelectedItem, null);
        }

        private void EditKerningButton_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Control)?.DataContext is not UndertaleFont.Glyph glyph)
                return;

            GlyphsGrid.IsVisible = false;
            GlyphsGrid.IsEnabled = false;

            GlyphKerningGrid.ItemsSource = glyph.Kerning;
            GlyphKerningBorder.IsVisible = true;
            GlyphKerningGrid.IsEnabled = true;

            char? ch = (char?)CharConverter.Instance.Convert(glyph.Character, null, null, null);
            ch ??= default;
            GlyphsLabel.Text = $"Kerning of glyph '{ch}' (code - {glyph.Character}):";
        }

        private void KerningBackButton_Click(object sender, RoutedEventArgs e)
        {
            GlyphKerningGrid.ItemsSource = null;
            GlyphKerningBorder.IsVisible = false;
            GlyphKerningGrid.IsEnabled = false;

            GlyphsGrid.IsVisible = true;
            GlyphsGrid.IsEnabled = true;

            GlyphsLabel.Text = "Glyphs:";
        }
    }

    public class CharConverter : IValueConverter
    {
        private static MainWindow mainWindow => MainWindow.Instance;
        public static readonly CharConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not ushort charNum)
            {
                if (value is not short charNum1)
                    return "(error)";
                try
                {
                    charNum = (ushort)charNum1;
                }
                catch
                {
                    return "(error)";
                }
            }

            if (charNum == 0)
                return null;
            return System.Convert.ToChar(charNum);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not string charStr || charStr.Length == 0)
                return Avalonia.Data.BindingOperations.DoNothing;

            uint charNum = charStr[0];
            if (charNum > ushort.MaxValue)
            {
                mainWindow.ShowError("The character code is greater than the maximum (65535)");
                return Avalonia.Data.BindingOperations.DoNothing;
            }

            return (ushort)charNum;
        }
    }
}
