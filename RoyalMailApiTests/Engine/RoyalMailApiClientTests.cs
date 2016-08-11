using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RoyalMailApi.Engine;
using RoyalMailApi.Model;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoyalMailApi.Engine.Tests
{
    [TestClass()]
    public class RoyalMailApiClientTests
    {
        private readonly static ILog _log = LogManager.GetLogger(typeof(RoyalMailApiClientTests));

        private static RoyalMailApiCredentials _credentials = null;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            _credentials = new RoyalMailApiCredentials();

            _credentials.HttpSecurity.ClientId = ConfigurationManager.AppSettings["RoyalMailApiClientId"];
            _credentials.HttpSecurity.ClientSecret = ConfigurationManager.AppSettings["RoyalMailApiClientSecret"];

            _credentials.SoapSecurity.Username = ConfigurationManager.AppSettings["RoyalMailApiUsername"];
            _credentials.SoapSecurity.Password = ConfigurationManager.AppSettings["RoyalMailApiPassword"];
            _credentials.SoapSecurity.ApplicationId = ConfigurationManager.AppSettings["RoyalMailApiApplicationId"];

        }

        [TestMethod()]
        public void SendCreateShipmentTest_LocalAddress()
        {
            try
            {
                var client = new RoyalMailApiClient(_credentials);

                var label = new PPLabel
                {
                    OrderId = Guid.NewGuid().ToString(),
                    Printer = "Zebra",
                    IntegrationCode1 = "D", // <!-- RoyalMail Api serviceType -->
                    IntegrationCode2 = "SD1", // <!-- RoyalMail Api serviceOffering -->
                    IntegrationCode3 = "P", // <!-- RoyalMail Api serviceFormat -->
                    IntegrationCode4 = false, // <!-- RoyalMail Api signature -->
                    ShippingDate = DateTime.Now.ToString("yyyy-MM-dd"),
                    OrderReference = "1028172222", // <!-- RoyalMail Api senderReference -->

                    Address = new PPLabelAddress
                    {
                        ContactName = "Name Surname", // <!-- RoyalMail Api complementaryName -->
                        AddLine1 = "10 Cherry Tree Grove",
                        AddLine2 = "Derbyshire",
                        Town = "Chesterfield",
                        County = "",
                        Postcode = "S42 5QT",
                        CountryCodeIso2 = "GB",
                        Email = "test@test.co.uk",
                        Phone = "07512222222",
                    },

                    Items = new PPLabelItem[]
                    {
                        new PPLabelItem { Weight = 111 }
                    }
                };
                client.CreateShipment(label);

            }
            catch (Exception ex)
            {
                _log.Error("Test has failed", ex);
                throw;
            }
        }

        [TestMethod()]
        public void SendCreateShipmentTest_GermanyAddress()
        {
            try
            {
                var client = new RoyalMailApiClient(_credentials);

                var label = new PPLabel
                {
                    OrderId = Guid.NewGuid().ToString(),
                    Printer = "Zebra",
                    IntegrationCode1 = "I", // <!-- RoyalMail Api serviceType -->
                    IntegrationCode2 = "MP1", // <!-- RoyalMail Api serviceOffering -->
                    IntegrationCode3 = "E", // <!-- RoyalMail Api serviceFormat -->
                    IntegrationCode4 = false, // <!-- RoyalMail Api signature -->
                    ShippingDate = DateTime.Now.ToString("yyyy-MM-dd"),
                    OrderReference = "1028172222", // <!-- RoyalMail Api senderReference -->

                    Address = new PPLabelAddress
                    {
                        ContactName = "Name Surname", // <!-- RoyalMail Api complementaryName -->
                        AddLine1 = "Nauwieserstrasse 22",
                        Town = "SAARBRUECKEN",
                        County = "",
                        Postcode = "66111",
                        CountryCodeIso2 = "DE",
                        Email = "test@gmail.com",
                        Phone = "07512222222",
                    },

                    Items = new PPLabelItem[]
                    {
                        new PPLabelItem { Weight = 111, Price = 100 },
                    }
                };
                client.CreateShipment(label);

            }
            catch (Exception ex)
            {
                _log.Error("Test has failed", ex);
                throw;
            }
        }

        [TestMethod()]
        public void SendCreateShipmentTest_GermanyAddress_TwoShipmentLabels()
        {
            try
            {
                var client = new RoyalMailApiClient(_credentials);

                var label = new PPLabel
                {
                    OrderId = Guid.NewGuid().ToString(),
                    Printer = "Zebra",
                    IntegrationCode1 = "I", // <!-- RoyalMail Api serviceType -->
                    IntegrationCode2 = "MP1", // <!-- RoyalMail Api serviceOffering -->
                    IntegrationCode3 = "E", // <!-- RoyalMail Api serviceFormat -->
                    IntegrationCode4 = false, // <!-- RoyalMail Api signature -->
                    ShippingDate = DateTime.Now.ToString("yyyy-MM-dd"),
                    OrderReference = "1028172222", // <!-- RoyalMail Api senderReference -->

                    Address = new PPLabelAddress
                    {
                        ContactName = "Name Surname", // <!-- RoyalMail Api complementaryName -->
                        AddLine1 = "Nauwieserstrasse 17",
                        Town = "SAARBRUECKEN",
                        County = "",
                        Postcode = "66111",
                        CountryCodeIso2 = "DE",
                        Email = "test@gmail.com",
                        Phone = "07512222222",
                    },

                    Items = new PPLabelItem[]
                    {
                        new PPLabelItem { Weight = 111, Price = 100 },
                        new PPLabelItem { Weight = 111, Price = 100 },
                    }
                };
                client.CreateShipment(label);

            }
            catch (Exception ex)
            {
                _log.Error("Test has failed", ex);
                throw;
            }
        }


        /// <summary>
        /// Get shipment number from the above tests
        /// </summary>
        [TestMethod()]
        public void PrintLabelTest()
        {
            const string shipmentNumber = "TTT004661225GB";

            try
            {
                var client = new RoyalMailApiClient(_credentials);
                var labelPdf = client.PrintLabel(shipmentNumber);

                var pdfSavePath = @"c:\test\royalmailapilablenew.pdf";
                File.WriteAllBytes(pdfSavePath, labelPdf);
                Process.Start(pdfSavePath);
            }
            catch (Exception ex)
            {
                _log.Error("Test has failed", ex);
                throw;
            }
        }

    }
}