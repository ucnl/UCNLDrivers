// UCNLDrivers/uAux/AuxManager.cs
namespace UCNLDrivers.uAux
{
    public class AuxManager
    {
        private readonly Dictionary<string, IAuxSource> _sources = new();
        private readonly Dictionary<AuxSourceKind, IAuxSource?> _activeByKind = new();

        public event EventHandler<AuxSourceStatusEventArgs>? OnSourceStatusChanged;

        // ======== РЕГИСТРАЦИЯ ========

        public void Register(IAuxSource source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (_sources.ContainsKey(source.Id))
                throw new InvalidOperationException($"Source '{source.Id}' already registered");

            _sources[source.Id] = source;
            source.OnStatusChanged += (_, _) =>
            {
                OnSourceStatusChanged?.Invoke(this,
                    new AuxSourceStatusEventArgs(GetInfo(source)));
            };
        }

        public bool Remove(string id)
        {
            if (!_sources.TryGetValue(id, out var source)) return false;
            source.Stop();
            DeactivateKind(source.Kind);
            _sources.Remove(id);
            return true;
        }

        // ======== АКТИВАЦИЯ ========

        #region Runtime management

        /// <summary>
        /// Зарегистрировать и сразу активировать источник
        /// </summary>
        public void AddAndActivate(IAuxSource source)
        {
            Register(source);
            Activate(source.Id);
        }

        /// <summary>
        /// Переактивировать источник (Stop → Start)
        /// </summary>
        public void Restart(string id)
        {
            Deactivate(id);
            Activate(id);
        }

        /// <summary>
        /// Запустить цепочку активации: каждый следующий запускается после обнаружения предыдущего
        /// </summary>
        public void ActivateChain(params string[] ids)
        {
            if (ids.Length == 0) return;

            Activate(ids[0]);

            for (int i = 1; i < ids.Length; i++)
            {
                var prevId = ids[i - 1];
                var nextId = ids[i];

                var prev = GetSource(prevId);
                if (prev != null)
                {
                    prev.OnStatusChanged += (_, _) =>
                    {
                        if (prev.Status == AuxStatus.Detected)
                            Activate(nextId);
                    };
                }
            }
        }

        #endregion

        public void Activate(string id)
        {
            if (!_sources.TryGetValue(id, out var source))
                throw new ArgumentException($"Source '{id}' not found");

            if (_activeByKind.TryGetValue(source.Kind, out var current) && current != null && current != source)
                current.Stop();

            source.Start();
            _activeByKind[source.Kind] = source;
        }

        public void Deactivate(string id)
        {
            if (!_sources.TryGetValue(id, out var source)) return;
            source.Stop();
            if (_activeByKind.TryGetValue(source.Kind, out var current) && current == source)
                _activeByKind[source.Kind] = null;
        }

        private void DeactivateKind(AuxSourceKind kind)
        {
            if (_activeByKind.TryGetValue(kind, out var source) && source != null)
            {
                source.Stop();
                _activeByKind[kind] = null;
            }
        }

        // ======== ИНФОРМАЦИЯ ========

        public AuxSourceInfo GetInfo(string id)
        {
            if (!_sources.TryGetValue(id, out var source))
                throw new ArgumentException($"Source '{id}' not found");
            return GetInfo(source);
        }

        private static AuxSourceInfo GetInfo(IAuxSource source) => new()
        {
            Id = source.Id,
            Kind = source.Kind,
            Description = source.Description,
            Status = source.Status,
            PortName = source.PortName
        };

        public IEnumerable<AuxSourceInfo> GetAllSources() =>
            _sources.Values.Select(GetInfo);

        public IAuxSource? GetSource(string id) =>
            _sources.TryGetValue(id, out var s) ? s : null;
    }
}