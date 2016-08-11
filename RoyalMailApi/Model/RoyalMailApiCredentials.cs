using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

namespace RoyalMailApi.Model
{
    /// <summary>
    /// RoyalMail Api v2 uses double authentication. One in the xml message itself, other
    /// in the http post request header
    /// </summary>
    public class RoyalMailApiCredentials
    {
        private HttpSecurityHeader _httpSecurityHeader = new HttpSecurityHeader();
        private SoapSecurityHeader _securityHeaderCredentials = new SoapSecurityHeader();

        public HttpSecurityHeader HttpSecurity
        {
            get { return _httpSecurityHeader; }
            set { _httpSecurityHeader = value; }
        }

        public SoapSecurityHeader SoapSecurity
        {
            get { return _securityHeaderCredentials; }
            set { _securityHeaderCredentials = value; }
        }

        /// <summary>
        /// These comes in the email from Royal Mail after you register for API
        /// </summary>
        public class SoapSecurityHeader
        {

            private string _userName;

            /// <summary>
            /// Will look something like youraddress@yourcompany.co.ukAPI
            /// </summary>
            public string Username
            {
                get { return _userName; }
                set { _userName = value; }
            }


            private string _password;

            /// <summary>
            /// Length will be ~13 characters and readable text plus numbers 
            /// </summary>public string Password
            public string Password
            {
                get { return _password; }
                set { _password = value; }

            }

            private string _applicationId;

            /// <summary>
            /// Usually the same for everyone RMG-API-G-01 as long as you have a single application registered in their portal
            /// </summary>public string ApplicationId
            public string ApplicationId
            {
                get { return _applicationId; }
                set { _applicationId = value; }
            }
        }

        /// <summary>
        /// These are acquired from https://developer.royalmail.net
        /// </summary>
        public class HttpSecurityHeader
        {
            private string _clientId;
            /// <summary>
            /// In http header it's X-IBM-Client-Id
            /// </summary>
            public string ClientId
            {
                get { return _clientId; }
                set { _clientId = value; }
            }

            private string _clientSecret;
            /// <summary>
            /// In http header it's X-IBM-Client-Secret
            /// </summary>
            public string ClientSecret
            {
                get { return _clientSecret; }
                set { _clientSecret = value; }
            }
        }
    }
}
