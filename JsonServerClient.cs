using log4net;
using System.Net.Http.Headers;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

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
            string serializedJSON = JsonSerializer.Serialize(dataToSerialize, 
                new JsonSerializerOptions { IncludeFields = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });

            try
            {
                Task.Run(() => PostJSONDataToServer(serializedJSON));
            }

            catch (Exception _Exception)
            {
                log.Error(String.Format("Exception caught in process: {0}", _Exception.ToString()), _Exception);
            }
        }

        private async Task PostJSONDataToServer(string postData)
        {
            using (HttpClient client = new(new LoggingHandler(new HttpClientHandler())))
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

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    string responseContent = await response.Content.ReadAsStringAsync();
                    log.Error($"Sending error to JSON Server. HTTP code: ${response.StatusCode}, response:\n ${responseContent}");
                }
            }
        }
    }

    public class LoggingHandler : DelegatingHandler
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public LoggingHandler(HttpMessageHandler innerHandler)
            : base(innerHandler)
        {
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            HttpResponseMessage response = await base.SendAsync(request, cancellationToken);

            if (log.IsDebugEnabled)
            {
                StringBuilder protocolDump = new();
                protocolDump.Append("Request:\n").Append(request.ToString());
                if (request.Content != null)
                {
                    protocolDump.Append(await request.Content.ReadAsStringAsync());
                }
                protocolDump.Append("\nResponse:\n").Append(response.ToString());
                if (response.Content != null)
                {
                    protocolDump.Append(await response.Content.ReadAsStringAsync(cancellationToken));
                }
                protocolDump.Append('\n');
                log.Debug(protocolDump.ToString());
            }

            return response;
        }
    }
}
