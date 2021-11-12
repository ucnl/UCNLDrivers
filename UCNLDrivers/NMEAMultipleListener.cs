using System;
using System.Collections.Generic;
using System.Text;
using UCNLNMEA;

namespace UCNLDrivers
{
    #region Custom items

    public struct SatelliteData
    {
        #region Properties

        public int PRNNumber;
        public int Elevation;
        public int Azimuth;
        public int SNR;

        #endregion

        #region Constructor

        public SatelliteData(int prn, int elevation, int azimuth, int snr)
        {
            PRNNumber = prn;
            Elevation = elevation;
            Azimuth = azimuth;
            SNR = snr;
        }

        #endregion

        #region Methods

        public override string ToString()
        {
            return string.Format("PRN: {0:00}, Elevation: {1:00}°, Azimuth: {2:000}°, SNR: {3:00} dB", PRNNumber, Elevation, Azimuth, SNR);
        }

        #endregion
    }

    #endregion

    #region Custom EventArgs

    public class NMEAMessageEventArgs : EventArgs
    {
        #region Properties

        public int SourceID { get; private set; }
        public string Message { get; private set; }

        #endregion

        #region Contructor

        public NMEAMessageEventArgs(int sourceID, string message)
        {
            SourceID = sourceID;
            Message = message;
        }

        #endregion
    }

    public class NMEAUnsupportedStandartEventArgs : EventArgs
    {
        #region Properties

        public int SourceID { get; private set; }
        public NMEAStandartSentence Sentence { get; private set; }

        #endregion

        #region Contructor

        public NMEAUnsupportedStandartEventArgs(int sourceID, NMEAStandartSentence sentence)
        {
            SourceID = sourceID;
            Sentence = sentence;
        }

        #endregion
    }

    public class NMEAUnsupportedProprietaryEventArgs : EventArgs
    {
        #region Properties

        public int SourceID { get; private set; }
        public NMEAProprietarySentence Sentence { get; private set; }

        #endregion

        #region Contructor

        public NMEAUnsupportedProprietaryEventArgs(int sourceID, NMEAProprietarySentence sentence)
        {
            SourceID = sourceID;
            Sentence = sentence;
        }

        #endregion
    }


    public class RMCMessageEventArgs : EventArgs
    {
        #region Properties

        public int SourceID { get; private set; }
        public TalkerIdentifiers TalkerID { get; private set; }
        public DateTime TimeFix { get; private set; }
        public double Latitude { get; private set; }
        public double Longitude { get; private set; }
        public double SpeedKmh { get; private set; }
        public double TrackTrue { get; private set; }
        public double MagneticVariation { get; private set; }
        public bool IsValid { get; private set; }       

        #endregion

        #region Constructor

        public RMCMessageEventArgs(int sourceID, TalkerIdentifiers talkerID, DateTime timeFix, double lat, double lon, double speedKmh, double trackTrue, double mVar, bool isValid)
        {
            SourceID = sourceID;
            TalkerID = talkerID;
            TimeFix = timeFix;
            Latitude = lat;
            Longitude = lon;
            SpeedKmh = speedKmh;
            TrackTrue = trackTrue;
            MagneticVariation = mVar;
            IsValid = isValid;
        }

        #endregion
    }

    public class VTGMessageEventArgs : EventArgs
    {
        #region Properties

        public int SourceID { get; private set; }
        public TalkerIdentifiers TalkerID { get; private set; }
        public double TrackTrue { get; private set; }
        public double TrackMagnetic { get; private set; }
        public double SpeedKmh { get; private set; }
        public bool IsValid { get; private set; }

        #endregion

        #region Constructor

        public VTGMessageEventArgs(int sourceID, TalkerIdentifiers talkerID, double trackTrue, double trackMagnetic, double speedKmh, bool isValid)
        {
            SourceID = sourceID;
            TalkerID = talkerID;
            TrackTrue = trackTrue;
            TrackMagnetic = TrackMagnetic;
            SpeedKmh = speedKmh;
            IsValid = isValid;
        }

        #endregion

    }

    public class HDGMessageEventArgs : EventArgs
    {
        #region Properties

        public int SourceID { get; private set; }             
        public TalkerIdentifiers TalkerID { get; private set; }
        public double MagneticHeading { get; private set; }
        public double MagneticVariation { get; private set; }
        public bool IsValid { get; private set; }

        #endregion

        #region Constructor

