using System;
namespace OrmDataAccess.Persistence
{
    public readonly struct EntityKey : IEquatable<EntityKey>
    {
        public string Value { get; }
        public EntityKey(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Entity key is required.", nameof(value));
            Value = value;
        }
        public static EntityKey Of(string v) => new EntityKey(v);
        public override string ToString() => Value;
        public bool Equals(EntityKey other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object? obj) => obj is EntityKey k && Equals(k);
        public override int GetHashCode() => Value?.GetHashCode() ?? 0;
    }
}
