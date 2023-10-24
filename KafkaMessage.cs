using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static VSCaptureWave.DSerialPort;

namespace VSCaptureWave
{
    public class KafkaMessage
    {
        readonly List<KafkaRecord> records;

        public KafkaMessage(String key, List<NumericValResult> m_NumericValList)
        {
            records = new List<KafkaRecord>();
            foreach (NumericValResult numericValResult in m_NumericValList)
            {
                records.Add(new KafkaRecord(key, numericValResult));
            }
        }
    }

    public class KafkaRecord
    {
        readonly string key;
        readonly object value;

        public KafkaRecord(string key, object value)
        {
            this.key = key;
            this.value = value;
        }
    }
}
