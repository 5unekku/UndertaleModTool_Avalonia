using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using UndertaleModLib;
using UndertaleModLib.Models;

namespace UndertaleModTool
{
    public partial class UndertaleStringReference : UserControl
    {
        private static MainWindow mainWindow => MainWindow.Instance;

        public static readonly StyledProperty<UndertaleString> ObjectReferenceProperty =
            AvaloniaProperty.Register<UndertaleStringReference, UndertaleString>(nameof(ObjectReference), defaultBindingMode: BindingMode.TwoWay);

        public UndertaleString ObjectReference
        {
            get => GetValue(ObjectReferenceProperty);
            set => SetValue(ObjectReferenceProperty, value);
        }

        public UndertaleStringReference()
        {
            InitializeComponent();

            DragDrop.SetAllowDrop(ObjectText, true);
            ObjectText.AddHandler(DragDrop.DragOverEvent, TextBox_DragOver);
            ObjectText.AddHandler(DragDrop.DropEvent, TextBox_Drop);
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == ObjectReferenceProperty)
            {
                UndertaleString reference = change.GetNewValue<UndertaleString>();
                UpdateContextMenu(reference);
                if (ObjectText is not null)
                {
                    // display the referenced string; the edit is committed manually on lost focus (see below)
                    ObjectText.Text = reference?.Content ?? string.Empty;
                    ObjectText.Watermark = reference is null ? "(null)" : "(empty)";
                }
                if (DetailsButton is not null)
                    DetailsButton.IsEnabled = reference is not null;
            }
        }

        private void UpdateContextMenu(UndertaleString reference)
        {
            try
            {
                if (reference is not null && Resources["contextMenu"] is ContextMenu menu)
                {
                    menu.DataContext = reference;
                    ObjectText.ContextMenu = menu;
                }
                else if (ObjectText is not null)
                {
                    ObjectText.ContextMenu = null;
                }
            }
            catch { }
        }

        private void Details_Click(object sender, RoutedEventArgs e)
        {
            mainWindow.ChangeSelection(ObjectReference);
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
            ObjectReference = null;
        }

        private void TextBox_DragOver(object sender, DragEventArgs e)
        {
            UndertaleString sourceItem = UndertaleObjectReference.GetDragObject(e) as UndertaleString;
            e.DragEffects = sourceItem is not null ? DragDropEffects.Link : DragDropEffects.None;
            e.Handled = true;
        }

        private void TextBox_Drop(object sender, DragEventArgs e)
        {
            UndertaleString sourceItem = UndertaleObjectReference.GetDragObject(e) as UndertaleString;
            e.DragEffects = sourceItem is not null ? DragDropEffects.Link : DragDropEffects.None;
            if (sourceItem is not null)
                ObjectReference = sourceItem;
            e.Handled = true;
        }

        // wpf used an explicit binding + BindingExpression.IsDirty; avalonia has no such api, so the edit is
        // detected by comparing the box text against the referenced string and committed here on lost focus.
        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is not TextBox tb)
                return;

            string current = ObjectReference?.Content ?? string.Empty;
            if (tb.Text == current)
                return; // not dirty

            if (ObjectReference is not null)
            {
                var dialog = new StringUpdateWindow();
                dialog.ShowDialogSync(this.GetVisualRoot() as Window);
                switch (dialog.Result)
                {
                    case StringUpdateWindow.ResultType.ChangeOneValue:
                        ObjectReference = mainWindow.Data.Strings.MakeString(tb.Text);
                        break;
                    case StringUpdateWindow.ResultType.ChangeReferencedValue:
                        ObjectReference.Content = tb.Text;
                        break;
                    case StringUpdateWindow.ResultType.Cancel:
                        tb.Text = current; // revert
                        break;
                }
            }
            else
            {
                ObjectReference = mainWindow.Data.Strings.MakeString(tb.Text);
            }
        }
    }
}
