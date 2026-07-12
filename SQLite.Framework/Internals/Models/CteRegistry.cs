namespace SQLite.Framework.Internals.Models;

internal class CteRegistry
{
    private readonly List<CteInfo> ctes = [];
    private readonly Dictionary<SQLiteCte, CteInfo> registeredCtes = new(ReferenceEqualityComparer.Instance);

    public IReadOnlyList<CteInfo> Ctes => ctes;

    public string? TryGetName(SQLiteCte cte)
    {
        return registeredCtes.GetValueOrDefault(cte)?.Name;
    }

    public CteInfo Info(SQLiteCte cte)
    {
        return registeredCtes[cte];
    }

    public string Register(string sql, SQLiteParameter[]? parameters, bool isRecursive, SQLiteCte key, string[]? columnNames = null, HashSet<string>? dayOfWeekColumns = null, HashSet<string>? constructedPaths = null)
    {
        string name = $"cte{ctes.Count}";
        CteInfo info = new()
        {
            Name = name,
            Sql = sql,
            Parameters = parameters,
            IsRecursive = isRecursive,
            Materialization = key.Materialization,
            ColumnNames = columnNames,
            DayOfWeekColumns = dayOfWeekColumns,
            ConstructedPaths = constructedPaths
        };
        ctes.Add(info);
        registeredCtes[key] = info;
        return name;
    }
}
