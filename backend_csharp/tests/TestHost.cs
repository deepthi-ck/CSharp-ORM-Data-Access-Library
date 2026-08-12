using System.Collections.Generic;
using OrmDataAccess.Distribution;
using OrmDataAccess.Persistence;

namespace OrmDataAccess.Backend.Tests
{
    internal static class TestHost
    {
        public static (DataAccessService Service, UnitOfWorkManager Uow, ReplicationManager Replication, List<InMemoryStore> Stores) Create(
            int maxEntries = 64, int ttlSeconds = 300, int slots = 3)
        {
            var policy = new PersistencePolicy { MaxEntries = maxEntries, EntryTtlSeconds = ttlSeconds, NodeSlotCount = slots };
            var stores = new List<InMemoryStore>();
            var registry = new NodeRegistry();
            for (var i = 0; i < slots; i++)
            {
                var store = new InMemoryStore();
                stores.Add(store);
                registry.Register(new StoreNode(i, store, policy));
            }
            var stats = new DataAccessStatistics();
            stats.SetNodeCount(slots);
            var router = new PartitionRouter(registry, slots);
            var replication = new ReplicationManager();
            var eviction = new EvictionManager(stores, policy);
            var expiration = new ExpirationManager(stores, replication);
            var db = new ApplicationDbContext(router, replication, stores);
            var uow = new UnitOfWorkManager(db, stats, eviction, expiration);
            return (new DataAccessService(uow, stats), uow, replication, stores);
        }
    }
}
