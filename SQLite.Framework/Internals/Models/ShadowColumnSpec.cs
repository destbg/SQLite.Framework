namespace SQLite.Framework.Internals.Models;

/// <summary>
/// A column declared on the model that has no CLR property. The framework creates it in
/// <c>CreateTable</c> and keeps it across a <c>Migrate</c> rebuild, but never reads or writes it.
/// </summary>
internal sealed class ShadowColumnSpec
{
    public ShadowColumnSpec(string name, SQLiteColumnType type, bool isNullable, string? defaultSql)
    {
        Name = name;
        Type = type;
        IsNullable = isNullable;
        DefaultSql = defaultSql;
    }

    public string Name { get; }
    public SQLiteColumnType Type { get; }
    public bool IsNullable { get; }
    public string? DefaultSql { get; }

    public string ToColumnSql()
    {
        StringBuilder sb = new();
        sb.Append(IdentifierGuard.Quote(Name));
        sb.Append(' ');
        sb.Append(Type.ToString().ToUpperInvariant());
        if (!IsNullable)
        {
            sb.Append(" NOT NULL");
        }
        if (DefaultSql != null)
        {
            sb.Append(" DEFAULT ");
            sb.Append(DefaultSql);
        }

        return sb.ToString();
    }
}
