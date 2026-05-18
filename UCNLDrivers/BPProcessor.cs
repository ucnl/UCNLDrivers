using UCNLNMEA;

namespace UCNLDrivers
{
    public class BPStreamReader : IDisposable
    {
        private readonly byte[] _signature;
        private readonly int _packetLength;
        private readonly byte[] _buffer;
        private int _head;
        private int _tail;
        private readonly int _capacity;

        public event Action<byte[]> OnRawPacketFound;

        public BPStreamReader(byte[] signature, int packetLength, int capacity = 2048)
        {
            _signature = signature;
            _packetLength = packetLength;
            _capacity = capacity;
            _buffer = new byte[capacity];
        }

        public void ProcessData(byte[] data, int offset, int count)
        {
            for (int i = 0; i < count; i++)
            {
                _buffer[_tail] = data[offset + i];
                _tail = (_tail + 1) % _capacity;

                if (_tail == _head)
                    _head = (_head + 1) % _capacity;
            }

            FindPackets();
        }

        private void FindPackets()
        {
            while (GetAvailableBytes() >= _packetLength)
            {
                if (IsSignatureAt(_head))
                {
                    byte[] packet = ExtractPacket(_head);

                    if (VerifyFletcherChecksum(packet))
                    {
                        OnRawPacketFound?.Invoke(packet);
                        _head = (_head + _packetLength) % _capacity;
                    }
                    else
                    {
                        _head = (_head + 1) % _capacity;
                    }
                }
                else
                {
                    _head = (_head + 1) % _capacity;
                }
            }
        }

        private bool IsSignatureAt(int position)
        {
            for (int i = 0; i < _signature.Length; i++)
            {
                int idx = (position + i) % _capacity;
                if (_buffer[idx] != _signature[i])
                    return false;
            }
            return true;
        }

        private byte[] ExtractPacket(int start)
        {
            byte[] packet = new byte[_packetLength];
            for (int i = 0; i < _packetLength; i++)
            {
                packet[i] = _buffer[(start + i) % _capacity];
            }
            return packet;
        }

        private int GetAvailableBytes()
        {
            return _tail >= _head
                ? _tail - _head
                : _capacity - _head + _tail;
        }

        private static bool VerifyFletcherChecksum(byte[] packet)
        {
            if (packet.Length < 2)
                return false;

            int dataLength = packet.Length - 2;

            byte expectedA = packet[packet.Length - 2]; // младший байт (a)
            byte expectedB = packet[packet.Length - 1]; // старший байт (b)

            byte a = 0;
            byte b = 0;

            for (int i = 0; i < dataLength; i++)
            {
                a += packet[i];
                a &= 0xFF;
                b += a;
                b &= 0xFF;
            }

            return (a == expectedA) && (b == expectedB);
        }

        public void Reset()
        {
            _head = 0;
            _tail = 0;
        }

        public void Dispose()
        {
        }
    }

    public class BPPacketParser
    {
        private readonly byte[] _signature;
        private readonly int _expectedLength;
        private const double LAT_LON_SCALE = 90.0 / 1073741824.0; // 90 * 2^-30
        private const double ALT_SCALE = 1.0 / 1024.0; // 2^-10
        private const double SPEED_SCALE = 842.865 / 1073741824.0;

        public event Action<ParsedPacket> OnPacketParsed;

        public BPPacketParser(byte[] signature, int expectedLength)
        {
            _signature = signature;
            _expectedLength = expectedLength;
        }        

