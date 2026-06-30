using System;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using UndertaleModLib;
using UndertaleModLib.Models;

namespace UndertaleModTool
{
    public partial class UndertaleObjectReference : UserControl
    {
        /// <summary>drag-and-drop data format used by the resource tree to carry a dragged resource.</summary>
        public const string DragFormat = "UndertaleObject";

        private static MainWindow mainWindow => MainWindow.Instance;
        private static readonly Regex camelCaseRegex = new("(?<=[a-z])([A-Z])", RegexOptions.Compiled);
        private static readonly char[] vowels = { 'a', 'o', 'u', 'e', 'i', 'y' };

        public event EventHandler<ObjectReferenceChangedEventArgs> ObjectReferenceChanged;

        public class ObjectReferenceChangedEventArgs : EventArgs
        {
            private object OldObject { get; }
            private object NewObject { get; }

            public ObjectReferenceChangedEventArgs(object oldObj, object newObj)
            {
                OldObject = oldObj;
                NewObject = newObj;
            }
        }

        public static readonly StyledProperty<object> ObjectReferenceProperty =
            AvaloniaProperty.Register<UndertaleObjectReference, object>(nameof(ObjectReference), defaultBindingMode: BindingMode.TwoWay);

        public static readonly StyledProperty<Type> ObjectTypeProperty =
            AvaloniaProperty.Register<UndertaleObjectReference, Type>(nameof(ObjectType));

        public static readonly StyledProperty<bool> CanRemoveProperty =
            AvaloniaProperty.Register<UndertaleObjectReference, bool>(nameof(CanRemove), true, defaultBindingMode: BindingMode.TwoWay);

        public static readonly StyledProperty<bool> CanChangeProperty =
            AvaloniaProperty.Register<UndertaleObjectReference, bool>(nameof(CanChange), true, defaultBindingMode: BindingMode.TwoWay);

        public static readonly StyledProperty<UndertaleGameObject> GameObjectProperty =
            AvaloniaProperty.Register<UndertaleObjectReference, UndertaleGameObject>(nameof(GameObject));

        public static readonly StyledProperty<EventType> ObjectEventTypeProperty =
            AvaloniaProperty.Register<UndertaleObjectReference, EventType>(nameof(ObjectEventType), EventType.Create, defaultBindingMode: BindingMode.TwoWay);

        public static readonly StyledProperty<uint> ObjectEventSubtypeProperty =
            AvaloniaProperty.Register<UndertaleObjectReference, uint>(nameof(ObjectEventSubtype), 0u, defaultBindingMode: BindingMode.TwoWay);

        public static readonly StyledProperty<UndertaleRoom> RoomProperty =
            AvaloniaProperty.Register<UndertaleObjectReference, UndertaleRoom>(nameof(Room));

        public static readonly StyledProperty<UndertaleRoom.GameObject> RoomGameObjectProperty =
            AvaloniaProperty.Register<UndertaleObjectReference, UndertaleRoom.GameObject>(nameof(RoomGameObject));

        public object ObjectReference { get => GetValue(ObjectReferenceProperty); set => SetValue(ObjectReferenceProperty, value); }
        public Type ObjectType { get => GetValue(ObjectTypeProperty); set => SetValue(ObjectTypeProperty, value); }
        public bool CanRemove { get => GetValue(CanRemoveProperty); set => SetValue(CanRemoveProperty, value); }
        public bool CanChange { get => GetValue(CanChangeProperty); set => SetValue(CanChangeProperty, value); }
        public UndertaleGameObject GameObject { get => GetValue(GameObjectProperty); set => SetValue(GameObjectProperty, value); }
        public EventType ObjectEventType { get => GetValue(ObjectEventTypeProperty); set => SetValue(ObjectEventTypeProperty, value); }
        public uint ObjectEventSubtype { get => GetValue(ObjectEventSubtypeProperty); set => SetValue(ObjectEventSubtypeProperty, value); }
        public UndertaleRoom Room { get => GetValue(RoomProperty); set => SetValue(RoomProperty, value); }
        public UndertaleRoom.GameObject RoomGameObject { get => GetValue(RoomGameObjectProperty); set => SetValue(RoomGameObjectProperty, value); }

        public bool IsPreCreate { get; set; } = false;

        public UndertaleObjectReference()
        {
            InitializeComponent();

            DragDrop.SetAllowDrop(ObjectText, true);
            ObjectText.AddHandler(DragDrop.DragOverEvent, TextBox_DragOver);
            ObjectText.AddHandler(DragDrop.DropEvent, TextBox_Drop);

            Loaded += UndertaleObjectReference_Loaded;
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == ObjectReferenceProperty)
            {
                UpdateContextMenu(change.NewValue);
                UpdateVisualState();
            }
            else if (change.Property == ObjectTypeProperty
                     || change.Property == CanChangeProperty
                     || change.Property == CanRemoveProperty)
            {
                UpdateVisualState();
            }
        }

        private void UpdateContextMenu(object reference)
        {
            try
            {
                if (reference is not null && Resources["contextMenu"] is ContextMenu menu)
                {
                    menu.DataContext = ObjectReference;
                    ObjectText.ContextMenu = menu;
                }
                else
                {
                    ObjectText.ContextMenu = null;
                }
            }
            catch { }
        }

        // drives the button content/enabled state that wpf expressed through datatriggers
        private void UpdateVisualState()
        {
            if (DetailsButton is null || RemoveButton is null)
                return;

            bool isCreateCode = ObjectType == typeof(UndertaleCode) && ObjectReference is null;
            DetailsButton.Content = isCreateCode ? " + " : " ... ";
            ToolTip.SetTip(DetailsButton, isCreateCode ? "Create new code entry" : "Open referenced object");
            DetailsButton.IsEnabled = isCreateCode || ObjectReference is not null;

            RemoveButton.IsEnabled = CanRemove && ObjectReference is not null;
        }