        public HDGMessageEventArgs(int sourceID, TalkerIdentifiers talkerID, double mHeading, double mVariation, bool isValid)
        {
            SourceID = sourceID;
            TalkerID = talkerID;
            MagneticHeading = mHeading;
            MagneticVariation = mVariation;
            IsValid = isValid;
        }


        #endregion
    }

    public class HDTMessageEventArgs : EventArgs
    {
        // $GPHDT,253.423,T*34

        #region Properties

        public int SourceID { get; private set; }
        public TalkerIdentifiers TalkerID { get; private set; }
        public double Heading { get; private set; }        
        public bool IsValid { get; private set; }

        #endregion

        #region Constructor

        public HDTMessageEventArgs(int sourceID, TalkerIdentifiers talkerID, double heading, bool isValid)
        {
            SourceID = sourceID;
            TalkerID = talkerID;
            Heading = heading;            
            IsValid = isValid;
        }


        #endregion
    }

    public class GGAMessageEventArgs : EventArgs
    {
        #region Properties

        public int SourceID { get; private set; }
        public TalkerIdentifiers TalkerID { get; private set; }
        public DateTime TimeStamp { get; private set; }
        public double Latitude { get; private set; }
        public double Longitude { get; private set; }

        public string GNSSQualityIndicator { get; private set; }
        public int SatellitesInUse { get; private set; }
        public double HDOP { get; private set; }
        public double OrthometricHeight { get; private set; } 
                // orthometric height units parameters[9]
        public double GeiodSeparation { get; private set; }
                // geoid separation units parameters[11]
        public double DGPSRecordAge { get; private set; }
        public int DatumID { get; private set; }
        public bool IsValid { get; private set; }

        #endregion

        #region Constructor

        public GGAMessageEventArgs(int sourceID, TalkerIdentifiers talkerID, DateTime timeStamp, 
            double lat, double lon, string gnssQuality, int satNum, double hdop, double orHeight, double gSep, double dgpsAge, int datumID, bool isValid)
        {
            SourceID = sourceID;
            TalkerID = talkerID;
            TimeStamp = timeStamp;
            Latitude = lat;
            Longitude = lon;
            GNSSQualityIndicator = gnssQuality;
            SatellitesInUse = satNum;
            HDOP = hdop;
            OrthometricHeight = orHeight;
            GeiodSeparation = gSep;
            DGPSRecordAge = dgpsAge;
            DatumID = datumID;
            IsValid = isValid;           
        }

        #endregion
    }

    public class GSVMessageEventArgs : EventArgs
    {
        #region Properties

        public int SourceID { get; private set; }
        public TalkerIdentifiers TalkerID { get; private set; }
        public SatelliteData[] SatellitesData { get; private set; }

        #endregion

        #region Constructor

        public GSVMessageEventArgs(int sourceID, TalkerIdentifiers taklerID, SatelliteData[] satsData)
        {
            SourceID = sourceID;
            TalkerID = taklerID;
            SatellitesData = satsData;
        }

        #endregion
    }

    public class GLLMessageEventArgs : EventArgs
    {
        #region Properties

        public int SourceID { get; private set; }
        public TalkerIdentifiers TalkerID { get; private set; }
        public double Latitude { get; private set; }
        public double Longitude { get; private set; }
        public DateTime TimeStamp { get; private set; }
        public bool IsValid { get; private set; }

        #endregion

        #region Constructor

        public GLLMessageEventArgs(int sourceID, TalkerIdentifiers talkerID, double latitude, double longitude, DateTime tStamp, bool isValid)
        {
            SourceID = sourceID;
            TalkerID = talkerID;
            Latitude = latitude;
            Longitude = longitude;
            TimeStamp = tStamp;
            IsValid = isValid;
        }

        #endregion
    }

    public class GSAMessageEventArgs : EventArgs
    {
        #region Properties

        public int SourceID { get; private set; }
        public TalkerIdentifiers TalkerID { get; private set; }    
        public string FixSelection { get; private set; }
        public string FixType { get; private set; }
        public int[] UsedSatellitesIDs { get; private set; }
        public double PDOP { get; private set; }
        public double HDOP { get; private set; }
        public double VDOP { get; private set; }
        public bool IsValid { get; private set; }

        #endregion

        #region Constructor

        public GSAMessageEventArgs(int sourceID, TalkerIdentifiers talkerID, string fSelection, string fType, List<int> satIDs, double pDOP, double hDOP, double vDOP, bool isValid)
        {
            SourceID = sourceID;
            TalkerID = talkerID;
            FixSelection = fSelection;
            FixType = fType;
            UsedSatellitesIDs = satIDs.ToArray();
            PDOP = pDOP;
            HDOP = hDOP;
            VDOP = vDOP;
            IsValid = isValid;
        }

