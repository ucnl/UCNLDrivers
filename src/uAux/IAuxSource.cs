// UCNLDrivers/uAux/IAuxSource.cs
namespace UCNLDrivers.uAux
{
    public enum AuxSourceKind
    {
        None,
        GNSS,
        Compass,
        GNSSCompass,
        Manual,
        Custom
    }

    public enum AuxStatus
    {
        Inactive,   // выключен, не ищем
        Active,     // включен, ищем порт
        Detected,   // обнаружен, работает
        Error       // ошибка
    }

    public interface IAuxSource
    {
        string Id { get; }
        AuxSourceKind Kind { get; }
        string Description { get; }
        AuxStatus Status { get; }
        string? PortName { get; }

        event EventHandler? OnStatusChanged;

        void Start();
        void Stop();
    }
}