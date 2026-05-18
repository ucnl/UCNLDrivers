using UCNLNMEA;

namespace UCNLDrivers
{
    [Obsolete]
    public class uMagneticCompassPort : uSerialPort
    {
        #region Properties

        public double Heading { get; set; }


        #endregion

        #region Constructor

        public uMagneticCompassPort(BaudRate baudRate)
            : base(baudRate)
        {
            base.PortDescription = "HDM";
            base.IsLogIncoming = true;
            base.IsTryAlways = true;

            Heading = double.NaN;

        }


        #endregion

        #region Methods

        private void DiscardData()
        {
            Heading = double.NaN;
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
            // no request needed for magnetic compass devices
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

                if ((nSentence.SentenceID == SentenceIdentifiers.HDT) ||
                    (nSentence.SentenceID == SentenceIdentifiers.HDG) ||
                    (nSentence.SentenceID == SentenceIdentifiers.HDM))

                {
                    if (!detected)
                        detected = true;

                    if (detected)
                        ResetTimer();

                    double hdn = O2D(nSentence.parameters[0]);
                    if (!double.IsNaN(hdn))
                    {
                        Heading = hdn;
                        HeadingUpdated.Rise(this, new EventArgs());
                    }
                }
            }
        }

        #endregion

        #region Events

        public EventHandler HeadingUpdated;

        #endregion
    }
}
