using log4net;
using System.Text;

namespace VSCaptureWave
{
    public class CsvExport
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public static readonly string DEFAULT_EXPORT_FILE_NAME = "S5DataExport.csv";

        public readonly string ExportFileName;
        private readonly string ExportWaveFileNameTemplate;
        private bool HeaderSaved = false;

        public CsvExport(string ExportFileName)
        {
            this.ExportFileName = ExportFileName;
            ExportWaveFileNameTemplate = Path.Combine(Path.GetDirectoryName(ExportFileName), Path.GetFileNameWithoutExtension(ExportFileName));
        }

        public void WriteNumericHeadersList(ReceivedDataBlock DataBlock)
        {
            if (DataBlock != null && !DataBlock.IsEmpty() && !HeaderSaved)
            {
                log.Debug("Creating a file header: " + this.ExportFileName);
                StringBuilder strbuildheaders = new();
                strbuildheaders.Append("Time");
                strbuildheaders.Append(',');

                foreach (ReceivedDataValue dataValue in DataBlock.Values)
                {
                    strbuildheaders.Append(dataValue.DataType);
                    strbuildheaders.Append(',');

                }

                strbuildheaders.Remove(strbuildheaders.Length - 1, 1);
                strbuildheaders.Replace(",,", ",");
                strbuildheaders.AppendLine();
                ExportNumValListToCSVFile(this.ExportFileName, strbuildheaders);

                HeaderSaved = true;
            }
        }

        public void SaveRow(ReceivedDataBlock dataBlock)
        {
            if (dataBlock != null && !dataBlock.IsEmpty())
            {
                WriteNumericHeadersList(dataBlock);

                StringBuilder strbuildvalues = new();
                strbuildvalues.Append(dataBlock.Time);
                strbuildvalues.Append(',');

                foreach (ReceivedDataValue dataValue in dataBlock.Values)
                {
                    if (String.IsNullOrEmpty(dataValue.DecimalFormat))
                    {
                        strbuildvalues.Append(dataValue.Value != null ? dataValue.Value : "-");
                    }
                    else
                    {
                        strbuildvalues.Append(dataValue.Value != null ? String.Format(dataValue.DecimalFormat, dataValue.Value) : "-");
                    }
                    strbuildvalues.Append(',');
                }

                strbuildvalues.Remove(strbuildvalues.Length - 1, 1);
                strbuildvalues.Replace(",,", ",");
                strbuildvalues.AppendLine();

                ExportNumValListToCSVFile(this.ExportFileName, strbuildvalues);
                strbuildvalues.Clear();
            }
        }

        public static void ExportNumValListToCSVFile(string _FileName, StringBuilder strbuildNumVal)
        {
            try
            {
                // Open file for reading. 
                using StreamWriter wrStream = new(_FileName, true, Encoding.UTF8);
                wrStream.Write(strbuildNumVal);
                strbuildNumVal.Clear();

                // close file stream. 
                wrStream.Close();
            }

            catch (Exception _Exception)
            {
                // Error. 
                log.Error(String.Format("Exception caught in process: {0}", _Exception.ToString()), _Exception);
            }
        }

        public void ExportWaveToCSV(List<ReceivedWaveData> m_WaveValResultList)
        {
            int wavevallistcount = m_WaveValResultList.Count;

            if (wavevallistcount != 0)
            {
                StringBuilder strbuildwavevalues = new();

                foreach (ReceivedWaveData WavValResult in m_WaveValResultList)
                {
                    string pathcsv = ExportWaveFileNameTemplate + "-wave-" + WavValResult.DataType + Path.GetExtension(ExportFileName);

                    int wavvalarraylength = WavValResult.Values.GetLength(0);

                    for (int index = 0; index < wavvalarraylength; index++)
                    {
                        double waveval = WavValResult.Values.ElementAt(index);                                             
                        strbuildwavevalues.Append(WavValResult.Time);
                        strbuildwavevalues.Append(',');
                        strbuildwavevalues.Append(Double.IsNaN(waveval) ? "-" : waveval);
                        strbuildwavevalues.Append(',');
                        strbuildwavevalues.AppendLine();
                    }

                    CsvExport.ExportNumValListToCSVFile(pathcsv, strbuildwavevalues);

                    strbuildwavevalues.Clear();
                }

                m_WaveValResultList.RemoveRange(0, wavevallistcount);
            }
        }
    }
}
