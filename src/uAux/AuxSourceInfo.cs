// UCNLDrivers/uAux/AuxSourceInfo.cs
namespace UCNLDrivers.uAux
{
    public class AuxSourceInfo
    {
        public string Id { get; set; } = "";
        public AuxSourceKind Kind { get; set; }
        public string Description { get; set; } = "";
        public AuxStatus Status { get; set; }
        public string? PortName { get; set; }

        public override string ToString()
        {
            var status = Status switch
            {
                AuxStatus.Detected => "✓",
                AuxStatus.Active => "↻",
                AuxStatus.Error => "✗",
                _ => "○"
            };
            return $"{status} [{Kind}] {Description}";
        }
    }

    public class AuxSourceStatusEventArgs : EventArgs
    {
        public AuxSourceInfo Info { get; }
        public AuxSourceStatusEventArgs(AuxSourceInfo info) => Info = info;
    }
}