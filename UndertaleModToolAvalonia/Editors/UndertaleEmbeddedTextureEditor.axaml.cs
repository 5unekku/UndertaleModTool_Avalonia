using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using ImageMagick;
using UndertaleModLib.Models;
using UndertaleModLib.Util;

namespace UndertaleModTool
{
    public partial class UndertaleEmbeddedTextureEditor : DataUserControl
    {
        private static MainWindow mainWindow => MainWindow.Instance;

        private readonly ContextMenuDark pageContextMenu = new();
        private UndertaleTexturePageItem[] items = Array.Empty<UndertaleTexturePageItem>();
        private UndertaleTexturePageItem hoveredItem;
        private UndertaleEmbeddedTexture.TexData _textureDataContext = null;
        private double zoomScale = 1;

        public UndertaleEmbeddedTextureEditor()
        {
            InitializeComponent();

            var newTabItem = new MenuItem { Header = "Open in new tab" };
            newTabItem.Click += OpenInNewTabItem_Click;
            var referencesItem = new MenuItem { Header = "Find all references to this page item" };
            referencesItem.Click += FindAllItemReferencesItem_Click;
            pageContextMenu.Items.Add(newTabItem);
            pageContextMenu.Items.Add(referencesItem);

            DataContextChanged += SwitchDataContext;
            Unloaded += UnloadTexture;
        }

        private void UpdateImage(UndertaleEmbeddedTexture texture)
        {
            if (texture.TextureData?.Image is null)
            {
                TexturePageImage.Source = null;
                return;
            }

            Bitmap bitmap = mainWindow.GetBitmapSourceForImage(texture.TextureData.Image);
            TexturePageImage.Source = bitmap;
        }

        private void SwitchDataContext(object sender, EventArgs e)
        {
            UndertaleEmbeddedTexture texture = DataContext as UndertaleEmbeddedTexture;
            if (texture is null)
                return;

            items = mainWindow.Data.TexturePageItems.Where(x => x.TexturePage == texture).ToArray();
            UpdateImage(texture);

            if (_textureDataContext is not null)
                _textureDataContext.PropertyChanged -= ReloadTextureImage;
            _textureDataContext = texture.TextureData;
            if (_textureDataContext is not null)
                _textureDataContext.PropertyChanged += ReloadTextureImage;
        }

        private void ReloadTextureImage(object sender, PropertyChangedEventArgs e)
        {
            Dispatcher.UIThread.Post(() =>
            {
                UndertaleEmbeddedTexture texture = DataContext as UndertaleEmbeddedTexture;
                if (texture is null)
                    return;
                if (e.PropertyName != nameof(UndertaleEmbeddedTexture.TexData.Image))
                    return;
                UpdateImage(texture);
            });
        }

        private void UnloadTexture(object sender, RoutedEventArgs e)
        {
            TexturePageImage.Source = null;
            if (_textureDataContext is not null)
            {
                _textureDataContext.PropertyChanged -= ReloadTextureImage;
                _textureDataContext = null;
            }
        }

        private void DataUserControl_Loaded(object sender, RoutedEventArgs e)
        {
            TextureViewbox.LayoutTransform = new ScaleTransform(zoomScale, zoomScale);
        }

        private void DataUserControl_Unloaded(object sender, RoutedEventArgs e)
        {
        }

        private void OpenInNewTabItem_Click(object sender, RoutedEventArgs e)
        {
            mainWindow.ChangeSelection(hoveredItem, true);
        }

        private void FindAllItemReferencesItem_Click(object sender, RoutedEventArgs e)
        {
            // TODO phase 8: open FindReferencesTypesDialog
            mainWindow.ShowMessage("Find all references is not yet available in the Avalonia port.");
        }

