namespace SQLite.Framework.Internals.Models;

internal class ColumnMapping
{
    public ColumnMapping(string alias, string columnName, string? propertyName)
    {
        PropertyName = propertyName;
        Sql = $"{alias}.{columnName}";
    }

    public ColumnMapping(string sql)
    {
        Sql = sql;
    }

    public string? PropertyName { get; }
    public string Sql { get; }
}