using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace UCNLDrivers
{
    public class UDPTranslator : IDisposable
    {
        #region Properties

        bool disposed = false;

        UdpClient udpClient;
        IPEndPoint ipendpoint;

        int port;

        public int Port {  get { return port; } }

        public IPAddress Address { get { return ipendpoint.Address; } }

        #endregion

        #region Constructor

        public UDPTranslator(int _port, IPAddress _address)
        {
            port = _port;

            ipendpoint = new IPEndPoint(_address, _port);
            udpClient = new UdpClient();
        }

        #endregion

        #region Methods

        public int Send(string message)
        {
            var bytes = Encoding.ASCII.GetBytes(message);
            return Send(bytes);
        }

        public int Send(byte[] data)
        {
            return udpClient.Send(data, data.Length, ipendpoint);
        }

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
                    if (udpClient != null)
                    {
                        try
                        {
                            udpClient.Close();
                        }
                        catch { }
                    }
                }

                disposed = true;
            }
        }

        #endregion

    }
}
