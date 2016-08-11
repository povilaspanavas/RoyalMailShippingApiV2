using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.Text;
using System.Threading.Tasks;

namespace RoyalMailApi.Engine.Helpers
{
    class RoyalMailIEndpointBehavior : IOperationBehavior
    {

        public RoyalMailIEndpointBehavior() { }

        public void ApplyClientBehavior(OperationDescription description, ClientOperation proxy)
        {
            IClientMessageFormatter currentFormatter = proxy.Formatter;
            proxy.Formatter = new RoyalMailMessageFormatter(currentFormatter);
        }

        public void AddBindingParameters(OperationDescription operationDescription, BindingParameterCollection bindingParameters)
        {

        }

        public void ApplyDispatchBehavior(OperationDescription operationDescription, DispatchOperation dispatchOperation)
        {

        }

        public void Validate(OperationDescription operationDescription)
        {

        }

    }
}
