namespace SQLite.Framework.Internals.Helpers;

/// <summary>
/// Builds the SQL fragment for a <see cref="ForeignKeyInfo" />, inline on a column or as a
/// table-level constraint.
/// </summary>
internal static class ForeignKeySql
{
    public static void WriteSql(ForeignKeyInfo foreignKey, StringBuilder sb, bool inline)
    {
        if (inline)
        {
            sb.Append("REFERENCES \"");
            sb.Append(foreignKey.TargetTable);
            sb.Append("\"(\"");
            sb.Append(foreignKey.TargetColumns[0]);
            sb.Append("\")");
        }
        else
        {
            sb.Append("FOREIGN KEY (");
            for (int i = 0; i < foreignKey.Columns.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(", ");
                }
                sb.Append('"');
                sb.Append(foreignKey.Columns[i]);
                sb.Append('"');
            }
            sb.Append(") REFERENCES \"");
            sb.Append(foreignKey.TargetTable);
            sb.Append("\"(");
            for (int i = 0; i < foreignKey.TargetColumns.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(", ");
                }
                sb.Append('"');
                sb.Append(foreignKey.TargetColumns[i]);
                sb.Append('"');
            }
            sb.Append(')');
        }

        AppendAction(sb, " ON DELETE ", foreignKey.OnDelete);
        AppendAction(sb, " ON UPDATE ", foreignKey.OnUpdate);
        if (foreignKey.Deferred)
        {
            sb.Append(" DEFERRABLE INITIALLY DEFERRED");
        }
    }

    private static void AppendAction(StringBuilder sb, string clause, SQLiteForeignKeyAction action)
    {
        if (action == SQLiteForeignKeyAction.NoAction)
        {
            return;
        }
        sb.Append(clause);
        sb.Append(action switch
        {
            SQLiteForeignKeyAction.Restrict => "RESTRICT",
            SQLiteForeignKeyAction.SetNull => "SET NULL",
            SQLiteForeignKeyAction.SetDefault => "SET DEFAULT",
            SQLiteForeignKeyAction.Cascade => "CASCADE",
            _ => throw new InvalidOperationException($"Unknown foreign key action: {action}"),
        });
    }
}
