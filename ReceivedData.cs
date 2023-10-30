using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace VSCaptureWave
{
    public class ReceivedDataBlock
    {
        [JsonPropertyName("id")]
        public readonly string Id;
        [JsonPropertyName("time")]
        public readonly DateTime Time;
        [JsonPropertyName("deviceId")]
        public readonly string DeviceId;
        [JsonPropertyName("type")]
        public readonly DataType Type;
        [JsonPropertyName("values")]
        public List<ReceivedDataValue> Values;
        [JsonPropertyName("curves")]
        public List<ReceivedDataValue> Curves;
        public ReceivedDataBlock(string deviceId, uint unixtime)
        {
            this.Id = deviceId + "-" + Guid.NewGuid();
            this.Time = new(1970, 1, 1, 0, 0, 0, 0);
            this.Time = this.Time.AddSeconds(unixtime).ToLocalTime();
            this.DeviceId = deviceId;
            this.Type = DataType.DATA;
        }

        public bool IsEmpty()
        {
            return Values == null || Values.Count == 0;
        }

        public double? ValidateAndAddData(string physio_id, object value, double decimalshift, bool rounddata, string decimalformat = null)
        {
            int val = Convert.ToInt32(value);

            double? dval = null;
            if (val >= DataConstants.DATA_INVALID_LIMIT)
            {
                dval = Convert.ToDouble(value, CultureInfo.InvariantCulture) * decimalshift;
                if (rounddata) dval = Math.Round((double)dval);
            }

            if (this.Values == null) {
                this.Values = new();
            }

            Values.Add(new ReceivedDataValue(physio_id, dval, decimalformat));

            return dval;
        }
    }

    public class ReceivedDataValue
    {
        [JsonPropertyName("dataType")]
        public string DataType;
        [JsonPropertyName("value")]
        public object Value;
        [JsonIgnore]
        public string DecimalFormat;

        public ReceivedDataValue(string dataType, object value, string decimalFormat = null)
        {
            DataType = dataType;
            Value = value;
            DecimalFormat = decimalFormat;
        }
    }

    public class ReceivedWaveData
    {
        [JsonPropertyName("dataType")]
        public string DataType;
        [JsonPropertyName("time")]
        public DateTime Time;
        [JsonPropertyName("values")]
        public double[] Values;
    }

    public enum DataType
    {
        DATA,
        CURVE
    }
}
