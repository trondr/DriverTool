using System;
using System.Threading;

namespace DriverTool.x86.Service
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "Service1" in both code and config file together.
    public class Service1 : IService1
    {
        public string GetData(int value)
        {
            return string.Format("You entered: {0}", value);
        }

        [STAOperationBehavior]
        public CompositeType GetDataUsingDataContract(CompositeType composite)
        {
            var apartmentState = Thread.CurrentThread.GetApartmentState().ToString();
            if (composite == null)
            {
                throw new ArgumentNullException("composite");
            }
            if (composite.BoolValue)
            {
                composite.StringValue += "Suffix" + "-" + apartmentState;
            }
            return composite;
        }
    }
}
