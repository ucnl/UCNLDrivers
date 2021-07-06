using System;
using System.IO.Ports;
using System.Text;
using System.Threading;

namespace UCNLDrivers
{
    #region Custom eventArgs

    public class RawDataReceivedEventArgs : EventArgs
    {
        #region Properties

        public byte[] Data { get; private set; }

        #endregion

        #region Constructor

        public RawDataReceivedEventArgs(byte[] data)
        {
            Data = data;
        }

        #endregion
    }

    #endregion

    public sealed class NMEASerialPort : NMEAPort, IDisposable
    {
        #region Properties

        bool disposed = false;
        int writeLock = 0;
        int readLock = 0;

        bool pendingClose = false;

        SerialPort serialPort;

        public SerialPortSettings PortSettings { get; private set; }

        SerialErrorReceivedEventHandler serialErrorReceivedHandler;
        SerialDataReceivedEventHandler serialDataReceivedHandler;

        #region NMEAPort

        public override bool IsOpen
        {
            get { return serialPort.IsOpen; }
        }

        public string PortName
        {
            get { return serialPort.PortName; }
            set
            {
                if (!serialPort.IsOpen)
                    serialPort.PortName = value;
                else
                    throw new InvalidOperationException("Unable to change port name while it is open");
            }
        }

        public BaudRate PortBaudRate
        {
            get { return (BaudRate)Enum.ToObject(typeof(BaudRate), serialPort.BaudRate); }
            set
            {
                if (!serialPort.IsOpen)
                    serialPort.BaudRate = (int)value;
                else
                    throw new InvalidOperationException("Unable to change port baudrate while it it open");
            }
        }



        public bool IsRawModeOnly { get; set; }        

        #endregion

        #endregion

        #region Constructor

        public NMEASerialPort(SerialPortSettings portSettings)
            : base()
        {
            #region serialPort initialization

            if (portSettings == null)
                throw new ArgumentNullException("portSettings");

            PortSettings = portSettings;

            serialPort = new SerialPort(
                PortSettings.PortName,
                (int)PortSettings.PortBaudRate,
                PortSettings.PortParity,
                (int)PortSettings.PortDataBits,
                PortSettings.PortStopBits);            

            serialPort.Handshake = portSettings.PortHandshake;
            serialPort.Encoding = Encoding.ASCII;
            serialPort.WriteTimeout = 1000;
            serialPort.ReadTimeout = 1000;

            serialPort.ReceivedBytesThreshold = 1;            

            serialErrorReceivedHandler = new SerialErrorReceivedEventHandler(serialPort_ErrorReceived);
            serialDataReceivedHandler = new SerialDataReceivedEventHandler(serialPort_DataReceived);

            #endregion
        }

        #endregion

        #region Methods

        #region NMEAPort

        public override void Open()
        {
            while (Interlocked.CompareExchange(ref writeLock, 1, 0) != 0)
                Thread.SpinWait(1);

            while (Interlocked.CompareExchange(ref readLock, 1, 0) != 0)
                Thread.SpinWait(1);

            try
            {
                OnConnectionOpening();
                serialPort.ErrorReceived += serialErrorReceivedHandler;
                serialPort.DataReceived += serialDataReceivedHandler;
                serialPort.Open();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(string.Format("Error occured during opening port {1}: {0}", ex.Message, serialPort.PortName));
            }
            finally
            {
                Interlocked.Decrement(ref readLock);
                Interlocked.Decrement(ref writeLock);
            }
        }

        public override void Close()
        {
            serialPort.ErrorReceived -= serialErrorReceivedHandler;
            serialPort.DataReceived -= serialDataReceivedHandler;

            while (Interlocked.CompareExchange(ref writeLock, 1, 0) != 0)
                Thread.SpinWait(1);

            while (Interlocked.CompareExchange(ref readLock, 1, 0) != 0)
                Thread.SpinWait(1);

            try
            {
                pendingClose = true;
                serialPort.DiscardInBuffer();
                serialPort.DiscardOutBuffer();                
                OnConnectionClosing();

                new Thread(() =>
                {
                    try
                    {
                        serialPort.Close();
                    }
                    catch { }
                }).Start();

                Thread.Sleep(1000); 
                pendingClose = false;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(string.Format("Error occured during closing port {1}: {0}", ex.Message, serialPort.PortName));
            }
            finally
            {
                Interlocked.Decrement(ref readLock);
                Interlocked.Decrement(ref writeLock);
            }
        }

        public override void SendData(string message)
        {

            while (Interlocked.CompareExchange(ref writeLock, 1, 0) != 0)
                Thread.SpinWait(1);

            try
            {
                byte[] bytes = Encoding.ASCII.GetBytes(message);
                serialPort.Write(bytes, 0, bytes.Length);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(string.Format("Error {0} while writing in port {1}", ex.Message, serialPort.PortName));
            }
            finally
            {
                Interlocked.Decrement(ref writeLock);
            }
        }

        public void SendRaw(byte[] data)
        {
            while (Interlocked.CompareExchange(ref writeLock, 1, 0) != 0)
                Thread.SpinWait(1);

            try
            {
                serialPort.Write(data, 0, data.Length);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(string.Format("Error {0} while writing in port {1}", ex.Message, serialPort.PortName));
            }
            finally
            {
                Interlocked.Decrement(ref writeLock);
            }
        }

        #endregion

        #endregion

        #region Handlers

        #region serialPort

        private void serialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (!pendingClose)
            {
                while (Interlocked.CompareExchange(ref readLock, 1, 0) != 0)
                    Thread.SpinWait(1);

                int bytesToRead = serialPort.BytesToRead;
                byte[] buffer = new byte[bytesToRead];
                serialPort.Read(buffer, 0, bytesToRead);

                Interlocked.Decrement(ref readLock);

                RawDataReceived.Rise(this, new RawDataReceivedEventArgs(buffer));

                if (!IsRawModeOnly)
                    //OnIncomingData(buffer);
                    OnIncomingDataEx(Encoding.ASCII.GetString(buffer));
            }
        }

        private void serialPort_ErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            if (!pendingClose)
                PortError.Rise(this, e);
        }

        #endregion

        #endregion

        #region Events

        public EventHandler<SerialErrorReceivedEventArgs> PortError;
        public EventHandler<RawDataReceivedEventArgs> RawDataReceived;

        #endregion

        #region IDisposable

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    serialPort.Dispose();
                }

                disposed = true;
            }
        }

        #endregion
    }
}
