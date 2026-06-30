using System;
using System.ComponentModel;
using Avalonia.Interactivity;
using Avalonia.Threading;
using UndertaleModLib.Models;

namespace UndertaleModTool
{
    public partial class UndertaleScriptEditor : DataUserControl
    {
        private static MainWindow mainWindow => MainWindow.Instance;

        // avalonia's DataContextChanged carries no old/new value, so the subscription target is tracked here
        private UndertaleScript subscribed;

        public UndertaleScriptEditor()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
            Unloaded += OnUnloaded;
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (subscribed is not null)
            {
                subscribed.PropertyChanged -= OnPropertyChanged;
                subscribed = null;
            }
        }

        private void OnDataContextChanged(object sender, EventArgs e)
        {
            if (subscribed is not null)
                subscribed.PropertyChanged -= OnPropertyChanged;

            subscribed = DataContext as UndertaleScript;

            if (subscribed is not null)
                subscribed.PropertyChanged += OnPropertyChanged;
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            OnAssetUpdated();
        }

        private void OnAssetUpdated()
        {
            if (mainWindow.Project is null || !mainWindow.IsSelectedProjectExportable)
                return;

            Dispatcher.UIThread.Post(() =>
            {
                if (DataContext is UndertaleScript obj)
                    mainWindow.Project?.MarkAssetForExport(obj);
            });
        }
    }
}
