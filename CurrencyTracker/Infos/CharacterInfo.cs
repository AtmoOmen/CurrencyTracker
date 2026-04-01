using System;

namespace CurrencyTracker.Infos;

public class CharacterInfo : IEquatable<CharacterInfo>
{
    public string Name      { get; set; } = null!;
    public string Server    { get; set; } = null!;
    public ulong  ContentID { get; set; }

    public override bool Equals(object? obj) =>
        Equals(obj as CharacterInfo);

    public bool Equals(CharacterInfo? other) =>
        other != null && ContentID == other.ContentID;

    public override int GetHashCode() =>
        HashCode.Combine(ContentID);
}
