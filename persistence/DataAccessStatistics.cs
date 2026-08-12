using System.Threading;
namespace OrmDataAccess.Persistence
{
    public sealed class DataAccessStatistics
    {
        private long _hits, _misses, _puts, _gets, _deletes;
        private int _entries, _nodes;
        public long HitCount => Interlocked.Read(ref _hits);
        public long MissCount => Interlocked.Read(ref _misses);
        public long PutCount => Interlocked.Read(ref _puts);
        public long GetCount => Interlocked.Read(ref _gets);
        public long DeleteCount => Interlocked.Read(ref _deletes);
        public int EntryCount => Volatile.Read(ref _entries);
        public int NodeCount => Volatile.Read(ref _nodes);
        public void RecordHit() => Interlocked.Increment(ref _hits);
        public void RecordMiss() => Interlocked.Increment(ref _misses);
        public void RecordPut() => Interlocked.Increment(ref _puts);
        public void RecordGet() => Interlocked.Increment(ref _gets);
        public void RecordDelete() => Interlocked.Increment(ref _deletes);
        public void SetEntryCount(int v) => Volatile.Write(ref _entries, v);
        public void SetNodeCount(int v) => Volatile.Write(ref _nodes, v);
        public object Snapshot() => new
        {
            hit_count = HitCount,
            miss_count = MissCount,
            put_count = PutCount,
            get_count = GetCount,
            delete_count = DeleteCount,
            entry_count = EntryCount,
            node_count = NodeCount
        };
    }
}
