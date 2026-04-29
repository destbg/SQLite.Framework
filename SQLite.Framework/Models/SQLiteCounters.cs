namespace SQLite.Framework.Internals;

/// <summary>
/// Maintains counters for generating unique aliases and parameter names during SQL translation.
/// </summary>
public class SQLiteCounters
{
    private readonly Dictionary<char, int> tableIndex = [];

    /// <summary>
    /// Counter for generating unique parameter names in SQL queries.
    /// </summary>
    public int ParamIndex;

    /// <summary>
    /// Counter for generating unique identifiers for table aliases and other SQL elements that require uniqueness.
    /// </summary>
    public int IdentifierIndex;

    /// <summary>
    /// Generates the next unique table alias index for a given starting character.
    /// </summary>
    public int NextTableIndex(char c)
    {
        if (!tableIndex.TryGetValue(c, out int v))
        {
            v = tableIndex.Count;
        }
        tableIndex[c] = v + 1;
        return v;
    }
}
