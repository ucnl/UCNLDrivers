// UCNLDrivers/uAux/uAuxPort.cs
using System.IO.Ports;
using System.Text;
using UCNLNMEA;

namespace UCNLDrivers.uAux
{
    public abstract class uAuxPort : uAuxDataSource, IAuxSource
    {
        #region Properties

        NMEASerialPort? port;
        System.Timers.Timer timer;

        public string PortName => port?.PortName ?? "N/A";
        public bool IsOpen => port?.IsOpen ?? false;

        protected string PortDescription = string.Empty;

        string detectedPortName = string.Empty;

        readonly int timer_period_ms = 200;
        int timer_cnt = 0;
        int timer_cnt_max = 10;
        int timerlock = 0;

        List<string> checkedPortNames;

        protected bool supress_try_send = false;

        public bool IsTryAlways { get; set; }
        public string ProposedPortName { get; set; } = string.Empty;
        public bool IsFixedPort { get; set; } = false;
        public bool IsRawModeOnly { get; set; } = false;

        BaudRate baudrate = BaudRate.baudRate9600;
        public BaudRate Baudrate => baudrate;

        #endregion

        #region IAuxSource

        public abstract string Id { get; }
        public abstract AuxSourceKind Kind { get; }

        private AuxStatus _status = AuxStatus.Inactive;
        public AuxStatus Status
        {
            get => _status;
            private set
            {
                if (_status != value)
                {
                    _status = value;
                    OnStatusChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        string? IAuxSource.PortName => PortName;

        public event EventHandler? OnStatusChanged;

        #endregion

        #region Detection (для наследников)

        protected bool detected
        {
            get => Status == AuxStatus.Detected;
            set
            {
                if (value && Status == AuxStatus.Active)
                {
                    detectedPortName = PortName;
                    Status = AuxStatus.Detected;

                    LogEventHandler?.Invoke(this, new LogEventArgs(LogLineType.INFO,
                        $"{PortDescription} detected on {detectedPortName}"));
                }
                else if (!value && Status == AuxStatus.Detected)
                {
                    Status = AuxStatus.Active;
                }
            }
        }

        #endregion

        #region Constructor

        public uAuxPort(BaudRate baudRate)
        {
            baudrate = baudRate;
            checkedPortNames = new List<string>();

            timer = new System.Timers.Timer
            {
                AutoReset = true,
                Interval = timer_period_ms
            };
            timer.Elapsed += (_, _) =>
            {
                if (Status != AuxStatus.Inactive)
                {
                    if (timer_cnt++ > timer_cnt_max)
                    {
                        timer_cnt = 0;

                        if (!detected)
                            TryNextPort();
                        else
                        {
                            // Таймаут — порт перестал отвечать
                            PortTimeout?.Invoke(this, EventArgs.Empty);
                            detected = false;

                            if (IsTryAlways)
                            {
                                if (!IsFixedPort)
                                    checkedPortNames.Clear();
                                TryNextPort();
                            }
                        }
                    }
                }
            };
        }

        #endregion

        #region Abstract methods

        public abstract void InitQuerySend();

        #endregion

        #region uDataSource overrides

        public override void Start()
        {
            Emulation = false;
            StopTimer();
            checkedPortNames.Clear();

            if (port != null && port.IsOpen)
                SafelyClosePort(true);

            Status = AuxStatus.Active;

            LogEventHandler?.Invoke(this, new LogEventArgs(LogLineType.INFO,
                $"{PortDescription} Starting detection..."));
            TryNextPort();
        }

        public override void Stop()
        {
            StopTimer();
            SafelyClosePort(false);

            Status = AuxStatus.Inactive;

            OnClosed();
            LogEventHandler?.Invoke(this, new LogEventArgs(LogLineType.INFO,
                $"{PortDescription} Stopped"));
        }

        public override void Send(string message)
        {
            if (port != null && port.IsOpen)
            {
                port.SendData(message);
                LogEventHandler?.Invoke(this, new LogEventArgs(LogLineType.INFO,
                    $"{PortName} ({PortDescription}) << {message}"));
            }
        }

        public override void EmulateInput(string message)
        {
            if (!Emulation)
                Emulation = true;

            var sentence = NMEAParser.Parse(message);
            ProcessIncoming(sentence);
        }

        #endregion

        #region Timer methods

        protected void ResetTimer()
        {
            while (Interlocked.CompareExchange(ref timerlock, 1, 0) != 0)
                Thread.SpinWait(1);
            timer_cnt = 0;
            Interlocked.Decrement(ref timerlock);
        }

        protected void StopTimer()
        {
            while (Interlocked.CompareExchange(ref timerlock, 1, 0) != 0)
                Thread.SpinWait(1);
            if (timer.Enabled)
                timer.Stop();
            timer_cnt = 0;
            Interlocked.Decrement(ref timerlock);
        }

        protected void StartTimer(int timeout_ms)
        {
            while (Interlocked.CompareExchange(ref timerlock, 1, 0) != 0)
                Thread.SpinWait(1);
            if (timer.Enabled)
                timer.Stop();
            timer_cnt = 0;
            timer_cnt_max = Math.Max(1, timeout_ms / timer_period_ms);
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

        #region Port detection

        private string GetNextPortName()
        {
            var pNames = SerialPort.GetPortNames();
            foreach (var name in pNames)
            {
                if (!checkedPortNames.Contains(name))
                    return name;
            }
            return string.Empty;
        }

        private void SafelyClosePort(bool callOnClosed)
        {
            if (port != null)
            {
                try
                {
                    if (port.IsOpen)
                    {
                        port.Close();
                        LogEventHandler?.Invoke(this, new LogEventArgs(LogLineType.INFO,
                            $"{port.PortName} ({PortDescription}) Closed"));
                    }
                }
                catch { }
            }

            if (callOnClosed)
                OnClosed();
        }

        private void TryNextPort()
        {
            StopTimer();
            SafelyClosePort(true);

            string pName;

            if (IsFixedPort)
            {
                pName = ProposedPortName;
            }
            else
            {
                if (!string.IsNullOrEmpty(detectedPortName) &&
                    !checkedPortNames.Contains(detectedPortName))
                    pName = detectedPortName;
                else if (!string.IsNullOrEmpty(ProposedPortName) &&
                         !checkedPortNames.Contains(ProposedPortName))
                    pName = ProposedPortName;
                else
                    pName = GetNextPortName();
            }

            if (!string.IsNullOrEmpty(pName))
            {
                checkedPortNames.Add(pName);

                LogEventHandler?.Invoke(this, new LogEventArgs(LogLineType.INFO,
                    $"Trying {pName} as {PortDescription}..."));

                try
                {
                    port = new NMEASerialPort(new SerialPortSettings(pName,
                        Baudrate, Parity.None, DataBits.dataBits8, StopBits.One, Handshake.None))
                    {
                        IsRawModeOnly = IsRawModeOnly
                    };

                    port.NewNMEAMessage += Port_NewNMEAMessage;
                    port.PortError += Port_PortError;
                    port.RawDataReceived += Port_RawDataReceived;

                    port.Open();

                    if (!supress_try_send)
                        port.SendRaw(new byte[] { 0x00, 0x00, 0x00 });

                    InitQuerySend();
                    StartTimer(1000);
                }
                catch (Exception ex)
                {
                    LogEventHandler?.Invoke(this, new LogEventArgs(LogLineType.ERROR, ex));
                    if (Status != AuxStatus.Inactive)
                        StartTimer(0);
                }
            }
            else if (Status != AuxStatus.Inactive)
            {
                if (IsTryAlways)
                {
                    checkedPortNames.Clear();
                    LogEventHandler?.Invoke(this, new LogEventArgs(LogLineType.ERROR,
                        $"{PortDescription} was not detected, retrying..."));
                    StartTimer(1000);
                }
                else
                {
                    PortDetectionFailed?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        #endregion

        #region NMEA Handlers

        private void Port_NewNMEAMessage(object? sender, NewNMEAMessageEventArgs e)
        {
            ResetTimer();

            if (IsLogIncoming)
                LogEventHandler?.Invoke(this, new LogEventArgs(LogLineType.INFO,
                    $"{PortName} ({PortDescription}) >> {e.Message}"));

            try
            {
                var sentence = NMEAParser.Parse(e.Message);
                ProcessIncoming(sentence);
            }
            catch (Exception ex)
            {
                LogEventHandler?.Invoke(this, new LogEventArgs(LogLineType.ERROR,
                    $"{PortName} ({PortDescription}) >> {e.Message} Caused error ({ex.Message})"));
            }
        }

        private void Port_PortError(object? sender, SerialErrorReceivedEventArgs e)
        {
            LogEventHandler?.Invoke(this, new LogEventArgs(LogLineType.ERROR,
                $"{PortName} ({PortDescription}) >> {e.EventType}"));
        }

        private void Port_RawDataReceived(object? sender, RawDataReceivedEventArgs e)
        {
            if (!IsLogIncoming && e.Data != null)
                LogEventHandler?.Invoke(this, new LogEventArgs(LogLineType.INFO,
                    $"{PortName} ({PortDescription}) >> {string.Join(" ", e.Data.Select(b => b.ToString("X2")))}"));

            RawDataReceived?.Invoke(this, e);
        }

        #endregion

        #region ToString

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append(PortDescription);
            sb.Append(Status switch
            {
                AuxStatus.Inactive => ", Not active",
                AuxStatus.Active => ", Active, searching...",
                AuxStatus.Detected => $", Active on {PortName}",
                AuxStatus.Error => ", Error",
                _ => ""
            });
            return sb.ToString();
        }

        #endregion

        #region Events

        public event EventHandler? PortTimeout;
        public event EventHandler? PortDetectionFailed;

        #endregion
    }
}