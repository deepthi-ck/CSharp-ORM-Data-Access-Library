using System;
using System.Collections.Generic;

namespace OrmDataAccess.Distribution
{
    public sealed class NodeRegistry
    {
        private readonly Dictionary<int, StoreNode> _nodes = new Dictionary<int, StoreNode>();
        public void Register(StoreNode node)
        {
            if (node is null) throw new ArgumentNullException(nameof(node));
            _nodes[node.Slot] = node;
        }
        public StoreNode Get(int slot) =>
            _nodes.TryGetValue(slot, out var n) ? n : throw new KeyNotFoundException("Node slot not registered: " + slot);
        public IReadOnlyCollection<StoreNode> All => _nodes.Values;
        public int Count => _nodes.Count;
    }
}
