using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using UCNLNMEA;

namespace UCNLDrivers
{
    public abstract class uSerialPort : IDisposable
    {
        #region Properties

        bool disposed = false;

        NMEASerialPort port;
        PrecisionTimer timer;

        public string PortName
        {
            get { return port == null ? "N/A" : port.PortName; }
        }

        public bool IsOpen
        {
            get { return port != null ? port.IsOpen : false; }

        }

        protected string PortDescription;

        string detectedPortName = string.Empty;
        bool _detected = false;

        protected bool timerIsRunning
        {
            get { return timer.IsRunning; }
        }

        protected bool detected
        {
            get { return _detected; }
            set
            {
                if (_detected != value)
                {
                    _detected = value;
                    DetectedChanged.Rise(this, new EventArgs());

                    if (_detected)
                    {
                        detectedPortName = PortName;
                        LogEventHandler.Rise(this,
                            new LogEventArgs(LogLineType.INFO,
                            string.Format("{0} detected on {1}", PortDescription, PortName)));
                    }
                }
            }
        }
        public bool Detected { get { return _detected; } }

        BaudRate baudrate = BaudRate.baudRate9600;
        public BaudRate Baudrate
        {
            get { return baudrate; }
        }

        public bool IsLogIncoming { get; set; }

        EventHandler<NewNMEAMessageEventArgs> port_NewNMEAMessageHandler;
        EventHandler<SerialErrorReceivedEventArgs> port_PortErrorHandler;

        readonly int timer_period_ms = 200;
        int timer_cnt = 0;
        int timer_cnt_max = 10;
        int timerlock = 0;

        List<string> checkedPortNames;

        protected bool supress_try_send = false;

        bool isActive = false;
        public bool IsActive
        {
            get { return isActive; }
            private set
            {
                if (value != isActive)
                {
                    isActive = value;
                    IsActiveChanged.Rise(this, new EventArgs());
                }
            }
        }
        public bool IsTryAlways { get; set; }

        #endregion

        #region Constructor

        public uSerialPort(BaudRate _baudrate)
        {
            baudrate = _baudrate;

            checkedPortNames = new List<string>();

            port_NewNMEAMessageHandler = new EventHandler<NewNMEAMessageEventArgs>(port_NewNMEAMessage);
            port_PortErrorHandler = new EventHandler<SerialErrorReceivedEventArgs>(port_PortError);

            timer = new PrecisionTimer();
            timer.Mode = Mode.Periodic;
            timer.Period = timer_period_ms;
            timer.Tick += (o, e) =>
            {
                if (timer_cnt++ > timer_cnt_max)
                {
                    timer_cnt = 0;

                    if (!_detected)
                        TryNextPort();
                    else
                    {
                        PortTimeout.Rise(this, new EventArgs());
                        detected = false;

                        if (IsTryAlways)
                        {
                            checkedPortNames.Clear();
                            TryNextPort();
                        }
                    }
                }
            };
        }

        #endregion

        #region Abstract methods
        public abstract void InitQuerySend();
        public abstract void ProcessIncoming(NMEASentence sentence);
        public abstract void OnClosed();

        #endregion

        #region Protected methods

        protected void ResetTimer()
        {
            timer_cnt = 0;
        }

        protected void StopTimer()
        {
            while (Interlocked.CompareExchange(ref timerlock, 1, 0) != 0)
                Thread.SpinWait(1);

            if (timer.IsRunning)
                timer.Stop();

            timer_cnt = 0;

            Interlocked.Decrement(ref timerlock);
        }

        protected void StartTimer(int timeout_ms)
        {
            while (Interlocked.CompareExchange(ref timerlock, 1, 0) != 0)
                Thread.SpinWait(1);

            if (timer.IsRunning)
                timer.Stop();

            timer_cnt = 0;
            timer_cnt_max = timeout_ms / timer_period_ms;
            timer_cnt_max = timer_cnt_max > 0 ? timer_cnt_max : 1;

            timer.Start();

            Interlocked.Decrement(ref timerlock);
        }

        protected void ForceTimer()
        {
            while (Interlocked.CompareExchange(ref timerlock, 1, 0) != 0)
                Thread.SpinWait(1);

            timer_cnt = timer_cnt_max - 1;

            Interlocked.Decrement(ref timerlock);
        }

        #endregion

        #region Private methods

        private string GetNextPortName(IEnumerable<string> _namesList)
        {
            _ = _namesList ?? throw new ArgumentNullException(nameof(_namesList));

            string result = string.Empty;
            var pNames = SerialPort.GetPortNames();

            if (pNames.Length > 0)
            {
                bool nextExists = false;
                for (int i = 0; (i < pNames.Length) && (!nextExists); i++)
                {
                    if (!_namesList.Contains(pNames[i]))
                    {
                        result = pNames[i];
                        nextExists = true;
                    }
                }
            }

            return result;
        }

