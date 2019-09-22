using System.Reflection;
using System.Threading;
using Common.Logging;
using SHDocVw;

namespace DriverTool.CSharpLib
{

    public static class WebParser
    {
        private static InternetExplorer GetInternetExplorer(ILog logger)
        {
            var ie = new InternetExplorer { Visible = false };
            //Add event handlers
            ie.BeforeNavigate2 += (object sender, ref object url, ref object flags, ref object name, ref object data,
                ref object headers, ref bool cancel) =>
            {
                logger.Info($"Before navigating to url: '{url}'.");
            };
            ie.BeforeScriptExecute += window => { logger.Info($"Before script execute"); };
            ie.NavigateComplete2 += (object sender, ref object url) =>
            {
                logger.Info($"Navigating complete to url: '{url}'.");
            };
            ie.NavigateError += (object sender, ref object url, ref object frame, ref object code, ref bool cancel) =>
            {
                logger.Error($"Navigation failed to url '{url}'");
            };
            return ie;
        }

        public static void AssertStaApartmentState()
        {
            if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
                throw new ThreadStateException("The current threads apartment state is not STA");
        }

        public static string GetWebPageContentUnSafe(string uri, ILog logger)
        {
            AssertStaApartmentState();
            InternetExplorer internetExplorer = null;
            try
            {
                internetExplorer = GetInternetExplorer(logger);
                //Navigate to page
                internetExplorer.Navigate(uri);
                while (internetExplorer.ReadyState != tagREADYSTATE.READYSTATE_COMPLETE)
                {
                    Thread.Sleep(100);
                    logger.Info($"Waiting for '{uri}' to complete loading...");
                }
                logger.Info($"Done loading '{uri}'!");
                var document = internetExplorer.Document;
                var parentWindow = document.parentWindow;
                parentWindow.execScript("var JSIEVariable = new XMLSerializer().serializeToString(document);", "javascript");
                var parentWindowType = parentWindow.GetType();
                var contentObject = parentWindowType.InvokeMember("JSIEVariable", BindingFlags.GetProperty, null, parentWindow, null);
                var html = contentObject.ToString();
                return html;
            }            
            finally
            {
                internetExplorer?.Quit();
            }
        }
    }
}
