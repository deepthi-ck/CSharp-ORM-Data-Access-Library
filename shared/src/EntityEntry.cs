using System;
namespace OrmDataAccess.Shared
{
    public sealed class EntityEntry
    {
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Kind { get; set; } = "entity";
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? ExpiresAt { get; set; }
        public int Revision { get; set; }
        public string NodeSlot { get; set; } = string.Empty;

        public EntityEntry Clone() => new EntityEntry
        {
            Key = Key, Value = Value, Kind = Kind, CreatedAt = CreatedAt,
            ExpiresAt = ExpiresAt, Revision = Revision, NodeSlot = NodeSlot
        };
    }
}
