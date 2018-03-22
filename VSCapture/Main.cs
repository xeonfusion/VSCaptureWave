/*
 * This file is part of VitalSignsCaptureWave v1.005.
 * Copyright (C) 2015 John George K., xeonfusion@users.sourceforge.net

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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Ports;
using System.Threading;
using System.Timers;


namespace VSCapture
{
    class Program
    {
        static EventHandler dataEvent;
		
        static void Main(string[] args)
        {
            Console.WriteLine("VitalSignsCaptureWave v1.005 (C)2017 John George K.");
            // Create a new SerialPort object with default settings.
			DSerialPort _serialPort = DSerialPort.getInstance;

            Console.WriteLine("Select the Port to which Datex AS3 Monitor is to be connected, Available Ports:");
            foreach (string s in SerialPort.GetPortNames())
            {
                Console.WriteLine(" {0}", s);
            }


            Console.Write("COM port({0}): ", _serialPort.PortName.ToString());
            string portName = Console.ReadLine();

            if (portName != "")
            {
                // Allow the user to set the appropriate properties.
                _serialPort.PortName = portName;
             }


            try
            {
                _serialPort.Open();
                
				if (_serialPort.OSIsUnix())
                {
                    dataEvent += new EventHandler ((object sender, EventArgs e) => ReadData (sender));
                }
								
				if(!_serialPort.OSIsUnix())
				{
				_serialPort.DataReceived += new SerialDataReceivedEventHandler(p_DataReceived);
				}

                Console.WriteLine("You may now connect the serial cable to the Datex AS3 Monitor");
                Console.WriteLine("Press any key to continue..");
                
				Console.ReadKey(true);
												
                //if (_serialPort.CtsHolding)
                {
                    Console.WriteLine();
                    Console.Write("Enter Numeric data Transmission interval (seconds):");
                    string sInterval = Console.ReadLine();

                    short nInterval = 5;
                    if (sInterval != "") nInterval = Convert.ToInt16(sInterval);

					Console.WriteLine();
					Console.WriteLine("Waveform data Transmission sets:");
					Console.WriteLine("0. None");
					Console.WriteLine("1. ECG1, INVP1, INVP2, PLETH");
					Console.WriteLine("2. ECG1, INVP1, PLETH, CO2, RESP");
					Console.WriteLine("3. ECG1, PLETH, CO2, RESP, AWP, VOL, FLOW");
					Console.WriteLine("4. ECG1, ECG2");
					Console.WriteLine("5. EEG1, EEG2, EEG3, EEG4");
					Console.WriteLine();
					Console.Write("Choose Waveform data Transmission set (0-5):");

					string sWaveformSet = Console.ReadLine();
					short nWaveformSet = 1;
					if (sWaveformSet != "") nWaveformSet = Convert.ToInt16(sWaveformSet);


					Console.WriteLine("Requesting {0} second Transmission from monitor", nInterval);

					//Console.WriteLine("Requesting Transmission from monitor");
                    Console.WriteLine();
                    Console.WriteLine("Data will be written to CSV file AS3ExportData.csv in same folder");

                    //_serialPort.RequestTransfer(DataConstants.DRI_PH_DISPL, nInterval); // Add Request Transmission
								
                    //_serialPort.RequestTransfer(DataConstants.DRI_PH_DISPL, -1); // Add Single Request Transmission
					
					//_serialPort.RequestTransfer(DataConstants.DRI_PH_60S_TREND, 60); // Add Trend Request Transmission

					//Request transfer based on the DRI level of the monitor
					_serialPort.RequestTransfer(DataConstants.DRI_PH_DISPL, nInterval, DataConstants.DRI_LEVEL_2005); // Add Request Transmission
					_serialPort.RequestTransfer(DataConstants.DRI_PH_DISPL, nInterval, DataConstants.DRI_LEVEL_2003); // Add Request Transmission
					_serialPort.RequestTransfer(DataConstants.DRI_PH_DISPL, nInterval, DataConstants.DRI_LEVEL_2001); // Add Request Transmission
					
					//Request only a single waveform 
					//_serialPort.RequestWaveTransfer(DataConstants.DRI_WF_ECG1, DataConstants.WF_REQ_CONT_START);
					//_serialPort.RequestWaveTransfer(DataConstants.DRI_WF_INVP1, DataConstants.WF_REQ_CONT_START);

					//Request upto 8 waveforms but total sample rate should be less than 600 samples/sec
					//Sample rate for ECG is 300, INVP 100, PLETH 100, respiratory 25 each
					byte [] WaveTrtype = new byte[8];

					/*WaveTrtype[0] = DataConstants.DRI_WF_ECG1;
                    //WaveTrtype[1] = DataConstants.DRI_WF_INVP1;
                    WaveTrtype[1] = DataConstants.DRI_WF_VOL;
					//WaveTrtype[2] = DataConstants.DRI_WF_INVP2;
					WaveTrtype[2] = DataConstants.DRI_WF_PLETH;
					WaveTrtype[3] = DataConstants.DRI_WF_CO2;
					WaveTrtype[4] = DataConstants.DRI_WF_RESP;
					WaveTrtype[5] = DataConstants.DRI_WF_AWP;
                    //WaveTrtype[6] = DataConstants.DRI_WF_AA;
                    WaveTrtype[6] = DataConstants.DRI_WF_FLOW;
					WaveTrtype[7] = DataConstants.DRI_EOL_SUBR_LIST;*/

					CreateWaveformSet (nWaveformSet, WaveTrtype);

					if (nWaveformSet !=0)
					{
						Console.WriteLine();
	                    Console.WriteLine("Requesting Waveform data from monitor");
						Console.WriteLine("Waveform data will be written to multiple CSV files in same folder");

						_serialPort.RequestMultipleWaveTransfer ( WaveTrtype, DataConstants.WF_REQ_CONT_START, DataConstants.DRI_LEVEL_2005);
						_serialPort.RequestMultipleWaveTransfer ( WaveTrtype, DataConstants.WF_REQ_CONT_START, DataConstants.DRI_LEVEL_2003);
						_serialPort.RequestMultipleWaveTransfer ( WaveTrtype, DataConstants.WF_REQ_CONT_START, DataConstants.DRI_LEVEL_2001);
					}
                }
                //WaitForSeconds(5);

                Console.WriteLine("Press Escape button to Stop");
				
				if(_serialPort.OSIsUnix()) 
					{
						do
	                    {
	                        if (_serialPort.BytesToRead != 0)
	                        {
	                            dataEvent.Invoke(_serialPort, new EventArgs());
	                        }
	                        
	                        if (Console.KeyAvailable == true)
	                        {
	                            if (Console.ReadKey(true).Key == ConsoleKey.Escape) break;
	                        }
	                    }
	                    while (Console.KeyAvailable == false);
						
					}                
				
				if(!_serialPort.OSIsUnix())
				{
					ConsoleKeyInfo cki;
				
					do
	                {
						cki = Console.ReadKey(true);
					}
	                while (cki.Key != ConsoleKey.Escape);
				}
				

            }
            catch (Exception ex)
            {
                Console.WriteLine("Error opening/writing to serial port :: " + ex.Message, "Error!");
            }
            finally
            {
                _serialPort.StopTransfer();
				
				_serialPort.StopwaveTransfer();

                _serialPort.Close();
				
            }


        }

		
		static void p_DataReceived(object sender, SerialDataReceivedEventArgs e)
		{

			ReadData(sender);

		}

		public static void ReadData(object sender)
		{
			try
			{
				(sender as DSerialPort).ReadBuffer();

			}
			catch (TimeoutException) { }
		}

        public static void WaitForSeconds(int nsec)
        {
            DateTime dt = DateTime.Now;
            DateTime dt2 = dt.AddSeconds(nsec);
            do
            {
                dt = DateTime.Now;
            }
            while (dt2 > dt);

        }

		public static void CreateWaveformSet(int nWaveSetType, byte []WaveTrtype)
		{
			//Request upto 8 waveforms but total sample rate should be less than 600 samples/sec
			//Sample rate for ECG is 300, INVP 100, PLETH 100, respiratory 25 each

			switch (nWaveSetType) {
			case 0:
				break;
			case 1:
				WaveTrtype [0] = DataConstants.DRI_WF_ECG1;
				WaveTrtype [1] = DataConstants.DRI_WF_INVP1;
				WaveTrtype [2] = DataConstants.DRI_WF_INVP2;
				WaveTrtype [3] = DataConstants.DRI_WF_PLETH;
				WaveTrtype [4] = DataConstants.DRI_EOL_SUBR_LIST;
				break;
			case 2:
				WaveTrtype [0] = DataConstants.DRI_WF_ECG1;
				WaveTrtype [1] = DataConstants.DRI_WF_INVP1;
				WaveTrtype [2] = DataConstants.DRI_WF_PLETH;
				WaveTrtype [3] = DataConstants.DRI_WF_CO2;
				WaveTrtype [4] = DataConstants.DRI_WF_RESP;
				WaveTrtype [5] = DataConstants.DRI_EOL_SUBR_LIST;
				break;
			case 3:
				WaveTrtype [0] = DataConstants.DRI_WF_ECG1;
				WaveTrtype [1] = DataConstants.DRI_WF_PLETH;
				WaveTrtype [2] = DataConstants.DRI_WF_CO2;
				WaveTrtype [3] = DataConstants.DRI_WF_RESP;
				WaveTrtype [4] = DataConstants.DRI_WF_AWP;
				WaveTrtype [5] = DataConstants.DRI_WF_VOL;
				WaveTrtype [6] = DataConstants.DRI_WF_FLOW;
				WaveTrtype [7] = DataConstants.DRI_EOL_SUBR_LIST;
				break;
			case 4:
				WaveTrtype [0] = DataConstants.DRI_WF_ECG1;
				WaveTrtype [1] = DataConstants.DRI_WF_ECG2;
				WaveTrtype [2] = DataConstants.DRI_EOL_SUBR_LIST;
				break;
			case 5:
				WaveTrtype [0] = DataConstants.DRI_WF_EEG1;
				WaveTrtype [1] = DataConstants.DRI_WF_EEG2;
				WaveTrtype [2] = DataConstants.DRI_WF_EEG3;
				WaveTrtype [3] = DataConstants.DRI_WF_EEG4;
				WaveTrtype [4] = DataConstants.DRI_EOL_SUBR_LIST;
				break;
			default:
				WaveTrtype [0] = DataConstants.DRI_WF_ECG1;
				WaveTrtype [1] = DataConstants.DRI_WF_INVP1;
				WaveTrtype [2] = DataConstants.DRI_WF_INVP2;
				WaveTrtype [3] = DataConstants.DRI_WF_PLETH;
				WaveTrtype [4] = DataConstants.DRI_EOL_SUBR_LIST;
				break;
			}



		}


    }


}