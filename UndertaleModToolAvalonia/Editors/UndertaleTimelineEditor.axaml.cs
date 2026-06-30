using System.Collections.Generic;
using System.Linq;
using Avalonia.Interactivity;
using UndertaleModLib;
using UndertaleModLib.Models;

namespace UndertaleModTool
{
    public partial class UndertaleTimelineEditor : DataUserControl
    {
        public UndertaleTimelineEditor()
        {
            InitializeComponent();
            MomentsGrid.AddingNewItem += DataGrid_AddingNewItem;
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            MomentsGrid.AddNew();
        }

        private void DataGrid_AddingNewItem(object sender, AddingNewItemEventArgs e)
        {
            UndertaleTimeline.UndertaleTimelineMoment obj = new();

            // find the last timeline moment (which should have the biggest step value)
            var lastMoment = (MomentsGrid.ItemsSource as IList<UndertaleTimeline.UndertaleTimelineMoment>).LastOrDefault();

            // the default value is 0 anyway.
            if (lastMoment != null)
                obj.Step = lastMoment.Step + 1;

            // make an empty event with a null code entry.
            obj.Event = new UndertalePointerList<UndertaleGameObject.EventAction>();
            obj.Event.Add(new UndertaleGameObject.EventAction());

            e.NewItem = obj;
        }
    }
}
