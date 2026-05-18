// UCNLDrivers/uAux/ManualLocationAndHeadingSource.cs
namespace UCNLDrivers.uAux
{
    /// <summary>
    /// Источник с фиксированными координатами и курсом (пирс, стационарная установка).
    /// </summary>
    public class ManualLocationAndHeadingSource : IAuxSource
    {
        private System.Timers.Timer? _timer;
        private AuxStatus _status = AuxStatus.Inactive;

        public string Id { get; }
        public AuxSourceKind Kind => AuxSourceKind.Manual;
        public string Description { get; }
        public AuxStatus Status => _status;
        public string? PortName => "Manual";

        public event EventHandler? OnStatusChanged;
        public event EventHandler<ManualLocationData>? OnDataReceived;

        public double Latitude { get; private set; }
        public double Longitude { get; private set; }
        public double Depth { get; private set; }
        public double Heading { get; private set; }

        public ManualLocationAndHeadingSource(string id, string description,
            double lat, double lon, double depth = 0, double heading = 0)
        {
            Id = id;
            Description = description;
            Latitude = lat;
            Longitude = lon;
            Depth = depth;
            Heading = heading;
        }

        public void Start()
        {
            _status = AuxStatus.Detected;
            OnStatusChanged?.Invoke(this, EventArgs.Empty);

            _timer = new System.Timers.Timer(1000);
            _timer.AutoReset = true;
            _timer.Elapsed += (_, _) =>
            {
                OnDataReceived?.Invoke(this, new ManualLocationData
                {
                    Latitude = Latitude,
                    Longitude = Longitude,
                    Depth = Depth,
                    Heading = Heading,
                    Timestamp = DateTime.UtcNow
                });
            };
            _timer.Start();
        }

        public void Stop()
        {
            _timer?.Stop();
            _timer?.Dispose();
            _timer = null;
            _status = AuxStatus.Inactive;
            OnStatusChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Update(double lat, double lon, double? depth = null, double? heading = null)
        {
            Latitude = lat;
            Longitude = lon;
            if (depth.HasValue) Depth = depth.Value;
            if (heading.HasValue) Heading = heading.Value;
        }
    }

    public class ManualLocationData
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Depth { get; set; }
        public double Heading { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}