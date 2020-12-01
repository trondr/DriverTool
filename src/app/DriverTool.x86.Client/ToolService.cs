using DriverTool.x86.Client.ToolServiceReference;

namespace DriverTool.x86.Client
{
    public class ToolService
    {
        public static string GetWebPageContent(string url)
        {
            var client = new ToolServiceClient();
            return client.GetWebPageContent(url);
        }
    }
}
