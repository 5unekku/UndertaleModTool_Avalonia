using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using UndertaleModLib;
using UndertaleModLib.Models;

namespace UndertaleModTool
{
    public partial class UndertalePathEditor : DataUserControl
    {
        private static MainWindow mainWindow => MainWindow.Instance;

        private UndertalePath subscribed;

        public UndertalePathEditor()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
            Unloaded += OnUnloaded;
        }

        private void Add_Click(object sender, RoutedEventArgs e) => PointsGrid.AddNew();

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            Detach();
        }

        private void Detach()
        {
            if (subscribed is not null)
            {
                subscribed.PropertyChanged -= OnPropertyChanged;
                if (subscribed.Points is UndertaleObservableList<UndertalePath.PathPoint> points)
                    points.CollectionChanged -= CollectionChanged;
                subscribed = null;
            }
        }

        private void OnDataContextChanged(object sender, EventArgs e)
        {
            Detach();

            subscribed = DataContext as UndertalePath;
            if (subscribed is not null)
            {
                subscribed.PropertyChanged += OnPropertyChanged;
                if (subscribed.Points is UndertaleObservableList<UndertalePath.PathPoint> points)
                    points.CollectionChanged += CollectionChanged;
            }
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e) => OnAssetUpdated();
        private void CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) => OnAssetUpdated();

        private void OnAssetUpdated()
        {
            if (mainWindow.Project is null || !mainWindow.IsSelectedProjectExportable)
                return;

            Dispatcher.UIThread.Post(() =>
            {
                if (DataContext is UndertalePath obj)
                    mainWindow.Project?.MarkAssetForExport(obj);
            });
        }
    }

    /// <summary>builds a green polyline <see cref="PathGeometry"/> preview from a path's points.</summary>
    public class PointsDisplayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not UndertalePath path || path.Points.Count == 0)
                return null;

            var figure = new PathFigure
            {
                StartPoint = new Point(path.Points[0].X, path.Points[0].Y),
                IsClosed = path.IsClosed,
                Segments = new PathSegments()
            };
            for (int i = 1; i < path.Points.Count; i++)
                figure.Segments.Add(new LineSegment { Point = new Point(path.Points[i].X, path.Points[i].Y) });

            var geometry = new PathGeometry { Figures = new PathFigures { figure } };
            return geometry;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
