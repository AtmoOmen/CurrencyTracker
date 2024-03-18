using System;

namespace CurrencyTracker.Manager.Infos;

public class CharacterInfo : IEquatable<CharacterInfo>
{
    public string Name { get; set; } = null!;
    public string Server { get; set; } = null!;
    public ulong ContentID { get; set; }

    public override bool Equals(object? obj)
    {
        return Equals(obj as CharacterInfo);
    }

    public bool Equals(CharacterInfo? other)
    {
        return other != null && ContentID == other.ContentID;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(ContentID);
    }
}
