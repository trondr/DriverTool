namespace DriverTool.x86.Service
{
    public class ToolService : IToolService
    {
        [STAOperationBehavior]
        public string GetWebPageContent(string url)
        {
            var logger = Common.Logging.LogManager.GetLogger("ToolService.GetWebPageContent");
            var webPageContent = DriverTool.CSharpLib.WebParser.GetWebPageContentUnSafe(url, logger);
            return webPageContent;
        }
    }
}
