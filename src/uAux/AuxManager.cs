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
                var info = GetInfo(source);
                OnSourceStatusChanged?.Invoke(this, new AuxSourceStatusEventArgs(info));

                // Автозапуск следующего в цепочке
                if (info.Status == AuxStatus.Detected &&
                    _chainMap.TryGetValue(source.Id, out var nextId) &&
                    _sources.TryGetValue(nextId, out var next) &&
                    next.Status == AuxStatus.Inactive)
                {
                    Activate(nextId);
                }
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
        private Dictionary<string, string> _chainMap = new();

        public void ActivateChain(params string[] ids)
        {
            if (ids.Length == 0) return;

            // Строим карту цепочки: prevId -> nextId
            _chainMap.Clear();
            for (int i = 1; i < ids.Length; i++)
                _chainMap[ids[i - 1]] = ids[i];

            // Запускаем первый в цепочке
            var first = GetSource(ids[0]);
            if (first == null) return;

            if (first.Status == AuxStatus.Detected)
            {
                // Уже обнаружен — запускаем следующий сразу
                if (_chainMap.TryGetValue(ids[0], out var nextId) &&
                    _sources.TryGetValue(nextId, out var next) &&
                    next.Status == AuxStatus.Inactive)
                {
                    Activate(nextId);
                }
            }
            else if (first.Status == AuxStatus.Inactive)
            {
                Activate(ids[0]);
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