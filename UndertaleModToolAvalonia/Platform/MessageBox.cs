using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;

namespace UndertaleModTool
{
    /// <summary>specifies the buttons displayed on a <see cref="MessageBox"/>. mirrors the wpf enum.</summary>
    public enum MessageBoxButton
    {
        OK = 0,
        OKCancel = 1,
        YesNoCancel = 3,
        YesNo = 4
    }

    /// <summary>specifies the icon displayed on a <see cref="MessageBox"/>. mirrors the wpf enum.</summary>
    public enum MessageBoxImage
    {
        None = 0,
        Error = 16,
        Hand = 16,
        Stop = 16,
        Question = 32,
        Warning = 48,
        Exclamation = 48,
        Information = 64,
        Asterisk = 64
    }

    /// <summary>specifies which message box button was clicked. mirrors the wpf enum.</summary>
    public enum MessageBoxResult
    {
        None = 0,
        OK = 1,
        Cancel = 2,
        Yes = 6,
        No = 7
    }

    /// <summary>
    /// a drop-in replacement for wpf's <c>System.Windows.MessageBox</c>. shows a modal dialog synchronously
    /// (using a nested dispatcher frame, like wpf does), so the many synchronous call sites port unchanged.
    /// </summary>
    public static class MessageBox
    {
        public static MessageBoxResult Show(string messageBoxText)
            => Show(null, messageBoxText, "UndertaleModTool", MessageBoxButton.OK, MessageBoxImage.None);

        public static MessageBoxResult Show(string messageBoxText, string caption)
            => Show(null, messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.None);

        public static MessageBoxResult Show(string messageBoxText, string caption, MessageBoxButton button)
            => Show(null, messageBoxText, caption, button, MessageBoxImage.None);

        public static MessageBoxResult Show(string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon)
            => Show(null, messageBoxText, caption, button, icon);

        public static MessageBoxResult Show(Window owner, string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon)
        {
            // marshal to the ui thread if we're called from a background (script) thread, blocking the caller
            if (!Dispatcher.UIThread.CheckAccess())
                return Dispatcher.UIThread.Invoke(() => Show(owner, messageBoxText, caption, button, icon));

            owner ??= MainDesktopWindow;

            var dialog = new MessageBoxWindow(messageBoxText, caption, button, icon);
            var frame = new DispatcherFrame();
            dialog.Closed += (_, _) => frame.Continue = false;

            if (owner is not null && owner.IsVisible)
                _ = dialog.ShowDialog(owner);
            else
                dialog.Show();

            Dispatcher.UIThread.PushFrame(frame);
            return dialog.Result;
        }

        /// <summary>the application's main window, used as a default owner when none is supplied.</summary>
        internal static Window MainDesktopWindow
            => (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
    }
}
