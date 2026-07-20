using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace UndertaleModTool
{
    // tab management: close all / close others / restore closed / switch tab, and the tab-header context menu.
    // ported from the wpf tab commands, adapted to this port's simpler ObservableCollection<Tab> model.
    public partial class MainWindow
    {
        /// <summary>recently closed resource tabs, most-recent last (Ctrl+Shift+T restores the last one).</summary>
        public List<Tab> ClosedTabsHistory { get; } = new();

        private bool IsOnlyWelcomeTab => Tabs.Count == 1 && Tabs[0].CurrentObject is DescriptionView;

        internal void CloseAllTabs()
        {
            if (IsOnlyWelcomeTab)
                return;
            ClosedTabsHistory.Clear();
            Tabs.Clear();
            CurrentTab = null;
            OpenInTab(new DescriptionView("Welcome to UndertaleModTool!",
                "Open a data.win file to get started, then click items on the left to view them."), true, "Welcome!");
        }

        internal void CloseOtherTabs(Tab keep)
        {
            for (int i = Tabs.Count - 1; i >= 0; i--)
            {
                Tab t = Tabs[i];
                if (t == keep)
                    continue;
                if (t.CurrentObject is not DescriptionView)
                    ClosedTabsHistory.Add(t);
                Tabs.RemoveAt(i);
            }
            for (int i = 0; i < Tabs.Count; i++)
                Tabs[i].TabIndex = i;
            CurrentTab = keep;
            EditorTabs.SelectedItem = keep;
        }

        internal void RestoreClosedTab()
        {
            if (ClosedTabsHistory.Count == 0)
                return;
            Tab lastTab = ClosedTabsHistory[^1];
            ClosedTabsHistory.RemoveAt(ClosedTabsHistory.Count - 1);

            // drop a lone welcome tab so the restored one takes its place
            if (IsOnlyWelcomeTab)
                Tabs.Clear();

            int insertAt = System.Math.Clamp(lastTab.TabIndex, 0, Tabs.Count);
            Tabs.Insert(insertAt, lastTab);
            for (int i = 0; i < Tabs.Count; i++)
                Tabs[i].TabIndex = i;
            CurrentTab = lastTab;
            EditorTabs.SelectedItem = lastTab;
        }

        internal void SwitchTab(int delta)
        {
            if (Tabs.Count == 0)
                return;
            int index = EditorTabs.SelectedIndex + delta;
            if (index >= 0 && index < Tabs.Count)
                EditorTabs.SelectedIndex = index;
        }

        private void CloseTabMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Control)?.DataContext is Tab tab)
                CloseTab(tab);
        }

        private void CloseOtherTabsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Control)?.DataContext is Tab tab)
                CloseOtherTabs(tab);
        }

        private void CloseAllTabsMenuItem_Click(object sender, RoutedEventArgs e) => CloseAllTabs();
    }
}
