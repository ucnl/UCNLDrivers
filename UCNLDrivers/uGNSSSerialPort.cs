using System;
using UCNLNMEA;

namespace UCNLDrivers
{
    public class uGNSSSerialPort : uSerialPort
    {
        #region Properties

        public bool MagneticOnly { get; set; }

        public double Heading { get; private set; }
        public double Latitude { get; private set; }
        public double Longitude { get; private set; }
        public double GroundSpeed { get; private set; }
        public double CourseOverGround { get; private set; }
        public DateTime GNSSTime { get; private set; }

        #endregion

        #region Constructor

        public uGNSSSerialPort(BaudRate baudRate)
            : base(baudRate)
        {
            base.PortDescription = "GNSS";
            base.IsLogIncoming = true;
            base.IsTryAlways = true;

            Heading = double.NaN;
            Latitude = double.NaN;
            Longitude = double.NaN;
            GroundSpeed = double.NaN;
            CourseOverGround = double.NaN;
            GNSSTime = DateTime.MinValue;
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
        }

        private static double O2D(object o)
        {
            return (o == null) ? double.NaN : (double)o;
        }

        private static int O2I(object o)
        {
            return (o == null) ? -1 : (int)o;
        }

        #endregion

        #region uSerialPort

        public override void InitQuerySend()
        {
            // no request needed for a GNSS or a GNSS-compass
        }

        public override void OnClosed()
        {
            //
        }

        public override void ProcessIncoming(NMEASentence sentence)
        {
            if (sentence is NMEAStandartSentence)
            {
                NMEAStandartSentence nSentence = (sentence as NMEAStandartSentence);

                if (detected)
                    ResetTimer();

                if (nSentence.SentenceID == SentenceIdentifiers.HDT)
                {
                    if (!detected)
                        detected = true;

                    if (!MagneticOnly)
                    {
                        double hdn = O2D(nSentence.parameters[0]);
                        if (!double.IsNaN(hdn))
                        {
                            Heading = hdn;
                            HeadingUpdated.Rise(this, new EventArgs());
                        }
                    }
                }
                else if (nSentence.SentenceID == SentenceIdentifiers.HDG)
                {
                    if (MagneticOnly)
                    {
                        double hdn = O2D(nSentence.parameters[0]);
                        if (!double.IsNaN(hdn))
                        {
                            Heading = hdn;
                            HeadingUpdated.Rise(this, new EventArgs());
                        }
                    }
                }
                else if (nSentence.SentenceID == SentenceIdentifiers.HDM)
                {
                    if (MagneticOnly)
                    {
                        double hdn = O2D(nSentence.parameters[0]);
                        if (!double.IsNaN(hdn))
                        {
                            Heading = hdn;
                            HeadingUpdated.Rise(this, new EventArgs());
                        }
                    }
                }
                else if (nSentence.SentenceID == SentenceIdentifiers.RMC)
                {
                    if (!detected)
                        detected = true;

                    // $GBRMC,072202.00,A,4332.85825286,N,03941.01553346,E,    ,      ,240524,7.6,E,D,C*48
                    // $GPRMC,112931.50,A,4405.850551,  N,03900.187941,  E,0.27,112.74,180425,   ,*30

                    DateTime tStamp = nSentence.parameters[0] == null ? DateTime.MinValue : (DateTime)nSentence.parameters[0];

                    var latitude = O2D(nSentence.parameters[2]);
                    var longitude = O2D(nSentence.parameters[4]);
                    var groundSpeed = O2D(nSentence.parameters[6]);
                    var courseOverGround = O2D(nSentence.parameters[7]);
                    DateTime dateTime = nSentence.parameters[8] == null ? DateTime.MinValue : (DateTime)nSentence.parameters[8];

                    bool isValid = (nSentence.parameters[1].ToString() != "Invalid") &&
                                   (!double.IsNaN(latitude)) && latitude.IsValidLatDeg() &&
                                   (!double.IsNaN(longitude)) && longitude.IsValidLonDeg() &&
                                   (nSentence.parameters[11].ToString() != "N");

                    if (isValid)
                    {
                        dateTime = dateTime.AddHours(tStamp.Hour);
                        dateTime = dateTime.AddMinutes(tStamp.Minute);
                        dateTime = dateTime.AddSeconds(tStamp.Second);
                        dateTime = dateTime.AddMilliseconds(tStamp.Millisecond);
                        

                        if (nSentence.parameters[3].ToString() == "S") latitude = -latitude;
                        if (nSentence.parameters[5].ToString() == "W") longitude = -longitude;

                        Latitude = latitude;
                        Longitude = longitude;

                        if (!double.IsNaN(groundSpeed))
                        {
                            groundSpeed = 3.6 * NMEAParser.Bend2MpS(groundSpeed);
                            GroundSpeed = groundSpeed;
                        }

                        if (!double.IsNaN(courseOverGround))
                            CourseOverGround = courseOverGround;

                        GNSSTime = dateTime;

                        LocationUpdated.Rise(this, new EventArgs());
                    }
                }
            }
        }

        #endregion

        #region Events

        public EventHandler HeadingUpdated;
        public EventHandler LocationUpdated;

        #endregion
    }
}
