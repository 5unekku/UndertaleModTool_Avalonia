using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using UndertaleModLib;
using UndertaleModLib.Util;

namespace UndertaleModTool
{
    public partial class MainWindow : Window
    {
        /// <summary>
        /// the single main window instance. avalonia has no <c>Application.Current.MainWindow</c>, so this
        /// static accessor stands in for it (the wpf code used that property throughout).
        /// </summary>
        public static MainWindow Instance { get; private set; }

        /// <summary>the currently loaded data file, or null if nothing is open.</summary>
        public UndertaleData Data { get; set; }

        public MainWindow()
        {
            Instance = this;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        /// <summary>
        /// returns an avalonia bitmap for the given <see cref="GMImage"/>, reusing a cached instance when one is
        /// still alive. (1:1 with the wpf method of the same name, which returned a <c>BitmapSource</c>.)
        /// </summary>
        public Bitmap GetBitmapSourceForImage(GMImage image)
        {
            return TextureCache.GetBitmap(image);
        }
    }
}
