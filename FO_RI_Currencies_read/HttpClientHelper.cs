using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace FO_RI_Currencies_read
{
    /// <summary>
    /// Helper class for dequeue/ ack http requests
    /// </summary>
    class HttpClientHelper
    {

        /// <summary>
        /// get AuthenticationHeaderValue based on client/secret
        /// </summary>
        /// <returns></returns>
        public static AuthenticationHeaderValue GetAuthHeaderValue()
        {
            UriBuilder uri = new UriBuilder(ConfigurationManager.AppSettings["AzureAuthEndpoint"]);
            uri.Path = ConfigurationManager.AppSettings["AadTenant"];

            AuthenticationContext authenticationContext = new AuthenticationContext(uri.ToString());
            var credential = new ClientCredential(ConfigurationManager.AppSettings["AzureAppID"], ConfigurationManager.AppSettings["AzureClientsecret"]);
            string uriFO = ConfigurationManager.AppSettings["FOUri"];
            AuthenticationResult authenticationResult = authenticationContext.AcquireTokenAsync(ConfigurationManager.AppSettings["FOUri"], credential).Result;

            string a = authenticationResult.CreateAuthorizationHeader();

            string[] split = a.Split(' ');
            string scheme = split[0];
            string parameter = split[1];
            AuthenticationHeaderValue ahv = new AuthenticationHeaderValue(scheme, parameter);
            return ahv;
        }
        /// <summary>
        /// Post request stream
        /// </summary>
        /// <returns></returns>
        public async Task<HttpResponseMessage> SendPostRequestAsync( string ackbody)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls |
                SecurityProtocolType.Tls11 |
                SecurityProtocolType.Tls12;

            using (HttpClientHandler handler = new HttpClientHandler() { UseCookies = false })
            {
                using (HttpClient httpClient = new HttpClient(handler))
                {
                    httpClient.DefaultRequestHeaders.Authorization = GetAuthHeaderValue();


                    var content = new StringContent(ackbody, Encoding.UTF8, "application/json");
                    
                    return await httpClient.PostAsync(this.GetAckUri(), content);
                    

                }
            }
            
        }
        /// <summary>        
        /// </summary>
        /// <param name="uri">Request URI</param>
        /// <returns>Task of type HttpResponseMessage</returns>
        public async Task<HttpResponseMessage> GetRequestAsync(Uri uri)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls |
                SecurityProtocolType.Tls11 |
                SecurityProtocolType.Tls12;

            HttpResponseMessage responseMessage;

            using (HttpClientHandler handler = new HttpClientHandler() { UseCookies = false })
            {
                using (HttpClient httpClient = new HttpClient(handler))
                {
                    httpClient.DefaultRequestHeaders.Authorization = GetAuthHeaderValue();

                    responseMessage = await httpClient.GetAsync(uri).ConfigureAwait(false);
                }
            }
            return responseMessage;
        }
        /// <summary>
        /// Get the dequeue URI
        /// </summary>
        /// <returns>dequeue URI</returns>
        public Uri GetDequeueUri()
        {
            //access the Connector API
            UriBuilder dequeueUri = new UriBuilder(ConfigurationManager.AppSettings["FOUri"]);
            dequeueUri.Path = "api/connector/dequeue/" + ConfigurationManager.AppSettings["RecurringJobId"];           
            return dequeueUri.Uri;
        }
        /// <summary>
        /// Get the ack URI
        /// </summary>
        /// <returns>ack URI</returns>
        public Uri GetAckUri()
        {
            //access the Connector API
            UriBuilder dequeueUri = new UriBuilder(ConfigurationManager.AppSettings["FOUri"]);
            dequeueUri.Path = "api/connector/ack/" + ConfigurationManager.AppSettings["RecurringJobId"];           
            return dequeueUri.Uri;
        }

    }
}
