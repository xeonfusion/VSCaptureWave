using System.Text.Json.Serialization;

namespace VSCaptureWave
{
    public class KafkaRestProxyMessage
    {
        [JsonPropertyName("records")]
        public readonly List<KafkaDataRecord> Records;

        public KafkaRestProxyMessage(ReceivedDataBlock dataBlock)
        {
            Records = new List<KafkaDataRecord>
            {
                new KafkaDataRecord(dataBlock.DeviceId, dataBlock)
            };
        }
    }

    public class KafkaDataRecord
    {
        [JsonPropertyName("key")]
        public readonly string Key;
        [JsonPropertyName("value")]
        public readonly ReceivedDataBlock Value;

        public KafkaDataRecord(string key, ReceivedDataBlock value)
        {
            this.Key = key;
            this.Value = value;
        }
    }
}
