using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;

namespace UCNLDrivers
{
    public class StringEventArgs : EventArgs
    {
        public TimeSpan TS { get; private set; }
        public string Line { get; private set; }

        public StringEventArgs(string line, TimeSpan ts)
        {
            TS = ts;
            Line = line;
        }

        public StringEventArgs(string line)
        {
            TS = TimeSpan.Zero;
            Line = line;
        }
    }

    public class LogPlayer
    {
        #region Properties

        public bool IsRunning { get; private set; }

        public string LogFileName { get; private set; }

        readonly char[] tSplitter = new char[] { ':' };
        readonly char[] tExSplitter = new char[] { '-', '_' };

        #endregion

        #region Methods

        public void Playback(string fileName)
        {
            if (!IsRunning)
            {
                LogFileName = fileName;
                _ = ThreadPool.QueueUserWorkItem(PlaybackThread, fileName);
                IsRunning = true;
            }
        }

        public void PlaybackInstant(string fileName)
        {
            if (!IsRunning)
            {
                LogFileName = fileName;
                _ = ThreadPool.QueueUserWorkItem(PlaybackInstantThread, fileName);
                IsRunning = true;
            }
        }


        public void RequestToStop()
        {
            if (IsRunning)
                IsRunning = false;
        }

        public List<Tuple<double, string>> ParseLog(string fileName)
        {
            List<Tuple<double, string>> result = new List<Tuple<double, string>>();

            TimeSpan lstartTs = TimeSpan.MinValue;
            TimeSpan ts = TimeSpan.MinValue;
            bool tsInitialized = false;

            try
            {
                using (StreamReader sr = File.OpenText(fileName))
                {
                    string s = string.Empty;
                    while ((s = sr.ReadLine()) != null)
                    {
                        int idx = s.IndexOf(' ');
                        if (idx >= 0)
                        {
                            string rs = s.Substring(idx + 1);
                            string ls = s.Substring(0, idx);

                            if (ParseTime(ls, out ts) || ParseTimeEx(ls, out ts))
                            {
                                if (!tsInitialized)
                                {
                                    lstartTs = ts;
                                    tsInitialized = true;
                                }

                                var interval = ts.Subtract(lstartTs).TotalMilliseconds / 1000.0;

                                result.Add(new Tuple<double, string>(interval, rs));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogEventHandler.Rise(this, new LogEventArgs(LogLineType.ERROR, ex));
            }

            return result;
        }


        private bool ParseTime(string s, out TimeSpan ts)
        {
            bool result = false;
            ts = TimeSpan.MinValue;
            var splits = s.Split(tSplitter, StringSplitOptions.RemoveEmptyEntries);
            if (splits.Length == 3)
            {
                int hr = int.Parse(splits[0]);
                int mn = int.Parse(splits[1]);
                double sec = double.Parse(splits[2], CultureInfo.InvariantCulture);
                int sc = Convert.ToInt32(sec);
                int ms = Convert.ToInt32(1000 * (sec - sc));

                ts = new TimeSpan(0, hr, mn, sc, ms);
                result = true;
            }

            return result;
        }        

        private bool ParseTimeEx(string s, out TimeSpan ts)
        {
            bool result = false;
            ts = TimeSpan.MinValue;
            var splits = s.TrimEnd(tSplitter).Split(tExSplitter, StringSplitOptions.RemoveEmptyEntries);

            // 2022-05-21_16-47-52

            if (splits.Length == 6)
            {
                int hr = int.Parse(splits[3]);
                int mn = int.Parse(splits[4]);
                double sec = double.Parse(splits[5], CultureInfo.InvariantCulture);
                int sc = Convert.ToInt32(sec);
                int ms = Convert.ToInt32(1000 * (sec - sc));

                ts = new TimeSpan(0, hr, mn, sc, ms);
                result = true;
            }

            return result;
        }

        private void PlaybackThread(object sinfo)
        {
            string fileName = sinfo as string;
            TimeSpan prevLineTS = TimeSpan.MinValue;
            TimeSpan ts = TimeSpan.MinValue;
            bool tsInitialized = false;

            try
            {
                using (StreamReader sr = File.OpenText(fileName))
                {
                    string s = string.Empty;
                    while (((s = sr.ReadLine()) != null) && IsRunning)
                    {
                        int idx = s.IndexOf(' ');
                        if (idx >= 0)
                        {
                            string rs = s.Substring(idx + 1);
                            string ls = s.Substring(0, idx);

                            if (ParseTime(ls, out ts) || ParseTimeEx(ls, out ts))
                            {
                                if (!tsInitialized)
                                {
                                    prevLineTS = ts;
                                    tsInitialized = true;
                                }

                                var interval = Convert.ToInt32(ts.Subtract(prevLineTS).TotalMilliseconds);

                                if (interval > 10)
                                    Thread.Sleep(interval);

                                prevLineTS = ts;
                                NewLogLineHandler.Rise(this, new StringEventArgs(rs));
                            }
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                LogEventHandler.Rise(this, new LogEventArgs(LogLineType.ERROR, ex));
            }

            IsRunning = false;
            LogPlaybackFinishedHandler.Rise(this, new EventArgs());
        }

        private void PlaybackInstantThread(object sinfo)
        {
            string fileName = sinfo as string;           

            try
            {
                using (StreamReader sr = File.OpenText(fileName))
                {
                    string s = string.Empty;
                    while (((s = sr.ReadLine()) != null) && IsRunning)
                    {
                        int idx = s.IndexOf(' ');
                        if (idx >= 0)
                        {
                            string rs = s.Substring(idx + 1);
                            string ls = s.Substring(0, idx);

                            if (ParseTime(ls, out TimeSpan ts) || ParseTimeEx(ls, out ts))
                            {                                
                                NewLogLineHandler.Rise(this, new StringEventArgs(rs, ts));
                            }
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                LogEventHandler.Rise(this, new LogEventArgs(LogLineType.ERROR, ex));
            }

            IsRunning = false;
            LogPlaybackFinishedHandler.Rise(this, new EventArgs());
        }

        #endregion

        #region Events

        public EventHandler<StringEventArgs> NewLogLineHandler;
        public EventHandler LogPlaybackFinishedHandler;
        public EventHandler<LogEventArgs> LogEventHandler;

        #endregion
    }
}
