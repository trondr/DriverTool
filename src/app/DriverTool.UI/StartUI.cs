using System.Diagnostics;
using Common.Logging;
using System.Windows;

namespace DriverTool.UI
{
    public static class StartUi
    {
        private static ILog _logger;

        public static int Start(ILog logger)
        {
            _logger = logger;
            logger.Info("Starting CM device driver user interface.");
            var app = new Application();
            app.Startup += AppOnStartup;
            var windows = new MainWindow();
            app.Run(windows);
            logger.Info("Stopping CM device driver user interface.");
            return 0;
        }

        private static void AppOnStartup(object sender, StartupEventArgs e)
        {
            PresentationTraceSources.Refresh();
            PresentationTraceSources.DataBindingSource.Listeners.Add(new LoggerTraceListener(_logger));
            PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.Warning | SourceLevels.Error | SourceLevels.Critical;
        }
    }
}
