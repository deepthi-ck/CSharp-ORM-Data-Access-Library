using OrmDataAccess.Persistence;
using OrmDataAccess.Shared;

namespace OrmDataAccess.Distribution
{
    public sealed class StoreNode
    {
        private readonly InMemoryStore _store;
        private readonly PersistencePolicy _policy;
        public int Slot { get; }
        public StoreNode(int slot, InMemoryStore store, PersistencePolicy policy)
        {
            Slot = slot;
            _store = store;
            _policy = policy;
        }
        public EntityEntry Put(string key, string value, string kind) =>
            _store.Put(key, value, kind, System.TimeSpan.FromSeconds(_policy.EntryTtlSeconds), "slot-" + Slot);
        public EntityEntry? Get(string key) => _store.Get(key);
        public bool Delete(string key) => _store.Delete(key);
        public bool Contains(string key) => _store.Contains(key);
    }
}
