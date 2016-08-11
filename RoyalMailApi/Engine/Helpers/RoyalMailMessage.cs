using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace RoyalMailApi.Engine.Helpers
{
    class RoyalMailMessage : Message
    {
        private readonly Message message;

        public RoyalMailMessage(Message message)
        {
            this.message = message;
        }
        public override MessageHeaders Headers
        {
            get
            {
                return this.message.Headers;
            }
        }
        public override MessageProperties Properties
        {
            get
            {
                return this.message.Properties;
            }
        }
        public override MessageVersion Version
        {
            get
            {
                return this.message.Version;
            }
        }
        protected override void OnWriteStartBody(XmlDictionaryWriter writer)
        {
            writer.WriteStartElement("Body", "http://schemas.xmlsoap.org/soap/envelope/");
        }
        protected override void OnWriteBodyContents(XmlDictionaryWriter writer)
        {
            this.message.WriteBodyContents(writer);
        }
        protected override void OnWriteStartEnvelope(XmlDictionaryWriter writer)
        {
            writer.WriteStartElement("s", "Envelope", "http://schemas.xmlsoap.org/soap/envelope/");
            writer.WriteAttributeString("xmlns", "v2", null, "http://www.royalmailgroup.com/api/ship/V2");
            writer.WriteAttributeString("xmlns", "v1", null, "http://www.royalmailgroup.com/integration/core/V1");
            writer.WriteAttributeString("xmlns", "xsi", null, "http://www.w3.org/2001/XMLSchema-instance");
            writer.WriteAttributeString("xmlns", "xsd", null, "http://www.w3.org/2001/XMLSchema");
            writer.WriteAttributeString("xmlns", "xsd", null, "http://www.w3.org/2001/XMLSchema");
        }
    }
}