        private void SafelyClosePort(bool isCallOnClosed)
        {
            if (port != null)
            {
                try
                {
                    port.NewNMEAMessage -= port_NewNMEAMessageHandler;
                    port.PortError -= port_PortErrorHandler;

                    if (port.IsOpen)
                    {
                        port.Close();
                        LogEventHandler.Rise(this, new LogEventArgs(LogLineType.INFO, string.Format("{0} ({1}) Closed", port.PortName, PortDescription)));
                    }
                }
                catch { }
            }

            if (isCallOnClosed)
                OnClosed();
        }

        private void TryNextPort()
        {
            StopTimer();
            SafelyClosePort(true);

            string pName = string.Empty;

            // Will try the previously detected port, if it has been
            if (!string.IsNullOrEmpty(detectedPortName) &&
                !checkedPortNames.Contains(detectedPortName))
                pName = detectedPortName;
            else
                pName = GetNextPortName(checkedPortNames);

            if (!string.IsNullOrEmpty(pName))
            {
                checkedPortNames.Add(pName);
                LogEventHandler.Rise(this,
                    new LogEventArgs(LogLineType.INFO,
                    string.Format("Trying {0} as {1}...", pName, PortDescription)));

                try
                {
                    port = new NMEASerialPort(new SerialPortSettings(pName,
                        Baudrate, Parity.None, DataBits.dataBits8, StopBits.One, Handshake.None));

                    port.IsRawModeOnly = false;
                    port.PortError += port_PortErrorHandler;
                    port.NewNMEAMessage += port_NewNMEAMessageHandler;

                    port.Open();

                    if (!supress_try_send)
                        port.SendRaw(new byte[] { 0x00, 0x00, 0x00 });

                    InitQuerySend();
                    StartTimer(1000);
                }
                catch (Exception ex)
                {
                    LogEventHandler.Rise(this, new LogEventArgs(LogLineType.ERROR, ex));
                    StartTimer(0); // To avoid recursion - timer expiration will cause a next TryNextPort() call
                }
            }
            else
            {
                if (IsTryAlways)
                {
                    checkedPortNames.Clear();
                    LogEventHandler.Rise(this, new LogEventArgs(LogLineType.ERROR, string.Format("{0} was not detected, retrying in a moment... ", PortDescription)));
                    StartTimer(1000);
                }
                else
                {
                    PortDetectionFailed.Rise(this, new EventArgs());
                }
            }
        }

        #endregion

        #region Public methods

        public void Send(string message)
        {
            if ((port != null) && port.IsOpen)
            {
                port.SendData(message);
                LogEventHandler.Rise(this, new LogEventArgs(LogLineType.INFO, string.Format("{0} ({1}) << {2}", PortName, PortDescription, message)));
            }
        }

        public void EmulateInput(string message)
        {
            port_NewNMEAMessage(this, new NewNMEAMessageEventArgs(message));
        }

        public void Start()
        {
            StopTimer();
            checkedPortNames.Clear();

            if ((port != null) && port.IsOpen)
                SafelyClosePort(true);

            IsActive = true;
            detected = false;

            LogEventHandler.Rise(this, new LogEventArgs(LogLineType.INFO, string.Format("{0} Starting detection...", PortDescription)));
            TryNextPort();
        }

        public void Stop()
        {
            StopTimer();
            SafelyClosePort(false);

            detected = false;
            IsActive = false;

            OnClosed();
            LogEventHandler.Rise(this, new LogEventArgs(LogLineType.INFO, string.Format("{0} Stopped", PortDescription)));
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(PortDescription);

            if (isActive)
            {
                sb.Append(", Active");
                if (detected)
                    sb.AppendFormat(" on {0}", PortName);
                else
                    sb.Append(", searching...");
            }
            else
                sb.Append(", Not active");


            return sb.ToString();
        }

        #endregion

        #region Handlers

        private void port_NewNMEAMessage(object sender, NewNMEAMessageEventArgs e)
        {
            if (IsLogIncoming)
                LogEventHandler.Rise(this, new LogEventArgs(LogLineType.INFO, string.Format("{0} ({1}) >> {2}", PortName, PortDescription, e.Message)));

            try
            {
                var _result = NMEAParser.Parse(e.Message);
                ProcessIncoming(_result);
            }
            catch (Exception ex)
            {
                LogEventHandler.Rise(this, new LogEventArgs(LogLineType.ERROR, string.Format("{0} ({1}) >> {2} Caused error ({3})", PortName, PortDescription, e.Message, ex.Message)));
            }
        }

        private void port_PortError(object sender, SerialErrorReceivedEventArgs e)
        {
            LogEventHandler.Rise(this, new LogEventArgs(LogLineType.ERROR, string.Format("{0} ({1}) >> {2}", PortName, PortDescription, e.EventType.ToString())));
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
                    timer.Stop();
                    timer.Dispose();

                    //SafelyClosePort();

                    if (port != null)
                        port.Dispose();
                }

                disposed = true;
            }
        }

        #endregion

        #region Events

        public EventHandler DetectedChanged;
        public EventHandler IsActiveChanged;

        public EventHandler<LogEventArgs> LogEventHandler;
        public EventHandler PortTimeout;

        public EventHandler PortDetectionFailed;

        #endregion
    }
}
