using System;

namespace OrmDataAccess.Distribution
{
    public sealed class PartitionRouter
    {
        private readonly NodeRegistry _registry;
        private readonly int _slotCount;
        public PartitionRouter(NodeRegistry registry, int slotCount)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _slotCount = Math.Max(1, slotCount);
        }
        public int ResolveSlot(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Key is required.", nameof(key));
            var hash = Math.Abs(key.GetHashCode());
            return hash % _slotCount;
        }
        public StoreNode Route(string key) => _registry.Get(ResolveSlot(key));
        public StoreNode ReplicaOf(string key) => _registry.Get((ResolveSlot(key) + 1) % _slotCount);
    }
}
