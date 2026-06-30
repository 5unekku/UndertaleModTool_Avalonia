using Avalonia.Media;
using UndertaleModLib.Models;

namespace UndertaleModTool
{
    /// <summary>
    /// non-interactive room preview (used where a room thumbnail/render is wanted). renders the room's instances as
    /// positioned sprites plus the background color, sharing <see cref="RoomDrawable.BuildFor"/> with the room editor.
    /// the wpf renderer also drew backgrounds, tile layers and views; those remain simplified here.
    /// </summary>
    public partial class UndertaleRoomRenderer : DataUserControl
    {
        public UndertaleRoomRenderer()
        {
            InitializeComponent();
            DataContextChanged += (_, _) => Reload();
        }

        private void Reload()
        {
            if (DataContext is not UndertaleRoom room)
            {
                CanvasItems.ItemsSource = null;
                CaptionText.Text = null;
                return;
            }

            CaptionText.Text = room.Name?.Content;
            RoomBorder.Background = ColorFromBgra(room.BackgroundColor | 0xFF000000u);
            CanvasItems.Width = room.Width;
            CanvasItems.Height = room.Height;
            CanvasItems.ItemsSource = RoomDrawable.BuildFor(room);
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
}
