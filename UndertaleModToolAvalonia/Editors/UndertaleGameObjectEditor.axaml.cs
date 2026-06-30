using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Interactivity;
using Avalonia.Threading;
using UndertaleModLib;
using UndertaleModLib.Models;

namespace UndertaleModTool
{
    public partial class UndertaleGameObjectEditor : DataUserControl
    {
        private static MainWindow mainWindow => MainWindow.Instance;

        private UndertaleGameObject subscribed;

        public UndertaleGameObjectEditor()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
            Unloaded += OnUnloaded;
        }

        private void OnUnloaded(object sender, RoutedEventArgs e) => Detach();

        private void Detach()
        {
            if (subscribed is not null)
            {
                subscribed.PropertyChanged -= OnPropertyChanged;
                if (subscribed.PhysicsVertices is UndertaleObservableList<UndertaleGameObject.UndertalePhysicsVertex> vertices)
                    vertices.CollectionChanged -= DataGrid_CollectionChanged;
                subscribed = null;
            }
        }

        private void OnDataContextChanged(object sender, EventArgs e)
        {
            Detach();

            subscribed = DataContext as UndertaleGameObject;
            if (subscribed is not null)
            {
                subscribed.PropertyChanged += OnPropertyChanged;
                if (subscribed.PhysicsVertices is UndertaleObservableList<UndertaleGameObject.UndertalePhysicsVertex> vertices)
                    vertices.CollectionChanged += DataGrid_CollectionChanged;

                // project Events (one list per event type) into wrappers so the type is known per group
                // (replaces wpf's ItemsControl.AlternationIndex trick)
                var groups = subscribed.Events
                    .Select((list, i) => new GameObjectEventGroup((uint)i, list))
                    .ToList();
                EventsItemsControl.ItemsSource = groups;
            }
            else
            {
                EventsItemsControl.ItemsSource = null;
            }
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e) => OnAssetUpdated();
        private void DataGrid_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) => OnAssetUpdated();

        private void OnAssetUpdated()
        {
            if (mainWindow.Project is null || !mainWindow.IsSelectedProjectExportable)
                return;

            Dispatcher.UIThread.Post(() =>
            {
                if (DataContext is UndertaleGameObject obj)
                    mainWindow.Project?.MarkAssetForExport(obj);
            });
        }

        private void AddPhysicsVertex_Click(object sender, RoutedEventArgs e) => PhysicsVerticesGrid.AddNew();

        private void AddEvent_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Control)?.DataContext is not GameObjectEventGroup group)
                return;

            UndertaleGameObject.Event ev = new();
            ev.Actions.Add(new UndertaleGameObject.EventAction());
            group.Events.Add(ev);
        }

        private void AddAction_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Control)?.DataContext is not UndertaleGameObject.Event ev)
                return;
            ev.Actions.Add(new UndertaleGameObject.EventAction());
        }

        private void UndertaleObjectReference_ObjectReferenceChanged_ActionCode(object sender, UndertaleObjectReference.ObjectReferenceChangedEventArgs e)
        {
            OnAssetUpdated();
        }

        private void UndertaleObjectReference_Loaded(object sender, RoutedEventArgs e)
        {
            var objRef = sender as UndertaleObjectReference;
            objRef.ClearRemoveClickHandler();
            objRef.RemoveButton.Click += Remove_Click_Override;
            ToolTip.SetTip(objRef.RemoveButton, "Remove action");
            objRef.RemoveButton.IsEnabled = true;
        }

        private void Remove_Click_Override(object sender, RoutedEventArgs e)
        {
            var btn = (ButtonDark)sender;

            var obj = (UndertaleGameObject)DataContext;
            // the remove button's DataContext is the EventAction being removed
            if (btn.DataContext is not UndertaleGameObject.EventAction action)
                return;

            // find the event (and its type list) that contains this action
            for (int type = 0; type < obj.Events.Count; type++)
            {
                var evList = obj.Events[type];
                var ev = evList.FirstOrDefault(x => x.Actions.Contains(action));
                if (ev is null)
                    continue;

                ev.Actions.Remove(action);
                if (ev.Actions.Count <= 0)
                    evList.Remove(ev);
                return;
            }
        }
    }

    /// <summary>one event type's list of events, carrying the type so the subtype editor can pick the right control.</summary>
    public class GameObjectEventGroup
    {
        public uint EventTypeId { get; }
        public EventType EventType => (EventType)EventTypeId;
        public string TypeName { get; }
        public IList<UndertaleGameObject.Event> Events { get; }

        public GameObjectEventGroup(uint eventTypeId, IList<UndertaleGameObject.Event> events)
        {
            EventTypeId = eventTypeId;
            TypeName = ((EventType)eventTypeId).ToString();
            Events = events;
        }
    }

    /// <summary>inverts <c>IsGMS2</c> for the Depth field (shown only before GMS2). value bound is a bool.</summary>
    public class IsGMS2Converter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is bool b && !b;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    /// <summary>
    /// picks which subtype editor is visible for an event type (replaces wpf's ContentControl Style DataTriggers).
    /// value = the group's TypeName; parameter = editor kind ("key"/"step"/"mouse"/"other"/"draw"/"collision"/"none"/"raw").
    /// </summary>
    public class EventEditorVisibleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string typeName = value as string ?? "";
            string kind = parameter as string ?? "";

            string actualKind = typeName switch
            {
                "KeyPress" or "KeyRelease" or "Keyboard" => "key",
                "Step" => "step",
                "Mouse" => "mouse",
                "Other" => "other",
                "Draw" => "draw",
                "Collision" => "collision",
                "Create" or "Destroy" => "none",
                _ => "raw"
            };
            return actualKind == kind;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
