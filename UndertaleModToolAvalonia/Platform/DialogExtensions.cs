using Avalonia.Controls;
using Avalonia.Threading;

namespace UndertaleModTool
{
    /// <summary>
    /// synchronous modal helpers for the ported windows. wpf's <c>Window.ShowDialog()</c> blocks and returns a
    /// result; avalonia's is async-only, so these pump a nested dispatcher frame to restore the blocking behavior
    /// (the windows expose their outcome through their own properties, as in the wpf code).
    /// </summary>
    public static class DialogExtensions
    {
        /// <summary>shows <paramref name="dialog"/> modally and blocks until it closes.</summary>
        public static void ShowDialogSync(this Window dialog, Window owner = null)
        {
            if (!Dispatcher.UIThread.CheckAccess())
            {
                Dispatcher.UIThread.Invoke(() => dialog.ShowDialogSync(owner));
                return;
            }

            owner ??= MessageBox.MainDesktopWindow;

            var frame = new DispatcherFrame();
            dialog.Closed += (_, _) => frame.Continue = false;

            if (owner is not null && owner.IsVisible)
                _ = dialog.ShowDialog(owner);
            else
                dialog.Show();

            Dispatcher.UIThread.PushFrame(frame);
        }

        /// <summary>shows <paramref name="dialog"/> modally, blocks, and returns the value passed to <c>Close(result)</c>.</summary>
        public static TResult ShowDialogSync<TResult>(this Window dialog, Window owner = null)
        {
            if (!Dispatcher.UIThread.CheckAccess())
                return Dispatcher.UIThread.Invoke(() => dialog.ShowDialogSync<TResult>(owner));

            owner ??= MessageBox.MainDesktopWindow;
            if (owner is null || !owner.IsVisible)
            {
                ShowDialogSync(dialog, owner);
                return default;
            }

            var frame = new DispatcherFrame();
            var task = dialog.ShowDialog<TResult>(owner);
            task.ContinueWith(_ => frame.Continue = false, System.Threading.Tasks.TaskScheduler.FromCurrentSynchronizationContext());
            Dispatcher.UIThread.PushFrame(frame);
            return task.IsCompletedSuccessfully ? task.Result : default;
        }
    }
}
