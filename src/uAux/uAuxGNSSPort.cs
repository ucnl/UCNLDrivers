// UCNLDrivers/uAux/uAuxGNSSPort.cs
using UCNLNMEA;

namespace UCNLDrivers.uAux
{
    public enum GNSSMode
    {
        Auto,         // HDT + HDG/HDM + RMC
        CompassOnly,  // только HDG/HDM
        GNSSOnly      // HDT + RMC
    }

    public class uAuxGNSSPort : uAuxPort, IGNSSPort
    {
        #region Properties

        public GNSSMode Mode { get; set; } = GNSSMode.Auto;

        public bool IsHeadingTrue { get; private set; }

        public double Heading { get; private set; }
        public double Latitude { get; private set; }
        public double Longitude { get; private set; }
        public double GroundSpeed { get; private set; }
        public double CourseOverGround { get; private set; }
        public DateTime GNSSTime { get; private set; }

        #endregion

        #region IGNSSPort (для обратной совместимости)

        bool IGNSSPort.MagneticOnly
        {
            get => Mode == GNSSMode.CompassOnly;
            set => Mode = value ? GNSSMode.CompassOnly : GNSSMode.Auto;
        }

        #endregion

        #region IAuxSource

        public override string Id { get; }
        public override AuxSourceKind Kind
        {
            get
            {
                if (Mode == GNSSMode.CompassOnly)
                    return AuxSourceKind.Compass;
                return AuxSourceKind.GNSS;
            }
        }

        #endregion

        #region Constructor

        public uAuxGNSSPort(string id, BaudRate baudRate)
            : base(baudRate)
        {
            Id = id;
            PortDescription = "GNSS";
            IsLogIncoming = true;
            IsTryAlways = true;

            DiscardData();
        }

        #endregion

        #region Methods

        private void DiscardData()
        {
            Heading = double.NaN;
            Latitude = double.NaN;
            Longitude = double.NaN;
            GroundSpeed = double.NaN;
            CourseOverGround = double.NaN;
            GNSSTime = DateTime.MinValue;
            IsHeadingTrue = false;
        }

        private static double O2D(object? o) =>
            (o == null) ? double.NaN : (double)o;

        #endregion

        #region uAuxPort overrides

        public override void InitQuerySend()
        {
            // no request needed
        }

        public override void OnClosed()
        {
            DiscardData();
        }

        public override void ProcessIncoming(NMEASentence sentence)
        {
            if (sentence is not NMEAStandartSentence nSentence) return;
            if (detected) ResetTimer();

            switch (nSentence.SentenceID)
            {
                case SentenceIdentifiers.HDT:
                    if (Mode != GNSSMode.CompassOnly)
                        HandleHDT(nSentence);
                    break;

                case SentenceIdentifiers.HDG:
                case SentenceIdentifiers.HDM:
                    if (Mode != GNSSMode.GNSSOnly)
                        HandleMagneticHeading(nSentence);
                    break;

                case SentenceIdentifiers.RMC:
                    if (Mode != GNSSMode.CompassOnly)
                        HandleRMC(nSentence);
                    break;
            }
        }

        #endregion

        #region NMEA Handlers

        private void HandleHDT(NMEAStandartSentence nSentence)
        {
            double hdn = O2D(nSentence.parameters[0]);
            if (double.IsNaN(hdn)) return;

            if (!detected) detected = true;

            Heading = hdn;
            IsHeadingTrue = true;
            HeadingUpdated?.Invoke(this, EventArgs.Empty);
        }

        private void HandleMagneticHeading(NMEAStandartSentence nSentence)
        {
            double hdn = O2D(nSentence.parameters[0]);
            if (double.IsNaN(hdn)) return;

            if (!detected) detected = true;

            Heading = hdn;
            IsHeadingTrue = false;
            HeadingUpdated?.Invoke(this, EventArgs.Empty);
        }

        private void HandleRMC(NMEAStandartSentence nSentence)
        {
            // Парсим параметры с нормальными именами
            var timeOfDay = nSentence.parameters[0] as DateTime?;
            var status = nSentence.parameters[1]?.ToString();
            var lat = O2D(nSentence.parameters[2]);
            var latHemi = nSentence.parameters[3]?.ToString();
            var lon = O2D(nSentence.parameters[4]);
            var lonHemi = nSentence.parameters[5]?.ToString();
            var speedKnots = O2D(nSentence.parameters[6]);
            var courseGnd = O2D(nSentence.parameters[7]);
            var date = nSentence.parameters[8] as DateTime?;
            var mode = nSentence.parameters[11]?.ToString();

            if (status == "Invalid") return;
            if (double.IsNaN(lat) || !lat.IsValidLatDeg()) return;
            if (double.IsNaN(lon) || !lon.IsValidLonDeg()) return;
            if (mode == "N") return;

            if (latHemi == "S") lat = -lat;
            if (lonHemi == "W") lon = -lon;

            if (!detected) detected = true;

            Latitude = lat;
            Longitude = lon;

            if (!double.IsNaN(speedKnots))
                GroundSpeed = 3.6 * NMEAParser.Bend2MpS(speedKnots);

            if (!double.IsNaN(courseGnd))
                CourseOverGround = courseGnd;

            GNSSTime = CombineDateTime(date, timeOfDay);
            LocationUpdated?.Invoke(this, EventArgs.Empty);
        }

        private static DateTime CombineDateTime(DateTime? date, DateTime? time)
        {
            if (date == null || time == null) return DateTime.MinValue;
            return date.Value
                .AddHours(time.Value.Hour)
                .AddMinutes(time.Value.Minute)
                .AddSeconds(time.Value.Second)
                .AddMilliseconds(time.Value.Millisecond);
        }

        #endregion

        #region Events

        public event EventHandler? HeadingUpdated;
        public event EventHandler? LocationUpdated;

        #endregion

        #region ToString

        public override string ToString()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append(base.ToString());
            if (!double.IsNaN(Heading))
            {
                sb.Append($", Heading: {Heading:F1}°");
                sb.Append(IsHeadingTrue ? " (T)" : " (M)");
            }
            if (!double.IsNaN(Latitude) && !double.IsNaN(Longitude))
                sb.Append($", Pos: {Latitude:F6}, {Longitude:F6}");
            return sb.ToString();
        }

        #endregion
    }
}