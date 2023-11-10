/*
 * This file is part of VitalSignsCaptureWave v1.011.
 * Copyright (C) 2015-22 John George K., xeonfusion@users.sourceforge.net

    VitalSignsCapture is free software: you can redistribute it and/or modify
    it under the terms of the GNU Lesser General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    VitalSignsCapture is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU Lesser General Public License for more details.

    You should have received a copy of the GNU Lesser General Public License
    along with VitalSignsCapture.  If not, see <http://www.gnu.org/licenses/>.*/

using System.Text;
using System.IO.Ports;
using System.Runtime.InteropServices;
using System.Globalization;
using log4net;

namespace VSCaptureWave
{
    public sealed class DSerialPort : SerialPort
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        // Main Datex Record variables
        private datex_record_req_type request_ptr = new();
        private datex_record_wave_req_type wave_request_ptr = new();
        public List<datex_record_type> RecordList = new();
        public List<byte[]> FrameList = new();
        private int DPortBufSize;
        public byte[] DPort_rxbuf;
        private datex_tx_type DPort_txbuf = new();
        private datex_wave_tx_type DPort_wave_txbuf = new();

        private bool m_fstart = true;
        private bool m_storestart = false;
        private bool m_storeend = false;
        private bool m_bitshiftnext = false;
        private List<byte> m_bList = new();

        public ReceivedDataBlock m_ResultDataBlock = null;

        public CsvExport CsvExport = null;
        public JsonServerClient JsonServerClient = null;
        public MQTTClient MQTTClient = null;

        public List<ReceivedWaveData> m_WaveValResultList = new();
        public StringBuilder m_strbuildwavevalues = new();

        public string m_strTimestamp;
        public int m_dataexportset = 1;
        private bool m_transmissionstart = true;

        public string m_DeviceID;

        //Create a singleton serialport subclass
        private static volatile DSerialPort DPort = null;

        public static DSerialPort GetInstance
        {

            get
            {
                if (DPort == null)
                {
                    lock (typeof(DSerialPort))
                        if (DPort == null)
                        {
                            DPort = new DSerialPort();
                        }

                }
                return DPort;
            }

        }

        public DSerialPort()
        {
            DPort = this;

            DPortBufSize = 4096;
            DPort_rxbuf = new byte[DPortBufSize];

            if (OSIsUnix())
                DPort.PortName = "/dev/ttyUSB0"; //default Unix port
            else 
                DPort.PortName = "COM1"; //default Windows port

            DPort.BaudRate = 19200;
            DPort.Parity = Parity.Even;
            DPort.DataBits = 8;
            DPort.StopBits = StopBits.One;

            DPort.Handshake = Handshake.RequestToSend;

            // Set the read/write timeouts
            DPort.ReadTimeout = 600000;
            DPort.WriteTimeout = 600000;

            //ASCII Encoding in C# is only 7bit so
            DPort.Encoding = Encoding.GetEncoding("ISO-8859-1");

        }

        public static void WriteBuffer(byte[] txbuf)
        {
            log.Debug($"{txbuf.Length} bytes sent");

            byte[] framebyte = { DataConstants.CTRLCHAR, (DataConstants.FRAMECHAR & DataConstants.BIT5COMPL), 0 };
            byte[] ctrlbyte = { DataConstants.CTRLCHAR, (DataConstants.CTRLCHAR & DataConstants.BIT5COMPL), 0 };

            byte check_sum = 0x00;
            byte b1 = 0x00;
            byte b2 = 0x00;

            int txbuflen = (txbuf.GetLength(0) + 1);
            //Create write packet buffer
            byte[] temptxbuff = new byte[txbuflen];
            //Clear the buffer
            for (int j = 0; j < txbuflen; j++)
            {
                temptxbuff[j] = 0;
            }

            // Send start frame characters
            temptxbuff[0] = DataConstants.FRAMECHAR;

            int i = 1;

            foreach (byte b in txbuf)
            {
                switch (b)
                {
                    case DataConstants.FRAMECHAR:
                        temptxbuff[i] = framebyte[0];
                        temptxbuff[i + 1] = framebyte[1];
                        i += 2;
                        b1 += framebyte[0];
                        b1 += framebyte[1];
                        check_sum += b1;
                        break;
                    case DataConstants.CTRLCHAR:
                        temptxbuff[i] = ctrlbyte[0];
                        temptxbuff[i + 1] = ctrlbyte[1];
                        i += 2;
                        b2 += ctrlbyte[0];
                        b2 += ctrlbyte[1];
                        check_sum += b2;
                        break;
                    default:
                        temptxbuff[i] = b;
                        i++;
                        check_sum += b;
                        break;
                };
            }

            int buflen = i;
            byte[] finaltxbuff = new byte[buflen + 2];

            for (int j = 0; j < buflen; j++)
            {
                finaltxbuff[j] = temptxbuff[j];
            }

            // Send Checksum
            finaltxbuff[buflen] = check_sum;
            // Send stop frame characters
            finaltxbuff[buflen + 1] = DataConstants.FRAMECHAR;

            try
            {
                DPort.Write(finaltxbuff, 0, buflen + 2);
            }
            catch (Exception ex)
            {
                log.Error($"Error opening/writing to serial port :: {ex.Message} Error!", ex);
            }
        }

