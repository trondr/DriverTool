using System;
using System.ServiceModel.Dispatcher;
using System.Threading;

namespace DriverTool.x86.Service
{
    //Source: https://scottseely.com/2009/07/17/calling-an-sta-com-object-from-a-wcf-operation/
    public class STAOperationInvoker : IOperationInvoker
    {
        readonly IOperationInvoker _innerInvoker;
        public STAOperationInvoker(IOperationInvoker invoker)
        {
            _innerInvoker = invoker;
        }
        
        public object[] AllocateInputs()
        {
            return _innerInvoker.AllocateInputs();
        }

        public object Invoke(object instance, object[] inputs, out object[] outputs)
        {
            // Create a new, STA thread
            object[] staOutputs = null;
            object retval = null;
            Thread thread = new Thread(
                delegate () {
                    retval = _innerInvoker.Invoke(instance, inputs, out staOutputs);
                });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
            outputs = staOutputs;
            return retval;
        }

        public IAsyncResult InvokeBegin(object instance, object[] inputs, AsyncCallback callback, object state)
        {
            // We don’t handle async…
            throw new NotImplementedException();
        }

        public object InvokeEnd(object instance, out object[] outputs, IAsyncResult result)
        {
            // We don’t handle async…
            throw new NotImplementedException();
        }

        public bool IsSynchronous => true;
    }
}