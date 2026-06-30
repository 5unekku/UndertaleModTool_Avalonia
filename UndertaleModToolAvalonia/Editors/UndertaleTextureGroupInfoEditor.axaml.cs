using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Interactivity;
using UndertaleModLib;

namespace UndertaleModTool
{
    public partial class UndertaleTextureGroupInfoEditor : DataUserControl
    {
        public UndertaleTextureGroupInfoEditor()
        {
            InitializeComponent();
        }

        private void AddTexture_Click(object sender, RoutedEventArgs e) => TextureListGrid.AddNew();
        private void AddSprite_Click(object sender, RoutedEventArgs e) => SpriteListGrid.AddNew();
        private void AddSpineSprite_Click(object sender, RoutedEventArgs e) => SpineSprListGrid.AddNew();
        private void AddFont_Click(object sender, RoutedEventArgs e) => FontListGrid.AddNew();
        private void AddTileset_Click(object sender, RoutedEventArgs e) => TilesetListGrid.AddNew();
    }

    /// <summary>yields an <c>IsVisible</c> bool: visible (true) before GM 2023.1, collapsed (false) after.</summary>
    public class IsGM2023Converter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not UndertaleData data)
                return true;

            return !data.IsVersionAtLeast(2023, 1);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
