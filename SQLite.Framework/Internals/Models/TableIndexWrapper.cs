using System.Diagnostics.CodeAnalysis;

namespace SQLite.Framework.Internals.Models;

/// <summary>
/// Used to generate the alias for a SQLite table.
/// </summary>
[ExcludeFromCodeCoverage]
internal class TableIndexWrapper
{
    private readonly Dictionary<char, int> dict = [];

    public int this[char c]
    {
        get
        {
            if (!dict.TryGetValue(c, out int index))
            {
                index = dict.Count;
                dict[c] = index;
            }

            return index;
        }
        set { dict[c] = value; }
    }
}