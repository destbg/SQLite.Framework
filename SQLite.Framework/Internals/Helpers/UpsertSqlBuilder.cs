namespace SQLite.Framework.Internals.Helpers;

/// <summary>
/// Renders an <c>INSERT INTO ... ON CONFLICT (...) DO ...</c> statement for an
/// <see cref="UpsertConflictTarget{T}" />.
/// </summary>
internal static class UpsertSqlBuilder
{
    public static (TableColumn[] Columns, string Sql) Build<T>(SQLiteDatabase database, TableMapping table, UpsertConflictTarget<T> target, Func<TableColumn, string, string> wrapParam)
    {
        TableColumn[] insertColumns = table.Columns
            .Where(c => !c.IsPrimaryKey || !c.IsAutoIncrement)
            .ToArray();

        string columnsList = string.Join(", ", insertColumns.Select(c => IdentifierGuard.Quote(c.Name)));
        string parameters = string.Join(", ", insertColumns.Select((c, i) => wrapParam(c, $"@p{i}")));

        StringBuilder sb = new();
        sb.Append("INSERT INTO \"");
        sb.Append(table.TableName);
        sb.Append("\" (");
        sb.Append(columnsList);
        sb.Append(") VALUES (");
        sb.Append(parameters);
        sb.Append(')');

        sb.Append(" ON CONFLICT (");
        sb.Append(string.Join(", ", target.ConflictColumns.Select(name => IdentifierGuard.Quote(ResolveSqlName(table, name)))));
        sb.Append(')');

        if (target.WherePredicate != null)
        {
            sb.Append(" WHERE ");
            sb.Append(BareSqlTranslator.Translate(database, table, target.WherePredicate));
        }

        UpsertAction<T> action = target.ResolvedAction;
        switch (action.Kind)
        {
            case UpsertActionKind.DoNothing:
                sb.Append(" DO NOTHING");
                break;

            case UpsertActionKind.DoUpdateAll:
            {
                IEnumerable<TableColumn> setColumns = insertColumns.Where(c => !target.ConflictColumns.Contains(c.PropertyInfo.Name) && !target.ConflictColumns.Contains(c.Name));
                AppendUpdate(sb, database, table, setColumns, action);
                break;
            }

            case UpsertActionKind.DoUpdate:
            {
                List<TableColumn> setColumns = [];
                foreach (string propertyName in action.Columns!)
                {
                    TableColumn column = table.Columns.FirstOrDefault(c => c.PropertyInfo.Name == propertyName)
                        ?? throw new InvalidOperationException($"Upsert.DoUpdate references property '{propertyName}' which is not a mapped column on '{table.TableName}'.");
                    setColumns.Add(column);
                }
                AppendUpdate(sb, database, table, setColumns, action);
                break;
            }

            case UpsertActionKind.DoUpdateSet:
            {
                AppendUpdateSet(sb, database, table, action);
                break;
            }

            default:
                throw new InvalidOperationException($"Unknown UpsertActionKind: {action.Kind}");
        }

        return (insertColumns, sb.ToString());
    }

    private static void AppendUpdate<T>(StringBuilder sb, SQLiteDatabase database, TableMapping table, IEnumerable<TableColumn> setColumns, UpsertAction<T> action)
    {
        TableColumn[] columns = setColumns.ToArray();
        if (columns.Length == 0)
        {
            sb.Append(" DO NOTHING");
            return;
        }

        sb.Append(" DO UPDATE SET ");
        for (int i = 0; i < columns.Length; i++)
        {
            if (i > 0)
            {
                sb.Append(", ");
            }
            string quoted = IdentifierGuard.Quote(columns[i].Name);
            sb.Append(quoted);
            sb.Append(" = excluded.");
            sb.Append(quoted);
        }

        AppendUpdateWhere(sb, database, table, action);
    }

    private static void AppendUpdateSet<T>(StringBuilder sb, SQLiteDatabase database, TableMapping table, UpsertAction<T> action)
    {
        sb.Append(" DO UPDATE SET ");
        IReadOnlyList<(string Column, LambdaExpression Rhs)> setters = action.Setters!;
        for (int i = 0; i < setters.Count; i++)
        {
            if (i > 0)
            {
                sb.Append(", ");
            }
            (string columnProperty, LambdaExpression rhs) = setters[i];
            TableColumn column = table.Columns.FirstOrDefault(c => c.PropertyInfo.Name == columnProperty)
                ?? throw new InvalidOperationException($"Upsert.DoUpdate references property '{columnProperty}' which is not a mapped column on '{table.TableName}'.");
            sb.Append(IdentifierGuard.Quote(column.Name));
            sb.Append(" = ");
            sb.Append(BareSqlTranslator.TranslateUpdateRowExpression(database, table, rhs));
        }

        AppendUpdateWhere(sb, database, table, action);
    }

    private static void AppendUpdateWhere<T>(StringBuilder sb, SQLiteDatabase database, TableMapping table, UpsertAction<T> action)
    {
        if (action.UpdateWhere != null)
        {
            sb.Append(" WHERE ");
            sb.Append(BareSqlTranslator.TranslateUpdateRowExpression(database, table, action.UpdateWhere));
        }
    }

    private static string ResolveSqlName(TableMapping table, string propertyOrColumnName)
    {
        TableColumn? byProperty = table.Columns.FirstOrDefault(c => c.PropertyInfo.Name == propertyOrColumnName);
        if (byProperty != null)
        {
            return byProperty.Name;
        }

        TableColumn? byColumn = table.Columns.FirstOrDefault(c => c.Name == propertyOrColumnName);
        if (byColumn != null)
        {
            return byColumn.Name;
        }

        throw new InvalidOperationException($"OnConflict references '{propertyOrColumnName}' which is not a mapped column on '{table.TableName}'.");
    }
}
