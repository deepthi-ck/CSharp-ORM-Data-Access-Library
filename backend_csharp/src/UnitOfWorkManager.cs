using System;
using OrmDataAccess.Persistence;
using OrmDataAccess.Shared;

namespace OrmDataAccess.Backend
{
    public sealed class UnitOfWorkManager
    {
        private readonly ApplicationDbContext _db;
        private readonly DataAccessStatistics _stats;
        private readonly EvictionManager _eviction;
        private readonly ExpirationManager _expiration;

        public UnitOfWorkManager(
            ApplicationDbContext db,
            DataAccessStatistics stats,
            EvictionManager eviction,
            ExpirationManager expiration)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _stats = stats;
            _eviction = eviction;
            _expiration = expiration;
        }

        public EntityEntry Put(string key, string value, string kind = "entity")
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Key is required.", nameof(key));
            _expiration.Sweep();
            var entry = _db.Save(key, value, kind);
            _eviction.EnforceCapacity();
            _stats.RecordPut();
            _stats.SetEntryCount(_db.EntryCount);
            return entry;
        }

        public EntityEntry? Get(string key, bool preferReplica = false)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Key is required.", nameof(key));
            _expiration.Sweep();
            _stats.RecordGet();
            var entry = _db.Find(key, preferReplica);
            if (entry is null) { _stats.RecordMiss(); return null; }
            _stats.RecordHit();
            return entry;
        }

        public bool Delete(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Key is required.", nameof(key));
            _expiration.Sweep();
            var ok = _db.Remove(key);
            if (ok) _stats.RecordDelete();
            _stats.SetEntryCount(_db.EntryCount);
            return ok;
        }

        public ApplicationDbContext Db => _db;
        public DataAccessStatistics Statistics => _stats;
    }
}
