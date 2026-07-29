namespace SQLite.Framework.Internals.Models;

internal sealed class CheckConstraintSpec
{
    public CheckConstraintSpec(string sql, string? name)
    {
        Sql = sql;
        Name = name;
    }

    public string Sql { get; set; }
    public string? Name { get; }
}
