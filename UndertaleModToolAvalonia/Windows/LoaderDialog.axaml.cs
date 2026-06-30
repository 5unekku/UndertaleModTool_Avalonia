using System;
using Avalonia.Controls;
using Avalonia.Threading;

namespace UndertaleModTool
{
    // progress dialog. drives its named text blocks directly from the property setters (avalonia controls cannot
    // add a plain INotifyPropertyChanged event). the wpf TaskbarItemInfo and FileMessageEvent hookup are dropped.
    public partial class LoaderDialog : Window
    {
        private string messageTitle;
        private string message;
        private string statusText = "Please wait...";

        public string MessageTitle
        {
            get => messageTitle;
            set { messageTitle = value; Dispatcher.UIThread.Post(() => Title = value); }
        }

        public string Message
        {
            get => message;
            set { message = value; if (MessageText is not null) Dispatcher.UIThread.Post(() => MessageText.Text = value); }
        }

        public string StatusText
        {
            get => statusText;
            set { statusText = value; if (StatusTextBlock is not null) Dispatcher.UIThread.Post(() => StatusTextBlock.Text = value); }
        }

        public string SavedStatusText { get; set; }
        public bool PreventClose { get; set; }
        public bool IsClosed { get; set; }

        public double? Maximum
        {
            get => !ProgressBar.IsIndeterminate ? ProgressBar.Maximum : null;
            set
            {
                ProgressBar.IsIndeterminate = !value.HasValue;
                if (value.HasValue)
                    ProgressBar.Maximum = value.Value;
            }
        }

        public LoaderDialog(string title, string msg)
        {
            InitializeComponent();
            MessageTitle = title;
            Message = msg;
            StatusText = statusText;
        }

        protected override void OnClosing(WindowClosingEventArgs e)
        {
            e.Cancel = PreventClose;
            base.OnClosing(e);
        }

        protected override void OnClosed(EventArgs e)
        {
            IsClosed = true;
            base.OnClosed(e);
        }

        public void TryHide() => Dispatcher.UIThread.Post(() => { if (IsVisible) { PreventClose = false; Hide(); } });

        public void TryClose()
        {
            PreventClose = false;
            Dispatcher.UIThread.Post(() => { if (!IsClosed) Close(); });
        }

        public void TryShowDialog() => Dispatcher.UIThread.Post(() => { if (!IsClosed) this.ShowDialogSync(); });

        public void ReportProgress(string status) => StatusText = status;

        public void ReportProgress(double value)
        {
            ReportProgress(value + "/" + Maximum + (!string.IsNullOrEmpty(SavedStatusText) ? ": " + SavedStatusText : ""));
            UpdateValue(value);
        }

        public void UpdateValue(double value) => Dispatcher.UIThread.Post(() => ProgressBar.Value = value);

        public void ReportProgress(string status, double value)
        {
            ReportProgress(value + "/" + Maximum + (!string.IsNullOrEmpty(status) ? ": " + status : ""));
            UpdateValue(value);
        }

        public void Update(string message, string status, double progressValue, double maxValue)
        {
            if (!IsVisible)
                Dispatcher.UIThread.Post(Show);
            if (message != null)
                Message = message;
            if (maxValue != 0)
                Dispatcher.UIThread.Post(() => Maximum = maxValue);
            ReportProgress(status, progressValue);
        }

        public void Update(string status)
        {
            if (!IsVisible)
                Dispatcher.UIThread.Post(Show);
            ReportProgress(status);
        }
    }
}