        #endregion

    }

    public class MTWMessageEventArgs : EventArgs
    {
        #region Properties

        public int SourceID { get; private set; }
        public TalkerIdentifiers TalkerID { get; private set; }
        public double MeanWaterTemperature { get; private set; }
        public bool IsValid { get; private set; }

        #endregion

        #region Constructor

        public MTWMessageEventArgs(int sourceID, TalkerIdentifiers talkerID, double temp, bool isValid)
        {
            SourceID = sourceID;
            TalkerID = talkerID;
            MeanWaterTemperature = temp;
            IsValid = isValid;
        }

        #endregion
    }

    #endregion
    
    public class NMEAMultipleListener
    {
        #region Properties

        readonly int buffer_size = 4096;
        Dictionary<int, byte[]> buffer;
        Dictionary<int, int> bIdx;
        Dictionary<int, bool> isSntStarted;

        static byte nmeaSntStartByte = Convert.ToByte(NMEAParser.SentenceStartDelimiter);
        static byte nmeaEndByte = Convert.ToByte(NMEAParser.SentenceEndDelimiter[NMEAParser.SentenceEndDelimiter.Length - 1]);



        //Dictionary<int, StringBuilder> buffer_;
        Dictionary<int, List<SatelliteData>> fullSatellitesData;

        private delegate void ProcessCommandDelegate(int sourceID, TalkerIdentifiers talkerID, object[] parameters);
        private Dictionary<SentenceIdentifiers, ProcessCommandDelegate> standardSenteceParsers;

        delegate T NullChecker<T>(object parameter);
        NullChecker<int> intNullChecker = (x => x == null ? -1 : (int)x);
        NullChecker<double> doubleNullChecker = (x => x == null ? double.NaN : (double)x);
        NullChecker<string> stringNullChecker = (x => x == null ? string.Empty : (string)x);
               
        #endregion

        #region Constructor

        public NMEAMultipleListener()
        {
            #region buffer

            //buffer_ = new Dictionary<int, StringBuilder>();
            buffer = new Dictionary<int, byte[]>();
            bIdx = new Dictionary<int, int>();
            isSntStarted = new Dictionary<int, bool>();

            fullSatellitesData = new Dictionary<int,List<SatelliteData>>();

            #endregion
            
            #region parsers initialization

            standardSenteceParsers = new Dictionary<SentenceIdentifiers, ProcessCommandDelegate>()
            {
                // GNSS receivers
                { SentenceIdentifiers.GGA, new ProcessCommandDelegate(OnGGASentence) },
                { SentenceIdentifiers.GSV, new ProcessCommandDelegate(OnGSVSentence) },
                { SentenceIdentifiers.GLL, new ProcessCommandDelegate(OnGLLSentence) },
                { SentenceIdentifiers.RMC, new ProcessCommandDelegate(OnRMCSentence) },
                { SentenceIdentifiers.VTG, new ProcessCommandDelegate(OnVTGSentence) },
                { SentenceIdentifiers.GSA, new ProcessCommandDelegate(OnGSASentence) },                

                // Magnetic compass
                { SentenceIdentifiers.HDG, new ProcessCommandDelegate(OnHDGSentence) },
                { SentenceIdentifiers.HDT, new ProcessCommandDelegate(OnHDTSentence) },

                // Custom marine equipment
                { SentenceIdentifiers.MTW, new ProcessCommandDelegate(OnMTWSentence) },
            };

            #endregion
        }

        #endregion

        #region Methods

        #region Public
        
        public void ProcessIncoming(int sourceID, byte[] data)
        {
            if (!buffer.ContainsKey(sourceID))
            {
                buffer.Add(sourceID, new byte[buffer_size]);
                bIdx.Add(sourceID, 0);
                isSntStarted.Add(sourceID, false);
            }

            for (int i = 0; i < data.Length; i++)
            {
                if (data[i] == nmeaSntStartByte)
                {
                    isSntStarted[sourceID] = true;
                    Array.Clear(buffer[sourceID], 0, buffer[sourceID].Length);
                    bIdx[sourceID] = 0;
                    buffer[sourceID][bIdx[sourceID]] = data[i];
                    bIdx[sourceID]++;
                }
                else
                {
                    if (isSntStarted[sourceID])
                    {
                        buffer[sourceID][bIdx[sourceID]] = data[i];
                        bIdx[sourceID]++;

                        if (data[i] == nmeaEndByte)
                        {
                            isSntStarted[sourceID] = false;
                            Parse(sourceID, Encoding.ASCII.GetString(buffer[sourceID], 0, bIdx[sourceID]));
                        }
                        else
                        {
                            if (bIdx[sourceID] >= buffer[sourceID].Length - 1)
                                isSntStarted[sourceID] = false;
                        }
                    }
                }
            }
        }

