/*
 * This file is part of VitalSignsCaptureWave v1.011.
 * Copyright (C) 2015-22 John George K., xeonfusion@users.sourceforge.net
 * Portions of code (C) 1998 Stefan Lombaard

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
using System.IO.Ports;
using System.Runtime.InteropServices;

namespace VSCaptureWave
{
    #region

    // Datex data types
    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 3, CharSet = CharSet.Ansi)]
    public class sr_desc_type
    {
        public short sr_offset;
        public byte sr_type;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 32, CharSet = CharSet.Ansi)]
    public class wf_req
    {
        public short req_type;
        public short res;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] type = new byte[8];
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
        public short[] reserved = new short[10];

    };

    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 6, CharSet = CharSet.Ansi)]
    public class wf_hdr
    {
        public short act_len;
        public ushort status;
        public ushort reserved;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 40, CharSet = CharSet.Ansi)]
    [Serializable]
    public class datex_hdr_type
    {
        public short r_len;
        public byte r_nbr;
        public byte r_dri_level;
        public ushort plug_id;
        public uint r_time;
        public byte reserved1;
        public byte reserved2;
        public ushort reserved3;
        public short r_maintype;
        //public sr_desc_type[] sr_desc = new sr_desc_type[8];
        public short sr_offset1;
        public byte sr_type1;
        public short sr_offset2;
        public byte sr_type2;
        public short sr_offset3;
        public byte sr_type3;
        public short sr_offset4;
        public byte sr_type4;
        public short sr_offset5;
        public byte sr_type5;
        public short sr_offset6;
        public byte sr_type6;
        public short sr_offset7;
        public byte sr_type7;
        public short sr_offset8;
        public byte sr_type8;

    };

    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 1490, CharSet = CharSet.Ansi)]
    public class datex_record_type
    {
        public datex_hdr_type hdr = new datex_hdr_type();
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1450)]
        public byte[] data = new byte[1450];
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 49, CharSet = CharSet.Ansi)]
    public class datex_tx_type
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 49)]
        public byte[] data = new byte[49];
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 72, CharSet = CharSet.Ansi)]
    public class datex_wave_tx_type
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 72)]
        public byte[] data = new byte[72];
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 49, CharSet = CharSet.Ansi)]
    public class datex_record_req_type
    {
        public datex_hdr_type hdr = new datex_hdr_type();
        public phdb_req_type phdbr = new phdb_req_type();
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 72, CharSet = CharSet.Ansi)]
    public class datex_record_wave_req_type
    {
        public datex_hdr_type hdr = new datex_hdr_type();
        public wf_req wfreq = new wf_req();
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 9, CharSet = CharSet.Ansi)]
    public class phdb_req_type
    {
        public byte phdb_rcrd_type;
        public short tx_interval;
        public uint phdb_class_bf;
        public short reserved;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 6, CharSet = CharSet.Ansi)]
    public struct group_hdr_type
    {
        public uint status_bits;
        public ushort label_info;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 16, CharSet = CharSet.Ansi)]
    public class ecg_group_type
    {
        public group_hdr_type hdr = new group_hdr_type();
        public short hr;
        public short st1;
        public short st2;
        public short st3;
        public short imp_rr;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 14, CharSet = CharSet.Ansi)]
    public class p_group_type
    {
        public group_hdr_type hdr = new group_hdr_type();
        public short sys;
        public short dia;
        public short mean;
        public short hr;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 14, CharSet = CharSet.Ansi)]
    public class nibp_group_type
    {
        public group_hdr_type hdr = new group_hdr_type();
        public short sys;
        public short dia;
        public short mean;
        public short hr;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 8, CharSet = CharSet.Ansi)]
    public class t_group_type
    {
        public group_hdr_type hdr = new group_hdr_type();
        public short temp;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 14, CharSet = CharSet.Ansi)]
    public class SpO2_group_type
    {
        public group_hdr_type hdr = new group_hdr_type();
        public short SpO2;
        public short pr;
        public short ir_amp;
        public short svo2;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 14, CharSet = CharSet.Ansi)]
    public class co2_group_type
    {
        public group_hdr_type hdr = new group_hdr_type();
        public short et;
        public short fi;
        public short rr;
        public short amb_press;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 10, CharSet = CharSet.Ansi)]
    public class o2_group_type
    {
        public group_hdr_type hdr = new group_hdr_type();
        public short et;
        public short fi;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 10, CharSet = CharSet.Ansi)]
    public class n2o_group_type
    {
        public group_hdr_type hdr = new group_hdr_type();
        public short et;
        public short fi;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 12, CharSet = CharSet.Ansi)]
    public class aa_group_type
    {
        public group_hdr_type hdr = new group_hdr_type();
        public short et;
        public short fi;
        public short mac_sum;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 22, CharSet = CharSet.Ansi)]
    public class flow_vol_group_type
    {
        public group_hdr_type hdr = new group_hdr_type();
        public short rr;
        public short ppeak;
        public short peep;
        public short pplat;
        public short tv_insp;
        public short tv_exp;
        public short compliance;
        public short mv_exp;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 14, CharSet = CharSet.Ansi)]
    public class co_wedge_group_type
    {
        public group_hdr_type hdr = new group_hdr_type();
        public short co;
        public short blood_temp;
        public short rightef;
        public short pcwp;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 12, CharSet = CharSet.Ansi)]
    public class nmt_group
    {
        public group_hdr_type hdr = new group_hdr_type();
        public short t1;
        public short tratio;
        public short ptc;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 6, CharSet = CharSet.Ansi)]
    public class ecg_extra_group
    {
        public short hr_ecg;
        public short hr_max;
        public short hr_min;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 8, CharSet = CharSet.Ansi)]
    public class svo2_group
    {
        public group_hdr_type hdr = new group_hdr_type();
        public short svo2;
    };


    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 270, CharSet = CharSet.Ansi)]
    public class basic_phdb_type
    {
        //uint time;
        public ecg_group_type ecg = new ecg_group_type();
        //public p_group_type[] p1234 = new p_group_type[4];
        public p_group_type p1 = new p_group_type();
        public p_group_type p2 = new p_group_type();
        public p_group_type p3 = new p_group_type();
        public p_group_type p4 = new p_group_type();
        public nibp_group_type nibp;
        //public t_group_type[] t = new t_group_type[4];
        public t_group_type t1 = new t_group_type();
        public t_group_type t2 = new t_group_type();
        public t_group_type t3 = new t_group_type();
        public t_group_type t4 = new t_group_type();
        public SpO2_group_type SpO2 = new SpO2_group_type();
        public co2_group_type co2 = new co2_group_type();
        public o2_group_type o2 = new o2_group_type();
        public n2o_group_type n2o = new n2o_group_type();
        public aa_group_type aa = new aa_group_type();
        public flow_vol_group_type flow_vol = new flow_vol_group_type();
        public co_wedge_group_type co_wedge = new co_wedge_group_type();
        public nmt_group nmt = new nmt_group();
        public ecg_extra_group ecg_extra = new ecg_extra_group();
        public svo2_group svo2 = new svo2_group();
        //public p_group_type[] p56 = new p_group_type[2];
        public p_group_type p5 = new p_group_type();
        public p_group_type p6 = new p_group_type();
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public byte[] reserved = new byte[2];

    };

    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 48, CharSet = CharSet.Ansi)]
    public class arrh_ecg_group
    {
        public group_hdr_type hdr = new group_hdr_type();
        public short hr;
        public short rr_time;
        public short pvc;
        public uint arrh_reserved;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public short[] reserved = new short[16];

    };

    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 30, CharSet = CharSet.Ansi)]
    public class ecg_12_group
    {
        public group_hdr_type hdr = new group_hdr_type();
        public short stI;
        public short stII;
        public short stIII;
        public short stAVL;
        public short stAVR;
        public short stAVF;
        public short stV1;
        public short stV2;
        public short stV3;
        public short stV4;
        public short stV5;
        public short stV6;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 270, CharSet = CharSet.Ansi)]
    public class ext1_phdb
    {
        public arrh_ecg_group ecg = new arrh_ecg_group();
        public ecg_12_group ecg12 = new ecg_12_group();
        public p_group_type p7 = new p_group_type();
        public p_group_type p8 = new p_group_type();
        public SpO2_group_type SpO2_ch2 = new SpO2_group_type();
        public t_group_type t5 = new t_group_type();
        public t_group_type t6 = new t_group_type();
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 134)]
        public byte[] reserved = new byte[134];

    };

    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 24, CharSet = CharSet.Ansi)]
    public class nmt2_group
    {
        public group_hdr_type hdr = new group_hdr_type();
        public short count;
        public short nmt_t1;
        public short nmt_t2;
        public short nmt_t3;
        public short nmt_t4;
        public short nmt_resv1;
        public short nmt_resv2;
        public short nmt_resv3;
        public short nmt_resv4;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 16, CharSet = CharSet.Ansi)]
    public class eeg_channel
    {
        public short ampl;
        public short sef;
        public short mf;
        public short delta_proc;
        public short theta_proc;
        public short alpha_proc;
        public short beta_proc;
        public short bsr;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 72, CharSet = CharSet.Ansi)]
    public class eeg_group
    {
        public group_hdr_type hdr = new group_hdr_type();
        public short femg;
        public eeg_channel eeg1;
        public eeg_channel eeg2;
        public eeg_channel eeg3;
        public eeg_channel eeg4;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 16, CharSet = CharSet.Ansi)]
    public class eeg_bis_group
    {
        public group_hdr_type hdr = new group_hdr_type();
        public short bis;
        public short sqi_val;
        public short emg_val;
        public short sr_val;
        public short reserved;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 28, CharSet = CharSet.Ansi)]
    public class entropy_group
    {
        public group_hdr_type hdr = new group_hdr_type();
        public short eeg_ent;
        public short emg_ent;
        public short bsr_ent;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public short[] reserved = new short[8];

    };

    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 28, CharSet = CharSet.Ansi)]
    public class entropyrd_group
    {
        public group_hdr_type hdr = new group_hdr_type();
        public short eeg_ent;
        public short emg_ent;
        public short bsr_ent;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public short[] reserved = new short[8];
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 32, CharSet = CharSet.Ansi)]
    public class eeg2_group
    {
        public group_hdr_type hdr = new group_hdr_type();
        public byte common_reference;
        public byte montage_label_ch_1_m;
        public byte montage_label_ch_1_p;
        public byte montage_label_ch_2_m;
        public byte montage_label_ch_2_p;
        public byte montage_label_ch_3_m;
        public byte montage_label_ch_3_p;
        public byte montage_label_ch_4_m;
        public byte montage_label_ch_4_p;
        public byte reserved_byte;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public short[] reserved = new short[8];

    };

    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 16, CharSet = CharSet.Ansi)]
    public class spi_group
    {
        public group_hdr_type hdr = new group_hdr_type();
        public short spiVal;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public short[] reserved = new short[4];
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 270, CharSet = CharSet.Ansi)]
    public class ext2_phdb
    {
        public nmt2_group nmt2 = new nmt2_group();
        public eeg_group eeg = new eeg_group();
        public eeg_bis_group eeg_bis = new eeg_bis_group();
        public entropy_group ent = new entropy_group();
        public entropyrd_group ent_rd = new entropyrd_group();
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 30)]
        public byte[] reserved1 = new byte[30];
        public eeg2_group eeg2 = new eeg2_group();
        public spi_group spi = new spi_group();
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 24)]
        public byte[] reserved = new byte[24];

    };

    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 14, CharSet = CharSet.Ansi)]
    public class gasex_group
    {
        public group_hdr_type hdr = new group_hdr_type();
        public short vo2;
        public short vco2;
        public short ee;
        public short rq;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 46, CharSet = CharSet.Ansi)]
    public class flow_vol_group2
    {
        public group_hdr_type hdr = new group_hdr_type();
        public short ipeep;
        public short pmean;
        public short raw;
        public short mv_insp;
        public short epeep;
        public short mv_spont;
        public short ie_ratio;
        public short insp_time;
        public short exp_time;
        public short static_compliance;
        public short static_pplat;
        public short static_peepe;
        public short static_peepi;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 7)]
        public short[] reserved = new short[7];

    };

    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 10, CharSet = CharSet.Ansi)]
    public class bal_gas_group
    {
        public group_hdr_type hdr = new group_hdr_type();
        public short et;
        public short fi;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 22, CharSet = CharSet.Ansi)]
    public class tono_group
    {
        public group_hdr_type hdr = new group_hdr_type();
        public short prco2;
        public short pr_et;
        public short pr_pa;
        public short pa_delay;
        public short phi;
        public short phi_delay;
        public short amb_press;
        public short cpma;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 24, CharSet = CharSet.Ansi)]
    public class aa2_group
    {
        public group_hdr_type hdr = new group_hdr_type();
        public short mac_age_sum;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] reserved = new byte[16];

    };

    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 10, CharSet = CharSet.Ansi)]
    public class delp_group
    {
        public group_hdr_type hdr = new group_hdr_type();
        public short spv;
        public short ppv;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 8, CharSet = CharSet.Ansi)]
    public class cpp_group
    {
        public group_hdr_type hdr = new group_hdr_type();
        public short value;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 8, CharSet = CharSet.Ansi)]
    public class cpp2_group
    {
        public group_hdr_type hdr = new group_hdr_type();
        public short value;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 54, CharSet = CharSet.Ansi)]
    public class picco_group
    {
        public group_hdr_type hdr = new group_hdr_type();
        public short cci;
        public short cco;
        public short cfi;
        public short ci;
        public short co;
        public short cpi;
        public short cpo;
        public short dpmax;
        public short elwi;
        public short evlw;
        public short gedi;
        public short gedv;
        public short gef;
        public short itbi;
        public short itbv;
        public short ppv;
        public short pvpi;
        public short sv;
        public short svi;
        public short svr;
        public short svri;
        public short svv;
        public short tblood;
        public short tinj;

    };

    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 270, CharSet = CharSet.Ansi)]
    public class ext3_phdb
    {
        public gasex_group gasex = new gasex_group();
        public flow_vol_group2 flow_vol2 = new flow_vol_group2();
        public bal_gas_group bal = new bal_gas_group();
        public tono_group tono = new tono_group();
        public aa2_group aa2 = new aa2_group();
        public delp_group depl = new delp_group();
        public cpp_group cpp = new cpp_group();
        public cpp2_group cpp2 = new cpp2_group();
        public picco_group picco = new picco_group();
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 74)]
        public byte[] reserved = new byte[74];

    };

    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 1088, CharSet = CharSet.Ansi)]
    public class dri_phdb
    {
        public uint time;
        public basic_phdb_type basic = new basic_phdb_type();
        public ext1_phdb ext1 = new ext1_phdb();
        public ext2_phdb ext2 = new ext2_phdb();
        public ext3_phdb ext3 = new ext3_phdb();
        public byte marker;
        public byte reserved;
        public ushort cl_drilvl_subt;
    };


    //*********************************************************************


    static class DataConstants
    {

        // Data validity macros

        public const int DATA_INVALID_LIMIT = (-32001);	/* limit for special invalid data values */
        public const int DATA_INVALID = (-32767);	/* there is no valid data */
        public const int DATA_NOT_UPDATED = (-32766);	/* data is not updated */
        public const int DATA_DISCONT = (-32765);	/* data discontinuity (calibration ...) */
        public const int DATA_UNDER_RANGE = (-32764);	/* data exceeds lower valid limit */
        public const int DATA_OVER_RANGE = (-32763);	/* data exceeds upper valid limit */
        public const int DATA_NOT_CALIBRATED = (-32762);	/* data is not calibrated */


        /*---------- General definitions ---------*/

        /*
        **	Asynchronous interface specific constants
        */
        public const byte FRAMECHAR = 0x7E;
        public const byte CTRLCHAR = 0x7D;
        public const byte BIT5 = 0x7C;
        public const byte BIT5COMPL = 0x5F;


        /*---------- Datex Record Interface data structure definitions ----------*/

        public const int DRI_MAX_SUBRECS = 8;	/* # of subrecords in a packet */
        public const int DRI_MAX_PHDBRECS = 5;	/* # of phys.db records in a packet */

        /* data packet maintypes */

        public const short DRI_MT_PHDB = 0; //Physiological data and related transmission requests.
        public const short DRI_MT_WAVE = 1; //Waveform data and related transmission requests.
        public const short DRI_MT_ALARM = 4; //Alarm data and related transmission requests.

        /* data packet subtypes  */

        public const byte DRI_PH_DISPL = 1;
        public const byte DRI_PH_10S_TREND = 2;
        public const byte DRI_PH_60S_TREND = 3;

        public const uint DRI_PHDBCL_REQ_BASIC_MASK = 0x0000; //Enable sending of Basic physiological data class
        public const uint DRI_PHDBCL_DENY_BASIC_MASK = 0x0001; //Disable sending of Basic physiological data class
        public const uint DRI_PHDBCL_REQ_EXT1_MASK = 0x0002; //Enable sending of Ext1 physiological data class
        public const uint DRI_PHDBCL_REQ_EXT2_MASK = 0x0004; //Enable sending of Ext2 physiological data class
        public const uint DRI_PHDBCL_REQ_EXT3_MASK = 0x0008; //Enable sending of Ext3 physiological data class

        /* Datex Record Interface level types */
        public const byte DRI_LEVEL_95 = 0x02;
        public const byte DRI_LEVEL_97 = 0x03;
        public const byte DRI_LEVEL_98 = 0x04;
        public const byte DRI_LEVEL_99 = 0x05;
        public const byte DRI_LEVEL_2000 = 0x06;
        public const byte DRI_LEVEL_2001 = 0x07;
        public const byte DRI_LEVEL_2003 = 0x08;
        public const byte DRI_LEVEL_2005 = 0x09;
        public const byte DRI_LEVEL_2009 = 0x0A;
        public const byte DRI_LEVEL_2015 = 0x0B;


        public const short WF_REQ_CONT_START = 0;
        public const short WF_REQ_CONT_STOP = 1;

        public const byte DRI_EOL_SUBR_LIST = 0xFF;

        public const byte DRI_WF_CMD = 0;
        public const byte DRI_WF_ECG1 = 1;
        public const byte DRI_WF_ECG2 = 2;
        public const byte DRI_WF_ECG3 = 3;
        public const byte DRI_WF_INVP1 = 4;
        public const byte DRI_WF_INVP2 = 5;
        public const byte DRI_WF_INVP3 = 6;
        public const byte DRI_WF_INVP4 = 7;
        public const byte DRI_WF_PLETH = 8;
        public const byte DRI_WF_CO2 = 9;
        public const byte DRI_WF_O2 = 10;
        public const byte DRI_WF_N2O = 11;
        public const byte DRI_WF_AA = 12;
        public const byte DRI_WF_AWP = 13;
        public const byte DRI_WF_FLOW = 14;
        public const byte DRI_WF_RESP = 15;
        public const byte DRI_WF_INVP5 = 16;
        public const byte DRI_WF_INVP6 = 17;
        public const byte DRI_WF_EEG1 = 18;
        public const byte DRI_WF_EEG2 = 19;
        public const byte DRI_WF_EEG3 = 20;
        public const byte DRI_WF_EEG4 = 21;
        public const byte DRI_WF_VOL = 23;
        public const byte DRI_WF_TONO_PRESS = 24;
        public const byte DRI_WF_SPI_LOOP_STATUS = 29;
        public const byte DRI_WF_ENT_100 = 32;
        public const byte DRI_WF_EEG_BIS = 35;

        public enum WavesIDLabels : byte
        {
            DRI_WF_CMD = 0,
            DRI_WF_ECG1 = 1,
            DRI_WF_ECG2 = 2,
            DRI_WF_ECG3 = 3,
            DRI_WF_INVP1 = 4,
            DRI_WF_INVP2 = 5,
            DRI_WF_INVP3 = 6,
            DRI_WF_INVP4 = 7,
            DRI_WF_PLETH = 8,
            DRI_WF_CO2 = 9,
            DRI_WF_O2 = 10,
            DRI_WF_N2O = 11,
            DRI_WF_AA = 12,
            DRI_WF_AWP = 13,
            DRI_WF_FLOW = 14,
            DRI_WF_RESP = 15,
            DRI_WF_INVP5 = 16,
            DRI_WF_INVP6 = 17,
            DRI_WF_EEG1 = 18,
            DRI_WF_EEG2 = 19,
            DRI_WF_EEG3 = 20,
            DRI_WF_EEG4 = 21,
            DRI_WF_VOL = 23,
            DRI_WF_TONO_PRESS = 24,
            DRI_WF_SPI_LOOP_STATUS = 29,
            DRI_WF_ENT_100 = 32,
            DRI_WF_EEG_BIS = 35

        }

        [Flags]
        public enum stim_types
        {
            TOF = 0,
            DBS = 1,
            ST_STIM = 2,
            PTC_STIM = 3,
            NR_STIM_TYPES = 4
        }

        [Flags]
        enum pulse_width_types
        {
            PULSE_NOT_USED = 0,
            PULSE_100 = 1,
            PULSE_200 = 2,
            PULSE_300 = 3,
            PULSE_NR = 4
        }

    }


    #endregion



}
