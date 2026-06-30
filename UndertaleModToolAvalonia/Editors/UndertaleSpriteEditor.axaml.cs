using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using UndertaleModLib;
using UndertaleModLib.Models;
using UndertaleModLib.Util;
using static UndertaleModLib.Models.UndertaleSprite;

namespace UndertaleModTool
{
    public partial class UndertaleSpriteEditor : DataUserControl
    {
        private static MainWindow mainWindow => MainWindow.Instance;

        private UndertaleSprite subscribed;

        public UndertaleSpriteEditor()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
            Unloaded += OnUnloaded;
            MaskList.AddingNewItem += MaskList_AddingNewItem;
            UpdateMaskState();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e) => Detach();

        private void Detach()
        {
            if (subscribed is not null)
            {
                subscribed.PropertyChanged -= OnPropertyChanged;
                if (subscribed.Textures is INotifyCollectionChanged textures)
                    textures.CollectionChanged -= DataGrid_CollectionChanged;
                if (subscribed.CollisionMasks is INotifyCollectionChanged masks)
                    masks.CollectionChanged -= DataGrid_CollectionChanged;
                subscribed = null;
            }
        }

        private void OnDataContextChanged(object sender, EventArgs e)
        {
            Detach();

            subscribed = DataContext as UndertaleSprite;
            if (subscribed is not null)
            {
                subscribed.PropertyChanged += OnPropertyChanged;
                if (subscribed.Textures is INotifyCollectionChanged textures)
                    textures.CollectionChanged += DataGrid_CollectionChanged;
                if (subscribed.CollisionMasks is INotifyCollectionChanged masks)
                    masks.CollectionChanged += DataGrid_CollectionChanged;
            }
            UpdateMaskState();
        }