        public void ClearReadBuffer()
        {
            //Clear the buffer
            for (int i = 0; i < DPortBufSize; i++)
            {
                DPort_rxbuf[i] = 0;
            }
        }

        public int ReadBuffer()
        {
            int bytesreadtotal = 0;

            try
            {
                string path = Path.Combine(Directory.GetCurrentDirectory(), "S5Rawoutput.raw"); //TODO make optional

                int lenread = 0;

                do
                {
                    ClearReadBuffer();
                    lenread = DPort.Read(DPort_rxbuf, 0, DPortBufSize);

                    byte[] copyarray = new byte[lenread];

                    for (int i = 0; i < lenread; i++)
                    {
                        copyarray[i] = DPort_rxbuf[i];
                        CreateFrameListFromByte(copyarray[i]);
                    }

                    ByteArrayToFile(path, copyarray, copyarray.GetLength(0));
                    bytesreadtotal += lenread;
                    if (FrameList.Count > 0)
                    {
                        CreateRecordList();

                        ReadSubRecords();
                        ReadMultipleWaveSubRecords();

                        FrameList.RemoveRange(0, FrameList.Count);
                        RecordList.RemoveRange(0, RecordList.Count);

                    }

                }
                while (DPort.BytesToRead != 0);

            }
            catch (Exception ex)
            {
               log.Error($"Error opening/writing to serial port :: {ex.Message} Error!", ex);
            }

            return bytesreadtotal;
        }

        public void CreateFrameListFromByte(byte b)
        {

            if (b == DataConstants.FRAMECHAR && m_fstart)
            {
                m_fstart = false;
                m_storestart = true;
            }
            else if (b == DataConstants.FRAMECHAR && (m_fstart == false))
            {
                m_fstart = true;
                m_storeend = true;
                m_storestart = false;
                if (b != DataConstants.FRAMECHAR) m_bList.Add(b);
            }

            if (m_storestart == true)
            {
                if (b == DataConstants.CTRLCHAR)
                    m_bitshiftnext = true;
                else
                {
                    if (m_bitshiftnext == true)
                    {
                        b |= DataConstants.BIT5;
                        m_bitshiftnext = false;
                        m_bList.Add(b);
                    }
                    else if (b != DataConstants.FRAMECHAR) m_bList.Add(b);
                }

            }
            else if (m_storeend == true)
            {
                int framelen = m_bList.Count();
                if (framelen != 0)
                {
                    byte[] bArray = new byte[framelen];
                    bArray = m_bList.ToArray();
                    //Calculate checksum
                    byte checksum = 0x00;
                    for (int j = 0; j < (framelen - 1); j++)
                    {
                        checksum += bArray[j];
                    }
                    if (checksum == bArray[framelen - 1])
                    {
                        FrameList.Add(bArray);
                        log.Debug($"A new {bArray.Length} byte frame has been recognized");
                    }
                    m_bList.Clear();
                    m_storeend = false;
                }
                else
                {
                    m_storestart = true;
                    m_storeend = false;
                    m_fstart = false;
                }

            }

        }

        public void CreateRecordList()
        {
            //Read record from Framelist();

            int recorddatasize = 0;
            byte[] fullrecord = new byte[1490];

            foreach (byte[] fArray in FrameList)
            {
                datex_record_type record_dtx = new datex_record_type();

                for (int i = 0; i < fullrecord.GetLength(0); i++)
                {
                    fullrecord[i] = 0x00;
                }

                recorddatasize = fArray.GetLength(0);

                for (int n = 0; n < (fArray.GetLength(0)) && recorddatasize < 1490; n++)
                {
                    fullrecord[n] = fArray[n];
                }

                GCHandle handle2 = GCHandle.Alloc(fullrecord, GCHandleType.Pinned);
                Marshal.PtrToStructure(handle2.AddrOfPinnedObject(), record_dtx);

                RecordList.Add(record_dtx);
                log.Debug($"A new {record_dtx.hdr.r_dri_level} DRI level record has been recognized");
                handle2.Free();

            }
         }

        public void RequestTransfer(byte Trtype, short Interval, byte DRIlevel)
        {
            log.Info($"Requesting Transmission {Trtype} with interval {Interval} with DRI level {DRIlevel} from monitor");

            //Set Record Header
            request_ptr.hdr.r_len = 49; //size of hdr + phdb type
            request_ptr.hdr.r_dri_level = DRIlevel;
            request_ptr.hdr.r_time = 0;
            request_ptr.hdr.r_maintype = DataConstants.DRI_MT_PHDB;


            request_ptr.hdr.sr_offset1 = 0;
            request_ptr.hdr.sr_type1 = 0; // Physiological data request
            request_ptr.hdr.sr_offset2 = 0;
            request_ptr.hdr.sr_type2 = 0xFF; // Last subrecord

            // Request transmission subrecord
            request_ptr.phdbr.phdb_rcrd_type = Trtype;
            request_ptr.phdbr.tx_interval = Interval;
            if (Interval != 0) request_ptr.phdbr.phdb_class_bf =
                  DataConstants.DRI_PHDBCL_REQ_BASIC_MASK | DataConstants.DRI_PHDBCL_REQ_EXT1_MASK |
                  DataConstants.DRI_PHDBCL_REQ_EXT2_MASK | DataConstants.DRI_PHDBCL_REQ_EXT3_MASK;
            else request_ptr.phdbr.phdb_class_bf = 0x0000;

            //Get pointer to structure in memory
            IntPtr uMemoryValue = IntPtr.Zero;

            try
            {
                uMemoryValue = Marshal.AllocHGlobal(49);
                Marshal.StructureToPtr(request_ptr, uMemoryValue, true);
                Marshal.PtrToStructure(uMemoryValue, DPort_txbuf);
                WriteBuffer(DPort_txbuf.data);
            }
            finally
            {
                if (uMemoryValue != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(uMemoryValue);
                }
            }

            m_transmissionstart = true;
        }

