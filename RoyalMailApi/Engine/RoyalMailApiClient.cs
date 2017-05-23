using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Xml;
using System.Security.Cryptography;
using System.ServiceModel.Channels;
using System.Text.RegularExpressions;
using System.Globalization;
using RoyalMailApi.RoyalMailApiWsdl;
using RoyalMailApi.Engine.Helpers;
using RoyalMailApi.Model;
using log4net;
using System.IO.Compression;

namespace RoyalMailApi.Engine
{
    /// <summary>
    /// *
    /// *
    /// * SOAP Service & Methods
    /// *
    /// *
    /// * Most of it taken from http://stackoverflow.com/questions/34508811/consume-wcf-royal-mail-api-in-c-sharp-console-application
    /// </summary>
    public class RoyalMailApiClient
    {
        private readonly static ILog _log = LogManager.GetLogger(typeof(RoyalMailApiClient));

        public const decimal VERSION = 2;
        public const string SERVICE_OCCURANCE = "1"; // Service Occurence (Identifies Agreement on Customers Account) Default to 1. Not Required If There Is There Is Only 1 On Account
        public const string SHIPMENT_TYPE = "Delivery"; // Can be delivery or Return. We will never use return, hence hardcoded value

        private readonly RoyalMailApiCredentials _credentials;

        public RoyalMailApiClient(RoyalMailApiCredentials credentials)
        {
            if (credentials == null)
                throw new NullReferenceException("Credentials must be not null");

            _credentials = credentials;
        }


        private shippingAPIPortTypeClient GetProxy()
        {
            // binding comes from configuration file
            var shippingClient = new shippingAPIPortTypeClient();

            // TODOP is it even needed at this point?
            shippingClient.ClientCredentials.UserName.UserName = _credentials.SoapSecurity.Username;
            shippingClient.ClientCredentials.UserName.Password = _credentials.SoapSecurity.Password;

            shippingClient.ClientCredentials.UseIdentityConfiguration = true;

            foreach (OperationDescription od in shippingClient.Endpoint.Contract.Operations)
            {
                od.Behaviors.Add(new RoyalMailIEndpointBehavior());
            }

            return shippingClient;
        }


        private SecurityHeaderType GetSecurityHeaderType()
        {
            SecurityHeaderType securityHeader = new SecurityHeaderType();

            DateTime created = DateTime.Now;

            string creationDate;
            creationDate = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

            string nonce = nonce = (new Random().Next(0, int.MaxValue)).ToString();

            byte[] hashedPassword;
            hashedPassword = GetSHA1(_credentials.SoapSecurity.Password);

            // https://github.com/povilaspanavas/RoyalMailShippingApiV2/issues/1
            // after hippasus raised issue, I've changed the encoding (previously was using Default). Ran the tests and it worked
            // However, wouldn't work for ive server. So, the concatednatedDigestInput uses Default. But encodedNonce uses UK one.
            // Now it works on our live server, and in tests.

            string concatednatedDigestInput = string.Concat(nonce, creationDate, Encoding.Default.GetString(hashedPassword));
            //string concatednatedDigestInput = string.Concat(nonce, creationDate, encoding.GetString(hashedPassword));
            byte[] digest;
            digest = GetSHA1(concatednatedDigestInput);

            string passwordDigest;
            passwordDigest = Convert.ToBase64String(digest);

            var encoding = Encoding.GetEncoding("iso-8859-1");
            string encodedNonce = Convert.ToBase64String(encoding.GetBytes(nonce));

            XmlDocument doc = new XmlDocument();
            using (XmlWriter writer = doc.CreateNavigator().AppendChild())
            {
                writer.WriteStartDocument();
                writer.WriteStartElement("Security");
                writer.WriteStartElement("UsernameToken", "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd");
                writer.WriteElementString("Username", _credentials.SoapSecurity.Username);
                writer.WriteElementString("Password", passwordDigest);
                writer.WriteElementString("Nonce", encodedNonce);
                writer.WriteElementString("Created", creationDate);
                writer.WriteEndElement();
                writer.WriteEndElement();
                writer.WriteEndDocument();
                writer.Flush();
            }

            doc.DocumentElement.RemoveAllAttributes();

            System.Xml.XmlElement[] headers = doc.DocumentElement.ChildNodes.Cast<XmlElement>().ToArray<XmlElement>();

            securityHeader.Any = headers;

            return securityHeader;

        }

