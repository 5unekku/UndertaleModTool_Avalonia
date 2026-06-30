using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using UndertaleModLib;

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
    }
}
