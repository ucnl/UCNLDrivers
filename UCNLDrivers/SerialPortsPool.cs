using System;
using System.Collections.Generic;
using System.IO.Ports;

namespace UCNLDrivers
{
    #region Custom eventArgs

    public class SerialPortDataEventArgs : EventArgs
    {
        #region Properties

        public string PortName { get; private set; }
        public byte[] Data { get; private set; }

        #endregion

        #region Constructor

        public SerialPortDataEventArgs(string portName, byte[] data)
        {
            PortName = portName;
            Data = data;
        }

        #endregion
    }

    public class SerialPortErrorEventArgs : EventArgs
    {
        #region Properties

        public string PortName { get; private set; }
        public SerialError EventType { get; private set; }

        #endregion

        #region Constructor

        public SerialPortErrorEventArgs(string portName, SerialError eventType)
        {
            PortName = portName;
            EventType = eventType;
        }

        #endregion
    }

    #endregion

    public class SerialPortsPool : IDisposable
    {
        #region Properties

        Dictionary<string, SerialPort> ports;

        SerialDataReceivedEventHandler dataReceivedHandler;
        SerialErrorReceivedEventHandler errorReceivedHandler;

        #endregion

        #region Constructor

        public SerialPortsPool(SerialPortSettings[] portSettings)
        {
            ports = new Dictionary<string, SerialPort>();

            dataReceivedHandler = new SerialDataReceivedEventHandler(port_DataReceived);
            errorReceivedHandler = new SerialErrorReceivedEventHandler(port_ErrorReceived);

            foreach (var item in portSettings)
            {
                if (!ports.ContainsKey(item.PortName))
                {
                    var port = new SerialPort(item.PortName, (int)item.PortBaudRate, item.PortParity, (int)item.PortDataBits, item.PortStopBits);                    
                    ports.Add(port.PortName, port);
                }
            }
        }

        #endregion

        #region Methods

        public bool IsPresent(string portName)
        {
            return ports.ContainsKey(portName);
        }

        public bool IsOpen(string portName)
        {
            return ports[portName].IsOpen;
        }
        
        public void Open(string portName)
        {
            if (!ports[portName].IsOpen)
            {
                ports[portName].Open();
                ports[portName].DataReceived += dataReceivedHandler;
                ports[portName].ErrorReceived += errorReceivedHandler;
            }
        }

        public void Open()
        {
            foreach (var port in ports)
            {
                try
                {                    
                    port.Value.Open();
                    port.Value.DataReceived += dataReceivedHandler;
                    port.Value.ErrorReceived += errorReceivedHandler;
                }
                catch (Exception ex)
                {
                    LogEventHandler.Rise(this, new LogEventArgs(LogLineType.ERROR, ex));
                }                
            }
        }

        public void Close()
        {
            foreach (var port in ports)
            {
                if (port.Value.IsOpen)
                {
                    try
                    {
                        port.Value.DataReceived -= dataReceivedHandler;
                        port.Value.ErrorReceived -= errorReceivedHandler;
                        port.Value.Close();
                    }
                    catch (Exception ex)
                    {
                        LogEventHandler.Rise(this, new LogEventArgs(LogLineType.ERROR, ex));
                    }
                }
            }
        }

        public void Close(string portName)
        {
            if (ports[portName].IsOpen)
            {
                ports[portName].DataReceived -= dataReceivedHandler;
                ports[portName].ErrorReceived -= errorReceivedHandler;
                ports[portName].Close();
            }
        }

        public void Write(string portName, byte[] data)
        {
            ports[portName].Write(data, 0, data.Length);
        }

        #endregion

        #region Handlers

        #region ports

        private void port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {            
            byte[] data = new byte[((SerialPort)sender).BytesToRead];
            ((SerialPort)sender).Read(data, 0, data.Length);
            DataReceived.Rise(sender, new SerialPortDataEventArgs(((SerialPort)sender).PortName, data));            
        }

        private void port_ErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            ErrorReceived.Rise(sender, new SerialPortErrorEventArgs(((SerialPort)sender).PortName, e.EventType));
        }

        #endregion

        #endregion

        #region Events

        public EventHandler<SerialPortDataEventArgs> DataReceived;
        public EventHandler<SerialPortErrorEventArgs> ErrorReceived;

        public EventHandler<LogEventArgs> LogEventHandler;

        #endregion

        #region Interfaces

        #region IDisposable

        public void Dispose()
        {
            Close();
            foreach (var port in ports)
            {
                port.Value.Dispose();
            }
        }

        #endregion

        #endregion
    }
}
