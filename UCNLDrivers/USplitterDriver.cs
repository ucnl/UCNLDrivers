using System;
using System.IO.Ports;
using System.Threading;
namespace UCNLDrivers
{
    #region Custom enums

    public enum USPPorts : int
    {
        port_0 = 0,
        port_1 = 1,
        port_2 = 2,
        port_3 = 3,
        port_4 = 4,
        port_5 = 5,
        invalid
    }
    

    #endregion

    #region Custom eventArgs

    public class USPPortDataEventArgs : EventArgs
    {
        #region Properties

        public USPPorts SourceID { get; private set; }
        public byte[] Data { get; private set; }

        #endregion

        #region Constructor

        public USPPortDataEventArgs(USPPorts sourceID, byte[] data)
        {
            SourceID = sourceID;
            Data = data;
        }

        #endregion
    }

    #endregion

    public class USplitterDriver
    {
        #region Properties

        SerialPort port;
        bool pendingClose = false;

        byte inByte, crc;
        int dcIdx, dataSize;

        bool tr_isPacketStarted = false;
        int tr_hdrSignCnt = 0;
        int tr_packetIdx = 0;
        int tr_DataSize = -1;
        int tr_ChId = 0;
        int t_ticks = 0;

        byte[] TR_TX_RING = new byte[TR_CH_TX_BUFFER_SIZE];

        int TR_TX_WPos = 0;
        int TR_TX_Cnt  = 0;
        
        const int PACKET_SIZE = 32;
        const int TR_HEADER_SIGN = 0xB8;
        const int TR_HEADER_SIZE = 4;
        const int TR_OVERHEAD = 5;
        const int TR_CH_ID_OFFSET = 2;
        const int TR_DATA_SIZE_OFFSET = 3;

        const int RX_THRESHOLD = PACKET_SIZE + TR_OVERHEAD;

        const int CM_CH_NUMBER = 6;

        const int TR_CH_TX_BUFFER_SIZE = 4000;
        const int CM_CH_RX_BUFFER_SIZE = 1024;
        const int CM_CH_TX_BUFFER_SIZE = 2048;
            
        const int CH3IDX = 0;
        const int CH4IDX = 1;
        const int CH5IDX = 2;
        const int CH6IDX = 3;
        const int CH7IDX = 4;
        const int CH8IDX = 5;

        byte[][] C_CH_RX_RING = new byte[CM_CH_NUMBER][];
        byte[][] C_CH_TX_RING = new byte[CM_CH_NUMBER][];

        int[] CM_TX_WPos = new int[CM_CH_NUMBER];
        int[] CM_TX_RPos = new int[CM_CH_NUMBER];
        int[] CM_TX_Cnt = new int[CM_CH_NUMBER];
        int[] CM_RX_WPos = new int[CM_CH_NUMBER];
        int[] CM_RX_RPos = new int[CM_CH_NUMBER];
        int[] CM_RX_Cnt = new int[CM_CH_NUMBER];
        int[] CM_RX_Ticks = new int[CM_CH_NUMBER];

        const int RX_DATA_OBSOLETE_MS = 1000;

        static bool IS_VALID_CH_ID(int value)
        {
            return ((value >= CH3IDX) && (value <= CH8IDX));
        }

        static bool IS_VALID_DATA_SIZE(int value)
        {
            return ((value >= 0) && (value < PACKET_SIZE));
        }

        const int CRC8TableSize = 256;

