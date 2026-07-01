namespace SQLite.Framework.Models;

/// <summary>
/// Maintains counters for generating unique aliases and parameter names during SQL translation.
/// </summary>
public class SQLiteCounters
{
    private static readonly string[] paramNameCache = BuildParamNameCache();

    private readonly Dictionary<char, int> tableIndex = [];
    private readonly string? paramPrefix;
    private HashSet<string>? reservedParamNames;
    private int paramIndex;
    private int identifierIndex;

    /// <summary>
    /// Default counter. Parameters are emitted as <c>@p0</c>, <c>@p1</c> and so on.
    /// </summary>
    public SQLiteCounters()
    {
    }

    /// <summary>
    /// Counter with a custom parameter prefix. Used when two translation passes have to coexist
    /// in the same SQL statement (for example the <c>RETURNING</c> projection emitted alongside
    /// an entity-bound <c>INSERT</c>).
    /// </summary>
    public SQLiteCounters(string paramPrefix)
    {
        this.paramPrefix = paramPrefix;
    }

    internal bool IgnoreQueryFilters { get; set; }

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
    /// Returns the next parameter name (for example <c>@p0</c>, <c>@p1</c> and so on) and adds
    /// one to the counter. The first 256 names are kept in a shared array, so this call does not
    /// build a new string for each parameter.
    /// </summary>
    public string NextParamName()
    {
        while (true)
        {
            int idx = paramIndex++;
            string name = paramPrefix != null
                ? paramPrefix + idx
                : idx < paramNameCache.Length ? paramNameCache[idx] : "@p" + idx;
            if (reservedParamNames == null || !reservedParamNames.Contains(name))
            {
                return name;
            }
        }
    }

    /// <summary>
    /// Records parameter names that the caller supplied directly (for example through
    /// <c>FromSql</c>), so generated parameter names never collide with them.
    /// </summary>
    public void ReserveParamNames(IEnumerable<string> names)
    {
        reservedParamNames ??= new HashSet<string>(StringComparer.Ordinal);
        foreach (string name in names)
        {
            reservedParamNames.Add(name);
        }
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
