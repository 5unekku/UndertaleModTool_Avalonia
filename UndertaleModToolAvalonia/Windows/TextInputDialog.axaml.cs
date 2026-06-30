using Avalonia.Controls;
using Avalonia.Interactivity;

namespace UndertaleModTool
{
    public partial class TextInputDialog : Window
    {
        public bool CancelButtonVisibility => !PreventClose;
        public string Message { get; set; }
        public string MessageTitle { get; set; }
        public string ButtonTitle { get; set; }
        public string CancelButtonTitle { get; set; }
        public string InputText { get; set; }
        public bool PreventClose { get; set; }
        public bool IsMultiline { get; set; }

        /// <summary>the dialog's outcome (true if the submit button was pressed). read after <c>ShowDialogSync</c>.</summary>
        public bool Result { get; private set; }

        public TextInputDialog(string titleText, string labelText, string defaultInputBoxText, string cancelButtonText, string submitButtonText, bool isMultiline, bool preventClose)
        {
            IsMultiline = isMultiline;
            PreventClose = preventClose;
            MessageTitle = titleText;
            Message = labelText;
            ButtonTitle = submitButtonText;
            CancelButtonTitle = cancelButtonText;
            InputText = defaultInputBoxText;

            InitializeComponent();
            DataContext = this;
        }

        protected override void OnClosing(WindowClosingEventArgs e)
        {
            e.Cancel = PreventClose;
            base.OnClosing(e);
        }

        public void TryHide()
        {
            if (IsVisible)
            {
                PreventClose = false;
                Hide();
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            PreventClose = false;
            Result = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
