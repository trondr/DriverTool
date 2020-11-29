using System;
using System.Threading;

namespace DriverTool.x86.Service
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "Service1" in both code and config file together.
    public class Service1 : IService1
    {
        public string GetData(int value)
        {
            return $"You entered: {value}";
        }

        [STAOperationBehavior]
        public CompositeType GetDataUsingDataContract(CompositeType composite)
        {
            var apartmentState = Thread.CurrentThread.GetApartmentState().ToString();
            if (composite == null)
            {
                throw new ArgumentNullException(nameof(composite));
            }
            if (composite.BoolValue)
            {
                composite.StringValue += "Suffix" + "-" + apartmentState;
            }
            return composite;
        }
    }
}
