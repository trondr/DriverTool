using System.Diagnostics;
using Common.Logging;

namespace DriverTool.UI
{
    internal class LoggerTraceListener : TraceListener
    {
        private readonly ILog _logger;

        public LoggerTraceListener(ILog logger)
        {
            _logger = logger;
        }

        public override void Write(string message)
        {
            _logger.Warn(message);
        }

        public override void WriteLine(string message)
        {
            //Debugger.Break()
            _logger.Warn(message);
        }
    }
}