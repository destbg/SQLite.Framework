namespace SQLite.Framework.Internals;

/// <summary>
/// Maintains counters for generating unique aliases and parameter names during SQL translation.
/// </summary>
public class SQLiteCounters
{
    private static readonly string[] paramNameCache = BuildParamNameCache();

    private readonly Dictionary<char, int> tableIndex = [];
    private int paramIndex;
    private int identifierIndex;

    /// <summary>
    /// Returns the next unique number and adds one to the counter. The number gives a SQL
    /// expression a stable name that is used for caching and as a column alias.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int NextIdentifier()
    {
        return identifierIndex++;
    }

    /// <summary>
    /// Returns the next parameter name (for example <c>@p0</c>, <c>@p1</c>, and so on) and adds
    /// one to the counter. The first 256 names are kept in a shared array, so this call does not
    /// build a new string for each parameter.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string NextParamName()
    {
        int idx = paramIndex++;
        return idx < paramNameCache.Length ? paramNameCache[idx] : "@p" + idx;
    }

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

    private static string[] BuildParamNameCache()
    {
        const int size = 256;
        string[] cache = new string[size];
        for (int i = 0; i < size; i++)
        {
            cache[i] = "@p" + i;
        }
        return cache;
    }
}