        public bool TryParsePacket(byte[] rawPacket, out ParsedPacket packet)
        {
            packet = default;

            if (rawPacket == null || rawPacket.Length < _expectedLength)
                return false;

            for (int i = 0; i < _signature.Length; i++)
            {
                if (rawPacket[i] != _signature[i])
                    return false;
            }

            packet = new ParsedPacket();

            if (rawPacket.Length > 77)
                packet.DataValidity = rawPacket[77];
            else
                return false; 

            if (rawPacket.Length >= 61 + 4)
                packet.LatitudeRaw = BinUtils.ReadInt32LittleEndian(rawPacket, 61);
            else
                return false;

            if (rawPacket.Length >= 65 + 4)
                packet.LongitudeRaw = BinUtils.ReadInt32LittleEndian(rawPacket, 65);
            else
                return false;

            if (rawPacket.Length >= 69 + 4)
                packet.AltitudeRaw = BinUtils.ReadInt32LittleEndian(rawPacket, 69);
            else
                return false;

            if (rawPacket.Length >= 78 + 4)
                packet.NorthSpeedRaw = BinUtils.ReadInt32LittleEndian(rawPacket, 78);
            else
                return false;

            if (rawPacket.Length >= 82 + 4)
                packet.EastSpeedRaw = BinUtils.ReadInt32LittleEndian(rawPacket, 82);
            else
                return false;

            if (rawPacket.Length >= 86 + 4)
                packet.VerticalSpeedRaw = BinUtils.ReadInt32LittleEndian(rawPacket, 86);
            else
                return false;

            packet.Latitude = packet.LatitudeRaw * LAT_LON_SCALE;
            packet.Longitude = packet.LongitudeRaw * LAT_LON_SCALE;
            packet.Altitude = packet.AltitudeRaw * ALT_SCALE;
            packet.NorthSpeed = packet.NorthSpeedRaw * SPEED_SCALE;
            packet.EastSpeed = packet.EastSpeedRaw * SPEED_SCALE;
            packet.VerticalSpeed = packet.VerticalSpeedRaw * SPEED_SCALE;

            OnPacketParsed?.Invoke(packet);
            return true;
        }
    }

    public class BPProcessor : IDisposable
    {
        private readonly BPStreamReader _streamReader;
        private readonly BPPacketParser _packetParser;

        public event Action<ParsedPacket> OnPacketProcessed;
        public event Action<byte[]> OnRawPacketReceived;

        public BPProcessor(byte[] signature, int packetLength, int bufferCapacity = 8192)
        {
            _streamReader = new BPStreamReader(signature, packetLength, bufferCapacity);
            _packetParser = new BPPacketParser(signature, packetLength);

            _streamReader.OnRawPacketFound += OnRawPacketFound;
        }

        private void OnRawPacketFound(byte[] rawPacket)
        {
            OnRawPacketReceived?.Invoke(rawPacket);

            if (_packetParser.TryParsePacket(rawPacket, out var parsedPacket))
            {
                OnPacketProcessed?.Invoke(parsedPacket);
            }
        }

        public void ProcessData(byte[] data, int offset, int count)
        {
            _streamReader.ProcessData(data, offset, count);
        }

        public bool ProcessPacket(byte[] packet)
        {
            if (_packetParser.TryParsePacket(packet, out var parsedPacket))
            {
                OnPacketProcessed?.Invoke(parsedPacket);
                return true;
            }
            return false;
        }

        public void Reset()
        {
            _streamReader.Reset();
        }

        public void Dispose()
        {
            _streamReader?.Dispose();
        }
    }

    public struct ParsedPacket
    {
        private const byte VALID_MASK = 0x01;
        private const byte SOLUTION_TYPE_MASK = 0x02;
        private const byte VALID2_MASK = 0x04;
        private const byte CS_MASK = 0x30;
        private const byte CS_SHIFT = 4;
        private const byte VALID_A_MASK = 0x40;
        private const byte TYPE_T_MASK = 0x80;

        public int LatitudeRaw;
        public int LongitudeRaw;
        public int AltitudeRaw;
        public int NorthSpeedRaw;
        public int EastSpeedRaw;
        public int VerticalSpeedRaw;
        public byte DataValidity;

        public double Latitude;
        public double Longitude;
        public double Altitude;
        public double NorthSpeed;
        public double EastSpeed;
        public double VerticalSpeed;

        public bool IsValid => (DataValidity & VALID_MASK) != 0;
        public bool IsSolutionType => (DataValidity & SOLUTION_TYPE_MASK) != 0;
        public bool IsValid2 => (DataValidity & VALID2_MASK) != 0;
        public bool IsValidA => (DataValidity & VALID_A_MASK) != 0;
        public bool IsTType => (DataValidity & TYPE_T_MASK) != 0;

        public CsMode CSMode => (CsMode)((DataValidity & CS_MASK) >> CS_SHIFT);

        public bool IsFullyValid => IsValid && IsValid2 && IsValidA;

