namespace SQLite.Framework.Internals.Models;

[ExcludeFromCodeCoverage]
internal class CteRegistry
{
    private readonly List<CteInfo> ctes = [];
    private readonly Dictionary<SQLiteCte, string> registeredCtes = new(ReferenceEqualityComparer.Instance);

    public IReadOnlyList<CteInfo> Ctes => ctes;

    public string? TryGetName(SQLiteCte cte)
    {
        return registeredCtes.GetValueOrDefault(cte);
    }

    public string Register(string sql, SQLiteParameter[]? parameters, bool isRecursive, SQLiteCte? key = null)
    {
        string name = $"cte{ctes.Count}";
        ctes.Add(new CteInfo
        {
            Name = name,
            Sql = sql,
            Parameters = parameters,
            IsRecursive = isRecursive
        });
        if (key != null)
        {
            registeredCtes[key] = name;
        }

        return name;
    }
}