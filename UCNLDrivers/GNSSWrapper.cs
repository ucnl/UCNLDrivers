using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UCNLDrivers
{
    public class GNSSWrapper : IDisposable
    {
        private readonly uSerialPort _port;
        private readonly IGNSSPort _gnssPort;
        private bool _disposed;

        public event EventHandler DetectedChanged;
        public event EventHandler<LogEventArgs> LogEventHandler;
        public event EventHandler LocationUpdated;
        public event EventHandler HeadingUpdated;

        public GNSSWrapper(BaudRate baudRate, bool useAlternative = false)
        {
            // Создаем соответствующий порт
            if (useAlternative)
            {
                var bpPort = new BPSerialPort(baudRate);
                _port = bpPort;
                _gnssPort = bpPort; // BPSerialPort реализует IGNSSPort
            }
            else
            {
                var v1Port = new uGNSSSerialPort(baudRate);
                v1Port.MagneticOnly = false; // По умолчанию используем истинный курс
                _port = v1Port;
                _gnssPort = v1Port;
            }

            _port.DetectedChanged += OnDetectedChanged;
            _port.LogEventHandler += OnLogEvent;

            _gnssPort.LocationUpdated += OnLocationUpdated;
            _gnssPort.HeadingUpdated += OnHeadingUpdated;
        }

        #region Event Handlers

        private void OnDetectedChanged(object sender, EventArgs e)
        {
            DetectedChanged?.Invoke(this, e);
        }

        private void OnLogEvent(object sender, LogEventArgs e)
        {
            LogEventHandler?.Invoke(this, e);
        }

        private void OnLocationUpdated(object sender, EventArgs e)
        {
            LocationUpdated?.Invoke(this, e);
        }

        private void OnHeadingUpdated(object sender, EventArgs e)
        {
            HeadingUpdated?.Invoke(this, e);
        }

        #endregion

        #region Properties from uSerialPort

        public bool IsActive => _port?.IsActive ?? false;
        public bool Detected => _port?.Detected ?? false;
        public string PortName => _port?.PortName ?? "N/A";
        public bool IsOpen => _port?.IsOpen ?? false;

        public bool IsLogIncoming
        {
            get => _port?.IsLogIncoming ?? false;
            set
            {
                if (_port != null)
                    _port.IsLogIncoming = value;
            }
        }

        public bool IsTryAlways
        {
            get => _port?.IsTryAlways ?? false;
            set
            {
                if (_port != null)
                    _port.IsTryAlways = value;
            }
        }

        public string ProposedPortName
        {
            get => _port?.ProposedPortName ?? string.Empty;
            set
            {
                if (_port != null)
                    _port.ProposedPortName = value;
            }
        }

        public bool Emulation
        {
            get => _port?.Emulation ?? false;
            set
            {
                if (_port != null)
                    _port.Emulation = value;
            }
        }


        public double Latitude => _gnssPort?.Latitude ?? double.NaN;
        public double Longitude => _gnssPort?.Longitude ?? double.NaN;
        public double GroundSpeed => _gnssPort?.GroundSpeed ?? double.NaN;
        public double CourseOverGround => _gnssPort?.CourseOverGround ?? double.NaN;
        public double Heading => _gnssPort?.Heading ?? double.NaN;
        public DateTime? GNSSTime => _gnssPort?.GNSSTime ?? DateTime.MinValue;

        public bool MagneticOnly
        {
            get => _gnssPort?.MagneticOnly ?? false;
            set
            {
                if (_gnssPort != null)
                    _gnssPort.MagneticOnly = value;
            }
        }


        public double? NSpeed => (_gnssPort as BPSerialPort)?.NSpeed;
        public double? ESpeed => (_gnssPort as BPSerialPort)?.ESpeed;
        public double? VSpeed => (_gnssPort as BPSerialPort)?.VSpeed;
        public bool? IsValid => (_gnssPort as BPSerialPort)?.IsValid;

        

        #endregion

        #region Methods

        public void Start()
        {
            ThrowIfDisposed();
            _port?.Start();
        }

        public void Stop()
        {
            ThrowIfDisposed();
            _port?.Stop();
        }

        public void Send(string message)
        {
            ThrowIfDisposed();
            _port?.Send(message);
        }

        public void EmulateInput(string line)
        {
            ThrowIfDisposed();
            _port?.EmulateInput(line);
        }

        public void EmulateInput(byte[] data)
        {
            ThrowIfDisposed();
            _port?.EmulateInput(data);
        }

        public void EmulateBPData(byte[] data)
        {
            ThrowIfDisposed();
            if (_port is BPSerialPort bpPort)
            {
                bpPort.Emulate(data);
            }
            else
            {
                throw new InvalidOperationException("This method is only available for BPSerialPort");
            }
        }

        public void EmulateBPData(string hexString)
        {
            ThrowIfDisposed();
            if (_port is BPSerialPort bpPort)
            {
                bpPort.Emulate(hexString);
            }
            else
            {
                throw new InvalidOperationException("This method is only available for BPSerialPort");
            }
        }

        #endregion

        #region Helper Properties

        public string PortType => _port is BPSerialPort ? "BPSerialPort" : "uGNSSSerialPort";

        public uSerialPort RawPort => _port;

        public IGNSSPort RawGNSSPort => _gnssPort;

        #endregion

        #region IDisposable

        public void Dispose()
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
                if (_port != null)
                {
                    _port.DetectedChanged -= OnDetectedChanged;
                    _port.LogEventHandler -= OnLogEvent;
                }

                if (_gnssPort != null)
                {
                    _gnssPort.LocationUpdated -= OnLocationUpdated;
                    _gnssPort.HeadingUpdated -= OnHeadingUpdated;
                }

                DetectedChanged = null;
                LogEventHandler = null;
                LocationUpdated = null;
                HeadingUpdated = null;

                _port?.Stop();
                _port?.Dispose();
            }

            _disposed = true;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(GNSSWrapper));
        }

        #endregion

        #region Object Overrides

        public override string ToString()
        {
            if (_port == null)
                return "GNSSWrapper: No port initialized";

            var sb = new System.Text.StringBuilder();
            sb.Append(_port.ToString());

            if (!double.IsNaN(Heading))
            {
                sb.Append($", Heading: {Heading:F1}°");
                sb.Append(MagneticOnly ? " (M)" : " (T)");
            }

            if (_port is BPSerialPort bpPort)
            {
                sb.Append($", Valid: {bpPort.IsValid}");
            }

            return sb.ToString();
        }

        #endregion
    }
}