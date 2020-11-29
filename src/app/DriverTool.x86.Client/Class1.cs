using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using DriverTool.x86.Client.ServiceReference1;

namespace DriverTool.x86.Client
{
    public class Class1
    {
        public static CompositeType GetData()
        {
            var client = new ServiceReference1.Service1Client();
            return client.GetDataUsingDataContract(new CompositeType {BoolValue = true,StringValue = "TestString"});
        }
    }
}
