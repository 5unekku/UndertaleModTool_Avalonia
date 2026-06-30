using Avalonia.Interactivity;

namespace UndertaleModTool
{
    public partial class UndertaleGameEndEditor : DataUserControl
    {
        public UndertaleGameEndEditor()
        {
            InitializeComponent();
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            ScriptsGrid.AddNew();
        }
    }
}
