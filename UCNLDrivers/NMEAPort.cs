using System;
using System.Text;
using UCNLNMEA;

namespace UCNLDrivers
{
    #region Custom EventArgs

    public class NewNMEAMessageEventArgs : EventArgs
    {
        #region Properties

        public string Message { get; private set; }

        #endregion

        #region Constructor

        public NewNMEAMessageEventArgs(string message)
        {
            Message = message;
        }

        #endregion
    }    

    #endregion

    public abstract class NMEAPort
    {
        #region Properties

        public abstract bool IsOpen { get; }

        StringBuilder buffer_;

        byte[] buffer = new byte[8192];
        int bIdx = 0;
        bool isSntStarted = false;

        static byte nmeaSntStartByte = Convert.ToByte(NMEAParser.SentenceStartDelimiter);
        static byte nmeaEndByte = Convert.ToByte(NMEAParser.SentenceEndDelimiter[NMEAParser.SentenceEndDelimiter.Length - 1]);

        #endregion

        #region Methods

        #region Abstract

        public abstract void SendData(string message);
        public abstract void Open();
        public abstract void Close();

        #endregion

        #region Protected

        protected void OnIncomingData(byte[] data)
        {
            for (int i = 0; i < data.Length; i++)
            {
                if (data[i] == nmeaSntStartByte)
                {
                    isSntStarted = true;
                    Array.Clear(buffer, 0, buffer.Length);
                    bIdx = 0;
                    buffer[bIdx] = data[i];
                    bIdx++;
                }
                else
                {
                    if (isSntStarted)
                    {
                        buffer[bIdx] = data[i];
                        bIdx++;

                        if (data[i] == nmeaEndByte)
                        {
                            isSntStarted = false;
                            string snt = Encoding.ASCII.GetString(buffer, 0, bIdx);
                            NewNMEAMessage.BeginRise(this, new NewNMEAMessageEventArgs(snt), null, null);
                        }
                        else
                        {
                            if (bIdx >= buffer.Length - 1)
                                isSntStarted = false;
                        }
                    }
                }
            }
        }

        protected void OnIncomingDataEx(string data)
        {
            var dataBytes = Encoding.ASCII.GetBytes(data);
            OnIncomingData(dataBytes);
        }


        protected void OnIncomingData(string data)
        {
            buffer_.Append(data);
            var temp = buffer_.ToString();

            int lIndex = temp.LastIndexOf(NMEAParser.SentenceEndDelimiter);
            if (lIndex >= 0)
            {
                buffer_ = buffer_.Remove(0, lIndex + 2);
                if (lIndex + 2 < temp.Length)
                    temp = temp.Remove(lIndex + 2);

                temp = temp.Trim(new char[] { '\0' });

                int startIdx = temp.IndexOf(NMEAParser.SentenceStartDelimiter);
                if (startIdx > 0)
                    temp = temp.Remove(0, startIdx);

                var lines = temp.Split(NMEAParser.SentenceEndDelimiter.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

                if (NewNMEAMessage != null)
                {
                    for (int i = 0; i < lines.Length; i++)
                        NewNMEAMessage.BeginRise(this, new NewNMEAMessageEventArgs(string.Format("{0}{1}", lines[i], NMEAParser.SentenceEndDelimiter)), null, null);
                }
            }

            if (buffer_.Length >= ushort.MaxValue)
                buffer_.Remove(0, short.MaxValue);
        }

        protected void OnConnectionOpening()
        {
            buffer_ = new StringBuilder();

            Array.Clear(buffer, 0, buffer.Length);
            isSntStarted = false;
            bIdx = 0;
        }

        protected void OnConnectionClosing()
        {
            buffer_ = new StringBuilder();
            
            Array.Clear(buffer, 0, buffer.Length);
            isSntStarted = false;
            bIdx = 0;
        }

        #endregion

        #endregion

        #region Events

        public EventHandler<NewNMEAMessageEventArgs> NewNMEAMessage;        

        #endregion
    }
}
