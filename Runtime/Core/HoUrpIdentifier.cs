using System;

namespace HoUrp.Extensions.Core
{
    /// <summary>
    /// Stable dotted identifier used by the contract registries.
    /// </summary>
    public readonly struct HoUrpIdentifier : IEquatable<HoUrpIdentifier>
    {
        private readonly string value;

        public HoUrpIdentifier(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Identifier cannot be null, empty, or whitespace.", nameof(value));
            }

            this.value = value;
        }

        public string Value => value ?? string.Empty;

        public bool Equals(HoUrpIdentifier other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is HoUrpIdentifier other && Equals(other);
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(Value);
        }

        public override string ToString()
        {
            return Value;
        }

        public static HoUrpIdentifier From(string value)
        {
            return new HoUrpIdentifier(value);
        }

        public static implicit operator HoUrpIdentifier(string value)
        {
            return new HoUrpIdentifier(value);
        }

        public static bool operator ==(HoUrpIdentifier left, HoUrpIdentifier right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(HoUrpIdentifier left, HoUrpIdentifier right)
        {
            return !left.Equals(right);
        }
    }
}
