using System;

namespace NRules.Samples.ClaimsExpert.Domain;

public struct Address
{
    public static readonly Address Empty = new Address();

    public string? Line1 { get; set; }
    public string? Line2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Zip { get; set; }
    public bool IsEmpty => Equals(Empty);

    public bool Equals(Address other)
    {
        return Line1 == other.Line1 && Line2 == other.Line2 && City == other.City && State == other.State && Zip == other.Zip;
    }

    public override bool Equals(object? obj)
    {
        return obj is Address other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Line1, Line2, City, State, Zip);
    }
}