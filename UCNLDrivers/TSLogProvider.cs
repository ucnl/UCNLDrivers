using System.Text;

namespace UCNLDrivers
{    
    /// <summary>
    /// Thread-safe logger
    /// </summary>

    public enum LogLineType
    {
        INFO,
        ERROR,
        CRITICAL
    }

    public class LogEventArgs : EventArgs
    {
        #region Properties

        public LogLineType EventType { get; private set; }
        public string LogString { get; private set; }

        #endregion

        #region Constructor

        public LogEventArgs(LogLineType eventType, Exception ex)
            : this(eventType, string.Format("{0} {1}", ex.Message, ex.StackTrace))
        {
        }
        
        public LogEventArgs(LogLineType eventType, string logString)
        {
            EventType = eventType;
            LogString = logString;
        }

        #endregion
    }

    public class TextAddedEventArgs : EventArgs
    {
        #region Properties

        public string Text { get; private set; }

        #endregion

        #region Constructor

        public TextAddedEventArgs(string text)
        {
            Text = text;
        }

        #endregion
    }

    public class TSLogProvider
    {
        #region Properties

        public bool RepressEvent { get; set; }

        public string FileName { get; private set; }
        TSQueue<string> queue;
        int synLock;

        DateTime prevLogLineTimeStamp;

        #endregion

        #region Constructor

        public TSLogProvider(string fileName)
        {
            FileName = fileName;            
            queue = new TSQueue<string>(32);
            queue.ItemEnqueued += new EventHandler(queue_ItemEnquequed);

            prevLogLineTimeStamp = DateTime.Now;
        }

        #endregion

        #region Methods

        public void CleanOldLogs(string logRoot, long maxTotalSizeBytes, string mask, out int filesDeleted, out long bytesFreed)
        {
            filesDeleted = 0;
            bytesFreed = 0;

            if (!Directory.Exists(logRoot))
                return;

            var logFiles = Directory.GetFiles(logRoot, mask, SearchOption.AllDirectories)
                .Select(f => new FileInfo(f))
                .Select(fi => (Path: fi.FullName, Size: fi.Length, LastWrite: fi.LastWriteTime))
                .OrderBy(f => f.LastWrite)
                .ToList();

            long totalSize = logFiles.Sum(f => f.Size);

            foreach (var file in logFiles)
            {
                if (totalSize <= maxTotalSizeBytes) break;

                File.Delete(file.Path);
                totalSize -= file.Size;
                bytesFreed += file.Size;
                filesDeleted++;
            }

            CleanEmptyDirs(logRoot);
        }

        private void CleanEmptyDirs(string root)
        {
            foreach (var dir in Directory.GetDirectories(root))
            {
                CleanEmptyDirs(dir);
                if (!Directory.EnumerateFileSystemEntries(dir).Any())
                    Directory.Delete(dir);
            }
        }




        public static string ShortDateString(DateTime dt)
        {
            return string.Format("{0:00}-{1:00}-{2:0000}", dt.Day, dt.Month, dt.Year);
        }

        public static string LongTimeString(DateTime dt)
        {
            return string.Format("{0:00}:{1:00}:{2:00}.{3:000}", dt.Hour, dt.Minute, dt.Second, dt.Millisecond);
        }
        
        public void WriteStart()
        {
            DateTime now = DateTime.Now;
            Write(string.Format("\r\n<Log started at {0}, {1}>", ShortDateString(now), LongTimeString(now)), false);
        }

        public string Write(Exception ex)
        {
            return Write(ex, true);
        }

        public string Write(Exception ex, bool isTimeStamp)
        {
            if (ex != null)
                return Write(string.Format("{0} {1}", ex.Message, ex.StackTrace), isTimeStamp);
            else
                return string.Empty;
        }

        public string Write(string logString)
        {
            return Write(logString, true);
        }

        public string Write(string logString, bool isTimeStamp)
        {
            DateTime now = DateTime.Now;
            StringBuilder sb = new StringBuilder();

            if (isTimeStamp)
            {
                if (now.Subtract(prevLogLineTimeStamp).Days > 1)
                    sb.AppendFormat("Log continues at {0}", ShortDateString(now));

                sb.AppendFormat("{0}: ", LongTimeString(now));
            }

            sb.Append(logString);

            prevLogLineTimeStamp = now;

            if (!logString.EndsWith("\r\n"))
                sb.Append("\r\n");

            var result = sb.ToString();
            queue.Enqueue(result);

            if (!RepressEvent)
                TextAddedEvent.Rise(this, new TextAddedEventArgs(result));

            return result;
        }

        public string FinishLog()
        {
            DateTime now = DateTime.Now;
            var result = Write(string.Format("<Log finished at {0}, {1}>", ShortDateString(now), LongTimeString(now)), false);
            Flush();

            return result;
        }


        public void WriteStartSilent()
        {
            DateTime now = DateTime.Now;
            WriteSilent(string.Format("\r\n<Log started at {0}, {1}>", ShortDateString(now), LongTimeString(now)), false);
        }

        public string WriteSilent(Exception ex)
        {
            return WriteSilent(ex, true);
        }

        public string WriteSilent(Exception ex, bool isTimeStamp)
        {
            if (ex != null)
                return WriteSilent(string.Format("{0} {1}", ex.Message, ex.StackTrace), isTimeStamp);
            else
                return string.Empty;
        }

        public string WriteSilent(string logString)
        {
            return WriteSilent(logString, true);
        }

        public string WriteSilent(string logString, bool isTimeStamp)
        {
            DateTime now = DateTime.Now;
            StringBuilder sb = new StringBuilder();

            if (isTimeStamp)
            {
                if (now.Subtract(prevLogLineTimeStamp).Days > 1)
                    sb.AppendFormat("Log continues at {0}", ShortDateString(now));

                sb.AppendFormat("{0}: ", LongTimeString(now));
            }

            sb.Append(logString);

            prevLogLineTimeStamp = now;

            if (!logString.EndsWith("\r\n"))
                sb.Append("\r\n");

            var result = sb.ToString();
            queue.Enqueue(result);           

            return result;
        }


        public string FinishLogSilent()
        {
            DateTime now = DateTime.Now;
            var result = WriteSilent(string.Format("<Log finished at {0}, {1}>", ShortDateString(now), LongTimeString(now)), false);
            Flush();
            
            return result;
        }

        public void Flush()
        {
            while (queue.Count > 0)
                queue_ItemEnquequed(null, null);
        }

        public void Restart()
        {
            while (Interlocked.CompareExchange(ref synLock, 1, 0) != 0) { Thread.SpinWait(1); }            
            WriteStart();
            Interlocked.Decrement(ref synLock);
        }

        #endregion

        #region Handlers

        private void queue_ItemEnquequed(object sender, EventArgs e)
        {
            if (Interlocked.CompareExchange(ref synLock, 1, 0) == 0)
            {
                if (queue.Count > 0)
                {
                    var lines = queue.Dump();
                    StringBuilder sb = new StringBuilder();
                    for (int i = 0; i < lines.Length; i++)
                        sb.Append(lines[i]);

                    try
                    {
                        System.IO.File.AppendAllText(FileName, sb.ToString());
                        
                        
                    }
                    catch { }
                }

                Interlocked.Decrement(ref synLock);
            }
        }

        #endregion

        #region Events

        public EventHandler<TextAddedEventArgs> TextAddedEvent;

        #endregion
    }    
}