        public void ProcessIncoming(int sourceID, string data)
        {
            ProcessIncoming(sourceID, Encoding.ASCII.GetBytes(data));
        }

        //public void ProcessIncoming(int sourceID, string data)
        //{
        //    if (!buffer_.ContainsKey(sourceID))
        //        buffer_.Add(sourceID, new StringBuilder());

        //    buffer_[sourceID].Append(data);
        //    var temp = buffer_[sourceID].ToString();

        //    int lIndex = temp.LastIndexOf(NMEAParser.SentenceEndDelimiter);
        //    if (lIndex >= 0)
        //    {
        //        buffer_[sourceID].Remove(0, lIndex + 2);
        //        if (lIndex + 2 < temp.Length)
        //            temp = temp.Remove(lIndex + 2);

        //        temp = temp.Trim(new char[] { '\0' });

        //        int startIdx = temp.IndexOf(NMEAParser.SentenceStartDelimiter);
        //        if (startIdx > 0)
        //            temp = temp.Remove(0, startIdx);

        //        var lines = temp.Split(NMEAParser.SentenceEndDelimiter.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
              
        //        for (int i = 0; i < lines.Length; i++)
        //                Parse(sourceID, string.Format("{0}{1}", lines[i], NMEAParser.SentenceEndDelimiter));                
        //    }

        //    if (buffer_[sourceID].Length >= ushort.MaxValue)
        //        buffer_[sourceID].Remove(0, short.MaxValue);
        //}        

        #endregion

        #region Private

        #region Standard sentence parsers

        private void OnGGASentence(int sourceID, TalkerIdentifiers talkerID, object[] parameters)
        {
            if (GGASentenceReceived != null)
            {
                DateTime tStamp = (DateTime)parameters[0];

                double latitude = doubleNullChecker(parameters[1]);
                double longitude = doubleNullChecker(parameters[3]);

                string gnssQualityIndicator = parameters[5].ToString();
                int satellitesInUse = intNullChecker(parameters[6]);
                double HDOP = doubleNullChecker(parameters[7]);
                double orthometricHeight = doubleNullChecker(parameters[8]);
                // orthometric height units parameters[9]
                double geiodSeparation = doubleNullChecker(parameters[10]);
                // geoid separation units parameters[11]
                double DGPSRecordAge = doubleNullChecker(parameters[12]);
                int datumID = intNullChecker(parameters[13]);

                bool isValid = (!double.IsNaN(latitude)) && latitude.IsValidLatDeg() &&
                               (!double.IsNaN(longitude)) && longitude.IsValidLonDeg() &&
                               (!double.IsNaN(HDOP));

                if (isValid)
                {                    
                    if (parameters[2].ToString() == "S") latitude = -latitude;
                    if (parameters[4].ToString() == "W") longitude = -longitude;
                }

                GGASentenceReceived.Rise(this, new GGAMessageEventArgs(sourceID, talkerID, tStamp, 
                    latitude, longitude, gnssQualityIndicator, satellitesInUse, HDOP, orthometricHeight, geiodSeparation, DGPSRecordAge, datumID, isValid));
            }            
        }

        private void OnGSVSentence(int sourceID, TalkerIdentifiers talkerID, object[] parameters)
        {
            if (GSVSentenceReceived != null)
            {               
                List<SatelliteData> satellites = new List<SatelliteData>();

                int totalMessages = (int)parameters[0];
                int currentMessageNumber = (int)parameters[1];

                int satellitesDataItemsCount = (parameters.Length - 3) / 4;

                for (int i = 0; i < satellitesDataItemsCount; i++)
                {
                    satellites.Add(
                        new SatelliteData(
                            intNullChecker(parameters[3 + 4 * i]),
                            intNullChecker(parameters[4 + 4 * i]),
                            intNullChecker(parameters[5 + 4 * i]),
                            intNullChecker(parameters[6 + 4 * i])));
                }

                if (!fullSatellitesData.ContainsKey(sourceID))
                    fullSatellitesData.Add(sourceID, new List<SatelliteData>());

                if (currentMessageNumber == 1)
                    fullSatellitesData[sourceID] = new List<SatelliteData>();

                fullSatellitesData[sourceID].AddRange(satellites.ToArray());

                if (currentMessageNumber == totalMessages)
                    GSVSentenceReceived.Rise(this, new GSVMessageEventArgs(sourceID, talkerID, fullSatellitesData[sourceID].ToArray()));
            }
        }

