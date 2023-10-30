using log4net;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Options;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet;
using System.Text.Json;

namespace VSCaptureWave
{
    public class MQTTClient
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly string m_MQTTclientId = Guid.NewGuid().ToString();

        public string MQTTUrl;
        public string MQTTtopic;
        public string MQTTuser;
        public string MQTTpassw;

        public MQTTClient(string mQTTUrl, string mQTTtopic, string mQTTuser, string mQTTpassw)
        {
            MQTTUrl = mQTTUrl;
            MQTTtopic = mQTTtopic;
            MQTTuser = mQTTuser;
            MQTTpassw = mQTTpassw;
        }

        public void ExportNumValListToMQTT(ReceivedDataBlock dataBlock)
        {
            string serializedJSON = JsonSerializer.Serialize(dataBlock, new JsonSerializerOptions { IncludeFields = true });

            CancellationTokenSource source = new();
            CancellationToken token = source.Token;

            var mqttClient = new MqttFactory().CreateMqttClient();
            var logger = new MqttFactory().DefaultLogger;
            //var managedClient = new ManagedMqttClient(mqttClient, IMqttNetLogger);
            var managedClient = new ManagedMqttClient(mqttClient, logger);

            try
            {
                var task = Task.Run(async () =>
                {
                    var connected = GetConnectedTask(managedClient);
                    await ConnectMQTTAsync(managedClient, token, MQTTUrl, m_MQTTclientId, MQTTuser, MQTTpassw);
                    await connected;

                });

                task.ContinueWith(antecedent =>
                {
                    if (antecedent.Status == TaskStatus.RanToCompletion)
                    {
                        Task.Run(async () =>
                        {
                            await PublishMQTTAsync(managedClient, token, MQTTtopic, serializedJSON);
                            await managedClient.StopAsync();
                        });
                    }
                });

                //ConnectMQTTAsync(m_mqttClient, token, m_MQTTUrl, m_MQTTclientId, m_MQTTuser, m_MQTTpassw).Wait();
                //m_MQTTtopic = String.Format("/VSCapture/{0}/numericdata/", m_DeviceID);
                //PublishMQTTAsync(m_mqttClient, token, m_MQTTtopic, serializedJSON).Wait();
            }

            catch (Exception _Exception)
            {
                // Error. 
                log.Error(String.Format("Exception caught in process: {0}", _Exception.ToString()), _Exception);
            }

        }

        private static async Task ConnectMQTTAsync(ManagedMqttClient mqttClient, CancellationToken token, string mqtturl, string clientId, string mqttuser, string mqttpassw)
        {
            bool mqttSecure = true;

            var messageBuilder = new MqttClientOptionsBuilder()
            .WithClientId(clientId)
            .WithCredentials(mqttuser, mqttpassw)
            .WithCommunicationTimeout(new TimeSpan(0, 0, 10))
            .WithWebSocketServer(mqtturl)
            .WithCleanSession();

            var options = mqttSecure
            ? messageBuilder
                .WithTls()
                .Build()
            : messageBuilder
                .Build();

            var managedOptions = new ManagedMqttClientOptionsBuilder()
              .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
              .WithClientOptions(options)
              .Build();

            await mqttClient.StartAsync(managedOptions);

        }

        private static async Task PublishMQTTAsync(ManagedMqttClient mqttClient, CancellationToken token, string topic, string payload, bool retainFlag = true, int qos = 1)
        {
            if (mqttClient.IsConnected)
            {
                await mqttClient.PublishAsync(new MqttApplicationMessageBuilder()
               .WithTopic(topic)
               .WithPayload(payload)
               .WithQualityOfServiceLevel((MQTTnet.Protocol.MqttQualityOfServiceLevel)qos)
               .WithRetainFlag(retainFlag)
               .Build(), token);
            }

        }

        private Task GetConnectedTask(ManagedMqttClient managedClient)
        {
            TaskCompletionSource<bool> connected = new TaskCompletionSource<bool>();
            managedClient.ConnectedHandler = new MqttClientConnectedHandlerDelegate(e =>
            {
                managedClient.ConnectedHandler = null;
                connected.SetResult(true);
            });
            return connected.Task;
        }
    }
}
