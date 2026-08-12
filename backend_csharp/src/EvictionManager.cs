using System.Collections.Generic;
using System.Linq;
using OrmDataAccess.Persistence;

namespace OrmDataAccess.Backend
{
    public sealed class EvictionManager
    {
        private readonly List<InMemoryStore> _stores;
        private readonly PersistencePolicy _policy;
        public EvictionManager(IEnumerable<InMemoryStore> stores, PersistencePolicy policy)
        {
            _stores = stores.ToList();
            _policy = policy;
        }
        public int EnforceCapacity()
        {
            var removed = 0;
            while (_stores.Sum(s => s.Count) > _policy.MaxEntries)
            {
                var victimStore = _stores.OrderByDescending(s => s.Count).First();
                if (victimStore.EvictLru() is null) break;
                removed++;
            }
            return removed;
        }
    }
}
