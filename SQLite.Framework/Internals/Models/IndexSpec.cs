namespace SQLite.Framework.Internals.Models;

internal sealed class IndexSpec
{
    public IndexSpec(string[] columns, SQLiteCollation[] collations, string name, bool unique, string? filterSql)
    {
        Columns = columns;
        Collations = collations;
        Name = name;
        Unique = unique;
        FilterSql = filterSql;
    }

    public string[] Columns { get; }
    public SQLiteCollation[] Collations { get; }
    public string Name { get; }
    public bool Unique { get; }
    public string? FilterSql { get; }
}
