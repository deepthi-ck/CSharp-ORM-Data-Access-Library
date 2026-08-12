using System.Collections.Generic;
using System.Linq;
using OrmDataAccess.Distribution;
using OrmDataAccess.Persistence;
using OrmDataAccess.Shared;

namespace OrmDataAccess.Backend
{
    /// <summary>Minimal IApplicationDbContext-style persistence facade (CleanArchitecture concept).</summary>
    public sealed class ApplicationDbContext
    {
        private readonly PartitionRouter _router;
        private readonly ReplicationManager _replication;
        private readonly List<InMemoryStore> _stores;

        public ApplicationDbContext(PartitionRouter router, ReplicationManager replication, IEnumerable<InMemoryStore> stores)
        {
            _router = router;
            _replication = replication;
            _stores = stores.ToList();
        }

        public EntityEntry Save(string key, string value, string kind)
        {
            var primary = _router.Route(key);
            var entry = primary.Put(key, value, kind);
            _replication.Replicate(entry);
            return entry;
        }

        public EntityEntry? Find(string key, bool preferReplica = false)
        {
            return preferReplica
                ? _replication.GetReplica(key) ?? _router.Route(key).Get(key)
                : _router.Route(key).Get(key) ?? _replication.GetReplica(key);
        }

        public bool Remove(string key)
        {
            var ok = _router.Route(key).Delete(key);
            _replication.Remove(key);
            return ok;
        }

        public int EntryCount => _stores.Sum(s => s.Count);
        public PartitionRouter Router => _router;
        public ReplicationManager Replication => _replication;
        public IReadOnlyList<InMemoryStore> Stores => _stores;
    }
}
