using System;

namespace NRules.Samples.ClaimsExpert.Domain;

public struct Name
{
    public static readonly Name Empty = new Name();

    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? MiddleName { get; set; }

    public bool IsEmpty => Equals(Empty);

    public bool Equals(Name other)
    {
        return FirstName == other.FirstName && LastName == other.LastName && MiddleName == other.MiddleName;
    }

    public override bool Equals(object? obj)
    {
        return obj is Name other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(FirstName, LastName, MiddleName);
    }
}