        private void UndertaleObjectReference_ObjectReferenceChanged(object sender, UndertaleObjectReference.ObjectReferenceChangedEventArgs e) => OnAssetUpdated();
        private void DataGrid_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) => OnAssetUpdated();
        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e) => OnAssetUpdated();

        private void OnAssetUpdated()
        {
            if (mainWindow.Project is null || !mainWindow.IsSelectedProjectExportable)
                return;
            Dispatcher.UIThread.Post(() =>
            {
                if (DataContext is UndertaleSprite obj)
                    mainWindow.Project?.MarkAssetForExport(obj);
            });
        }

        private void AddTexture_Click(object sender, RoutedEventArgs e) => TextureList.AddNew();
        private void AddMask_Click(object sender, RoutedEventArgs e) => MaskList.AddNew();

        private void MaskList_AddingNewItem(object sender, AddingNewItemEventArgs e)
        {
            e.NewItem = (DataContext as UndertaleSprite).NewMaskEntry(mainWindow.Data);
        }

        private void MaskList_SelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateMaskState();

        // replaces the wpf ControlTemplate.Triggers that recolored / disabled the mask editor on invalid/null data
        private void UpdateMaskState()
        {
            if (MaskEditor is null)
                return;

            var mask = MaskList?.SelectedItem as MaskEntry;
            MaskEditor.IsVisible = mask is not null;
            if (mask is null)
                return;

            int stride = (mask.Width + 7) / 8;
            bool valid = mask.Data is not null && mask.Width > 0 && mask.Height > 0 && mask.Data.Length == stride * mask.Height;
            MaskBorder.BorderBrush = valid ? Brushes.Gray : Brushes.Red;
            MaskIsInvalid.IsVisible = !valid;
            MaskExport.IsEnabled = valid;
        }

        private void ExportAllSpine(SaveFileDialog dlg, UndertaleSprite sprite)
        {
            mainWindow.ShowWarning("This seems to be a Spine sprite, .json and .atlas files will be exported together with the frames. " +
                                 "PLEASE EDIT THEM CAREFULLY! SOME MANUAL EDITING OF THE JSON MAY BE REQUIRED! THE DATA IS EXPORTED AS-IS.", "Spine warning");

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    string dir = Path.GetDirectoryName(dlg.FileName);
                    string name = Path.GetFileNameWithoutExtension(dlg.FileName);
                    string path = Path.Join(dir, name);
                    string ext = Path.GetExtension(dlg.FileName);

                    if (sprite.SpineTextures.Count > 0)
                    {
                        Directory.CreateDirectory(path);
                        if (sprite.SpineHasTextureData)
                        {
                            foreach (var tex in sprite.SpineTextures.Select((tex, id) => new { id, tex }))
                            {
                                try
                                {
                                    File.WriteAllBytes(Paths.JoinVerifyWithinDirectory(path, tex.id + ext), tex.tex.TexBlob);
                                }
                                catch (Exception ex)
                                {
                                    mainWindow.ShowError("Failed to export file: " + ex.Message, "Failed to export file");
                                }
                            }
                        }
                        File.WriteAllText(Path.Join(path, "spine.json"), sprite.SpineJSON);
                        File.WriteAllText(Path.Join(path, "spine.atlas"), sprite.SpineAtlas);
                    }
                }
                catch (Exception ex)
                {
                    mainWindow.ShowError("Failed to export: " + ex.Message, "Failed to export sprite");
                }
            }
        }

        private void ExportAll_Click(object sender, RoutedEventArgs e)
        {
            UndertaleSprite sprite = DataContext as UndertaleSprite;

            SaveFileDialog dlg = new()
            {
                FileName = sprite.Name.Content + ".png",
                DefaultExt = ".png",
                Filter = "PNG files (.png)|*.png|All files|*"
            };

            if (sprite.IsSpineSprite)
            {
                ExportAllSpine(dlg, sprite);
                if (sprite.SpineHasTextureData)
                    return;
            }

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    bool includePadding = mainWindow.ShowQuestion("Include padding?") == MessageBoxResult.Yes;

                    using TextureWorker worker = new();
                    if (sprite.Textures.Count > 1)
                    {
                        string dir = Path.GetDirectoryName(dlg.FileName);
                        string name = Path.GetFileNameWithoutExtension(dlg.FileName);
                        string path = Path.Join(dir, name);
                        string ext = Path.GetExtension(dlg.FileName);

                        Directory.CreateDirectory(path);
                        foreach (var tex in sprite.Textures.Select((tex, id) => new { id, tex }))
                        {
                            try
                            {
                                worker.ExportAsPNG(tex.tex.Texture, Paths.JoinVerifyWithinDirectory(path, sprite.Name.Content + "_" + tex.id + ext), null, includePadding);
                            }
                            catch (Exception ex)
                            {
                                mainWindow.ShowError("Failed to export file: " + ex.Message, "Failed to export file");
                            }
                        }
                    }
                    else if (sprite.Textures.Count == 1)
                    {
                        try
                        {
                            worker.ExportAsPNG(sprite.Textures[0].Texture, dlg.FileName, null, includePadding);
                        }
                        catch (Exception ex)
                        {
                            mainWindow.ShowError("Failed to export file: " + ex.Message, "Failed to export file");
                        }
                    }
                    else
                    {
                        mainWindow.ShowError("No frames to export", "Failed to export sprite");
                    }
                }
                catch (Exception ex)
                {
                    mainWindow.ShowError("Failed to export: " + ex.Message, "Failed to export sprite");
                }
            }
        }

        private void MaskImport_Click(object sender, RoutedEventArgs e)
        {
            UndertaleSprite sprite = DataContext as UndertaleSprite;
            MaskEntry target = (sender as Control)?.DataContext as MaskEntry;
            if (target is null)
                return;

            OpenFileDialog dlg = new() { DefaultExt = ".png", Filter = "PNG files (.png)|*.png|All files|*" };
            if (dlg.ShowDialog() == true)
            {
                try
                {
                    (int maskWidth, int maskHeight) = sprite.CalculateMaskDimensions(mainWindow.Data);
                    target.Data = TextureWorker.ReadMaskData(dlg.FileName, maskWidth, maskHeight);
                    target.Width = maskWidth;
                    target.Height = maskHeight;
                    UpdateMaskState();
                    OnAssetUpdated();
                }
                catch (Exception ex)
                {
                    mainWindow.ShowError("Failed to import file: " + ex.Message, "Failed to import file");
                }
            }
        }

        private void MaskExport_Click(object sender, RoutedEventArgs e)
        {
            UndertaleSprite sprite = DataContext as UndertaleSprite;
            MaskEntry target = (sender as Control)?.DataContext as MaskEntry;
            if (target is null)
                return;

            SaveFileDialog dlg = new() { DefaultExt = ".png", Filter = "PNG files (.png)|*.png|All files|*" };
            if (dlg.ShowDialog() == true)
            {
                try
                {
                    (int maskWidth, int maskHeight) = sprite.CalculateMaskDimensions(mainWindow.Data);
                    TextureWorker.ExportCollisionMaskPNG(target, dlg.FileName, maskWidth, maskHeight);
                }
                catch (Exception ex)
                {
                    mainWindow.ShowError("Failed to export file: " + ex.Message, "Failed to export file");
                }
            }
        }

        private void UndertaleObjectReference_Loaded(object sender, RoutedEventArgs e)
        {
            var objRef = sender as UndertaleObjectReference;
            objRef.ClearRemoveClickHandler();
            objRef.RemoveButton.Click += Remove_Click_Override;
            ToolTip.SetTip(objRef.RemoveButton, "Remove texture entry");
            objRef.RemoveButton.IsEnabled = true;
            ToolTip.SetTip(objRef.DetailsButton, "Open texture entry");
            objRef.ObjectText.KeyDown += ObjectText_PreviewKeyDown;
        }

        private void ObjectText_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
                Remove_Click_Override(sender, null);
        }

        private void Remove_Click_Override(object sender, RoutedEventArgs e)
        {
            if (DataContext is UndertaleSprite sprite && (sender as Control)?.DataContext is TextureEntry entry)
                sprite.Textures.Remove(entry);
        }

        private void RemoveMask_Clicked(object sender, RoutedEventArgs e)
        {
            if (DataContext is UndertaleSprite sprite && (sender as Control)?.DataContext is MaskEntry entry)
                sprite.CollisionMasks.Remove(entry);
        }
    }
}
