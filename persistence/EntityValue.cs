using System;
using OrmDataAccess.Shared;
namespace OrmDataAccess.Persistence
{
    public sealed class EntityValue
    {
        public EntityEntry Entry { get; }
        public DateTimeOffset LastActivityUtc { get; set; } = DateTimeOffset.UtcNow;
        public EntityValue(EntityEntry entry) => Entry = entry ?? throw new ArgumentNullException(nameof(entry));
        public void Touch() => LastActivityUtc = DateTimeOffset.UtcNow;
    }
}
