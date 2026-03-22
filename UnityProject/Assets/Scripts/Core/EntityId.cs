using System;

namespace IsoRPG.Core
{
    /// <summary>
    /// Globally unique identifier for game entities (units, status effects, etc.).
    /// Wraps <see cref="System.Guid"/> with value semantics and a null sentinel.
    /// Survives serialization, deserialization, and scene reloads.
    /// </summary>
    [Serializable]
    public readonly struct EntityId : IEquatable<EntityId>
    {
        /// <summary>The underlying GUID value.</summary>
        public readonly string GuidString;

        /// <summary>Sentinel value representing no entity. Use instead of default.</summary>
        public static readonly EntityId None = new(Guid.Empty.ToString());

        /// <summary>
        /// Create an EntityId from an existing GUID string.
        /// </summary>
        /// <param name="guidString">A valid GUID string representation.</param>
        public EntityId(string guidString)
        {
            GuidString = guidString ?? Guid.Empty.ToString();
        }

        /// <summary>
        /// Generate a new unique EntityId.
        /// </summary>
        public static EntityId New()
        {
            return new EntityId(Guid.NewGuid().ToString());
        }

        /// <summary>
        /// Whether this EntityId represents a valid entity (not None).
        /// </summary>
        public bool IsValid => GuidString != null && GuidString != Guid.Empty.ToString();

        public bool Equals(EntityId other)
        {
            return string.Equals(GuidString, other.GuidString, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is EntityId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return GuidString != null ? GuidString.GetHashCode() : 0;
        }

        public override string ToString()
        {
            return IsValid ? GuidString[..8] : "None";
        }

        public static bool operator ==(EntityId left, EntityId right) => left.Equals(right);
        public static bool operator !=(EntityId left, EntityId right) => !left.Equals(right);
    }
}