        private void Import_Click(object sender, RoutedEventArgs e)
        {
            UndertaleEmbeddedTexture target = DataContext as UndertaleEmbeddedTexture;

            OpenFileDialog dlg = new() { DefaultExt = ".png", Filter = "PNG files (.png)|*.png|All files|*" };
            if (dlg.ShowDialog() != true)
                return;

            try
            {
                GMImage image;
                if (Path.GetExtension(dlg.FileName).Equals(".png", StringComparison.InvariantCultureIgnoreCase))
                {
                    image = GMImage.FromPng(File.ReadAllBytes(dlg.FileName), true)
                                   .ConvertToFormat(target.TextureData.Image?.Format ?? GMImage.ImageFormat.Png);
                }
                else
                {
                    using var magickImage = new MagickImage(dlg.FileName);
                    magickImage.Format = MagickFormat.Bgra;
                    magickImage.Depth = 8;
                    magickImage.Alpha(AlphaOption.Set);
                    magickImage.SetCompression(CompressionMethod.NoCompression);
                    image = GMImage.FromMagickImage(magickImage)
                                   .ConvertToFormat(target.TextureData.Image?.Format ?? GMImage.ImageFormat.Png);
                }

                uint width = (uint)image.Width, height = (uint)image.Height;
                if ((width & (width - 1)) != 0 || (height & (height - 1)) != 0)
                    mainWindow.ShowWarning("WARNING: Texture page dimensions are not powers of 2. Sprite blurring is very likely in-game.", "Unexpected texture dimensions");

                var previousFormat = target.TextureData.Image?.Format;
                target.TextureData.Image = image;
                var currentFormat = target.TextureData.Image.Format;

                if (previousFormat == GMImage.ImageFormat.Dds && currentFormat == GMImage.ImageFormat.Png)
                    mainWindow.ShowMessage($"{target} was converted into PNG format since we don't support converting images into DDS format. This might have performance issues in the game.");
            }
            catch (Exception ex)
            {
                mainWindow.ShowError("Failed to import file: " + ex.Message, "Failed to import file");
            }
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            UndertaleEmbeddedTexture target = DataContext as UndertaleEmbeddedTexture;

            SaveFileDialog dlg = new() { DefaultExt = ".png", Filter = "PNG files (.png)|*.png|All files|*" };
            if (dlg.ShowDialog() == true)
            {
                try
                {
                    using FileStream fs = new(dlg.FileName, FileMode.Create);
                    target.TextureData.Image.SavePng(fs);
                }
                catch (Exception ex)
                {
                    mainWindow.ShowError("Failed to export file: " + ex.Message, "Failed to export file");
                }
            }
        }

        private void Grid_MouseDown(object sender, PointerPressedEventArgs e)
        {
            if (hoveredItem is null)
                return;

            var props = e.GetCurrentPoint(this).Properties;
            if (props.IsRightButtonPressed)
            {
                pageContextMenu.DataContext = hoveredItem;
                pageContextMenu.Open(sender as Control);
                return;
            }

            mainWindow.ChangeSelection(hoveredItem, props.IsMiddleButtonPressed);
        }

        private void Grid_MouseMove(object sender, PointerEventArgs e)
        {
            var prevItem = hoveredItem;
            hoveredItem = null;

            var pos = e.GetPosition(sender as Visual);
            foreach (var item in items)
            {
                if (pos.X > item.SourceX && pos.X < item.SourceX + item.SourceWidth
                    && pos.Y > item.SourceY && pos.Y < item.SourceY + item.SourceHeight)
                {
                    hoveredItem = item;
                    break;
                }
            }

            if (hoveredItem is null)
            {
                PageItemBorder.Width = PageItemBorder.Height = 0;
                return;
            }
            if (prevItem == hoveredItem)
                return;

            PageItemBorder.Width = hoveredItem.SourceWidth;
            PageItemBorder.Height = hoveredItem.SourceHeight;
            Canvas.SetLeft(PageItemBorder, hoveredItem.SourceX);
            Canvas.SetTop(PageItemBorder, hoveredItem.SourceY);
        }

        private void Grid_MouseLeave(object sender, PointerEventArgs e)
        {
            PageItemBorder.Width = PageItemBorder.Height = 0;
            hoveredItem = null;
        }

        private void TextureViewbox_MouseWheel(object sender, PointerWheelEventArgs e)
        {
            e.Handled = true;
            double pow = Math.Pow(2, 1.0 / 8.0);
            double scale = e.Delta.Y >= 0 ? pow : (1.0 / pow);
            double next = zoomScale * scale;
            if (next < 0.001 || next > 1000)
                return;
            zoomScale = next;
            TextureViewbox.LayoutTransform = new ScaleTransform(zoomScale, zoomScale);
        }
    }

    public class TextureLoadedWrapper : IMultiValueConverter
    {
        public object Convert(IList<object> values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Any(v => v is null || v == AvaloniaProperty.UnsetValue))
                return false;

            bool textureLoaded, textureExternal;
            try
            {
                textureLoaded = (bool)values[0];
                textureExternal = (bool)values[1];
            }
            catch
            {
                return false;
            }

            // visible only when not loaded and external (was Visibility.Visible/Collapsed)
            return !(textureLoaded || !textureExternal);
        }
    }
}