        private integrationHeader GetIntegrationHeader()
        {
            integrationHeader header = new integrationHeader();

            DateTime created = DateTime.Now;
            String createdAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

            header.dateTime = created;
            header.version = VERSION;
            header.dateTimeSpecified = true;
            header.versionSpecified = true;

            identificationStructure idStructure = new identificationStructure();
            idStructure.applicationId = _credentials.SoapSecurity.ApplicationId;

            string nonce = nonce = (new Random().Next(0, int.MaxValue)).ToString();

            idStructure.transactionId = CalculateMD5Hash(nonce + createdAt);

            header.identification = idStructure;

            return header;
        }

        private static byte[] GetSHA1(string input)
        {
            return SHA1Managed.Create().ComputeHash(Encoding.Default.GetBytes(input));
        }

        public string CalculateMD5Hash(string input)
        {
            // step 1, calculate MD5 hash from input
            MD5 md5 = System.Security.Cryptography.MD5.Create();
            byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
            byte[] hash = md5.ComputeHash(inputBytes);

            // step 2, convert byte array to hex string
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("X2"));
            }
            return sb.ToString();
        }



        /*
         * Check Response Footer For Errors & Warnings From Service
         * If Error Return True So We Can Inform Filemaker Of Error
         * Ignore Warnings For Now
         * 
         */
        private bool CheckErrorsAndWarnings(integrationFooter integrationFooter)
        {
            if (integrationFooter != null)
            {
                if (integrationFooter.errors != null && integrationFooter.errors.Length > 0)
                {
                    errorDetail[] errors = integrationFooter.errors;
                    for (int i = 0; i < errors.Length; i++)
                    {
                        errorDetail error = errors[i];
                        _log.Error("Royal Mail Request Error: " + error.errorDescription + ". " + error.errorResolution);
                    }
                    if (errors.Length > 0)
                    {
                        return true;
                    }
                }

                if (integrationFooter.warnings != null && integrationFooter.warnings.Length > 0)
                {
                    warningDetail[] warnings = integrationFooter.warnings;
                    for (int i = 0; i < warnings.Length; i++)
                    {
                        warningDetail warning = warnings[i];
                        _log.Warn("Royal Mail Request Warning: " + warning.warningDescription + ". " + warning.warningResolution);
                    }
                }
            }

            return false;

        }

        /*
         * Show Message Box With SOAP Error If We Receive A Fault Code Back From Service
         *
         */
        private void LogSoapException(FaultException e)
        {
            MessageFault message = e.CreateMessageFault();
            XmlElement errorDetail = message.GetDetail<XmlElement>();
            XmlNodeList errorDetails = errorDetail.ChildNodes;
            String fullErrorDetails = "";

            for (int i = 0; i < errorDetails.Count; i++)
            {
                fullErrorDetails += errorDetails.Item(i).Name + ": " + errorDetails.Item(i).InnerText + "\n";
            }

            _log.Error("An Error Occured With Royal Mail Api Service: " + message.Reason.ToString() + "\n\n" + fullErrorDetails);
        }

        /// <summary>
        /// Returns decoded pdf file
        /// </summary>
        /// <param name="shipmentNumber"></param>
        /// <param name="printerName"></param>
        /// <returns></returns>
        public byte[] PrintLabel(string shipmentNumber)
        {
            shippingAPIPortTypeClient client = GetProxy();
            var request = new printLabelRequest();
            request.integrationHeader = GetIntegrationHeader();
            request.shipmentNumber = shipmentNumber;
            request.outputFormat = "PDF";

            // http://stackoverflow.com/questions/897782/how-to-add-custom-http-header-for-c-sharp-web-service-client-consuming-axis-1-4
            //XmlHelper.Serialise<createShipmentRequest>(request, @"C:\test\createShipmentRequest.xml");
            using (OperationContextScope scope = new OperationContextScope(client.InnerChannel))
            {
                var httpRequestProperty = new HttpRequestMessageProperty();
                httpRequestProperty.Headers.Add(@"X-IBM-Client-Id", _credentials.HttpSecurity.ClientId);
                httpRequestProperty.Headers.Add(@"X-IBM-Client-Secret", _credentials.HttpSecurity.ClientSecret);
                OperationContext.Current.OutgoingMessageProperties[HttpRequestMessageProperty.Name] = httpRequestProperty;

                printLabelResponse response = client.printLabel(GetSecurityHeaderType(), request);

                CheckErrorsAndWarnings(response.integrationFooter);
                var labelBytes = response.label;
                return labelBytes;
            }
        }

        static byte[] Decompress(byte[] gzip)
        {
            // Create a GZIP stream with decompression mode.
            // ... Then create a buffer and write into while reading from the GZIP stream.
            using (GZipStream stream = new GZipStream(new MemoryStream(gzip), CompressionMode.Decompress))
            {
                const int size = 4096;
                byte[] buffer = new byte[size];
                using (MemoryStream memory = new MemoryStream())
                {
                    int count = 0;
                    do
                    {
                        count = stream.Read(buffer, 0, size);
                        if (count > 0)
                        {
                            memory.Write(buffer, 0, count);
                        }
                    }
                    while (count > 0);
                    return memory.ToArray();
                }
            }
        }

        public static string Base64Decode(string base64EncodedData)
        {
            var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
            return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
        }

        public createShipmentResponse CreateShipment(PPLabel labelData)
        {
            shippingAPIPortTypeClient client = GetProxy();

            try
            {
                createShipmentRequest request = new createShipmentRequest();
                request.integrationHeader = GetIntegrationHeader();

                requestedShipment shipment = new requestedShipment();

                // Shipment Type Code (Delivery or Return)
                shipment.shipmentType = new referenceDataType() { code = SHIPMENT_TYPE }; ;

                // Service Occurence (Identifies Agreement on Customers Account) Default to 1. Not Required If There Is There Is Only 1 On Account
                shipment.serviceOccurrence = SERVICE_OCCURANCE;

                // Service Type Code (1:24H 1st Class, 2: 48H 2nd Class, D: Special Delivery Guaranteed, H: HM Forces (BFPO), I: International, R: Tracked Returns, T: Tracked Domestic)
                referenceDataType serviceType = new referenceDataType();
                serviceType.code = labelData.IntegrationCode1;
                shipment.serviceType = serviceType;

                // Service Offering (See Royal Mail Service Offering Type Codes. Too Many To List)
                serviceOfferingType serviceOfferingTypeContainer = new serviceOfferingType();
                referenceDataType serviceOffering = new referenceDataType() { code = labelData.IntegrationCode2 };
                serviceOfferingTypeContainer.serviceOfferingCode = serviceOffering;
                shipment.serviceOffering = serviceOfferingTypeContainer;

                // Service Format Code
                serviceFormatType serviceFormatTypeContainer = new serviceFormatType();
                referenceDataType serviceFormat = new referenceDataType();
                serviceFormat.code = labelData.IntegrationCode3;
                serviceFormatTypeContainer.serviceFormatCode = serviceFormat;
                shipment.serviceFormat = serviceFormatTypeContainer;

                // Shipping Date
                shipment.shippingDate = DateTime.ParseExact(labelData.ShippingDate, "yyyy-MM-dd", CultureInfo.InvariantCulture); ;
                shipment.shippingDateSpecified = true;

                //Signature Required (Only Available On Tracked Services)
                shipment.signature = labelData.IntegrationCode4;
                shipment.signatureSpecified = true;

                // Leave In Safe Place (Available On Tracked Non Signature Service Offerings)
                shipment.safePlace = labelData.Address.SafePlace;

                // Sender Reference Number (e.g. Invoice Number or RA Number)
                shipment.senderReference = labelData.OrderReference;

                /*
                 * Service Enhancements
                */

                //List<serviceEnhancementType> serviceEnhancements = new List<serviceEnhancementType>();

                //List<dataObjects.ServiceEnhancement> selectedEnhancements = shippingForm.GetServiceEnhancements();

                //for (int i = 0; i < selectedEnhancements.Count; i++)
                //{
                //    serviceEnhancementType enhancement = new serviceEnhancementType();
                //    referenceDataType enhancementCode = new referenceDataType();
                //    enhancementCode.code = selectedEnhancements.ElementAt(i).GetEnhancementType().ToString();
                //    enhancement.serviceEnhancementCode = enhancementCode;
                //    serviceEnhancements.Add(enhancement);
                //}

                //shipment.serviceEnhancements = serviceEnhancements.ToArray();


                /*
                 * Recipient Contact Details
                */

                contact recipientContact = new contact()
                {
                    name = labelData.Address.ContactName,
                    complementaryName = labelData.Address.CompanyName
                };

                if (string.IsNullOrWhiteSpace(labelData.Address.Email) == false)
                {
                    digitalAddress email = new digitalAddress();
                    email.electronicAddress = labelData.Address.Email;
                    recipientContact.electronicAddress = email;
                }

                if (string.IsNullOrWhiteSpace(labelData.Address.Phone) == false)
                {
                    telephoneNumber tel = new telephoneNumber();

                    Regex phoneRegex = new Regex(@"[^\d]");
                    tel.telephoneNumber1 = phoneRegex.Replace(labelData.Address.Phone, "");
                    // TODOP unhardcode dialing code
                    //tel.countryCode = "00" + shippingForm.GetCountry().GetDialingCode();
                    tel.countryCode = "00" + "44";
                    recipientContact.telephoneNumber = tel;
                }

                shipment.recipientContact = recipientContact;

                /*
                 * Recipient Address
                 * 
                */
                address recipientAddress = new address();
                recipientAddress.addressLine1 = labelData.Address.AddLine1;
                recipientAddress.addressLine2 = labelData.Address.AddLine2;
                //recipientAddress.addressLine3 = shippingForm.GetAddressLine3();
                //recipientAddress.addressLine4 = shippingForm.GetCounty();
                recipientAddress.postTown = labelData.Address.Town;
                countryType country = new countryType();
                referenceDataType countryCode = new referenceDataType();
                countryCode.code = labelData.Address.CountryCodeIso2;
                country.countryCode = countryCode;
                recipientAddress.country = country;
                recipientAddress.postcode = labelData.Address.Postcode;

                //recipientAddress.stateOrProvince = new stateOrProvinceType();
                //recipientAddress.stateOrProvince.stateOrProvinceCode = new referenceDataType();

                shipment.recipientAddress = recipientAddress;

                // Shipment Items

                if ("I".Equals(labelData.IntegrationCode1)) // International shipment
                {
                    internationalInfo InternationalInfo = new internationalInfo();
                    InternationalInfo.shipperExporterVatNo = "GB945777273";
                    InternationalInfo.documentsOnly = false;
                    InternationalInfo.shipmentDescription = "Invoice Number: " + labelData.OrderReference;
                    InternationalInfo.invoiceDate = DateTime.Now;
                    //InternationalInfo.termsOfDelivery = "EXW"; // TODOP don't know what it is
                    InternationalInfo.invoiceDateSpecified = true;
                    InternationalInfo.purchaseOrderRef = labelData.OrderReference;

                    List<parcel> parcels = new List<parcel>();
                    foreach (var i in labelData.Items)
                    {
                        parcel Parcel = new parcel();
                        Parcel.weight = new dimension();
                        Parcel.weight.value = (float)(i.Weight);
                        Parcel.weight.unitOfMeasure = new unitOfMeasureType();
                        Parcel.weight.unitOfMeasure.unitOfMeasureCode = new referenceDataType();
                        Parcel.weight.unitOfMeasure.unitOfMeasureCode.code = "g";

                        Parcel.invoiceNumber = labelData.OrderReference;
                        Parcel.purposeOfShipment = new referenceDataType();
                        Parcel.purposeOfShipment.code = "31";

                        List<contentDetail> Contents = new List<contentDetail>();

                        // Harcoded all these values according to my circumstances
                        contentDetail ContentDetail = new contentDetail();
                        ContentDetail.articleReference = "O";
                        ContentDetail.countryOfManufacture = new countryType();
                        ContentDetail.countryOfManufacture.countryCode = new referenceDataType();
                        ContentDetail.countryOfManufacture.countryCode.code = "GB";

                        ContentDetail.currencyCode = new referenceDataType();
                        ContentDetail.currencyCode.code = "GBP";
                        ContentDetail.description = "Printed goods";
                        ContentDetail.unitQuantity = "1";
                        ContentDetail.unitValue = Convert.ToDecimal(i.Price / 100.00m);
                        ContentDetail.unitWeight = new dimension();
                        ContentDetail.unitWeight.value = Convert.ToSingle(i.Weight);
                        ContentDetail.unitWeight.unitOfMeasure = new unitOfMeasureType();
                        ContentDetail.unitWeight.unitOfMeasure.unitOfMeasureCode = new referenceDataType();
                        ContentDetail.unitWeight.unitOfMeasure.unitOfMeasureCode.code = "g";


                        Contents.Add(ContentDetail);

                        Parcel.contentDetails = Contents.ToArray();

                        parcels.Add(Parcel);

                    }

                    InternationalInfo.parcels = parcels.ToArray();

                    shipment.internationalInfo = InternationalInfo;
                }
                else
                {
                    List<RoyalMailApiWsdl.item> items = new List<RoyalMailApiWsdl.item>();

                    foreach (var i in labelData.Items)
                    {
                        RoyalMailApiWsdl.item item = new RoyalMailApiWsdl.item();
                        item.numberOfItems = "1";
                        item.weight = new dimension();
                        item.weight.value = (float)(i.Weight);

                        item.weight.unitOfMeasure = new unitOfMeasureType();
                        item.weight.unitOfMeasure.unitOfMeasureCode = new referenceDataType();
                        item.weight.unitOfMeasure.unitOfMeasureCode.code = "g";

                        items.Add(item);
                    }
                    shipment.items = items.ToArray();
                }

                request.requestedShipment = shipment;
                // http://stackoverflow.com/questions/897782/how-to-add-custom-http-header-for-c-sharp-web-service-client-consuming-axis-1-4
                //XmlHelper.Serialise<createShipmentRequest>(request, @"C:\test\createShipmentRequest.xml");
                using (OperationContextScope scope = new OperationContextScope(client.InnerChannel))
                {
                    var httpRequestProperty = new HttpRequestMessageProperty();
                    httpRequestProperty.Headers.Add(@"X-IBM-Client-Id", _credentials.HttpSecurity.ClientId);
                    httpRequestProperty.Headers.Add(@"X-IBM-Client-Secret", _credentials.HttpSecurity.ClientSecret);
                    OperationContext.Current.OutgoingMessageProperties[HttpRequestMessageProperty.Name] = httpRequestProperty;

                    createShipmentResponse response = client.createShipment(GetSecurityHeaderType(), request);
                    CheckErrorsAndWarnings(response.integrationFooter);
                    //XmlHelper.Serialise(request, @"c:\test\createShipmentRequest.xml");
                    //XmlHelper.Serialise(response, @"c:\test\createShipmentResponse.xml");
                    return response;
                }
            }
            catch (TimeoutException e)
            {
                client.Abort();
                _log.Error("Request timed out", e);
            }
            catch (FaultException e)
            {
                client.Abort();
                LogSoapException(e);
            }
            catch (CommunicationException e)
            {
                client.Abort();
                _log.Error("A communication error has occured", e);
            }
            catch (Exception e)
            {
                client.Abort();
                _log.Error("Royal Mail Api error", e);
            }

            return null;
        }
    }
}
