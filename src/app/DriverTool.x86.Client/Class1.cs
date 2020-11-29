using DriverTool.x86.Client.ToolServiceReference;

namespace DriverTool.x86.Client
{
    public class Class1
    {
        public static CompositeType GetData()
        {
            var client = new ToolServiceClient();
            return client.GetDataUsingDataContract(new CompositeType {BoolValue = true,StringValue = "TestString"});
        }
    }
}
