using System;

namespace IsoRPG.Core
{
    /// <summary>
    /// Globally unique identifier for game entities (units, status effects, etc.).
    /// Wraps <see cref="System.Guid"/> with value semantics and a null sentinel.
    /// Survives serialization, deserialization, and scene reloads.
    ///
    /// Uses 16-byte Guid comparison — faster than string-based alternatives.
    /// </summary>
    [Serializable]
    public readonly struct EntityId : IEquatable<EntityId>
    {
        // Stored as 2 longs for Unity serialization compatibility (Guid is not serializable by JsonUtility)
        private readonly long _a;
        private readonly long _b;

        /// <summary>Sentinel value representing no entity. Use instead of default.</summary>
        public static readonly EntityId None = default;

        private EntityId(long a, long b)
        {
            _a = a;
            _b = b;
        }

        /// <summary>
        /// Create an EntityId from an existing GUID.
        /// </summary>
        public EntityId(Guid guid)
        {
            var bytes = guid.ToByteArray();
            _a = BitConverter.ToInt64(bytes, 0);
            _b = BitConverter.ToInt64(bytes, 8);
        }

        /// <summary>
        /// Generate a new unique EntityId.
        /// </summary>
        public static EntityId New()
        {
            return new EntityId(Guid.NewGuid());
        }

        /// <summary>
        /// Whether this EntityId represents a valid entity (not None/default).
        /// </summary>
        public bool IsValid => _a != 0 || _b != 0;

        /// <summary>
        /// Convert back to System.Guid.
        /// </summary>
        public Guid ToGuid()
        {
            var bytes = new byte[16];
            BitConverter.GetBytes(_a).CopyTo(bytes, 0);
            BitConverter.GetBytes(_b).CopyTo(bytes, 8);
            return new Guid(bytes);
        }

        public bool Equals(EntityId other)
        {
            return _a == other._a && _b == other._b;
        }

        public override bool Equals(object obj)
        {
            return obj is EntityId other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (_a.GetHashCode() * 397) ^ _b.GetHashCode();
            }
        }

        public override string ToString()
        {
            return IsValid ? ToGuid().ToString()[..8] : "None";
        }

        public static bool operator ==(EntityId left, EntityId right) => left.Equals(right);
        public static bool operator !=(EntityId left, EntityId right) => !left.Equals(right);
    }
}