        private void OnGLLSentence(int sourceID, TalkerIdentifiers talkerID, object[] parameters)
        {
            if (GLLSentenceReceived != null)
            {
                double latitude = doubleNullChecker(parameters[0]);
                double longitude = doubleNullChecker(parameters[2]);
                DateTime tStamp = (DateTime)parameters[4];
                bool isValid = (parameters[5].ToString() == "Valid") &&
                               (!double.IsNaN(latitude)) && latitude.IsValidLatDeg() &&
                               (!double.IsNaN(longitude)) && longitude.IsValidLonDeg();

                if (isValid)
                {                  
                    if (parameters[1].ToString() == "S") latitude = -latitude;
                    if (parameters[3].ToString() == "W") longitude = -longitude;
                }

                GLLSentenceReceived.Rise(this, new GLLMessageEventArgs(sourceID, talkerID, latitude, longitude, tStamp, isValid));
            }
        }

        private void OnRMCSentence(int sourceID, TalkerIdentifiers talkerID, object[] parameters)
        {           
            if (RMCSentenceReceived != null)
            {
                DateTime tStamp = parameters[0] == null ? DateTime.MinValue : (DateTime)parameters[0];

                var latitude = doubleNullChecker(parameters[2]);
                var longitude = doubleNullChecker(parameters[4]);
                var groundSpeed = doubleNullChecker(parameters[6]);
                var courseOverGround = doubleNullChecker(parameters[7]);
                DateTime dateTime = parameters[8] == null ? DateTime.MinValue : (DateTime)parameters[8];
                var magneticVariation = doubleNullChecker(parameters[9]);

                bool isValid = (parameters[1].ToString() != "Invalid") &&
                               (!double.IsNaN(latitude)) && latitude.IsValidLatDeg() &&
                               (!double.IsNaN(longitude)) && longitude.IsValidLonDeg() &&
                               (!double.IsNaN(groundSpeed)) &&
                               //(!double.IsNaN(courseOverGround)) &&
                               (parameters[11].ToString() != "N");

                if (isValid)
                {                    
                    dateTime = dateTime.AddHours(tStamp.Hour);
                    dateTime = dateTime.AddMinutes(tStamp.Minute);
                    dateTime = dateTime.AddSeconds(tStamp.Second);
                    dateTime = dateTime.AddMilliseconds(tStamp.Millisecond);
                    groundSpeed = 3.6 * NMEAParser.Bend2MpS(groundSpeed);

                    if (parameters[3].ToString() == "S") latitude = -latitude;
                    if (parameters[5].ToString() == "W") longitude = -longitude;
                }

                RMCSentenceReceived.Rise(this, new RMCMessageEventArgs(sourceID, talkerID, dateTime, latitude, longitude, groundSpeed, courseOverGround, magneticVariation, isValid));
            }
        }

        private void OnVTGSentence(int sourceID, TalkerIdentifiers talkerID, object[] parameters)
        {
            if (VTGSentenceReceived != null)
            {
                var trackTrue = doubleNullChecker(parameters[0]);
                var trackMagnetic = doubleNullChecker(parameters[2]);
                var speedKnots = doubleNullChecker(parameters[4]);
                var skUnits = (string)parameters[5];
                var speedKmh = doubleNullChecker(parameters[6]);
                var sKmUnits = (string)parameters[7];

                bool isValid = (!double.IsNaN(trackTrue) || !double.IsNaN(trackMagnetic)) &&
                               !double.IsNaN(speedKnots) &&
                               !double.IsNaN(speedKmh);


                VTGSentenceReceived.Rise(this, new VTGMessageEventArgs(sourceID, talkerID, trackTrue, trackMagnetic, speedKmh, isValid));
            }
        }