        byte[] CRC8Table = { 0x00, 0x31, 0x62, 0x53, 0xC4, 0xF5, 0xA6, 0x97,
	                         0xB9, 0x88, 0xDB, 0xEA, 0x7D, 0x4C, 0x1F, 0x2E,
	                         0x43, 0x72, 0x21, 0x10, 0x87, 0xB6, 0xE5, 0xD4,
	                         0xFA, 0xCB, 0x98, 0xA9, 0x3E, 0x0F, 0x5C, 0x6D,
	                         0x86, 0xB7, 0xE4, 0xD5, 0x42, 0x73, 0x20, 0x11,
	                         0x3F, 0x0E, 0x5D, 0x6C, 0xFB, 0xCA, 0x99, 0xA8,
	                         0xC5, 0xF4, 0xA7, 0x96, 0x01, 0x30, 0x63, 0x52,
	                         0x7C, 0x4D, 0x1E, 0x2F, 0xB8, 0x89, 0xDA, 0xEB,
	                         0x3D, 0x0C, 0x5F, 0x6E, 0xF9, 0xC8, 0x9B, 0xAA,
	                         0x84, 0xB5, 0xE6, 0xD7, 0x40, 0x71, 0x22, 0x13,
	                         0x7E, 0x4F, 0x1C, 0x2D, 0xBA, 0x8B, 0xD8, 0xE9,
	                         0xC7, 0xF6, 0xA5, 0x94, 0x03, 0x32, 0x61, 0x50,
	                         0xBB, 0x8A, 0xD9, 0xE8, 0x7F, 0x4E, 0x1D, 0x2C,
	                         0x02, 0x33, 0x60, 0x51, 0xC6, 0xF7, 0xA4, 0x95,
	                         0xF8, 0xC9, 0x9A, 0xAB, 0x3C, 0x0D, 0x5E, 0x6F,
	                         0x41, 0x70, 0x23, 0x12, 0x85, 0xB4, 0xE7, 0xD6,
	                         0x7A, 0x4B, 0x18, 0x29, 0xBE, 0x8F, 0xDC, 0xED,
	                         0xC3, 0xF2, 0xA1, 0x90, 0x07, 0x36, 0x65, 0x54,
	                         0x39, 0x08, 0x5B, 0x6A, 0xFD, 0xCC, 0x9F, 0xAE,
	                         0x80, 0xB1, 0xE2, 0xD3, 0x44, 0x75, 0x26, 0x17,
	                         0xFC, 0xCD, 0x9E, 0xAF, 0x38, 0x09, 0x5A, 0x6B,
	                         0x45, 0x74, 0x27, 0x16, 0x81, 0xB0, 0xE3, 0xD2,
	                         0xBF, 0x8E, 0xDD, 0xEC, 0x7B, 0x4A, 0x19, 0x28,
	                         0x06, 0x37, 0x64, 0x55, 0xC2, 0xF3, 0xA0, 0x91,
	                         0x47, 0x76, 0x25, 0x14, 0x83, 0xB2, 0xE1, 0xD0,
	                         0xFE, 0xCF, 0x9C, 0xAD, 0x3A, 0x0B, 0x58, 0x69,
	                         0x04, 0x35, 0x66, 0x57, 0xC0, 0xF1, 0xA2, 0x93,
	                         0xBD, 0x8C, 0xDF, 0xEE, 0x79, 0x48, 0x1B, 0x2A,
	                         0xC1, 0xF0, 0xA3, 0x92, 0x05, 0x34, 0x67, 0x56,
	                         0x78, 0x49, 0x1A, 0x2B, 0xBC, 0x8D, 0xDE, 0xEF,
	                         0x82, 0xB3, 0xE0, 0xD1, 0x46, 0x77, 0x24, 0x15,
	                         0x3B, 0x0A, 0x59, 0x68, 0xFF, 0xCE, 0x9D, 0xAC
                         };

        public bool IsOpen
        {
            get
            {
                return port.IsOpen;
            }                    
        }

        public string PortName
        {
            get
            {
                return port.PortName;
            }
        }

        #endregion

        #region Constructor

        public USplitterDriver(SerialPortSettings portSettings)
        {
            for (int i = 0; i < CM_CH_NUMBER; i++)
            {
                C_CH_RX_RING[i] = new byte[CM_CH_TX_BUFFER_SIZE];
                C_CH_TX_RING[i] = new byte[CM_CH_TX_BUFFER_SIZE];
            }

            port = new SerialPort(portSettings.PortName, (int)portSettings.PortBaudRate, portSettings.PortParity, (int)portSettings.PortDataBits, portSettings.PortStopBits);
            port.DataReceived += new SerialDataReceivedEventHandler(port_DataReceived);
            port.ErrorReceived += new SerialErrorReceivedEventHandler(port_ErrorReceived);
            port.ReadTimeout = 1000;
        }


        #endregion

        #region Methods

        public void Open()
        {
            port.Open();
        }

        public void Close()
        {
            pendingClose = true;
            Thread.Sleep(port.ReadTimeout);
            port.Close();
            pendingClose = false;
        }

