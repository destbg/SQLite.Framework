namespace SQLite.Framework.Internals.Helpers;

/// <summary>
/// Builds the column definition SQL for a <see cref="TableColumn" /> as it appears inside
/// <c>CREATE TABLE</c> or <c>ALTER TABLE ADD COLUMN</c>.
/// </summary>
internal static class ColumnSql
{
    public static string GetCreateColumnSql(TableColumn column, bool emitInlinePrimaryKey = true, string? defaultOverride = null)
    {
        string columnType = column.ColumnType.ToString().ToUpperInvariant();
        bool inlinePk = emitInlinePrimaryKey && column.IsPrimaryKey;
        bool rowidAlias = inlinePk && column.ColumnType == SQLiteColumnType.Integer;
        string nullability;
        if (!inlinePk)
        {
            nullability = column.IsNullable ? "NULL" : "NOT NULL";
        }
        else
        {
            nullability = rowidAlias ? string.Empty : "NOT NULL";
        }
        string primaryKey = inlinePk ? "PRIMARY KEY" : string.Empty;
        string autoIncrement = inlinePk && column.IsAutoIncrement ? "AUTOINCREMENT" : string.Empty;

        StringBuilder sb = new();
        sb.Append(IdentifierGuard.Quote(column.Name));
        sb.Append(' ');
        sb.Append(columnType);
        if (nullability.Length > 0)
        {
            sb.Append(' ');
            sb.Append(nullability);
        }
        if (primaryKey.Length > 0)
        {
            sb.Append(' ');
            sb.Append(primaryKey);
        }
        if (autoIncrement.Length > 0)
        {
            sb.Append(' ');
            sb.Append(autoIncrement);
        }
        if (column.ForeignKey != null)
        {
            sb.Append(' ');
            ForeignKeySql.WriteSql(column.ForeignKey, sb, inline: true);
        }

        string? defaultSql = defaultOverride ?? column.DefaultSql;
        if (defaultSql != null)
        {
            sb.Append(" DEFAULT ");
            sb.Append(defaultSql);
        }
        return sb.ToString();
    }
}
