// UCNLDrivers/uAux/uAuxDataSource.cs
using System.Text;
using UCNLNMEA;

namespace UCNLDrivers.uAux
{
    public abstract class uAuxDataSource : IDisposable
    {
        #region Properties

        protected bool disposed = false;

        private bool _isActive;
        public bool IsActive
        {
            get => _isActive;
            protected set
            {
                if (_isActive != value)
                {
                    _isActive = value;
                    OnIsActiveChanged();
                }
            }
        }

        public string Description { get; protected set; } = string.Empty;
        public bool Emulation { get; set; }
        public bool IsLogIncoming { get; set; }

        #endregion

        #region Abstract methods

        public abstract void Start();
        public abstract void Stop();
        public abstract void ProcessIncoming(NMEASentence sentence);

        #endregion

        #region Virtual methods

        public virtual void OnClosed() { }
        public virtual void Send(string message) { }
        public virtual void SendRaw(byte[] data) { }

        public virtual void EmulateInput(string message)
        {
            if (!Emulation)
                Emulation = true;

            RawDataReceived?.Invoke(this, new RawDataReceivedEventArgs(Encoding.ASCII.GetBytes(message)));

            try
            {
                var sentence = NMEAParser.Parse(message);
                ProcessIncoming(sentence);
            }
            catch (Exception ex)
            {
                LogEventHandler?.Invoke(this, new LogEventArgs(LogLineType.ERROR,
                    $"Emulated input parse error: {ex.Message}"));
            }
        }

        protected virtual void OnIsActiveChanged() { }

        #endregion

        #region Events

        public EventHandler<RawDataReceivedEventArgs>? RawDataReceived;
        public EventHandler<LogEventArgs>? LogEventHandler;

        #endregion

        #region IDisposable

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing) { }
                disposed = true;
            }
        }

        #endregion
    }
}