using System;
using System.ServiceModel.Description;

namespace DriverTool.x86.Service
{
    //Source: https://scottseely.com/2009/07/17/calling-an-sta-com-object-from-a-wcf-operation/
    public class STAOperationBehaviorAttribute : Attribute, IOperationBehavior
    {
        public void AddBindingParameters(OperationDescription operationDescription, System.ServiceModel.Channels.BindingParameterCollection bindingParameters)
        {

        }


        public void ApplyClientBehavior(OperationDescription operationDescription, System.ServiceModel.Dispatcher.ClientOperation clientOperation)
        {
            // If this is applied on the client, well, it just doesn’t make sense.
            // Don’t throw in case this attribute was applied on the contract
            // instead of the implementation.
        }

        public void ApplyDispatchBehavior(OperationDescription operationDescription, System.ServiceModel.Dispatcher.DispatchOperation dispatchOperation)
        {
            // Change the IOperationInvoker for this operation.
            dispatchOperation.Invoker = new STAOperationInvoker(dispatchOperation.Invoker);
        }

        public void Validate(OperationDescription operationDescription)
        {
            if (operationDescription.SyncMethod == null)
            {
                throw new InvalidOperationException("The STAOperationBehaviorAttribute only works for synchronous method invocations.");
            }
        }

    }
}