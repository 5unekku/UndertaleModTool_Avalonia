using System;
using System.IO;
using Avalonia;
using log4net;

namespace UndertaleModTool
{
    public static class Program
    {
        /// <summary>returns the directory the running executable lives in.</summary>
        public static string GetExecutableDirectory()
        {
            return Path.GetDirectoryName(Environment.ProcessPath);
        }

        // avalonia configuration, used by the visual designer and at startup.
        public static AppBuilder BuildAvaloniaApp()
        {
            return AppBuilder.Configure<App>()
                             .UsePlatformDetect()
                             .WithInterFont()
                             .LogToTrace();
        }

        [STAThread]
        public static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += GlobalUnhandledExceptionHandler;
            try
            {
                BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
            }
            catch (Exception e)
            {
                File.WriteAllText(Path.Join(GetExecutableDirectory(), "crash.txt"), e.ToString());
                throw;
            }
        }

        private static void GlobalUnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = (Exception)e.ExceptionObject;
            ILog log = LogManager.GetLogger(typeof(Program));
            log.Error(ex.Message + "\n" + ex.StackTrace);
            File.WriteAllText(Path.Join(GetExecutableDirectory(), "crash2.txt"), ex.ToString() + "\n" + ex.Message + "\n" + ex.StackTrace);
        }
    }
}