        public override string ToString()
        {
            return $"Lat: {Latitude:F6}, Lon: {Longitude:F6}, Alt: {Altitude:F1}m, " +
                   $"Speed: N:{NorthSpeed:F2} E:{EastSpeed:F2} V:{VerticalSpeed:F2}, " +
                   $"Valid: {IsValid}, CS: {CSMode}";
        }
    }

    public enum CsMode : byte
    {
        Cs1 = 0,
        Cs2 = 1,
        Cs3 = 2,
        Reserved = 3
    }

    public class BPSerialPort : uSerialPort, IGNSSPort
    {
        private bool _disposed = false;

        private readonly BPProcessor _bpProcessor;

        private bool _magneticOnly;

        public double Latitude { get; private set; }
        public double Longitude { get; private set; }
        public double GroundSpeed { get; private set; }
        public double CourseOverGround { get; private set; }
        public double Heading { get; private set; }
        public bool MagneticOnly
        {
            get => _magneticOnly;
            set
            {
                if (_magneticOnly != value)
                {
                    _magneticOnly = value;
                }
            }
        }
        public DateTime GNSSTime { get; private set; }


        public double NSpeed { get; private set; }
        public double ESpeed { get; private set; }
        public double VSpeed { get; private set; }
        public bool IsValid { get; private set; } = false;


        public BPSerialPort(BaudRate baudRate)
            : base(baudRate)
        {
            base.PortDescription = "BPS";
            base.IsLogIncoming = true;
            base.IsTryAlways = true;
            base.IsRawModeOnly = true;

            _bpProcessor = new BPProcessor(new byte[] { 89, 46, 41, 161, 99, 219 }, 298);
            _bpProcessor.OnPacketProcessed += OnPacketProcessed;
            base.RawDataReceived += OnRawDataReceived;
        }

        private double CalculateCourseOverGround()
        {
            double sog = Math.Sqrt(NSpeed * NSpeed + ESpeed * ESpeed);

            if (sog < 0.01)
                return double.NaN;

            double radians = Math.Atan2(ESpeed, NSpeed);
            double degrees = radians * 180.0 / Math.PI;

            if (degrees < 0)
                degrees += 360;

            return degrees;
        }

        private void ResetData()
        {
            Latitude = double.NaN;
            Longitude = double.NaN;
            GroundSpeed = double.NaN;
            CourseOverGround = double.NaN;
            Heading = double.NaN;
            GNSSTime = DateTime.MinValue;


            NSpeed = double.NaN;
            ESpeed = double.NaN;
            VSpeed = double.NaN;
            IsValid = false;
        }



        public void Emulate(byte[] data)
        {
            base.EmulateInput(data);
        }

        public void Emulate(string line)
        {
            string[] parts = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            byte[] bytes = parts.Select(b => Convert.ToByte(b, 16)).ToArray();
            base.EmulateInput(bytes);
        }


        public override void InitQuerySend()
        {
            //
        }

        public override void OnClosed()
        {
            ResetData();
        }

        public override void ProcessIncoming(NMEASentence sentence)
        {
            //
        }


        private void OnPacketProcessed(ParsedPacket packet)
        {
            if (!detected)
                detected = true;

            StopTimer();
            StartTimer(1500);

            Latitude = packet.Latitude;
            Longitude = packet.Longitude;
            NSpeed = packet.NorthSpeed;
            ESpeed = packet.EastSpeed;
            VSpeed = packet.VerticalSpeed;

            GroundSpeed = Math.Sqrt(NSpeed * NSpeed + ESpeed * ESpeed);
            CourseOverGround = CalculateCourseOverGround();

            IsValid = packet.IsValid;
            GNSSTime = DateTime.Now;

            LocationUpdated?.Invoke(this, EventArgs.Empty);
        }

        private void OnRawDataReceived(object sender, RawDataReceivedEventArgs e)
        {
            _bpProcessor?.ProcessData(e.Data, 0, e.Data.Length);
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
                if (_bpProcessor != null)
                    _bpProcessor.OnPacketProcessed -= OnPacketProcessed;

                base.RawDataReceived -= OnRawDataReceived;

                LocationUpdated = null;
                HeadingUpdated = null;
            }

            _disposed = true;

            base.Dispose();
        }

        public event EventHandler LocationUpdated;
        public event EventHandler HeadingUpdated;
    }
}