        private void UndertaleObjectReference_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateVisualState();

            if (ObjectType is null)
                return;

            // build the "(drag & drop a sprite)" / "(empty sprite reference)" watermark from the type name
            string typeName = ObjectType.ToString();
            string n = "";
            if (typeName.StartsWith("UndertaleModLib.Models.Undertale"))
            {
                typeName = typeName["UndertaleModLib.Models.Undertale".Length..];
                typeName = camelCaseRegex.Replace(typeName, " $1").ToLowerInvariant();
            }
            if (Array.IndexOf(vowels, typeName[0]) != -1)
                n = "n";

            ObjectText.Watermark = CanChange
                ? $"(drag & drop a{n} {typeName})"
                : $"(empty {typeName} reference)";
        }

        public void ClearRemoveClickHandler()
        {
            RemoveButton.Click -= Remove_Click;
        }

        private void Details_Click(object sender, RoutedEventArgs e)
        {
            if (ObjectReference is null)
            {
                object oldObj = ObjectReference;
                int oldCodeCount = mainWindow.Data.Code?.Count ?? -1;

                if (GameObject is not null)
                {
                    ObjectReference = GameObject.EventHandlerFor(ObjectEventType, ObjectEventSubtype, mainWindow.Data);
                }
                else if (Room is not null)
                {
                    if (RoomGameObject is null)
                    {
                        string name = $"gml_Room_{Room.Name.Content}_Create";
                        if (mainWindow.Data.Code.ByName(name) is UndertaleCode existing)
                        {
                            mainWindow.ShowWarning("Code entry for room already exists; reusing it.");
                            ObjectReference = existing;
                        }
                        else
                        {
                            ObjectReference = UndertaleCode.CreateEmptyEntry(mainWindow.Data, name);
                        }
                    }
                    else
                    {
                        string beginning = $"gml_RoomCC_{Room.Name.Content}_{RoomGameObject.InstanceID}";
                        string suffix = !IsPreCreate ? "_Create" : "_PreCreate";
                        string name = beginning + suffix;

                        int i = 0;
                        while (mainWindow.Data.Code.ByName(name) is not null)
                        {
                            name = beginning + "_" + (i++).ToString() + suffix;
                        }

                        ObjectReference = UndertaleCode.CreateEmptyEntry(mainWindow.Data, name);
                    }
                }
                else
                {
                    mainWindow.ShowError("Adding not supported in this situation.");
                }

                if (oldObj != ObjectReference)
                {
                    ObjectReferenceChanged?.Invoke(this, new ObjectReferenceChangedEventArgs(oldObj, ObjectReference));

                    if (mainWindow.Project is not null && oldCodeCount != -1
                        && oldCodeCount == mainWindow.Data.Code.Count - 1 && ObjectReference is UndertaleCode newCode)
                    {
                        mainWindow.Project.MarkAssetForExport(newCode);
                    }
                }
            }
            else
            {
                mainWindow.ChangeSelection(ObjectReference);
            }
        }

        private void Details_MouseDown(object sender, PointerPressedEventArgs e)
        {
            if (ObjectReference is null)
                return;

            if (e.GetCurrentPoint(this).Properties.IsMiddleButtonPressed)
                mainWindow.ChangeSelection(ObjectReference, true);
        }

        private void OpenInNewTabItem_Click(object sender, RoutedEventArgs e)
        {
            mainWindow.ChangeSelection(ObjectReference, true);
        }

        private void MenuItem_ContextMenuOpened(object sender, RoutedEventArgs e)
        {
            // TODO phase 8: hide "Find all references" for non-referenceable types via UndertaleResourceReferenceMap
        }

        private void FindAllReferencesItem_Click(object sender, RoutedEventArgs e)
        {
            // TODO phase 8: open FindReferencesTypesDialog
            mainWindow.ShowMessage("Find all references is not yet available in the Avalonia port.");
        }

        private void Remove_Click(object sender, RoutedEventArgs e)
        {
            ObjectReferenceChanged?.Invoke(this, new ObjectReferenceChangedEventArgs(ObjectReference, null));
            ObjectReference = null;
        }

        private void TextBox_MouseDoubleClick(object sender, TappedEventArgs e)
        {
            if (ObjectReference != null)
                mainWindow.ChangeSelection(ObjectReference);
        }

        private void TextBox_DragOver(object sender, DragEventArgs e)
        {
            UndertaleObject sourceItem = GetDragObject(e);
            e.DragEffects = sourceItem is not null && CanChange && sourceItem.GetType() == ObjectType
                ? DragDropEffects.Link : DragDropEffects.None;
            e.Handled = true;
        }

        private void TextBox_Drop(object sender, DragEventArgs e)
        {
            UndertaleObject sourceItem = GetDragObject(e);
            bool valid = sourceItem is not null && CanChange && sourceItem.GetType() == ObjectType;
            e.DragEffects = valid ? DragDropEffects.Link : DragDropEffects.None;
            if (valid)
            {
                ObjectReferenceChanged?.Invoke(this, new ObjectReferenceChangedEventArgs(ObjectReference, sourceItem));
                ObjectReference = sourceItem;
            }
            e.Handled = true;
        }

        internal static UndertaleObject GetDragObject(DragEventArgs e)
        {
            if (e.Data.Contains(DragFormat))
                return e.Data.Get(DragFormat) as UndertaleObject;
            foreach (string format in e.Data.GetDataFormats())
            {
                if (e.Data.Get(format) is UndertaleObject obj)
                    return obj;
            }
            return null;
        }
    }
}
