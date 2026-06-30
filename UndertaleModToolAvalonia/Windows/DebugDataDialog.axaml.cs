using Avalonia.Controls;
using Avalonia.Interactivity;

namespace UndertaleModTool
{
    public partial class DebugDataDialog : Window
    {
        public enum DebugDataMode
        {
            FullAssembler,
            PartialAssembler,
            Decompiled,
            NoDebug
        }

        public DebugDataMode Result { get; private set; } = DebugDataMode.NoDebug;

        public DebugDataDialog()
        {
            InitializeComponent();
        }

        private void Button1_Click(object sender, RoutedEventArgs e)
        {
            Result = DebugDataMode.Decompiled;
            Close();
        }

        private void Button2_Click(object sender, RoutedEventArgs e)
        {
            Result = DebugDataMode.PartialAssembler;
            Close();
        }

        private void Button3_Click(object sender, RoutedEventArgs e)
        {
            Result = DebugDataMode.FullAssembler;
            Close();
        }

        private void Button4_Click(object sender, RoutedEventArgs e)
        {
            Result = DebugDataMode.NoDebug;
            Close();
        }
    }
}
