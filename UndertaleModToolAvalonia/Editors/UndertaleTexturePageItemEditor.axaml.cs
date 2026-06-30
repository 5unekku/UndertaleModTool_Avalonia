using System;
using System.ComponentModel;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using ImageMagick;
using UndertaleModLib.Models;
using UndertaleModLib.Util;

namespace UndertaleModTool
{
    public partial class UndertaleTexturePageItemEditor : DataUserControl
    {
        private static MainWindow mainWindow => MainWindow.Instance;

        private UndertaleTexturePageItem _textureItemContext = null;
        private UndertaleEmbeddedTexture.TexData _textureDataContext = null;

        public UndertaleTexturePageItemEditor()
        {
            InitializeComponent();
            DataContextChanged += SwitchDataContext;
            Unloaded += UnloadTexture;
        }

        private void UpdateImages(UndertaleTexturePageItem item)
        {
            if (item.TexturePage?.TextureData?.Image is null)
            {
                ItemTextureBGImage.Source = null;
                ItemTextureImage.Source = null;
                return;
            }

            Bitmap bitmap = mainWindow.GetBitmapSourceForImage(item.TexturePage.TextureData.Image);
            ItemTextureBGImage.Source = bitmap;
            ItemTextureImage.Source = bitmap;
        }

        private void SwitchDataContext(object sender, EventArgs e)
        {
            UndertaleTexturePageItem item = DataContext as UndertaleTexturePageItem;
            if (item is null)
                return;

            UpdateImages(item);

            if (_textureItemContext is not null)
                _textureItemContext.PropertyChanged -= ReloadTexturePage;
            _textureItemContext = item;
            _textureItemContext.PropertyChanged += ReloadTexturePage;

            if (_textureDataContext is not null)
                _textureDataContext.PropertyChanged -= ReloadTextureImage;

            if (item.TexturePage?.TextureData is not null)
            {
                _textureDataContext = item.TexturePage.TextureData;
                _textureDataContext.PropertyChanged += ReloadTextureImage;
            }
        }

        private void ReloadTexturePage(object sender, PropertyChangedEventArgs e)
        {
            Dispatcher.UIThread.Post(() =>
            {
                UndertaleTexturePageItem item = DataContext as UndertaleTexturePageItem;
                if (item is null)
                    return;
                if (e.PropertyName != nameof(UndertaleTexturePageItem.TexturePage))
                    return;

                UpdateImages(item);

                if (_textureDataContext is not null)
                    _textureDataContext.PropertyChanged -= ReloadTextureImage;
                _textureDataContext = item.TexturePage.TextureData;
                _textureDataContext.PropertyChanged += ReloadTextureImage;
            });
        }

        private void ReloadTextureImage(object sender, PropertyChangedEventArgs e)
        {
            Dispatcher.UIThread.Post(() =>
            {
                UndertaleTexturePageItem item = DataContext as UndertaleTexturePageItem;
                if (item is null)
                    return;
                if (e.PropertyName != nameof(UndertaleEmbeddedTexture.TexData.Image))
                    return;

                UpdateImages(item);
            });
        }

        private void UnloadTexture(object sender, RoutedEventArgs e)
        {
            ItemTextureBGImage.Source = null;
            ItemTextureImage.Source = null;

            if (_textureItemContext is not null)
            {
                _textureItemContext.PropertyChanged -= ReloadTexturePage;
                _textureItemContext = null;
            }
            if (_textureDataContext is not null)
            {
                _textureDataContext.PropertyChanged -= ReloadTextureImage;
                _textureDataContext = null;
            }
        }

        private void Import_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new()
            {
                DefaultExt = ".png",
                Filter = "PNG files (.png)|*.png|All files|*"
            };

            if (!(dlg.ShowDialog() ?? false))
                return;

            try
            {
                using MagickImage image = TextureWorker.ReadBGRAImageFromFile(dlg.FileName);
                UndertaleTexturePageItem item = DataContext as UndertaleTexturePageItem;

                var previousFormat = item.TexturePage.TextureData.Image.Format;
                item.ReplaceTexture(image);
                var currentFormat = item.TexturePage.TextureData.Image.Format;

                if (previousFormat == GMImage.ImageFormat.Dds && currentFormat == GMImage.ImageFormat.Png)
                    mainWindow.ShowMessage($"{item.TexturePage} was converted into PNG format since we don't support converting images into DDS format. This might have performance issues in the game.");

                UpdateImages(item);
                // force the ItemDisplay preview (bound via the cached image loader) to re-evaluate
                ItemDisplay.DataContext = null;
                ItemDisplay.DataContext = item;
            }
            catch (Exception ex)
            {
                mainWindow.ShowError(ex.Message, "Failed to import image");
            }
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dlg = new()
            {
                DefaultExt = ".png",
                Filter = "PNG files (.png)|*.png|All files|*"
            };

            if (dlg.ShowDialog() == true)
            {
                using TextureWorker worker = new();
                try
                {
                    worker.ExportAsPNG((UndertaleTexturePageItem)DataContext, dlg.FileName);
                }
                catch (Exception ex)
                {
                    mainWindow.ShowError("Failed to export file: " + ex.Message, "Failed to export file");
                }
            }
        }

        private void FindReferencesButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO phase 8: open FindReferencesTypesDialog
            mainWindow.ShowMessage("Find all references is not yet available in the Avalonia port.");
        }
    }
}
