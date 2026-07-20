using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace UndertaleModTool
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            // load user settings (decompiler options, grid, etc.) before the main window
            try { Settings.Load(); } catch { }
            Settings.ApplyTheme();

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                desktop.MainWindow = new MainWindow(desktop.Args);

            base.OnFrameworkInitializationCompleted();
        }
    }
}
