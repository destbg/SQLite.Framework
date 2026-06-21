namespace SQLite.Framework.Internals.Helpers;

/// <summary>
/// Renders an <c>INSERT INTO ... ON CONFLICT (...) DO ...</c> statement for an
/// <see cref="SQLiteUpsertConflictTarget{T}" />.
/// </summary>
internal static class UpsertSqlBuilder
{
    public static (TableColumn[] Columns, string Sql) Build<T>(SQLiteDatabase database, TableMapping table, SQLiteUpsertConflictTarget<T> target, Func<TableColumn, string, string> wrapParam, IReadOnlyList<(string Column, string ValueSql)>? extraColumns = null, TableColumn[]? insertOverride = null)
    {
        TableColumn[] insertColumns = table.Columns.ToArray();

        if (table.ComputedColumns.Count > 0)
        {
            HashSet<string> computedNames = table.ComputedColumns.Select(c => c.Column.Name).ToHashSet();
            insertColumns = insertColumns.Where(c => !computedNames.Contains(c.Name)).ToArray();
        }

        if (extraColumns is { Count: > 0 })
        {
            HashSet<string> overridden = extraColumns.Select(e => e.Column).ToHashSet();
            insertColumns = insertColumns.Where(c => !overridden.Contains(c.Name)).ToArray();
        }

        TableColumn[] valueColumns = insertOverride ?? insertColumns;

        IEnumerable<string> names = valueColumns.Select(c => IdentifierGuard.Quote(c.Name));
        IEnumerable<string> values = valueColumns.Select((c, i) => wrapParam(c, $"@p{i}"));
        if (extraColumns is { Count: > 0 })
        {
            names = names.Concat(extraColumns.Select(e => IdentifierGuard.Quote(e.Column)));
            values = values.Concat(extraColumns.Select(e => e.ValueSql));
        }

        string columnsList = string.Join(", ", names);
        string parameters = string.Join(", ", values);

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

        SQLiteUpsertAction<T> action = target.ResolvedAction;
        switch (action.Kind)
        {
            case UpsertActionKind.DoNothing:
                sb.Append(" DO NOTHING");
                break;

            case UpsertActionKind.DoUpdateAll:
            {
                IEnumerable<TableColumn> setColumns = insertColumns.Where(c => !c.IsPrimaryKey && !target.ConflictColumns.Contains(c.PropertyInfo.Name) && !target.ConflictColumns.Contains(c.Name));
                List<string>? extraSetColumns = null;
                if (extraColumns is { Count: > 0 })
                {
                    HashSet<string> conflictNames = target.ConflictColumns.Select(name => ResolveSqlName(table, name)).ToHashSet();
                    HashSet<string> primaryKeyNames = table.Columns.Where(c => c.IsPrimaryKey).Select(c => c.Name).ToHashSet();
                    extraSetColumns = extraColumns
                        .Where(e => !conflictNames.Contains(e.Column) && !primaryKeyNames.Contains(e.Column))
                        .Select(e => e.Column)
                        .ToList();
                }
                AppendUpdate(sb, database, table, setColumns, action, extraSetColumns);
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

        return (valueColumns, sb.ToString());
    }

    private static void AppendUpdate<T>(StringBuilder sb, SQLiteDatabase database, TableMapping table, IEnumerable<TableColumn> setColumns, SQLiteUpsertAction<T> action, List<string>? extraSetColumns = null)
    {
        List<string> names = setColumns.Select(c => c.Name).ToList();
        if (extraSetColumns != null)
        {
            names.AddRange(extraSetColumns);
        }

        if (names.Count == 0)
        {
            sb.Append(" DO NOTHING");
            return;
        }

        sb.Append(" DO UPDATE SET ");
        for (int i = 0; i < names.Count; i++)
        {
            if (i > 0)
            {
                sb.Append(", ");
            }
            string quoted = IdentifierGuard.Quote(names[i]);
            sb.Append(quoted);
            sb.Append(" = excluded.");
            sb.Append(quoted);
        }

        AppendUpdateWhere(sb, database, table, action);
    }

    private static void AppendUpdateSet<T>(StringBuilder sb, SQLiteDatabase database, TableMapping table, SQLiteUpsertAction<T> action)
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
            string rhsSql = BareSqlTranslator.TranslateUpdateRowExpression(database, table, rhs);
            if (ExpressionHelpers.IsConstant(rhs.Body))
            {
                rhsSql = ConverterSql.WrapParameter(rhsSql, column.PropertyType, database.Options);
            }

            sb.Append(rhsSql);
        }

        AppendUpdateWhere(sb, database, table, action);
    }

    private static void AppendUpdateWhere<T>(StringBuilder sb, SQLiteDatabase database, TableMapping table, SQLiteUpsertAction<T> action)
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
