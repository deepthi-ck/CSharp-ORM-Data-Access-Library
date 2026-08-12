using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using OrmDataAccess.Shared;

namespace OrmDataAccess.Persistence
{
    public sealed class InMemoryStore
    {
        private readonly ConcurrentDictionary<string, EntityValue> _store =
            new ConcurrentDictionary<string, EntityValue>(StringComparer.Ordinal);

        public EntityEntry Put(string key, string value, string kind, TimeSpan ttl, string slot)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Key is required.", nameof(key));
            if (value is null) throw new ArgumentNullException(nameof(value));

            var revision = _store.TryGetValue(key, out var existing) ? existing.Entry.Revision + 1 : 1;
            var entry = new EntityEntry
            {
                Key = key,
                Value = value,
                Kind = kind,
                CreatedAt = DateTimeOffset.UtcNow,
                ExpiresAt = DateTimeOffset.UtcNow.Add(ttl),
                NodeSlot = slot,
                Revision = revision
            };
            _store[key] = new EntityValue(entry);
            return entry.Clone();
        }

        public EntityEntry? Get(string key)
        {
            if (!_store.TryGetValue(key, out var mv)) return null;
            if (IsExpired(mv))
            {
                _store.TryRemove(key, out _);
                return null;
            }
            mv.Touch();
            return mv.Entry.Clone();
        }

        public bool Delete(string key) => _store.TryRemove(key, out _);

        public bool Contains(string key)
        {
            if (!_store.TryGetValue(key, out var mv)) return false;
            if (IsExpired(mv))
            {
                _store.TryRemove(key, out _);
                return false;
            }
            return true;
        }

        public void Clear() => _store.Clear();
        public int Count => _store.Count;
        public bool Any() => _store.Any();

        public List<string> EvictExpired()
        {
            var expired = _store.Where(kv => IsExpired(kv.Value)).Select(kv => kv.Key).ToList();
            foreach (var k in expired) _store.TryRemove(k, out _);
            return expired;
        }

        public string? EvictLru()
        {
            if (_store.IsEmpty) return null;
            var victim = _store.OrderBy(kv => kv.Value.LastActivityUtc).FirstOrDefault();
            if (string.IsNullOrWhiteSpace(victim.Key)) return null;
            _store.TryRemove(victim.Key, out _);
            return victim.Key;
        }

        private static bool IsExpired(EntityValue mv) =>
            mv.Entry.ExpiresAt is DateTimeOffset exp && exp <= DateTimeOffset.UtcNow;
    }
}
