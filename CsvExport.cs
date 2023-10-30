using log4net;
using System.Text;

namespace VSCaptureWave
{
    public class CsvExport
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public static readonly string DEFAULT_EXPORT_FILE_NAME = "S5DataExport.csv";

        private readonly string ExportFileName;
        private bool HeaderSaved = false;

        public CsvExport(string ExportFileName)
        {
            this.ExportFileName = ExportFileName;
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
                    strbuildvalues.Append(dataValue.Value);
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
    }
}
