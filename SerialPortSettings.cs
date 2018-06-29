using System;
using System.IO.Ports;
using System.Text;

namespace UCNLDrivers
{
    #region Enums

    public enum BaudRate : int
    {
        baudRate75 = 75,
        baudRate110 = 110,
        baudRate134 = 134,
        baudRate150 = 150,
        baudRate300 = 300,
        baudRate600 = 600,
        baudRate1200 = 1200,
        baudRate1800 = 1800,
        baudRate2400 = 2400,
        baudRate4800 = 4800,
        baudRate7200 = 7200,
        baudRate9600 = 9600,
        baudRate14400 = 14400,
        baudRate19200 = 19200,
        baudRate38400 = 38400,
        baudRate57600 = 57600,
        baudRate115200 = 115200,
        baudRate128000 = 128000
    }

    public enum DataBits
    {
        dataBits4 = 4,
        dataBits5 = 5,
        dataBits6 = 6,
        dataBits7 = 7,
        dataBits8 = 8,
        dataBits9 = 9
    }

    #endregion

    [Serializable]
    public sealed class SerialPortSettings
    {
        #region Properties

        public string PortName { get; set; }

        public BaudRate PortBaudRate { get; set; }

        public Parity PortParity { get; set; }

        public DataBits PortDataBits { get; set; }

        public StopBits PortStopBits { get; set; }

        public Handshake PortHandshake { get; set; }

        #endregion

        #region Constructor
 
        public SerialPortSettings()
            : this("COM1")
        {
        }
        
        public SerialPortSettings(string portNameParameter)
            : this(portNameParameter, BaudRate.baudRate9600, Parity.Even, DataBits.dataBits8, StopBits.One, Handshake.None)
        {
        }
        
        public SerialPortSettings(string portNameParameter,
            BaudRate portBaudRateParameter,
            Parity portParityParameter,
            DataBits portDataBitsParameter,
            StopBits portStopBitsParameter,
            Handshake portHandshakeParameter)
        {
            PortName = portNameParameter;
            PortBaudRate = portBaudRateParameter;
            PortParity = portParityParameter;
            PortDataBits = portDataBitsParameter;
            PortStopBits = portStopBitsParameter;
            PortHandshake = portHandshakeParameter;
        }

        #endregion

        #region Methods

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(PortName);
            sb.Append(" (");
            sb.Append(PortBaudRate.ToString());
            sb.Append(", ");
            sb.Append(PortParity.ToString());
            sb.Append(", ");
            sb.Append(PortDataBits.ToString());
            sb.Append(", ");
            sb.Append(PortStopBits.ToString());
            sb.Append(", ");
            sb.Append(PortHandshake.ToString());
            sb.Append(')');

            return sb.ToString();
        }

        #endregion
    }
}
