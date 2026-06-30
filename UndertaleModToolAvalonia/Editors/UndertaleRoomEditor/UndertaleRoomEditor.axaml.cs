using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using UndertaleModLib.Models;
using GameObjectInstance = UndertaleModLib.Models.UndertaleRoom.GameObject;
using RoomLayer = UndertaleModLib.Models.UndertaleRoom.Layer;

namespace UndertaleModTool
{
    /// <summary>
    /// room editor: properties + a contents tree + per-element property panels + a canvas that renders the room's
    /// instances as positioned sprites (pan via scrollbars, zoom via the wheel). the wpf editor's full interactive
    /// rendering (tile-layer compositing, background tiling/viewports, drag-to-place, GMS2 layer effects) is a
    /// known simplification here; see avalonia-port memory.
    /// </summary>
    public partial class UndertaleRoomEditor : DataUserControl
    {
        private static readonly UndertaleCachedImageLoader imageLoader = new();
        private double zoom = 1.0;

        public UndertaleRoomEditor()
        {
            InitializeComponent();
            DataContextChanged += (_, _) => Reload();
        }

        private void Reload()
        {
            SelectedPanel.Content = null;
            if (DataContext is not UndertaleRoom room)
            {
                RoomObjectsTree.ItemsSource = null;
                CanvasItems.ItemsSource = null;
                return;
            }

            BuildTree(room);
            RoomBorder.Width = room.Width;
            RoomBorder.Height = room.Height;
            RoomBorder.Background = ColorFromBgra(room.BackgroundColor | 0xFF000000u);
            CanvasItems.ItemsSource = BuildDrawables(room);
        }

        private void BuildTree(UndertaleRoom room)
        {
            var groups = new List<RoomTreeGroup>();
            void Add(string name, IEnumerable items, bool hasAny)
            {
                if (hasAny)
                    groups.Add(new RoomTreeGroup(name, items));
            }

            Add("Layers", room.Layers, room.Layers is { Count: > 0 });
            Add("Game Objects", room.GameObjects, room.GameObjects is { Count: > 0 });
            Add("Backgrounds", room.Backgrounds, room.Backgrounds is { Count: > 0 });
            Add("Views", room.Views, room.Views is { Count: > 0 });
            Add("Tiles", room.Tiles, room.Tiles is { Count: > 0 });

            RoomObjectsTree.ItemsSource = groups;
        }

        // collects the room's drawable instances (GMS2 instance layers, else GMS1 GameObjects) as positioned sprites
        private List<RoomDrawable> BuildDrawables(UndertaleRoom room)
        {
            var drawables = new List<RoomDrawable>();

            IEnumerable<GameObjectInstance> instances;
            if (room.Layers is { Count: > 0 })
            {
                var list = new List<GameObjectInstance>();
                foreach (RoomLayer layer in room.Layers)
                    if (layer.InstancesData?.Instances is not null)
                        list.AddRange(layer.InstancesData.Instances);
                instances = list;
            }
            else
            {
                instances = room.GameObjects;
            }

            foreach (GameObjectInstance instance in instances)
            {
                UndertaleSprite sprite = instance.ObjectDefinition?.Sprite;
                if (sprite is null || sprite.Textures.Count == 0)
                    continue;
                UndertaleTexturePageItem texture = sprite.Textures[0]?.Texture;
                if (texture is null)
                    continue;

                if (imageLoader.Convert(texture, typeof(Bitmap), null, CultureInfo.InvariantCulture) is not Bitmap bitmap)
                    continue;

                double scaleX = instance.ScaleX == 0 ? 1 : instance.ScaleX;
                double scaleY = instance.ScaleY == 0 ? 1 : instance.ScaleY;
                drawables.Add(new RoomDrawable
                {
                    Image = bitmap,
                    Width = texture.SourceWidth * scaleX,
                    Height = texture.SourceHeight * scaleY,
                    X = instance.X - sprite.OriginX * scaleX,
                    Y = instance.Y - sprite.OriginY * scaleY,
                    Opacity = (instance.Color >> 24) / 255.0
                });
            }

            return drawables;
        }

        private void RoomObjectsTree_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            object selected = (sender as TreeView)?.SelectedItem;
            SelectedPanel.Content = selected is GameObjectInstance or RoomLayer ? selected : null;
        }

        private void Canvas_PointerWheelChanged(object sender, PointerWheelEventArgs e)
        {
            // wheel zooms the room view (matches the wpf editor)
            zoom += e.Delta.Y * 0.1 * zoom;
            zoom = System.Math.Clamp(zoom, 0.1, 10.0);
            CanvasTransform.LayoutTransform = new ScaleTransform(zoom, zoom);
            e.Handled = true;
        }

        private static SolidColorBrush ColorFromBgra(uint bgra)
        {
            byte a = (byte)(bgra >> 24);
            byte b = (byte)(bgra >> 16);
            byte g = (byte)(bgra >> 8);
            byte r = (byte)bgra;
            return new SolidColorBrush(Color.FromArgb(a, r, g, b));
        }
    }

    /// <summary>a named grouping of room elements in the contents tree.</summary>
    public class RoomTreeGroup
    {
        public string Name { get; }
        public IEnumerable Items { get; }

        public RoomTreeGroup(string name, IEnumerable items)
        {
            Name = name;
            Items = items;
        }
    }

    /// <summary>a single positioned sprite rendered on the room canvas.</summary>
    public class RoomDrawable
    {
        public Bitmap Image { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public double Opacity { get; set; } = 1.0;
    }
}
