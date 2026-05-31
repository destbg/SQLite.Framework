namespace SQLite.Framework.Internals.Helpers;

/// <summary>
/// Builds the schema SQL for a table from its <see cref="TableMapping" />. One generator is shared
/// by <c>CreateTable</c> and <c>Migrate</c> so every path produces the same SQL and drift detection
/// stays exact. All schema configuration is read from the mapping, which is the single source of
/// truth.
/// </summary>
internal static class SchemaSqlBuilder
{
    public static string BuildCreateTable(SQLiteDatabase database, TableMapping mapping, string tableName, bool ifNotExists)
    {
        TableColumn[] primaryKeyColumns = mapping.Columns.Where(c => c.IsPrimaryKey).ToArray();
        bool hasCompositePrimaryKey = primaryKeyColumns.Length > 1;

        StringBuilder sb = new();
        sb.Append("CREATE TABLE ");
        if (ifNotExists)
        {
            sb.Append("IF NOT EXISTS ");
        }
        sb.Append('"');
        sb.Append(tableName);
        sb.Append("\" (");

        bool first = true;
        foreach (TableColumn col in mapping.Columns)
        {
            if (!first) sb.Append(", ");
            first = false;

            ComputedColumnSpec? cc = mapping.ComputedColumns.FirstOrDefault(c => c.Column.Name == col.Name);
            if (cc != null)
            {
                sb.Append(IdentifierGuard.Quote(col.Name));
                sb.Append(' ');
                sb.Append(col.ColumnType.ToString().ToUpperInvariant());
                sb.Append(" GENERATED ALWAYS AS (");
                sb.Append(cc.ExpressionSql);
                sb.Append(") ");
                sb.Append(cc.Stored ? "STORED" : "VIRTUAL");
            }
            else
            {
                sb.Append(ColumnSql.GetCreateColumnSql(col, !hasCompositePrimaryKey));
            }
        }

        foreach (ShadowColumnSpec shadow in mapping.ShadowColumns)
        {
            sb.Append(", ");
            sb.Append(shadow.ToColumnSql());
        }

        if (hasCompositePrimaryKey)
        {
            sb.Append(", PRIMARY KEY (");
            for (int i = 0; i < primaryKeyColumns.Length; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append('"');
                sb.Append(primaryKeyColumns[i].Name);
                sb.Append('"');
            }
            sb.Append(')');
        }

        foreach (CheckConstraintSpec check in mapping.Checks)
        {
            sb.Append(", ");
            if (!string.IsNullOrEmpty(check.Name))
            {
                sb.Append("CONSTRAINT \"");
                sb.Append(check.Name.Replace("\"", "\"\""));
                sb.Append("\" ");
            }
            sb.Append("CHECK (");
            sb.Append(check.Sql);
            sb.Append(')');
        }

        foreach (ForeignKeyInfo composite in mapping.CompositeForeignKeys)
        {
            sb.Append(", ");
            ForeignKeySql.WriteSql(composite, sb, inline: false);
        }

        sb.Append(')');

        if (mapping.WithoutRowId)
        {
            sb.Append(" WITHOUT ROWID");
        }

        if (mapping.Strict)
        {
#if SQLITE_FRAMEWORK_VERSION_AWARE
            database.Options.EnsureMinimumVersion(SQLiteMinimumVersion.V3_37, "STRICT tables");
#endif
            sb.Append(mapping.WithoutRowId ? ", STRICT" : " STRICT");
        }

        return sb.ToString();
    }

    public static List<(string Name, string Sql)> BuildIndexes(TableMapping mapping, string tableName, bool ifNotExists)
    {
        string existsClause = ifNotExists ? "IF NOT EXISTS " : string.Empty;
        List<(string, string)> statements = [];

        var indexGroups = mapping.Columns
            .SelectMany(col => col.Indices.Select(idx => (
                Name: idx.Name ?? ("idx_" + col.Name + "_" + idx.Order),
                Column: col.Name,
                Order: idx.Order,
                IsUnique: idx.IsUnique,
                Collation: idx.Collation,
                Direction: idx.Direction)))
            .GroupBy(x => x.Name);

        foreach (var group in indexGroups)
        {
            var ordered = group.OrderBy(x => x.Order).ToArray();
            string uniqueClause = group.Any(x => x.IsUnique) ? "UNIQUE " : string.Empty;
            string columnList = string.Join(", ", ordered.Select(x => IdentifierGuard.Quote(x.Column) + CollationHelper.Clause(x.Collation) + IndexDirectionHelper.Clause(x.Direction)));
            statements.Add((group.Key, $"CREATE {uniqueClause}INDEX {existsClause}\"{group.Key.Replace("\"", "\"\"")}\" ON \"{tableName}\" ({columnList})"));
        }

        foreach (IndexSpec index in mapping.Indexes)
        {
            string uniqueClause = index.Unique ? "UNIQUE " : string.Empty;
            string columnList = string.Join(", ", index.Columns.Select((c, i) => c + CollationHelper.Clause(index.Collations[i]) + IndexDirectionHelper.Clause(index.Directions[i])));
            string where = index.FilterSql == null ? string.Empty : $" WHERE {index.FilterSql}";
            statements.Add((index.Name, $"CREATE {uniqueClause}INDEX {existsClause}\"{index.Name.Replace("\"", "\"\"")}\" ON \"{tableName}\" ({columnList}){where}"));
        }

        return statements;
    }

    public static List<(string Name, string Sql)> BuildTriggers(TableMapping mapping, string tableName, bool ifNotExists)
    {
        List<(string, string)> statements = [];
        foreach (TriggerSpec trigger in mapping.Triggers)
        {
            statements.Add((trigger.Name, BuildCreateTrigger(tableName, trigger.Name, trigger.Timing, trigger.Event, trigger.ForEachRow, trigger.WhenSql, trigger.BodySql, ifNotExists)));
        }

        return statements;
    }

    public static string BuildCreateTrigger(string tableName, string name, SQLiteTriggerTiming timing, SQLiteTriggerEvent @event, bool forEachRow, string? when, string body, bool ifNotExists)
    {
        StringBuilder sb = new();
        sb.Append("CREATE TRIGGER ");
        if (ifNotExists)
        {
            sb.Append("IF NOT EXISTS ");
        }
        sb.Append('"');
        sb.Append(name.Replace("\"", "\"\""));
        sb.Append("\" ");
        sb.Append(timing switch
        {
            SQLiteTriggerTiming.Before => "BEFORE",
            SQLiteTriggerTiming.After => "AFTER",
            SQLiteTriggerTiming.InsteadOf => "INSTEAD OF",
            _ => throw new ArgumentOutOfRangeException(nameof(timing)),
        });
        sb.Append(' ');
        sb.Append(@event switch
        {
            SQLiteTriggerEvent.Insert => "INSERT",
            SQLiteTriggerEvent.Update => "UPDATE",
            SQLiteTriggerEvent.Delete => "DELETE",
            _ => throw new ArgumentOutOfRangeException(nameof(@event)),
        });
        sb.Append(" ON \"");
        sb.Append(tableName);
        sb.Append('"');
        if (forEachRow)
        {
            sb.Append(" FOR EACH ROW");
        }
        if (!string.IsNullOrEmpty(when))
        {
            sb.Append(" WHEN ");
            sb.Append(when);
        }
        sb.Append(" BEGIN ");
        sb.Append(body);
        if (!body.TrimEnd().EndsWith(';'))
        {
            sb.Append(';');
        }
        sb.Append(" END");

        return sb.ToString();
    }
}
