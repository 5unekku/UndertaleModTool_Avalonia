using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace UndertaleModTool
{
    /// <summary>
    /// the dialog window rendered by <see cref="MessageBox"/>. built in code so it stays self-contained.
    /// </summary>
    internal sealed class MessageBoxWindow : Window
    {
        public MessageBoxResult Result { get; private set; } = MessageBoxResult.None;

        public MessageBoxWindow(string text, string title, MessageBoxButton buttons, MessageBoxImage image)
        {
            Title = title ?? "UndertaleModTool";
            SizeToContent = SizeToContent.WidthAndHeight;
            CanResize = false;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            MinWidth = 280;
            MaxWidth = 640;
            ShowInTaskbar = false;

            var message = new TextBlock
            {
                Text = text ?? string.Empty,
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center
            };

            var header = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 15,
                Margin = new Thickness(20, 20, 20, 10)
            };
            (string glyph, Color color) = IconFor(image);
            if (glyph is not null)
            {
                header.Children.Add(new Border
                {
                    Width = 32,
                    Height = 32,
                    CornerRadius = new CornerRadius(16),
                    Background = new SolidColorBrush(color),
                    VerticalAlignment = VerticalAlignment.Top,
                    Child = new TextBlock
                    {
                        Text = glyph,
                        Foreground = Brushes.White,
                        FontSize = 20,
                        FontWeight = FontWeight.Bold,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                });
            }
            header.Children.Add(message);

            var buttonBar = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Spacing = 10,
                Margin = new Thickness(20, 10, 20, 20)
            };
            foreach ((string label, MessageBoxResult result, bool isDefault, bool isCancel) in ButtonsFor(buttons))
            {
                var button = new Button
                {
                    Content = label,
                    MinWidth = 80,
                    IsDefault = isDefault,
                    IsCancel = isCancel
                };
                MessageBoxResult captured = result;
                button.Click += (_, _) =>
                {
                    Result = captured;
                    Close();
                };
                buttonBar.Children.Add(button);
            }

            DockPanel.SetDock(buttonBar, Dock.Bottom);
            var root = new DockPanel { LastChildFill = true };
            root.Children.Add(buttonBar);
            root.Children.Add(header);
            Content = root;
        }

        private static (string glyph, Color color) IconFor(MessageBoxImage image) => image switch
        {
            MessageBoxImage.Error => ("✕", Color.FromRgb(196, 43, 28)),
            MessageBoxImage.Warning => ("!", Color.FromRgb(157, 93, 0)),
            MessageBoxImage.Question => ("?", Color.FromRgb(0, 99, 177)),
            MessageBoxImage.Information => ("i", Color.FromRgb(0, 99, 177)),
            _ => (null, default)
        };

        private static IEnumerable<(string label, MessageBoxResult result, bool isDefault, bool isCancel)> ButtonsFor(MessageBoxButton buttons)
        {
            switch (buttons)
            {
                case MessageBoxButton.OKCancel:
                    yield return ("OK", MessageBoxResult.OK, true, false);
                    yield return ("Cancel", MessageBoxResult.Cancel, false, true);
                    break;
                case MessageBoxButton.YesNo:
                    yield return ("Yes", MessageBoxResult.Yes, true, false);
                    yield return ("No", MessageBoxResult.No, false, true);
                    break;
                case MessageBoxButton.YesNoCancel:
                    yield return ("Yes", MessageBoxResult.Yes, true, false);
                    yield return ("No", MessageBoxResult.No, false, false);
                    yield return ("Cancel", MessageBoxResult.Cancel, false, true);
                    break;
                default:
                    yield return ("OK", MessageBoxResult.OK, true, true);
                    break;
            }
        }
    }
}