        public void RequestWaveTransfer(byte TrWavetype, short TrSignaltype, byte DRIlevel)
        {
            log.Info($"Requesting single Wave transfer {TrWavetype} with signal type {TrSignaltype}, with DRI level {DRIlevel} from monitor");

            //Set Record Header
            wave_request_ptr.hdr.r_len = 72; //size of hdr + wfreq type
            wave_request_ptr.hdr.r_dri_level = DRIlevel;
            wave_request_ptr.hdr.r_time = 0;
            wave_request_ptr.hdr.r_maintype = DataConstants.DRI_MT_WAVE;

            // The packet contains only one subrecord
            // 0 = Waveform data transmission request
            wave_request_ptr.hdr.sr_offset1 = 0;
            wave_request_ptr.hdr.sr_type1 = DataConstants.DRI_WF_CMD;
            wave_request_ptr.hdr.sr_offset2 = 0;
            wave_request_ptr.hdr.sr_type2 = DataConstants.DRI_EOL_SUBR_LIST; // Last subrecord

            // Request transmission subrecord
            //wave_request_ptr.wfreq.req_type = DataConstants.WF_REQ_CONT_START;
            wave_request_ptr.wfreq.req_type = TrSignaltype;
            wave_request_ptr.wfreq.res = 0;
            //wave_request_ptr.wfreq.type[0] = DataConstants.DRI_WF_ECG1;
            wave_request_ptr.wfreq.type[0] = TrWavetype;
            wave_request_ptr.wfreq.type[1] = DataConstants.DRI_EOL_SUBR_LIST;

            //Get pointer to structure in memory
            IntPtr uMemoryValue = IntPtr.Zero;

            try
            {
                uMemoryValue = Marshal.AllocHGlobal(72);
                Marshal.StructureToPtr(wave_request_ptr, uMemoryValue, true);
                Marshal.PtrToStructure(uMemoryValue, DPort_wave_txbuf);
                WriteBuffer(DPort_wave_txbuf.data);
            }
            finally
            {
                if (uMemoryValue != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(uMemoryValue);
                }
            }


        }

        public void RequestMultipleWaveTransfer(byte[] TrWavetype, short TrSignaltype, byte DRIlevel)
        {
            log.Info($"Requesting wave {TrWavetype} with signal type {TrSignaltype}, with DRI level {DRIlevel} from monitor");

            //Set Record Header
            wave_request_ptr.hdr.r_len = 72; //size of hdr + wfreq type
            wave_request_ptr.hdr.r_dri_level = DRIlevel;
            wave_request_ptr.hdr.r_time = 0;
            wave_request_ptr.hdr.r_maintype = DataConstants.DRI_MT_WAVE;

            // The packet contains only one subrecord
            // 0 = Waveform data transmission request
            wave_request_ptr.hdr.sr_offset1 = 0;
            wave_request_ptr.hdr.sr_type1 = DataConstants.DRI_WF_CMD;
            wave_request_ptr.hdr.sr_offset2 = 0;
            wave_request_ptr.hdr.sr_type2 = DataConstants.DRI_EOL_SUBR_LIST; // Last subrecord

            // Request transmission subrecord
            //wave_request_ptr.wfreq.req_type = DataConstants.WF_REQ_CONT_START;
            wave_request_ptr.wfreq.req_type = TrSignaltype;
            wave_request_ptr.wfreq.res = 0;
            //wave_request_ptr.wfreq.type[0] = DataConstants.DRI_WF_ECG1;

            //wave_request_ptr.wfreq.type[0] = TrWavetype;
            //wave_request_ptr.wfreq.type[1] = DataConstants.DRI_EOL_SUBR_LIST;
            for (int i = 0; i < 8; i++)
            {
                wave_request_ptr.wfreq.type[i] = TrWavetype[i];
                if (i < 7) wave_request_ptr.wfreq.type[i + 1] = DataConstants.DRI_EOL_SUBR_LIST;
            }

            //Get pointer to structure in memory
            IntPtr uMemoryValue = IntPtr.Zero;

            try
            {
                uMemoryValue = Marshal.AllocHGlobal(72);
                Marshal.StructureToPtr(wave_request_ptr, uMemoryValue, true);
                Marshal.PtrToStructure(uMemoryValue, DPort_wave_txbuf);
                WriteBuffer(DPort_wave_txbuf.data);
            }
            finally
            {
                if (uMemoryValue != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(uMemoryValue);
                }
            }

        }

