using System.Runtime.Serialization;
using System.ServiceModel;

namespace DriverTool.x86.Service
{
    [ServiceContract]
    public interface IToolService
    {
        [OperationContract]
        string GetWebPageContent(string url);
    }
}
