using Avalonia.Controls;

namespace UndertaleModTool
{
    /// <summary>
    /// provides <see cref="MessageBox"/> extensions for <see cref="Window"/>s. (the wpf
    /// <c>WindowPlacementExtensions</c> half is ported later, with the main window, using avalonia window state.)
    /// </summary>
    public static class MessageBoxExtensions
    {
        /// <summary>shows an informational message box with <paramref name="window"/> as the parent.</summary>
        public static MessageBoxResult ShowMessage(this Window window, string messageBoxText, string title = "UndertaleModTool")
        {
            return ShowCore(window, messageBoxText, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>shows a yes/no question message box with <paramref name="window"/> as the parent.</summary>
        public static MessageBoxResult ShowQuestion(this Window window, string messageBoxText, MessageBoxImage icon = MessageBoxImage.Question, string title = "UndertaleModTool")
        {
            return ShowCore(window, messageBoxText, title, MessageBoxButton.YesNo, icon);
        }

        /// <summary>shows a yes/no/cancel question message box with <paramref name="window"/> as the parent.</summary>
        public static MessageBoxResult ShowQuestionWithCancel(this Window window, string messageBoxText, MessageBoxImage icon = MessageBoxImage.Question, string title = "UndertaleModTool")
        {
            return ShowCore(window, messageBoxText, title, MessageBoxButton.YesNoCancel, icon);
        }

        /// <summary>shows a warning message box with <paramref name="window"/> as the parent.</summary>
        public static MessageBoxResult ShowWarning(this Window window, string messageBoxText, string title = "Warning")
        {
            return ShowCore(window, messageBoxText, title, MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        /// <summary>shows an error message box with <paramref name="window"/> as the parent.</summary>
        public static MessageBoxResult ShowError(this Window window, string messageBoxText, string title = "Error")
        {
            return ShowCore(window, messageBoxText, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        // funnel: MessageBox.Show already marshals to the ui thread, so no explicit Dispatcher.Invoke is needed here
        private static MessageBoxResult ShowCore(this Window window, string text, string title, MessageBoxButton buttons, MessageBoxImage image)
        {
            return MessageBox.Show(window, text, title, buttons, image);
        }
    }
}
