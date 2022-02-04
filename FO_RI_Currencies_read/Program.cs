using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Dynamics.AX.Framework.Tools.DataManagement.Serialization;
using Newtonsoft.Json;

namespace FO_RI_Currencies_read
{

    public class dequeueResponse
    {
        public string CorrelationId { get; set; }
        public string PopReceipt { get; set; }
        public string DownloadLocation { get; set; }
        public bool IsDownLoadFileExist { get; set; }
        public object FileDownLoadErrorMessage { get; set; }
        public object LastDequeueDateTime { get; set; }
    }

    class Program
    {
        #region helper methods
       

        /// <summary>
        /// dequeue request to export data
        /// </summary>      
        /// <returns>HttpResponseMessage response of request to dequeue</returns>
        public static HttpResponseMessage dequeueImport()
        {            
            //instantiate http client helper
            var httpClientHelper = new HttpClientHelper();
            Uri enqueueUri = httpClientHelper.GetDequeueUri();
            // Post Enqueue request
            var response = httpClientHelper.GetRequestAsync(enqueueUri).Result;
            return response;
        }
        /// <summary>
        /// download file
        /// </summary>      
        /// <returns>HttpResponseMessage response of request to dequeue</returns>
        public static HttpResponseMessage DownloadFile(string url)
        {

            //instantiate http client helper
            var httpClientHelper = new HttpClientHelper();           
            Uri downloadUri = new Uri(url, UriKind.Absolute);
            var response = httpClientHelper.GetRequestAsync(downloadUri).Result;
            return response;
        }
        /// <summary>
        /// ack post to mark as ack
        /// </summary>      
        /// <returns>HttpResponseMessage response </returns>
        public static HttpResponseMessage AckPost(string responseContent)
        {

            var httpClientHelper = new HttpClientHelper();
            var responseack = httpClientHelper.SendPostRequestAsync(responseContent).Result;
            return responseack;
        }
       
        #endregion
        static void Main(string[] args)
        {
            #region add initial info
            Console.WriteLine("********************************************************************************");
            Console.WriteLine("           RECURRING INTEGRATION POC CURRENCIES ENTITY READ                      ");
            Console.WriteLine("********************************************************************************");
            Console.WriteLine("");

            Console.WriteLine("1. Call REST API GET api/connector/dequeue/");
            Console.WriteLine("********************************************************************************");
            Console.WriteLine("");
            #endregion
            var response = dequeueImport();
            //check response
            if (response.IsSuccessStatusCode)
            {
                var responseContent = response.Content.ReadAsStringAsync().Result;
                dequeueResponse resObj = JsonConvert.DeserializeObject<dequeueResponse>(responseContent);
                #region show response dequeue
                Console.WriteLine(string.Format("Response code   : {0}", response.StatusCode));
                Console.WriteLine(string.Format("Correlation ID  : {0}", resObj.CorrelationId));
                Console.WriteLine(string.Format("DownloadLocation: {0}", resObj.DownloadLocation));
                Console.WriteLine("");
                #endregion
                //download file
                Console.WriteLine("2. Call DownloadLocation to get the file dequeued");
                Console.WriteLine("********************************************************************************");
                Console.WriteLine("");

                var responsefile = DownloadFile(resObj.DownloadLocation);
                if (responsefile.IsSuccessStatusCode)
                {
                    Console.WriteLine(string.Format("Response code   : {0}", responsefile.StatusCode));
                    Stream receiveStream = responsefile.Content.ReadAsStreamAsync().Result;
                    string filename = string.Format(@"C:\temp\download\{0}.zip", resObj.CorrelationId);
                    using (Stream s = File.Create(filename))
                    {
                        receiveStream.CopyTo(s);
                    }                    
                    if (File.Exists(filename))
                    {
                        #region acknowledgement
                        Console.WriteLine(string.Format("File downloaded OK  : {0}", filename));
                        //ack
                        Console.WriteLine("");
                        Console.WriteLine("3. Call REST API POST api/connector/ack/ for acknowledgement");
                        Console.WriteLine("********************************************************************************");
                        Console.WriteLine("");
                        var responseack = AckPost(responseContent);
                        if (responseack.IsSuccessStatusCode)
                        {
                            Console.WriteLine(string.Format("Response code   : {0}", responsefile.StatusCode));
                            Console.WriteLine(string.Format("Status          : Acked"));
                        }
                        #endregion
                    }
                    else
                    {
                        Console.WriteLine(string.Format("File could not be downloaded  : {0}", filename));
                    }                    
                }
            }       
            else
            {
                Console.WriteLine("dequeue failed ");
                Console.WriteLine("Failure response:  Status: " + response.StatusCode + ", Reason: " + response.ReasonPhrase);
            }
            Console.ReadKey();
        }
    }
}
