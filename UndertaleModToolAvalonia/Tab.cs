using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using UndertaleModLib;

namespace UndertaleModTool
{
    /// <summary>
    /// one open editor tab. holds the resource it currently shows plus per-tab back/forward navigation history.
    /// (the wpf Tab also persisted detailed per-editor scroll/selection state; that polish is not ported yet.)
    /// </summary>
    public class Tab : INotifyPropertyChanged
    {
        private object currentObject;
        private string tabTitle = "Untitled";

        public ObservableCollection<object> History { get; } = new();
        public int HistoryPosition { get; set; }
        public bool IsCustomTitle { get; set; }
        public int TabIndex { get; set; }
        public bool AutoClose { get; set; }

        public object CurrentObject
        {
            get => currentObject;
            set
            {
                currentObject = value;
                OnPropertyChanged();
                if (!IsCustomTitle)
                    TabTitle = GenerateTabTitle(value);
            }
        }

        public string TabTitle
        {
            get => tabTitle;
            set { tabTitle = value; OnPropertyChanged(); }
        }

        public Tab(object obj, int tabIndex, string tabTitle = null)
        {
            TabIndex = tabIndex;
            if (tabTitle is not null)
            {
                this.tabTitle = tabTitle;
                IsCustomTitle = true;
            }
            History.Add(obj);
            HistoryPosition = 0;
            CurrentObject = obj;
        }

        /// <summary>navigates this tab to a new object, truncating any forward history.</summary>
        public void NavigateTo(object obj)
        {
            while (History.Count > HistoryPosition + 1)
                History.RemoveAt(History.Count - 1);
            History.Add(obj);
            HistoryPosition = History.Count - 1;
            CurrentObject = obj;
        }

        public bool CanGoBack => HistoryPosition > 0;
        public bool CanGoForward => HistoryPosition < History.Count - 1;

        public void GoBack()
        {
            if (CanGoBack)
                CurrentObject = History[--HistoryPosition];
        }

        public void GoForward()
        {
            if (CanGoForward)
                CurrentObject = History[++HistoryPosition];
        }

        public static string GenerateTabTitle(object obj) => obj switch
        {
            null => "(none)",
            DescriptionView dv => dv.Heading,
            UndertaleNamedResource named => named.Name?.Content ?? obj.GetType().Name,
            GeneralInfoEditor => "General info",
            GlobalInitEditor => "Global init",
            GameEndEditor => "Game end",
            _ => obj.ToString()
        };

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
