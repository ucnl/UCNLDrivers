using System.Globalization;

namespace UCNLDrivers
{
    public class StringEventArgs : EventArgs
    {
        public TimeSpan TS { get; }
        public string Line { get; }

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
        private CancellationTokenSource? _cts;
        private static readonly char[] _timeSplitters = { ':' };
        private static readonly char[] _timeSplittersEx = { '-', '_' };

        public bool IsRunning => _cts != null && !_cts.IsCancellationRequested;
        public string LogFileName { get; private set; } = string.Empty;

        public void Playback(string fileName) => StartPlayback(fileName, realtime: true);
        public void PlaybackInstant(string fileName) => StartPlayback(fileName, realtime: false);

        public void RequestToStop() => _cts?.Cancel();

        private void StartPlayback(string fileName, bool realtime)
        {
            if (IsRunning) return;

            LogFileName = fileName;
            _cts = new CancellationTokenSource();
            var ct = _cts.Token;

            _ = Task.Run(() =>
            {
                try
                {
                    ProcessLines(fileName, ct, realtime);
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    LogEventHandler?.Invoke(this, new LogEventArgs(LogLineType.ERROR, ex));
                }
                finally
                {
                    LogPlaybackFinishedHandler?.Invoke(this, EventArgs.Empty);
                }
            }, ct);
        }

        private void ProcessLines(string fileName, CancellationToken ct, bool realtime)
        {
            TimeSpan prevTS = TimeSpan.MinValue;
            bool firstLine = true;

            foreach (var (timestamp, text) in EnumerateLogLines(fileName))
            {
                if (ct.IsCancellationRequested) break;

                if (realtime && !firstLine)
                {
                    var delay = (int)(timestamp - prevTS).TotalMilliseconds;
                    if (delay > 10)
                        ct.WaitHandle.WaitOne(delay);
                }

                prevTS = timestamp;
                firstLine = false;

                NewLogLineHandler?.Invoke(this, new StringEventArgs(text, timestamp));
            }
        }

        public IEnumerable<(double Seconds, string Text)> ParseLog(string fileName)
        {
            TimeSpan? start = null;

            foreach (var (timestamp, text) in EnumerateLogLines(fileName))
            {
                start ??= timestamp;
                var seconds = (timestamp - start.Value).TotalMilliseconds / 1000.0;
                yield return (seconds, text);
            }
        }

        private static IEnumerable<(TimeSpan Timestamp, string Text)> EnumerateLogLines(string fileName)
        {
            using var sr = File.OpenText(fileName);

            string? line;
            while ((line = sr.ReadLine()) != null)
            {
                int idx = line.IndexOf(' ');
                if (idx < 0) continue;

                var timeStr = line.Substring(0, idx);
                var text = line.Substring(idx + 1);

                if (TryParseTime(timeStr, out var ts) || TryParseTimeEx(timeStr, out ts))
                    yield return (ts, text);
            }
        }

        private static bool TryParseTime(string s, out TimeSpan ts)
        {
            ts = TimeSpan.MinValue;
            var parts = s.Split(_timeSplitters, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 3) return false;

            if (!int.TryParse(parts[0], out var h) ||
                !int.TryParse(parts[1], out var m) ||
                !double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var sec))
                return false;

            int sInt = (int)sec;
            int ms = (int)(1000 * (sec - sInt));
            ts = new TimeSpan(0, h, m, sInt, ms);
            return true;
        }

        private static bool TryParseTimeEx(string s, out TimeSpan ts)
        {
            ts = TimeSpan.MinValue;
            var parts = s.TrimEnd('_').Split(_timeSplittersEx, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 6) return false;

            if (!int.TryParse(parts[3], out var h) ||
                !int.TryParse(parts[4], out var m) ||
                !double.TryParse(parts[5], NumberStyles.Float, CultureInfo.InvariantCulture, out var sec))
                return false;

            int sInt = (int)sec;
            int ms = (int)(1000 * (sec - sInt));
            ts = new TimeSpan(0, h, m, sInt, ms);
            return true;
        }

        public event EventHandler<StringEventArgs>? NewLogLineHandler;
        public event EventHandler? LogPlaybackFinishedHandler;
        public event EventHandler<LogEventArgs>? LogEventHandler;
    }
}