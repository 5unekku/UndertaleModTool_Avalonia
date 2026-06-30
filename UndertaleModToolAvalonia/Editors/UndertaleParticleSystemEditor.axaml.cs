using Avalonia.Interactivity;

namespace UndertaleModTool
{
    public partial class UndertaleParticleSystemEditor : DataUserControl
    {
        public UndertaleParticleSystemEditor()
        {
            InitializeComponent();
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            EmitterListGrid.AddNew();
        }
    }
}
