using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace UCNLDrivers
{
    public class UDPDataReceivedEventArgs : EventArgs
    {
        #region Properties
        public byte[] Data { get; private set; }

        #endregion

        #region Constructor

        public UDPDataReceivedEventArgs(byte[] data)
        {
            Data = data;
        }

        #endregion
    }

    public class UDPListener
    {
        #region Properties

        IPEndPoint ipEndpoint = null;
        UdpClient udpClient;

        int port;
        bool signalToStopListen = false;

        Thread listenerThread;

        bool _isListening = false;

        public bool IsListening
        {
            get { return _isListening; }
        }
        
        bool isListening
        {
            get { return _isListening; }
            set
            {
                _isListening = value;
                IsListeningChangedHandler.Rise(this, new EventArgs());
            }
        }

        #endregion

        #region Constructor

        public UDPListener(int _port)
        {
            port = _port;
            ipEndpoint = null;
        }

        #endregion

        #region Methods

        public void StartListen()
        {
            if (!_isListening)
            {
                signalToStopListen = false;
                listenerThread = new Thread(new ThreadStart(Listen));
                listenerThread.Start();
            }
            else
                throw new InvalidOperationException("Listener is already running");
        }

        void Listen()
        {
            try
            {
                if (udpClient != null) udpClient.Close();

                udpClient = new UdpClient(port);
                isListening = true;

                while (!signalToStopListen)
                {
                    ipEndpoint = null;
                    byte[] data = udpClient.Receive(ref ipEndpoint);
                    DataReceivedHandler.Rise(this, new UDPDataReceivedEventArgs(data));
                }

                udpClient.Close();
                isListening = false;
            }
            catch
            {
            }
        }

        public void StopListen()
        {
            if (IsListening)
            {
                signalToStopListen = true;

                if (udpClient != null) udpClient.Close();
                if (listenerThread != null) listenerThread.Join();
            }
        }

        #endregion

        #region Handlers


        #endregion

        #region Events

        public EventHandler<UDPDataReceivedEventArgs> DataReceivedHandler;
        public EventHandler IsListeningChangedHandler;

        #endregion
    }
}
