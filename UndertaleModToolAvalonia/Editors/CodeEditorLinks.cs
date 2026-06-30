using System.Collections.Generic;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using AvaloniaEdit.Highlighting;
using AvaloniaEdit.Rendering;
using UndertaleModLib;
using UndertaleModLib.Models;

namespace UndertaleModTool
{
    /// <summary>
    /// makes identifiers in decompiled GML that match a game resource clickable (ctrl+click navigates, middle-click
    /// opens in a new tab). this is the avalonia port of the wpf code editor's NameGenerator/ClickVisualLineText.
    /// </summary>
    public class NameLinkGenerator : VisualLineElementGenerator
    {
        private static readonly SolidColorBrush LinkBrush = new(Color.FromRgb(0xFF, 0x80, 0x80));

        private readonly IHighlighter highlighter;
        private readonly Dictionary<int, int> lineNameSections = new();

        public NameLinkGenerator(TextArea textArea)
        {
            highlighter = textArea.GetService(typeof(IHighlighter)) as IHighlighter;
        }

        private static UndertaleNamedResource Resolve(string name)
        {
            UndertaleData data = MainWindow.Instance?.Data;
            if (data is null || string.IsNullOrEmpty(name))
                return null;
            // search the common named resource lists by content name
            foreach (var list in new IEnumerable<UndertaleNamedResource>[]
                     { data.Scripts, data.GameObjects, data.Sprites, data.Sounds, data.Rooms, data.Code, data.Backgrounds, data.Fonts, data.Paths, data.Timelines, data.Shaders })
            {
                if (list is null)
                    continue;
                foreach (UndertaleNamedResource res in list)
                    if (res?.Name?.Content == name)
                        return res;
            }
            return null;
        }

        public override void StartGeneration(ITextRunConstructionContext context)
        {
            lineNameSections.Clear();
            DocumentLine docLine = context.VisualLine.FirstDocumentLine;
            if (highlighter is not null && docLine.Length != 0)
            {
                try
                {
                    HighlightedLine highlighted = highlighter.HighlightLine(docLine.LineNumber);
                    foreach (HighlightedSection section in highlighted.Sections)
                        if (section.Color.Name is "Identifier" or "Function")
                            lineNameSections[section.Offset] = section.Length;
                }
                catch
                {
                    // highlighting failure just disables links for this line
                }
            }
            base.StartGeneration(context);
        }

        public override int GetFirstInterestedOffset(int startOffset)
        {
            foreach (var section in lineNameSections)
                if (startOffset <= section.Key)
                    return section.Key;
            return -1;
        }

        public override VisualLineElement ConstructElement(int offset)
        {
            if (!lineNameSections.TryGetValue(offset, out int nameLength))
                return null;

            string nameText = CurrentContext.Document.GetText(offset, nameLength);
            UndertaleNamedResource resource = Resolve(nameText);
            if (resource is null)
                return null;

            var element = new ClickVisualLineText(CurrentContext.VisualLine, nameLength, LinkBrush);
            element.Clicked += button =>
            {
                if (button == 2)
                    MainWindow.Instance?.ChangeSelection(resource, true); // middle = new tab
                else
                    MainWindow.Instance?.ChangeSelection(resource, false);
            };
            return element;
        }
    }

    /// <summary>a clickable, colored run of text within a decompiled code line (ctrl+click / middle-click).</summary>
    public class ClickVisualLineText : VisualLineText
    {
        public delegate void ClickHandler(int button);
        public event ClickHandler Clicked;

        private readonly IBrush foregroundBrush;

        public ClickVisualLineText(VisualLine parentVisualLine, int length, IBrush foregroundBrush = null)
            : base(parentVisualLine, length)
        {
            this.foregroundBrush = foregroundBrush;
        }

        public override TextRun CreateTextRun(int startVisualColumn, ITextRunConstructionContext context)
        {
            if (foregroundBrush is not null)
                TextRunProperties.SetForegroundBrush(foregroundBrush);
            return base.CreateTextRun(startVisualColumn, context);
        }

        protected override void OnQueryCursor(PointerEventArgs e)
        {
            if ((e.KeyModifiers & KeyModifiers.Control) == KeyModifiers.Control)
                e.Handled = true;
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            if (e.Handled)
                return;
            var props = e.GetCurrentPoint(null).Properties;
            bool ctrlLeft = props.IsLeftButtonPressed && (e.KeyModifiers & KeyModifiers.Control) == KeyModifiers.Control;
            if (ctrlLeft || props.IsMiddleButtonPressed)
            {
                Clicked?.Invoke(props.IsMiddleButtonPressed ? 2 : 0);
                e.Handled = true;
            }
        }

        protected override VisualLineText CreateInstance(int length)
        {
            var clone = new ClickVisualLineText(ParentVisualLine, length, foregroundBrush);
            clone.Clicked += Clicked;
            return clone;
        }
    }
}
