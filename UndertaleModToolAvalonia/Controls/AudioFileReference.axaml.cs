using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using UndertaleModLib;
using UndertaleModLib.Models;

namespace UndertaleModTool
{
    public partial class AudioFileReference : UserControl
    {
        private static MainWindow mainWindow => MainWindow.Instance;

        public static readonly StyledProperty<UndertaleEmbeddedAudio> AudioReferenceProperty =
            AvaloniaProperty.Register<AudioFileReference, UndertaleEmbeddedAudio>(nameof(AudioReference), defaultBindingMode: BindingMode.TwoWay);

        public static readonly StyledProperty<UndertaleAudioGroup> GroupReferenceProperty =
            AvaloniaProperty.Register<AudioFileReference, UndertaleAudioGroup>(nameof(GroupReference), defaultBindingMode: BindingMode.TwoWay);

        public static readonly StyledProperty<int> AudioIDProperty =
            AvaloniaProperty.Register<AudioFileReference, int>(nameof(AudioID));

        public static readonly StyledProperty<int> GroupIDProperty =
            AvaloniaProperty.Register<AudioFileReference, int>(nameof(GroupID));

        public UndertaleEmbeddedAudio AudioReference { get => GetValue(AudioReferenceProperty); set => SetValue(AudioReferenceProperty, value); }
        public UndertaleAudioGroup GroupReference { get => GetValue(GroupReferenceProperty); set => SetValue(GroupReferenceProperty, value); }
        public int AudioID { get => GetValue(AudioIDProperty); set => SetValue(AudioIDProperty, value); }
        public int GroupID { get => GetValue(GroupIDProperty); set => SetValue(GroupIDProperty, value); }

        public AudioFileReference()
        {
            InitializeComponent();

            DragDrop.SetAllowDrop(ObjectText, true);
            ObjectText.AddHandler(DragDrop.DragOverEvent, TextBox_DragOver);
            ObjectText.AddHandler(DragDrop.DropEvent, TextBox_Drop);
            UpdateState();
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == AudioReferenceProperty
                || change.Property == GroupReferenceProperty
                || change.Property == AudioIDProperty
                || change.Property == GroupIDProperty)
            {
                UpdateState();
            }
        }

        private void UpdateState()
        {
            if (ObjectText is null)
                return;

            // computed display text (was wpf datatriggers)
            if (AudioID == -1)
                ObjectText.Text = "(null)";
            else if (AudioReference is null)
                ObjectText.Text = $"(UndertaleEmbeddedAudio#{AudioID} in UndertaleAudioGroup#{GroupID}:{GroupReference?.Name?.Content})";
            else
                ObjectText.Text = $"(UndertaleEmbeddedAudio#{AudioID})";

            ObjectText.ContextMenu = AudioReference is not null ? Resources["contextMenu"] as ContextMenu : null;

            if (DetailsButton is not null)
                DetailsButton.IsEnabled = AudioReference is not null;
            if (RemoveButton is not null)
                RemoveButton.IsEnabled = AudioReference is not null;
            if (AudioIdText is not null)
                AudioIdText.IsReadOnly = GroupID == 0;
        }

        private void Details_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e) => OpenReference();

        private void Details_MouseDown(object sender, PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.IsMiddleButtonPressed)
                OpenReference(true);
        }

        private void OpenInNewTabItem_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e) => OpenReference(true);

        private void Remove_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e) => AudioReference = null;

        private void TextBox_MouseDoubleClick(object sender, TappedEventArgs e) => OpenReference();

        private void OpenReference(bool inNewTab = false)
        {
            if (GroupID != 0 && AudioID != -1)
            {
                string relativePath;
                if (GroupReference is UndertaleAudioGroup { Path.Content: string customRelativePath })
                    relativePath = customRelativePath;
                else
                    relativePath = $"audiogroup{GroupID}.dat";
                mainWindow.OpenChildFile(relativePath, "AUDO", AudioID);
                return;
            }

            if (AudioReference == null)
                return;

            mainWindow.ChangeSelection(AudioReference, inNewTab);
        }

        private void TextBox_DragOver(object sender, DragEventArgs e)
        {
            UndertaleObject sourceItem = UndertaleObjectReference.GetDragObject(e);
            e.DragEffects = GroupID == 0 && sourceItem is UndertaleEmbeddedAudio ? DragDropEffects.Link : DragDropEffects.None;
            e.Handled = true;
        }

        private void TextBox_Drop(object sender, DragEventArgs e)
        {
            UndertaleObject sourceItem = UndertaleObjectReference.GetDragObject(e);
            bool valid = GroupID == 0 && sourceItem is UndertaleEmbeddedAudio;
            e.DragEffects = valid ? DragDropEffects.Link : DragDropEffects.None;
            if (valid)
                AudioReference = (UndertaleEmbeddedAudio)sourceItem;
            e.Handled = true;
        }
    }
}
