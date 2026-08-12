using System.Collections.Generic;
using System.Linq;
using OrmDataAccess.Distribution;
using OrmDataAccess.Persistence;

namespace OrmDataAccess.Backend
{
    public sealed class ExpirationManager
    {
        private readonly List<InMemoryStore> _stores;
        private readonly ReplicationManager _replication;
        public ExpirationManager(IEnumerable<InMemoryStore> stores, ReplicationManager replication)
        {
            _stores = stores.ToList();
            _replication = replication;
        }
        public int Sweep() => _stores.Sum(s => s.EvictExpired().Count) + _replication.SweepExpired();
    }
}
