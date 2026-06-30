using System;
using AvaloniaEdit;
using Avalonia.Interactivity;
using UndertaleModLib.Models;

namespace UndertaleModTool
{
    public partial class UndertaleShaderEditor : DataUserControl
    {
        private static MainWindow mainWindow => MainWindow.Instance;

        public UndertaleShaderEditor()
        {
            InitializeComponent();
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            AttributesGrid.AddNew();
        }

        private void TextEditor_Loaded(object sender, RoutedEventArgs e)
        {
            var editor = sender as TextEditor;
            if (editor is null)
            {
                mainWindow.ShowError("Cannot load the code of one of the shader properties - the editor is not found?");
                return;
            }

            var srcString = editor.DataContext as UndertaleString;
            if (srcString is null)
            {
                mainWindow.ShowError("Cannot load the code of one of the shader properties - the source string object is null.");
                return;
            }

            editor.Text = srcString.Content;
        }

        private void TextEditor_LostFocus(object sender, RoutedEventArgs e)
        {
            var editor = sender as TextEditor;
            if (editor is null)
            {
                mainWindow.ShowError("The changes weren't saved - the editor is not found?");
                return;
            }

            var srcString = editor.DataContext as UndertaleString;
            if (srcString is null)
            {
                mainWindow.ShowError("The changes weren't saved - the source string object is null.");
                return;
            }

            srcString.Content = editor.Text;
        }

        private void TextEditor_DataContextChanged(object sender, EventArgs e)
        {
            var editor = sender as TextEditor;
            if (editor is null)
                return;

            var srcString = editor.DataContext as UndertaleString;
            if (srcString is null)
                return;

            editor.Text = srcString.Content;
        }
    }
}
