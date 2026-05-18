using System.Text;
using UCNLNMEA;

namespace UCNLDrivers
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

    [Obsolete]
    public class uRadantPort : uSerialPort
    {
        private bool _disposed = false;

        byte[] inBuffer = new byte[128];
        int inBufferCnt = 0;

        bool _lastCmdToSetAngle = false;

        bool _busy = false;

        System.Timers.Timer _rotationTimer;

        public bool Busy
        {
            get => _busy;
            private set
            {
                _busy = value;
                BusyChangedEventHandler?.Invoke(this, EventArgs.Empty);
            }
        }

        bool _waitingToFinishRotation = false;
        public bool WaitingToFinishRotation
        {
            get => _waitingToFinishRotation;
            private set
            {
                _waitingToFinishRotation = value;
                WaitingToFinishRotationChangedEventHandler?.Invoke(this, EventArgs.Empty);

                if (_waitingToFinishRotation)
                    _rotationTimer.Start();
                else
                    _rotationTimer.Stop();
            }
        }

        double _targetAngle = 0.0;

        double _currentAngle = 0.0;
        public double CurrentAngle
        {
            get => _currentAngle;
            set
            {
                _currentAngle = value;
                CurrentAngleChangedEventHandler?.Invoke(this, EventArgs.Empty);
            }
        }

        public uRadantPort(BaudRate baudRate)
            : base(baudRate)
        {
            base.PortDescription = "RDT";
            base.IsLogIncoming = true;
            base.IsTryAlways = true;
            base.IsRawModeOnly = true;
            base.RawDataReceived += OnRawDataReceived;
            base.supress_try_send = true;

            _rotationTimer = new System.Timers.Timer();
            _rotationTimer.AutoReset = false;
            _rotationTimer.Interval = 120000;
            _rotationTimer.Elapsed += (o, e) => WaitingToFinishRotation = false;
        }

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


        private bool TrySend(string message)
        {
            if (!detected || _busy)
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
                LogEventHandler.Rise(this, new LogEventArgs(LogLineType.INFO, string.Format("{0} ({1}) >> {2}", PortName, PortDescription, response)));

            if (response.Equals("ACK", StringComparison.OrdinalIgnoreCase))
            {
                StopTimer();

                if (_lastCmdToSetAngle)
                {
                    if (Math.Abs(CurrentAngle - _targetAngle) < 0.5)
                    {
                        WaitingToFinishRotation = false;  // сразу завершаем
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
            //
        }

        private void OnRawDataReceived(object sender, RawDataReceivedEventArgs e)
        {
            if (IsLogIncoming)
                LogEventHandler.Rise(this, new LogEventArgs(LogLineType.INFO, string.Format("{0} ({1}) >> {2}", PortName, PortDescription, Encoding.ASCII.GetString(e.Data))));

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

        public new void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                base.RawDataReceived -= OnRawDataReceived;
            }

            _disposed = true;
            base.Dispose();
        }

        public EventHandler? BusyChangedEventHandler;
        public EventHandler? CurrentAngleChangedEventHandler;
        public EventHandler? WaitingToFinishRotationChangedEventHandler;
    }
}