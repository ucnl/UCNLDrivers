// UCNLDrivers/uAux/uAuxRadantPort.cs
using System.Text;
using UCNLNMEA;

namespace UCNLDrivers.uAux
{
    /* Основные команды
    
    Последовательный порт работает в режиме 9600 8N1.

    Запросы:
    Qaaa.a<CR>  - задать угол
    Y<CR>       - запросить угол
    S<CR>       - стоп

    Ответы:
    ACK<CR>     - принято
    ERR<CR>     - ошибка
    OKaaa.a<CR> - текущий угол
    */

    public class uAuxRadantPort : uAuxPort
    {
        #region Properties

        private byte[] inBuffer = new byte[128];
        private int inBufferCnt = 0;

        private bool _lastCmdToSetAngle = false;

        private bool _busy = false;
        public bool Busy
        {
            get => _busy;
            private set
            {
                if (_busy != value)
                {
                    _busy = value;
                    BusyChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        private bool _waitingToFinishRotation = false;
        public bool WaitingToFinishRotation
        {
            get => _waitingToFinishRotation;
            private set
            {
                if (_waitingToFinishRotation != value)
                {
                    _waitingToFinishRotation = value;
                    WaitingToFinishRotationChanged?.Invoke(this, EventArgs.Empty);

                    if (_waitingToFinishRotation)
                        _rotationTimer.Start();
                    else
                        _rotationTimer.Stop();
                }
            }
        }

        private double _targetAngle = 0.0;

        private double _currentAngle = 0.0;
        public double CurrentAngle
        {
            get => _currentAngle;
            private set
            {
                if (Math.Abs(_currentAngle - value) > 0.01)
                {
                    _currentAngle = value;
                    CurrentAngleChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        private System.Timers.Timer _rotationTimer;

        #endregion

        #region IAuxSource

        public override string Id { get; }
        public override AuxSourceKind Kind => AuxSourceKind.Custom;

        #endregion

        #region Constructor

        public uAuxRadantPort(string id, BaudRate baudRate)
            : base(baudRate)
        {
            Id = id;
            PortDescription = "Rotator";
            IsLogIncoming = true;
            IsTryAlways = true;
            IsRawModeOnly = true;
            supress_try_send = true;

            RawDataReceived += OnRawDataReceived;

            _rotationTimer = new System.Timers.Timer
            {
                AutoReset = false,
                Interval = 120000
            };
            _rotationTimer.Elapsed += (_, _) => WaitingToFinishRotation = false;
        }

        #endregion

        #region Public commands

        public bool RequestSetAngle(double angle)
        {
            bool result = TrySend($"Q{angle:F1}\r");

            if (result)
            {
                _targetAngle = angle;
                _lastCmdToSetAngle = true;
            }

            return result;
        }

        public bool RequestAngle()
        {
            return TrySend("Y\r");
        }

        public bool RequestStop()
        {
            return TrySend("S\r");
        }

        #endregion

        #region Private methods

        private bool TrySend(string message)
        {
            if (!detected || Busy)
                return false;

            try
            {
                Send(message);
                StartTimer(1000);
                Busy = true;
                return true;
            }
            catch (Exception ex)
            {
                LogEventHandler?.Invoke(this, new LogEventArgs(LogLineType.ERROR, ex));
                return false;
            }
        }

        private void ParseInput()
        {
            string response = Encoding.ASCII.GetString(inBuffer, 0, inBufferCnt).Trim();

            if (IsLogIncoming)
                LogEventHandler?.Invoke(this, new LogEventArgs(LogLineType.INFO,
                    $"{PortName} ({PortDescription}) >> {response}"));

            if (response.Equals("ACK", StringComparison.OrdinalIgnoreCase))
            {
                StopTimer();

                if (_lastCmdToSetAngle)
                {
                    if (Math.Abs(CurrentAngle - _targetAngle) < 0.5)
                    {
                        WaitingToFinishRotation = false;
                    }
                    else
                    {
                        WaitingToFinishRotation = true;
                    }
                }

                Busy = false;
            }
            else if (response.Equals("ERR", StringComparison.OrdinalIgnoreCase))
            {
                StopTimer();
                Busy = false;
            }
            else if (response.StartsWith("OK", StringComparison.OrdinalIgnoreCase))
            {
                if (!detected)
                    detected = true;

                StopTimer();
                Busy = false;

                string angleStr = response.Substring(2).Trim();

                int spaceIdx = angleStr.IndexOf(' ');
                if (spaceIdx > 0)
                    angleStr = angleStr.Substring(0, spaceIdx);

                if (float.TryParse(angleStr,
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out float angle))
                {
                    CurrentAngle = angle;

                    if (Math.Abs(CurrentAngle - _targetAngle) < 0.5)
                    {
                        WaitingToFinishRotation = false;
                    }
                    else
                    {
                        RequestSetAngle(_targetAngle);
                    }
                }
            }
        }

        #endregion

        #region uAuxPort overrides

        public override void InitQuerySend()
        {
            Send("Y\r");
        }

        public override void OnClosed()
        {
            StopTimer();
            Busy = false;
            inBufferCnt = 0;
        }

        public override void ProcessIncoming(NMEASentence sentence)
        {
            // NMEA не используется — всё через RawDataReceived
        }

        #endregion

        #region Event handlers

        private void OnRawDataReceived(object? sender, RawDataReceivedEventArgs e)
        {
            if (IsLogIncoming)
                LogEventHandler?.Invoke(this, new LogEventArgs(LogLineType.INFO,
                    $"{PortName} ({PortDescription}) >> {Encoding.ASCII.GetString(e.Data)}"));

            for (int i = 0; i < e.Data.Length; i++)
            {
                if (e.Data[i] == 0x0D) // '\r'
                {
                    ParseInput();
                    inBufferCnt = 0;
                }
                else
                {
                    inBuffer[inBufferCnt++] = e.Data[i];
                    if (inBufferCnt >= inBuffer.Length) inBufferCnt = 0;
                }
            }
        }

        #endregion

        #region Events

        public event EventHandler? BusyChanged;
        public event EventHandler? CurrentAngleChanged;
        public event EventHandler? WaitingToFinishRotationChanged;

        #endregion

        #region ToString

        public override string ToString()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append(base.ToString());
            if (!double.IsNaN(CurrentAngle))
                sb.Append($", Angle: {CurrentAngle:F1}°");
            if (Busy)
                sb.Append(", BUSY");
            return sb.ToString();
        }

        #endregion
    }
}