        public void StopwaveTransfer()
        {
            RequestWaveTransfer(0, DataConstants.WF_REQ_CONT_STOP, DataConstants.DRI_LEVEL_2015);
            RequestWaveTransfer(0, DataConstants.WF_REQ_CONT_STOP, DataConstants.DRI_LEVEL_2009);
            RequestWaveTransfer(0, DataConstants.WF_REQ_CONT_STOP, DataConstants.DRI_LEVEL_2005);
            RequestWaveTransfer(0, DataConstants.WF_REQ_CONT_STOP, DataConstants.DRI_LEVEL_2003);
            RequestWaveTransfer(0, DataConstants.WF_REQ_CONT_STOP, DataConstants.DRI_LEVEL_2001);

        }

        public void StopTransfer()
        {
            //RequestTransfer(DataConstants.DRI_PH_60S_TREND, 0);
            RequestTransfer(DataConstants.DRI_PH_DISPL, 0, DataConstants.DRI_LEVEL_2015);
            RequestTransfer(DataConstants.DRI_PH_DISPL, 0, DataConstants.DRI_LEVEL_2009);
            RequestTransfer(DataConstants.DRI_PH_DISPL, 0, DataConstants.DRI_LEVEL_2005);
            RequestTransfer(DataConstants.DRI_PH_DISPL, 0, DataConstants.DRI_LEVEL_2003);
            RequestTransfer(DataConstants.DRI_PH_DISPL, 0, DataConstants.DRI_LEVEL_2001);

        }

