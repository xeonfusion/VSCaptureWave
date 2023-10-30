using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace VSCaptureWave
{
    public class JsonServerClient
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public readonly string Uri;
        public readonly string User;
        public readonly string Password;
        public readonly bool KafkaProxyMode;

        public JsonServerClient(string uri, string user, string password, bool kafkaProxyMode)
        {
            Uri = uri;
            User = user;
            Password = password;
            KafkaProxyMode = kafkaProxyMode;
        }

        public void SendBlock(ReceivedDataBlock dataBlock)
        {
            object dataToSerialize = KafkaProxyMode ? new KafkaRestProxyMessage(dataBlock) : dataBlock;
            string serializedJSON = JsonSerializer.Serialize(dataToSerialize, new JsonSerializerOptions { IncludeFields = true });

            try
            {
                // Open file for reading. 
                //StreamWriter wrStream = new StreamWriter(pathjson, true, Encoding.UTF8);

                //wrStream.Write(serializedJSON);

                //wrStream.Close();

                Task.Run(() => PostJSONDataToServer(serializedJSON));

            }

            catch (Exception _Exception)
            {
                // Error. 
                log.Error(String.Format("Exception caught in process: {0}", _Exception.ToString()), _Exception);
            }
        }

        private async Task PostJSONDataToServer(string postData)
        {
            using (HttpClient client = new HttpClient())
            {
                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;
                client.BaseAddress = new Uri(Uri);

                var requestMessage = new HttpRequestMessage(HttpMethod.Post, (string)null);

                if (!String.IsNullOrEmpty(User) && !String.IsNullOrEmpty(Password))
                {
                    var authenticationString = $"{User}:{Password}";
                    var base64EncodedAuthenticationString = Convert.ToBase64String(Encoding.ASCII.GetBytes(authenticationString));
                    requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Basic", base64EncodedAuthenticationString);
                }

                var data = new StringContent(postData, Encoding.UTF8, KafkaProxyMode ? "application/vnd.kafka.json.v2+json" : "application /json");
                requestMessage.Content = data;

                var response = await client.SendAsync(requestMessage);
                response.EnsureSuccessStatusCode();

                string result = await response.Content.ReadAsStringAsync();

                log.Debug(result);
            }
        }
    }
}
