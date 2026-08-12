using System;
using System.Collections.Generic;
using System.Linq;
using OrmDataAccess.Shared;

namespace OrmDataAccess.Distribution
{
    public sealed class ReplicationManager
    {
        private readonly Dictionary<string, EntityEntry> _replica = new Dictionary<string, EntityEntry>(StringComparer.Ordinal);
        private readonly object _gate = new object();

        public void Replicate(EntityEntry entry)
        {
            if (entry is null) throw new ArgumentNullException(nameof(entry));
            lock (_gate) { _replica[entry.Key] = entry.Clone(); }
        }

        public EntityEntry? GetReplica(string key)
        {
            lock (_gate)
            {
                if (!_replica.TryGetValue(key, out var e)) return null;
                if (e.ExpiresAt is DateTimeOffset exp && exp <= DateTimeOffset.UtcNow)
                {
                    _replica.Remove(key);
                    return null;
                }
                return e.Clone();
            }
        }

        public void Remove(string key)
        {
            lock (_gate) { _replica.Remove(key); }
        }

        public int SweepExpired()
        {
            lock (_gate)
            {
                var expired = _replica.Where(kv => kv.Value.ExpiresAt is DateTimeOffset exp && exp <= DateTimeOffset.UtcNow)
                    .Select(kv => kv.Key).ToList();
                foreach (var k in expired) _replica.Remove(k);
                return expired.Count;
            }
        }
    }
}
