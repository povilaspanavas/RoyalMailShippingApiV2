using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;
using System.Text;
using System.Threading.Tasks;

namespace RoyalMailApi.Engine.Helpers
{
    public class RoyalMailMessageFormatter : IClientMessageFormatter
    {
        private readonly IClientMessageFormatter formatter;

        public RoyalMailMessageFormatter(IClientMessageFormatter formatter)
        {
            this.formatter = formatter;
        }

        public object DeserializeReply(Message message, object[] parameters)
        {
            return this.formatter.DeserializeReply(message, parameters);
        }

        public Message SerializeRequest(MessageVersion messageVersion, object[] parameters)
        {
            var message = this.formatter.SerializeRequest(messageVersion, parameters);
            return new RoyalMailMessage(message);
        }
    }
}
