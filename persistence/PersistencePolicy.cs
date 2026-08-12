using OrmDataAccess.Shared;
namespace OrmDataAccess.Persistence
{
    public sealed class PersistencePolicy
    {
        public int EntryTtlSeconds { get; init; } = 300;
        public int MaxEntries { get; init; } = 64;
        public int NodeSlotCount { get; init; } = 3;
        public static PersistencePolicy FromConfiguration(DataAccessConfiguration cfg) => new PersistencePolicy
        {
            EntryTtlSeconds = cfg.EntryTtlSeconds,
            MaxEntries = cfg.MaxEntries,
            NodeSlotCount = cfg.NodeSlotCount
        };
    }
}
