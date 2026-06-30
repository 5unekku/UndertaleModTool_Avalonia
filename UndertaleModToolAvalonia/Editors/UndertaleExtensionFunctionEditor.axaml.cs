using Avalonia.Interactivity;

namespace UndertaleModTool
{
    public partial class UndertaleExtensionFunctionEditor : DataUserControl
    {
        public UndertaleExtensionFunctionEditor()
        {
            InitializeComponent();
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            ArgumentTypesList.AddNew();
        }
    }
}