        private void OnGSASentence(int sourceID, TalkerIdentifiers talkerID, object[] parameters)
        {
            if (GSASentenceReceived != null)
            {                
                string fixSelection = parameters[0].ToString();
                string fixType = parameters[1].ToString();

                List<int> satIDs = new List<int>();

                for (int i = 2; i < 14; i++)
                {
                    int tPRN = intNullChecker(parameters[i]);
                    if (tPRN >= 0) satIDs.Add(tPRN);
                }

                double PDOP = doubleNullChecker(parameters[14]);
                double HDOP = doubleNullChecker(parameters[15]);
                double VDOP = doubleNullChecker(parameters[16]);

                bool isValid = (satIDs.Count > 0) && (!double.IsNaN(PDOP)) && (!double.IsNaN(HDOP)) && (!double.IsNaN(VDOP));

                GSASentenceReceived.Rise(this, new GSAMessageEventArgs(sourceID, talkerID, fixSelection, fixType, satIDs, PDOP, HDOP, VDOP, isValid));
            }
        }

        private void OnHDGSentence(int sourceID, TalkerIdentifiers talkerID, object[] parameters)
        {           
            if (HDGSentenceReceived != null)
            {
                double magneticHeading = doubleNullChecker(parameters[0]);
                double magneticVariation = doubleNullChecker(parameters[3]);
                if (double.IsNaN(magneticVariation)) magneticVariation = 0.0;

                bool isValid = !double.IsNaN(magneticHeading);

                HDGSentenceReceived.Rise(this, new HDGMessageEventArgs(sourceID, talkerID, magneticHeading, magneticVariation, isValid));
            }
        }

        private void OnHDTSentence(int sourceID, TalkerIdentifiers talkerID, object[] parameters)
        {
            // $GPHDT,253.423,T*34
            if (HDTSentenceReceived != null)
            {
                double heading = doubleNullChecker(parameters[0]);
                bool isValid = !double.IsNaN(heading);

                HDTSentenceReceived.Rise(this, new HDTMessageEventArgs(sourceID, talkerID, heading, isValid));    
            }
        }

        private void OnMTWSentence(int sourceID, TalkerIdentifiers talkerID, object[] parameters)
        {
            if (MTWSentenceReceived != null)
            {
                double temp = doubleNullChecker(parameters[0]);
                bool isValid = !double.IsNaN(temp);
                MTWSentenceReceived.Rise(this, new MTWMessageEventArgs(sourceID, talkerID, temp, isValid));
            }
        }

        #endregion

        private void Parse(int sourceID, string message)
        {            
            
            NMEAIncomingMessageReceived.Rise(this, new NMEAMessageEventArgs(sourceID, message));

            try
            {
                var pResult = NMEAParser.Parse(message);

                if (pResult is NMEAStandartSentence)
                {
                    NMEAStandartSentence sentence = (NMEAStandartSentence)pResult;
                    if (standardSenteceParsers.ContainsKey(sentence.SentenceID))
                        standardSenteceParsers[sentence.SentenceID](sourceID, sentence.TalkerID, sentence.parameters);
                    else
                    {
                        NMEAStandartUnsupportedSentenceParsed.Rise(this, new NMEAUnsupportedStandartEventArgs(sourceID, sentence));
                    }
                }
                else
                {
                    NMEAProprietaryUnsupportedSentenceParsed.Rise(this, new NMEAUnsupportedProprietaryEventArgs(sourceID, (pResult as NMEAProprietarySentence)));
                }

            }
            catch (Exception ex)
            {
                LogEventHandler.Rise(this, new LogEventArgs(LogLineType.ERROR, ex));
            }
        }

        #endregion

        #endregion

        #region Events

        public EventHandler<NMEAUnsupportedStandartEventArgs> NMEAStandartUnsupportedSentenceParsed;
        public EventHandler<NMEAUnsupportedProprietaryEventArgs> NMEAProprietaryUnsupportedSentenceParsed;
        public EventHandler<NMEAMessageEventArgs> NMEAIncomingMessageReceived;        

        public EventHandler<HDGMessageEventArgs> HDGSentenceReceived;
        public EventHandler<HDTMessageEventArgs> HDTSentenceReceived;
        public EventHandler<RMCMessageEventArgs> RMCSentenceReceived;
        public EventHandler<VTGMessageEventArgs> VTGSentenceReceived;
        public EventHandler<GGAMessageEventArgs> GGASentenceReceived;
        public EventHandler<GSVMessageEventArgs> GSVSentenceReceived;
        public EventHandler<GLLMessageEventArgs> GLLSentenceReceived;
        public EventHandler<GSAMessageEventArgs> GSASentenceReceived;
        public EventHandler<MTWMessageEventArgs> MTWSentenceReceived;

        public EventHandler<LogEventArgs> LogEventHandler;

        #endregion        
    }
}
