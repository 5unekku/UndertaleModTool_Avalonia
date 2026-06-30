namespace UndertaleModTool
{
    // minimal room property view. the full interactive room editor (layers/instances/tiles + renderer,
    // ~7000 lines in the wpf version) is the largest remaining piece of the port; see avalonia-port memory.
    public partial class UndertaleRoomEditor : DataUserControl
    {
        public UndertaleRoomEditor()
        {
            InitializeComponent();
        }
    }
}
