namespace SQLite.Framework.Internals.Models;

[ExcludeFromCodeCoverage]
internal sealed class IndexSpec
{
    public IndexSpec(string column, string name, bool unique, string? filterSql)
    {
        Column = column;
        Name = name;
        Unique = unique;
        FilterSql = filterSql;
    }

    public string Column { get; }
    public string Name { get; }
    public bool Unique { get; }
    public string? FilterSql { get; }
}
