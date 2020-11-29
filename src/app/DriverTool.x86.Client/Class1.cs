using System.Security.Policy;
using DriverTool.x86.Client.ToolServiceReference;

namespace DriverTool.x86.Client
{
    public class Class1
    {
        public static string GetData()
        {
            var client = new ToolServiceClient();
            var url = "https://support.lenovo.com/no/en/downloads/ds112090";
            return client.GetWebPageContent(url);
        }
    }
}