        public void Send(USPPorts targetID, byte[] data)
        {
            if (!pendingClose)
            {
                if (targetID != USPPorts.invalid)
                {

                    for (int i = 0; i < data.Length; i++)
                    {
                        C_CH_RX_RING[(int)targetID][CM_RX_WPos[(int)targetID]] = data[i];
                        CM_RX_WPos[(int)targetID] = (CM_RX_WPos[(int)targetID] + 1) % CM_CH_RX_BUFFER_SIZE;
                        CM_RX_Cnt[(int)targetID]++;
                        CM_RX_Ticks[(int)targetID] = DateTime.Now.Second * 1000 + DateTime.Now.Millisecond;


                        #region Combiner

                        for (dcIdx = CH3IDX; dcIdx <= CH8IDX; dcIdx++)
                        {
                            if ((CM_RX_Cnt[dcIdx] == PACKET_SIZE) ||
                                ((CM_RX_Cnt[dcIdx] > 0) && (t_ticks >= CM_RX_Ticks[dcIdx] + RX_DATA_OBSOLETE_MS)))
                            {
                                crc = 0xFF;
                                TR_TX_RING[TR_TX_WPos] = TR_HEADER_SIGN;
                                TR_TX_WPos = (TR_TX_WPos + 1) % TR_CH_TX_BUFFER_SIZE;
                                TR_TX_Cnt++;
                                crc = CRC8Table[crc ^ TR_HEADER_SIGN];

                                TR_TX_RING[TR_TX_WPos] = TR_HEADER_SIGN;
                                TR_TX_WPos = (TR_TX_WPos + 1) % TR_CH_TX_BUFFER_SIZE;
                                TR_TX_Cnt++;
                                crc = CRC8Table[crc ^ TR_HEADER_SIGN];

                                TR_TX_RING[TR_TX_WPos] = Convert.ToByte(dcIdx);
                                TR_TX_WPos = (TR_TX_WPos + 1) % TR_CH_TX_BUFFER_SIZE;
                                TR_TX_Cnt++;
                                crc = CRC8Table[crc ^ dcIdx];

                                dataSize = CM_RX_Cnt[dcIdx] - 1;

                                TR_TX_RING[TR_TX_WPos] = Convert.ToByte(dataSize);
                                TR_TX_WPos = (TR_TX_WPos + 1) % TR_CH_TX_BUFFER_SIZE;
                                TR_TX_Cnt++;
                                crc = CRC8Table[crc ^ dataSize];

                                for (i = 0; i <= dataSize; i++)
                                {
                                    inByte = C_CH_RX_RING[dcIdx][CM_RX_RPos[dcIdx]];
                                    CM_RX_RPos[dcIdx] = (CM_RX_RPos[dcIdx] + 1) % CM_CH_RX_BUFFER_SIZE;
                                    CM_RX_Cnt[dcIdx]--;

                                    TR_TX_RING[TR_TX_WPos] = inByte;
                                    TR_TX_WPos = (TR_TX_WPos + 1) % TR_CH_TX_BUFFER_SIZE;
                                    TR_TX_Cnt++;
                                    crc = CRC8Table[crc ^ inByte];
                                }

                                TR_TX_RING[TR_TX_WPos] = crc;
                                TR_TX_WPos = (TR_TX_WPos + 1) % TR_CH_TX_BUFFER_SIZE;
                                TR_TX_Cnt++;
                            }
                        }

                        #endregion
                    }
                }
                else
                {
                    throw new ArgumentException("targetID");
                }
            }
        }


        #endregion

        #region Handlers

        #region port

        private void port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (!pendingClose)
            {
                byte[] data = new byte[port.BytesToRead];
                port.Read(data, 0, data.Length);

                #region Parser

                for (int i = 0; i < data.Length; i++)
                {
                    inByte = data[i];

                    if (tr_isPacketStarted)
                    {
                        if (tr_packetIdx == TR_CH_ID_OFFSET)
                        {
                            tr_ChId = inByte;
                            tr_isPacketStarted = IS_VALID_CH_ID(tr_ChId);
                        }
                        else if (tr_packetIdx == TR_DATA_SIZE_OFFSET)
                        {
                            tr_DataSize = inByte;
                            tr_isPacketStarted = IS_VALID_DATA_SIZE(tr_DataSize);
                        }
                        else if (tr_packetIdx < tr_DataSize + TR_OVERHEAD)
                        {
                            C_CH_TX_RING[tr_ChId][CM_TX_WPos[tr_ChId]] = inByte;
                            CM_TX_WPos[tr_ChId] = (CM_TX_WPos[tr_ChId] + 1) % CM_CH_TX_BUFFER_SIZE;
                            CM_TX_Cnt[tr_ChId]++;
                        }
                        else
                        {
                            tr_isPacketStarted = false;
                        }

                        tr_packetIdx++;
                    }
                    else
                    {
                        if (inByte == TR_HEADER_SIGN)
                        {
                            if (++tr_hdrSignCnt == 2)
                            {
                                tr_isPacketStarted = true;
                                tr_packetIdx = 2;
                                tr_DataSize = -1;
                                tr_hdrSignCnt = 0;
                            }
                        }
                        else
                        {
                            tr_hdrSignCnt = 0;
                        }
                    }
                }

                #endregion

                #region splitter

                for (int i = CH3IDX; i <= CH8IDX; i++)
                {
                    if (CM_TX_Cnt[i] > 0)
                    {
                        byte[] dataBlock = new byte[CM_TX_Cnt[i]];
                        int dIdx = 0;
                        while (CM_TX_Cnt[i] > 0)
                        {
                            dataBlock[dIdx] = C_CH_TX_RING[i][CM_TX_RPos[i]];
                            CM_TX_RPos[i] = (CM_TX_RPos[i] + 1) % CM_CH_TX_BUFFER_SIZE;
                            CM_TX_Cnt[i]--;
                            dIdx++;
                        }

                        DataReceived.Rise(this, new USPPortDataEventArgs((USPPorts)i, dataBlock));
                    }
                }



                #endregion
            }
        }

        private void port_ErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            if (!pendingClose)
                ErrorReceived.Rise(this, e);
        }

        #endregion

        #endregion

        #region Events

        public EventHandler<USPPortDataEventArgs> DataReceived;
        public EventHandler<SerialErrorReceivedEventArgs> ErrorReceived;

        #endregion
    }
}
