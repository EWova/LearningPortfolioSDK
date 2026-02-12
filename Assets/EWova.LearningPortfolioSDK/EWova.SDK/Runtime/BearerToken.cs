using System;
using System.Net.Http.Headers;

namespace EWova.NetService
{
    public readonly struct BearerToken : IEquatable<BearerToken>
    {
        public BearerToken(string value)
        {
            Value = value ?? string.Empty;
        }

        public readonly string Value;

        public static implicit operator BearerToken(string value) => new BearerToken(value);

        public static readonly BearerToken Empty = new(string.Empty);

        public AuthenticationHeaderValue Header => new("Bearer", Value);

        public override bool Equals(object obj) =>
            obj is BearerToken other && Equals(other);

        public bool Equals(BearerToken other) =>
            string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);

        public override int GetHashCode() =>
            StringComparer.OrdinalIgnoreCase.GetHashCode(Value ?? string.Empty);

        public static bool operator ==(BearerToken left, BearerToken right) =>
            left.Equals(right);

        public static bool operator !=(BearerToken left, BearerToken right) =>
            !left.Equals(right);

        public override string ToString() => Value;
    }
}