        public void ReadSubRecords()
        {
            foreach (datex_record_type dx_record in RecordList)
            {
                short dxrecordmaintype = dx_record.hdr.r_maintype;

                if (dxrecordmaintype == DataConstants.DRI_MT_PHDB)
                {


                    short[] sroffArray = { dx_record.hdr.sr_offset1, dx_record.hdr.sr_offset2, dx_record.hdr.sr_offset3, dx_record.hdr.sr_offset4, dx_record.hdr.sr_offset5, dx_record.hdr.sr_offset6, dx_record.hdr.sr_offset7, dx_record.hdr.sr_offset8 };
                    byte[] srtypeArray = { dx_record.hdr.sr_type1, dx_record.hdr.sr_type2, dx_record.hdr.sr_type3, dx_record.hdr.sr_type4, dx_record.hdr.sr_type5, dx_record.hdr.sr_type6, dx_record.hdr.sr_type7, dx_record.hdr.sr_type8 };

                    uint unixtime = dx_record.hdr.r_time;
                    dri_phdb phdata_ptr = new dri_phdb();

                    for (int i = 0; i < 8 && (srtypeArray[i] != 0xFF); i++)
                    {
                        //if (srtypeArray[i] == DataConstants.DRI_PH_DISPL && srtypeArray[i] !=0xFF)
                        {
                            int offset = (int)sroffArray[i];

                            byte[] buffer = new byte[270];
                            for (int j = 0; j < 270; j++)
                            {
                                buffer[j] = dx_record.data[4 + j + offset];
                            }

                            GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);

                            switch (i)
                            {
                                case 0:
                                    Marshal.PtrToStructure(handle.AddrOfPinnedObject(), phdata_ptr.basic);
                                    break;
                                case 1:
                                    Marshal.PtrToStructure(handle.AddrOfPinnedObject(), phdata_ptr.ext1);
                                    break;
                                case 2:
                                    Marshal.PtrToStructure(handle.AddrOfPinnedObject(), phdata_ptr.ext2);
                                    break;
                                case 3:
                                    Marshal.PtrToStructure(handle.AddrOfPinnedObject(), phdata_ptr.ext3);
                                    break;
                            }

                            handle.Free();

                        }
                    }

                    m_ResultDataBlock = new(m_DeviceID, unixtime);

                    ProcessBasicSubRecord(phdata_ptr);
                    ProcessExtSubRecords(phdata_ptr);

                    switch (m_dataexportset)
                    {
                        case 1:
                            CsvExport.SaveRow(this.m_ResultDataBlock);
                            break;
                        case 2:
                            JsonServerClient.SendBlock(this.m_ResultDataBlock);
                            break;
                        case 3:
                            MQTTClient.ExportNumValListToMQTT(this.m_ResultDataBlock);
                            break;
                    }
                }
            }
        }

        public void ReadMultipleWaveSubRecords()
        {
            if (m_dataexportset == 3) return; // MQTT wave export isn't supported yet

            foreach (datex_record_type dx_record in RecordList)
            {

                short dxrecordmaintype = dx_record.hdr.r_maintype;

                if (dxrecordmaintype == DataConstants.DRI_MT_WAVE)
                {

                    short[] sroffArray = { dx_record.hdr.sr_offset1, dx_record.hdr.sr_offset2, dx_record.hdr.sr_offset3, dx_record.hdr.sr_offset4, dx_record.hdr.sr_offset5, dx_record.hdr.sr_offset6, dx_record.hdr.sr_offset7, dx_record.hdr.sr_offset8 };
                    byte[] srtypeArray = { dx_record.hdr.sr_type1, dx_record.hdr.sr_type2, dx_record.hdr.sr_type3, dx_record.hdr.sr_type4, dx_record.hdr.sr_type5, dx_record.hdr.sr_type6, dx_record.hdr.sr_type7, dx_record.hdr.sr_type8 };

                    uint unixtime = dx_record.hdr.r_time;
                    // Unix timestamp is seconds past epoch 
                    DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0);
                    //dtDateTime = dtDateTime.AddSeconds(unixtime).ToLocalTime();
                    dtDateTime = dtDateTime.AddSeconds(unixtime);

                    //Read upto 8 subrecords
                    for (int i = 0; i < 8 && (srtypeArray[i] != DataConstants.DRI_EOL_SUBR_LIST); i++)
                    {


                        int offset = (int)sroffArray[i];
                        int nextoffset = 0;

                        //read subrecord length from header to get nextoffset
                        byte[] srsamplelenbytes = new byte[2];
                        srsamplelenbytes[0] = dx_record.data[offset];
                        srsamplelenbytes[1] = dx_record.data[offset + 1];
                        int srheaderlen = 6;
                        int subrecordlen = srheaderlen + (BitConverter.ToInt16(srsamplelenbytes, 0)) * 2;
                        nextoffset = offset + subrecordlen;

                        int buflen = (nextoffset - offset - 6);

                        byte[] buffer = new byte[buflen];

                        for (int j = 0; j < buflen; j++)
                        {
                            buffer[j] = dx_record.data[6 + j + offset];
                        }

                        ReceivedWaveData WaveVal = new();
                        WaveVal.Time = dtDateTime;
                        WaveVal.DataType = Enum.GetName(typeof(DataConstants.WavesIDLabels), srtypeArray[i]);
                        
                        List<double> waveValList = new();

                        //Convert Byte array to 16 bit short values
                        for (int n = 0; n < buffer.Length; n += 2)
                        {
                            short wavedata = BitConverter.ToInt16(buffer, n);
                            int ival = Convert.ToInt32(wavedata);
                            double dval = Double.NaN;
                            if (ival >= DataConstants.DATA_INVALID_LIMIT)
                            {
                                dval = (Convert.ToDouble(wavedata, CultureInfo.InvariantCulture)) * GetWaveUnitShift(WaveVal.DataType);
                            }
                            waveValList.Add(dval);
                        }

                        WaveVal.Values = new double[waveValList.Count];
                        double[] wavedataarray = waveValList.ToArray();
                        Array.Copy(wavedataarray, WaveVal.Values, waveValList.Count);

                        m_WaveValResultList.Add(WaveVal);
                    }

                }
            }

            switch (m_dataexportset)
            {
                case 1:
                    CsvExport.ExportWaveToCSV(this.m_WaveValResultList);
                    break;
                case 2:
                    JsonServerClient.SendBlock(new ReceivedDataBlock(m_DeviceID, this.m_WaveValResultList));
                    break;
            }
        }

        public void ProcessBasicSubRecord(dri_phdb driSR)
        {
            short so1 = driSR.basic.ecg.hr;
            short so2 = driSR.basic.nibp.sys;
            short so3 = driSR.basic.nibp.dia;
            short so4 = driSR.basic.nibp.mean;
            short so5 = driSR.basic.SpO2.SpO2;
            short so6 = driSR.basic.co2.et;

            string s1 = m_ResultDataBlock.ValidateAndAddData("ECG_HR", so1, 1, true);

            string s2 = m_ResultDataBlock.ValidateAndAddData("NIBP_Systolic", so2, 0.01, true);

            string s3 = m_ResultDataBlock.ValidateAndAddData("NIBP_Diastolic", so3, 0.01, true);

            string s4 = m_ResultDataBlock.ValidateAndAddData("NIBP_Mean", so4, 0.01, true);

            string s5 = m_ResultDataBlock.ValidateAndAddData("SpO2", so5, 0.01, true);

            double et = (so6 * driSR.basic.co2.amb_press);
            string s6 = m_ResultDataBlock.ValidateAndAddData("ET_CO2", et, 0.00001, true);

            short so7 = driSR.basic.aa.et;
            short so8 = driSR.basic.aa.fi;
            short so9 = driSR.basic.aa.mac_sum;
            ushort so10 = driSR.basic.aa.hdr.label_info;

            m_ResultDataBlock.ValidateAndAddData("AA_ET", so7, 0.01, false, "{0:0.00}");
            m_ResultDataBlock.ValidateAndAddData("AA_FI", so8, 0.01, false, "{0:0.00}");
            string s9 = m_ResultDataBlock.ValidateAndAddData("AA_MAC_SUM", so9, 0.01, false, "{0:0.00}");

            string s10 = "";

            switch (so10)
            {
                case 0:
                    s10 = "Unknown";
                    break;
                case 1:
                    s10 = "None";
                    break;
                case 2:
                    s10 = "HAL";
                    break;
                case 3:
                    s10 = "ENF";
                    break;
                case 4:
                    s10 = "ISO";
                    break;
                case 5:
                    s10 = "DES";
                    break;
                case 6:
                    s10 = "SEV";
                    break;
            }

            AddDataString("Agent_AA", s10);

            double so11 = driSR.basic.o2.fi;
            double so12 = driSR.basic.n2o.fi;
            double so13 = driSR.basic.n2o.et;
            double so14 = driSR.basic.co2.rr;
            double so15 = driSR.basic.t1.temp;
            double so16 = driSR.basic.t2.temp;

            double so17 = driSR.basic.p1.hr;
            double so18 = driSR.basic.p1.sys;
            double so19 = driSR.basic.p1.dia;
            double so20 = driSR.basic.p1.mean;
            double so21 = driSR.basic.p2.hr;
            double so22 = driSR.basic.p2.sys;
            double so23 = driSR.basic.p2.dia;
            double so24 = driSR.basic.p2.mean;
            double so36 = driSR.basic.p3.hr;
            double so37 = driSR.basic.p3.sys;
            double so38 = driSR.basic.p3.dia;
            double so39 = driSR.basic.p3.mean;

            double so25 = driSR.basic.flow_vol.ppeak;
            double so26 = driSR.basic.flow_vol.pplat;
            double so27 = driSR.basic.flow_vol.tv_exp;
            double so28 = driSR.basic.flow_vol.tv_insp;
            double so29 = driSR.basic.flow_vol.peep;
            double so30 = driSR.basic.flow_vol.mv_exp;
            double so31 = driSR.basic.flow_vol.compliance;
            double so32 = driSR.basic.flow_vol.rr;


            m_ResultDataBlock.ValidateAndAddData("O2_FI", so11, 0.01, false, "{0:0.00}");
            m_ResultDataBlock.ValidateAndAddData("N2O_FI", so12, 0.01, false, "{0:0.00}");
            m_ResultDataBlock.ValidateAndAddData("N2O_ET", so13, 0.01, false, "{0:0.00}");
            m_ResultDataBlock.ValidateAndAddData("CO2_RR", so14, 1, true);
            string s15 = m_ResultDataBlock.ValidateAndAddData("T1_Temp", so15, 0.01, false, "{0:0.00}");
            string s16 = m_ResultDataBlock.ValidateAndAddData("T2_Temp", so16, 0.01, false, "{0:0.00}");

            /*string P1Label = GetInvasivePressureLabel(driSR.basic.p1.hdr.label_info);
            string P2Label = GetInvasivePressureLabel(driSR.basic.p2.hdr.label_info);
            string P3Label = GetInvasivePressureLabel(driSR.basic.p3.hdr.label_info);*/

            string P1Label = "P1";
            string P2Label = "P2";
            string P3Label = "P3";

            m_ResultDataBlock.ValidateAndAddData(P1Label + "_HR", so17, 1, true);
            string s18 = m_ResultDataBlock.ValidateAndAddData(P1Label + "_Systolic", so18, 0.01, true);
            string s19 = m_ResultDataBlock.ValidateAndAddData(P1Label + "_Diastolic", so19, 0.01, true);
            string s20 = m_ResultDataBlock.ValidateAndAddData(P1Label + "_Mean", so20, 0.01, true);
            m_ResultDataBlock.ValidateAndAddData(P2Label + "_HR", so21, 1, true);
            string s22 = m_ResultDataBlock.ValidateAndAddData(P2Label + "_Systolic", so22, 0.01, true);
            string s23 = m_ResultDataBlock.ValidateAndAddData(P2Label + "_Diastolic", so23, 0.01, true);
            string s24 = m_ResultDataBlock.ValidateAndAddData(P2Label + "_Mean", so24, 0.01, true);
            m_ResultDataBlock.ValidateAndAddData(P3Label + "_HR", so36, 1, true);
            string s37 = m_ResultDataBlock.ValidateAndAddData(P3Label + "_Systolic", so37, 0.01, true);
            string s38 = m_ResultDataBlock.ValidateAndAddData(P3Label + "_Diastolic", so38, 0.01, true);
            string s39 = m_ResultDataBlock.ValidateAndAddData(P3Label + "_Mean", so39, 0.01, true);

            m_ResultDataBlock.ValidateAndAddData("PPeak", so25, 0.01, true);
            m_ResultDataBlock.ValidateAndAddData("PPlat", so26, 0.01, true);
            m_ResultDataBlock.ValidateAndAddData("TV_Exp", so27, 0.1, true);
            m_ResultDataBlock.ValidateAndAddData("TV_Insp", so28, 0.1, true);
            m_ResultDataBlock.ValidateAndAddData("PEEP", so29, 0.01, true);
            m_ResultDataBlock.ValidateAndAddData("MV_Exp", so30, 0.01, false, "{0:0.00}");
            m_ResultDataBlock.ValidateAndAddData("Compliance", so31, 0.01, true);
            m_ResultDataBlock.ValidateAndAddData("RR", so32, 1, true);

            log.Info(String.Format("ECG HR {0:d}/min NIBP {1:d}/{2:d}({3:d})mmHg SpO2 {4:d}% ETCO2 {5:d}mmHg", s1, s2, s3, s4, s5, s6));
            log.Info(String.Format("IBP1 {0:d}/{1:d}({2:d})mmHg IBP2 {3:d}/{4:d}({5:d})mmHg MAC {6} T1 {7}°C T2 {8}°C", s18, s19, s20, s22, s23, s24, s9, s15, s16));

            short so33 = driSR.basic.nmt.tratio;
            short so34 = driSR.basic.nmt.t1;
            short so35 = driSR.basic.nmt.ptc;

            uint n1 = driSR.basic.nmt.hdr.status_bits;

            string nmtmode = "";
            if (((n1 >> 2) & (uint)DataConstants.stim_types.TOF) == (uint)DataConstants.stim_types.TOF)
            {
                nmtmode = DataConstants.stim_types.TOF.ToString();
            }
            else if (((n1 >> 2) & (uint)DataConstants.stim_types.DBS) == (uint)DataConstants.stim_types.DBS)
            {
                nmtmode = DataConstants.stim_types.DBS.ToString();
            }
            else if (((n1 >> 2) & (uint)DataConstants.stim_types.ST_STIM) == (uint)DataConstants.stim_types.ST_STIM)
            {
                nmtmode = DataConstants.stim_types.ST_STIM.ToString();
            }
            else if (((n1 >> 2) & (uint)DataConstants.stim_types.PTC_STIM) == (uint)DataConstants.stim_types.PTC_STIM)
            {
                nmtmode = DataConstants.stim_types.PTC_STIM.ToString();
            }
            AddDataString("NMT_MODE", nmtmode);

            m_ResultDataBlock.ValidateAndAddData("NMT_TWITCH_RATIO", so33, 0.1, false, "{0:0.00}");
            m_ResultDataBlock.ValidateAndAddData("NMT_T1", so34, 0.1, false);

        }

        static string GetInvasivePressureLabel(int labelinfo)
        {
            string InvPLabel;

            switch (labelinfo)
            {
                case 0:
                    InvPLabel = "NOTDEFINED";
                    break;
                case 1:
                    InvPLabel = "ART";
                    break;
                case 2:
                    InvPLabel = "CVP";
                    break;
                case 3:
                    InvPLabel = "PA";
                    break;
                case 4:
                    InvPLabel = "RAP";
                    break;
                case 5:
                    InvPLabel = "RVP";
                    break;
                case 6:
                    InvPLabel = "LAP";
                    break;
                case 7:
                    InvPLabel = "ICP";
                    break;
                case 8:
                    InvPLabel = "ABP";
                    break;
                case 9:
                    InvPLabel = "P1";
                    break;
                case 10:
                    InvPLabel = "P2";
                    break;
                case 11:
                    InvPLabel = "P3";
                    break;
                case 12:
                    InvPLabel = "P4";
                    break;
                case 13:
                    InvPLabel = "P5";
                    break;
                case 14:
                    InvPLabel = "P6";
                    break;
                case 15:
                    InvPLabel = "SP";
                    break;
                case 16:
                    InvPLabel = "FEM";
                    break;
                case 17:
                    InvPLabel = "UAC";
                    break;
                case 18:
                    InvPLabel = "UVC";
                    break;
                case 19:
                    InvPLabel = "ICP2";
                    break;
                case 20:
                    InvPLabel = "P7";
                    break;
                case 21:
                    InvPLabel = "P8";
                    break;
                case 22:
                    InvPLabel = "FEMV";
                    break;
                default:
                    InvPLabel = "UNDEFINED";
                    break;
            }
            return InvPLabel;
        }

        public void ProcessExtSubRecords(dri_phdb driSR)
        {
            short so1_I = driSR.ext1.ecg12.stI;
            short so1_II = driSR.ext1.ecg12.stII;
            short so1_III = driSR.ext1.ecg12.stIII;

            short so2_V1 = driSR.ext1.ecg12.stV1;
            short so2_V2 = driSR.ext1.ecg12.stV2;
            short so2_V3 = driSR.ext1.ecg12.stV3;
            short so2_V4 = driSR.ext1.ecg12.stV4;
            short so2_V5 = driSR.ext1.ecg12.stV5;
            short so2_V6 = driSR.ext1.ecg12.stV6;

            short so3_AVL = driSR.ext1.ecg12.stAVL;
            short so3_AVR = driSR.ext1.ecg12.stAVR;
            short so3_AVF = driSR.ext1.ecg12.stAVF;

            string s1_I = m_ResultDataBlock.ValidateAndAddData("ST_I", so1_I, 0.01, false, "{0:0.00}");
            string s1_II = m_ResultDataBlock.ValidateAndAddData("ST_II", so1_II, 0.01, false, "{0:0.00}");
            string s1_III = m_ResultDataBlock.ValidateAndAddData("ST_II", so1_III, 0.01, false, "{0:0.00}");

            string s2_V1 = m_ResultDataBlock.ValidateAndAddData("ST_V1", so2_V1, 0.01, false, "{0:0.00}");
            string s2_V2 = m_ResultDataBlock.ValidateAndAddData("ST_V2", so2_V2, 0.01, false, "{0:0.00}");
            string s2_V3 = m_ResultDataBlock.ValidateAndAddData("ST_V3", so2_V3, 0.01, false, "{0:0.00}");
            string s2_V4 = m_ResultDataBlock.ValidateAndAddData("ST_V4", so2_V4, 0.01, false, "{0:0.00}");
            string s2_V5 = m_ResultDataBlock.ValidateAndAddData("ST_V5", so2_V5, 0.01, false, "{0:0.00}");
            string s2_V6 = m_ResultDataBlock.ValidateAndAddData("ST_V6", so2_V6, 0.01, false, "{0:0.00}");

            string s3_AVL = m_ResultDataBlock.ValidateAndAddData("ST_aVL", so3_AVL, 0.01, false, "{0:0.00}");
            string s3_AVR = m_ResultDataBlock.ValidateAndAddData("ST_aVR", so3_AVR, 0.01, false, "{0:0.00}");
            string s3_AVF = m_ResultDataBlock.ValidateAndAddData("ST_aVF", so3_AVF, 0.01, false, "{0:0.00}");

            short so4 = driSR.ext2.ent.eeg_ent;
            short so5 = driSR.ext2.ent.emg_ent;
            short so6 = driSR.ext2.ent.bsr_ent;
            short so7 = driSR.ext2.eeg_bis.bis;
            short so8 = driSR.ext2.eeg_bis.sr_val;
            short so9 = driSR.ext2.eeg_bis.emg_val;
            short so10 = driSR.ext2.eeg_bis.sqi_val;

            m_ResultDataBlock.ValidateAndAddData("EEG_Entropy", so4, 1, true);
            m_ResultDataBlock.ValidateAndAddData("EMG_Entropy", so5, 1, true);
            m_ResultDataBlock.ValidateAndAddData("BSR_Entropy", so6, 1, true);
            m_ResultDataBlock.ValidateAndAddData("BIS", so7, 1, true);
            m_ResultDataBlock.ValidateAndAddData("BIS_BSR", so8, 1, true);
            m_ResultDataBlock.ValidateAndAddData("BIS_EMG", so9, 1, true);
            m_ResultDataBlock.ValidateAndAddData("BIS_SQI", so10, 1, true);

            log.Info(String.Format("ST I {0:0.0}mm ST II {0:0.0}mm ST III {0:0.0}mm ST aVL {2:0.0}mm", s1_I, s1_II, s1_III, s2_V5, s3_AVL));
            log.Info(String.Format("ST V1 {1:0.0}mm ST V2 {1:0.0}mm ST V3 {1:0.0}mm ST V4 {1:0.0}mm ST V5 {1:0.0}mm ST V6 {1:0.0}mm",
                s2_V1, s2_V2, s2_V3, s2_V4, s2_V5, s2_V6));

            short so11 = driSR.ext2.nmt2.count;
            short so12 = driSR.ext2.nmt2.nmt_t1;
            short so13 = driSR.ext2.nmt2.nmt_t2;
            short so14 = driSR.ext2.nmt2.nmt_t3;
            short so15 = driSR.ext2.nmt2.nmt_t4;

            double so16 = driSR.ext3.depl.spv;
            double so17 = driSR.ext3.depl.ppv;
            short so18 = driSR.ext3.aa2.mac_age_sum;

            m_ResultDataBlock.ValidateAndAddData("NMT_Count", so11, 1, true);
            m_ResultDataBlock.ValidateAndAddData("NMT_T1", so12, 1, false);
            m_ResultDataBlock.ValidateAndAddData("NMT_T2", so13, 1, false);
            m_ResultDataBlock.ValidateAndAddData("NMT_T3", so14, 1, false);
            m_ResultDataBlock.ValidateAndAddData("NMT_T4", so15, 1, false);

            m_ResultDataBlock.ValidateAndAddData("SPV", so16, 0.01, false, "{0:0.0}");
            m_ResultDataBlock.ValidateAndAddData("PPV", so17, 0.01, false, "{0:0.0}");
            m_ResultDataBlock.ValidateAndAddData("MAC_AGE_SUM", so18, 1, false, "{0:0.00}");
        }

        public void AddDataString(string physio_id, string valuestr)
        {
            m_ResultDataBlock.Values.Add(new ReceivedDataValue(physio_id, valuestr));
        }

        public double GetWaveUnitShift(string DataType)
        {
            double decimalshift = 1;

            if (DataType.Contains("ECG") == true)
                return (decimalshift = 0.01);
            if (DataType.Contains("INVP") == true)
                return (decimalshift = 0.01);
            if (DataType.Contains("PLETH") == true)
                return (decimalshift = 0.01);
            if (DataType.Contains("CO2") == true)
                return (decimalshift = 0.01);
            if (DataType.Contains("O2") == true)
                return (decimalshift = 0.01);
            if (DataType.Contains("RESP") == true)
                return (decimalshift = 0.01);
            if (DataType.Contains("AA") == true)
                return (decimalshift = 0.01);
            if (DataType.Contains("FLOW") == true)
                return (decimalshift = 0.01);
            if (DataType.Contains("AWP") == true)
                return (decimalshift = 0.1);
            if (DataType.Contains("VOL") == true)
                return (decimalshift = -1);
            if (DataType.Contains("EEG") == true)
                return (decimalshift = 1);
            if (DataType.Contains("ENT") == true)
                return (decimalshift = 0.1);
            else return decimalshift;

        }

        public bool ByteArrayToFile(string _FileName, byte[] _ByteArray, int nWriteLength)
        {
            try
            {
                // Open file for reading. 
                using (FileStream _FileStream = new FileStream(_FileName, FileMode.Append, FileAccess.Write))
                {
                    // Writes a block of bytes to this stream using data from a byte array
                    _FileStream.Write(_ByteArray, 0, nWriteLength);

                    // close file stream. 
                    _FileStream.Close();
                }

                return true;
            }

            catch (Exception _Exception)
            {
                // Error. 
                log.Error(String.Format("Exception caught in process: {0}", _Exception.ToString()), _Exception);
            }
            // error occured, return false. 
            return false;
        }

        public bool OSIsUnix()
        {
            int p = (int)Environment.OSVersion.Platform;
            if ((p == 4) || (p == 6) || (p == 128)) return true;
            else return false;

        }
    }

}
