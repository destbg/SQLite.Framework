namespace SQLite.Framework.Internals.Helpers;

/// <summary>
/// Compares an entity <see cref="TableMapping" /> against the live database schema using the
/// <c>pragma_table_info</c>, <c>pragma_index_list</c>, and <c>pragma_foreign_key_list</c>
/// introspection functions, and collects each difference as a message.
/// </summary>
internal static class ModelValidator
{
    public static IReadOnlyList<string> Validate(SQLiteDatabase database, TableMapping mapping)
    {
        List<string> issues = [];
        string table = mapping.TableName;

        List<PragmaTableInfo> dbColumns = database.Pragmas.TableInfo(table).ToList();
        if (dbColumns.Count == 0)
        {
            issues.Add($"Table '{table}' does not exist in the database.");
            return issues;
        }

        if (mapping.IsFullTextSearch || mapping.IsRTree)
        {
            return issues;
        }

        ValidateColumns(database, table, mapping, dbColumns, issues);
        ValidateIndexes(database, table, mapping, issues);
        ValidateForeignKeys(database, table, mapping, issues);
        ValidateTriggers(database, table, mapping, issues);
        return issues;
    }

    private static void ValidateColumns(SQLiteDatabase database, string table, TableMapping mapping, List<PragmaTableInfo> dbColumns, List<string> issues)
    {
        Dictionary<string, PragmaTableInfo> byName = dbColumns.ToDictionary(c => c.Name);
        HashSet<string> computedNames = mapping.ComputedColumns.Select(c => c.Column.Name).ToHashSet();
        HashSet<string>? generatedColumnNames = null;

        List<TableColumn> modelKey = mapping.Columns.Where(c => c.IsPrimaryKey).OrderBy(c => c.PrimaryKeyOrder).ToList();
        Dictionary<string, int> modelKeyRank = new();
        for (int i = 0; i < modelKey.Count; i++)
        {
            modelKeyRank[modelKey[i].Name] = i + 1;
        }

        int keyCount = modelKey.Count;

        foreach (TableColumn column in mapping.Columns)
        {
            if (computedNames.Contains(column.Name))
            {
                generatedColumnNames ??= ReadAllColumnNames(database, table);
                if (!generatedColumnNames.Contains(column.Name))
                {
                    issues.Add($"Column '{table}'.'{column.Name}' is missing in the database.");
                }

                continue;
            }

            if (!byName.TryGetValue(column.Name, out PragmaTableInfo? dbColumn))
            {
                issues.Add($"Column '{table}'.'{column.Name}' is missing in the database.");
                continue;
            }

            string expectedType = column.ColumnType.ToString().ToUpperInvariant();
            if (!string.Equals(dbColumn.Type, expectedType, StringComparison.OrdinalIgnoreCase))
            {
                issues.Add($"Column '{table}'.'{column.Name}' has type '{dbColumn.Type}' but the model expects '{expectedType}'.");
            }

            bool dbIsKey = dbColumn.PrimaryKeyOrder > 0;
            if (dbIsKey != column.IsPrimaryKey)
            {
                issues.Add($"Column '{table}'.'{column.Name}' primary-key flag does not match the model.");
            }
            else if (column.IsPrimaryKey && keyCount > 1 && dbColumn.PrimaryKeyOrder != modelKeyRank[column.Name])
            {
                issues.Add($"Column '{table}'.'{column.Name}' primary-key order does not match the model.");
            }

            bool isRowIdAlias = column.IsPrimaryKey && keyCount == 1 && column.ColumnType == SQLiteColumnType.Integer;
            if (!isRowIdAlias && dbColumn.IsNotNull == column.IsNullable)
            {
                issues.Add($"Column '{table}'.'{column.Name}' nullability does not match the model.");
            }
        }

        foreach (PragmaTableInfo dbColumn in dbColumns)
        {
            if (mapping.Columns.All(c => c.Name != dbColumn.Name)
                && mapping.ShadowColumns.All(s => s.Name != dbColumn.Name))
            {
                issues.Add($"Column '{table}'.'{dbColumn.Name}' exists in the database but not in the model.");
            }
        }
    }

    private static void ValidateIndexes(SQLiteDatabase database, string table, TableMapping mapping, List<string> issues)
    {
        Dictionary<string, (IReadOnlyList<string> Columns, bool Unique)> expected = BuildExpectedIndexes(mapping, table);
        Dictionary<string, PragmaIndexList> dbIndexes = database.Pragmas.IndexList(table).ToDictionary(i => i.Name);
        Dictionary<string, string> declaredSql = SchemaSqlBuilder.BuildIndexes(mapping, table, ifNotExists: false)
            .ToDictionary(x => x.Name, x => x.Sql);

        foreach ((string name, (IReadOnlyList<string> columns, bool unique)) in expected)
        {
            if (!dbIndexes.TryGetValue(name, out PragmaIndexList? dbIndex))
            {
                issues.Add($"Index '{name}' on table '{table}' is missing in the database.");
                continue;
            }

            bool mismatchReported = false;
            if (dbIndex.IsUnique != unique)
            {
                issues.Add($"Index '{name}' on table '{table}' uniqueness does not match the model.");
                mismatchReported = true;
            }

            IReadOnlyList<string> dbColumns = ReadIndexColumns(database, name);
            if (!dbColumns.SequenceEqual(columns))
            {
                issues.Add($"Index '{name}' on table '{table}' columns do not match the model.");
                mismatchReported = true;
            }

            if (!mismatchReported)
            {
                string? liveSql = database.ExecuteScalar<string?>(
                    $"SELECT sql FROM sqlite_master WHERE type = 'index' AND name = '{name.Replace("'", "''")}'");
                if (!string.Equals(declaredSql[name], liveSql, StringComparison.Ordinal))
                {
                    issues.Add($"Index '{name}' on table '{table}' definition does not match the model (such as a partial-index filter or column direction).");
                }
            }
        }
    }

    private static Dictionary<string, (IReadOnlyList<string> Columns, bool Unique)> BuildExpectedIndexes(TableMapping mapping, string table)
    {
        Dictionary<string, (IReadOnlyList<string>, bool)> expected = new();

        IEnumerable<IGrouping<string, (string Name, string Column, int Order, bool Unique)>> groups = mapping.Columns
            .SelectMany(col => col.Indices.Select(idx => (
                Name: idx.Name ?? $"idx_{table}_{col.Name}",
                Column: col.Name,
                idx.Order,
                Unique: idx.IsUnique)))
            .GroupBy(x => x.Name);

        foreach (IGrouping<string, (string Name, string Column, int Order, bool Unique)> group in groups)
        {
            IReadOnlyList<string> columns = group.OrderBy(x => x.Order).Select(x => x.Column).ToList();
            expected[group.Key] = (columns, group.Any(x => x.Unique));
        }

        foreach (IndexSpec index in mapping.Indexes)
        {
            IReadOnlyList<string> indexColumns = index.Columns.Where((_, i) => !index.Expressions[i]).ToList();
            expected[index.Name] = (indexColumns, index.Unique);
        }

        return expected;
    }

    [UnconditionalSuppressMessage("AOT", "IL2026", Justification = "Querying built-in dictionary rows keeps their public members reachable.")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Querying built-in dictionary rows keeps their public members reachable.")]
    private static HashSet<string> ReadAllColumnNames(SQLiteDatabase database, string table)
    {
        return database.Query<Dictionary<string, object?>>($"PRAGMA table_xinfo('{table.Replace("'", "''")}')")
            .Select(row => (string)row["name"]!)
            .ToHashSet();
    }

    [UnconditionalSuppressMessage("AOT", "IL2026", Justification = "Querying built-in dictionary rows keeps their public members reachable.")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Querying built-in dictionary rows keeps their public members reachable.")]
    private static List<string> ReadIndexColumns(SQLiteDatabase database, string indexName)
    {
        return database.Query<Dictionary<string, object?>>($"PRAGMA index_info('{indexName.Replace("'", "''")}')")
            .OrderBy(row => Convert.ToInt64(row["seqno"], CultureInfo.InvariantCulture))
            .Where(row => row["name"] != null)
            .Select(row => (string)row["name"]!)
            .ToList();
    }

    private static void ValidateForeignKeys(SQLiteDatabase database, string table, TableMapping mapping, List<string> issues)
    {
        List<PragmaForeignKey> dbForeignKeys = database.Pragmas.ForeignKeyList(table).ToList();

        foreach (TableColumn column in mapping.Columns)
        {
            if (column.ForeignKey != null)
            {
                CheckForeignKey(column.ForeignKey, dbForeignKeys, table, issues);
            }
        }

        foreach (ForeignKeyInfo foreignKey in mapping.CompositeForeignKeys)
        {
            CheckForeignKey(foreignKey, dbForeignKeys, table, issues);
        }
    }

    private static void CheckForeignKey(ForeignKeyInfo foreignKey, List<PragmaForeignKey> dbForeignKeys, string table, List<string> issues)
    {
        string from = string.Join(", ", foreignKey.Columns);

        PragmaForeignKey[]? matched = null;
        foreach (IGrouping<long, PragmaForeignKey> group in dbForeignKeys.GroupBy(d => d.Id))
        {
            PragmaForeignKey[] rows = group.OrderBy(r => r.ColumnPosition).ToArray();
            if (rows.Length != foreignKey.Columns.Count)
            {
                continue;
            }

            bool allMatch = true;
            for (int i = 0; i < rows.Length; i++)
            {
                if (rows[i].FromColumn != foreignKey.Columns[i]
                    || rows[i].ToColumn != foreignKey.TargetColumns[i]
                    || rows[i].ReferencedTable != foreignKey.TargetTable)
                {
                    allMatch = false;
                    break;
                }
            }

            if (allMatch)
            {
                matched = rows;
                break;
            }
        }

        if (matched == null)
        {
            issues.Add($"Foreign key '{table}'.'{from}' -> '{foreignKey.TargetTable}' is missing in the database.");
            return;
        }

        string expectedOnDelete = ForeignKeyActionToSql(foreignKey.OnDelete);
        if (!string.Equals(matched[0].OnDelete, expectedOnDelete, StringComparison.OrdinalIgnoreCase))
        {
            issues.Add($"Foreign key '{table}'.'{from}' -> '{foreignKey.TargetTable}' ON DELETE action is '{matched[0].OnDelete}' but the model expects '{expectedOnDelete}'.");
        }

        string expectedOnUpdate = ForeignKeyActionToSql(foreignKey.OnUpdate);
        if (!string.Equals(matched[0].OnUpdate, expectedOnUpdate, StringComparison.OrdinalIgnoreCase))
        {
            issues.Add($"Foreign key '{table}'.'{from}' -> '{foreignKey.TargetTable}' ON UPDATE action is '{matched[0].OnUpdate}' but the model expects '{expectedOnUpdate}'.");
        }
    }

    private static void ValidateTriggers(SQLiteDatabase database, string table, TableMapping mapping, List<string> issues)
    {
        foreach (TriggerSpec trigger in mapping.Triggers)
        {
            string escaped = trigger.Name.Replace("'", "''");
            string? live = database.ExecuteScalar<string?>(
                $"SELECT name FROM sqlite_master WHERE type = 'trigger' AND name = '{escaped}'");
            if (live == null)
            {
                issues.Add($"Trigger '{trigger.Name}' on table '{table}' is missing in the database.");
            }
        }
    }

    private static string ForeignKeyActionToSql(SQLiteForeignKeyAction action)
    {
        return action switch
        {
            SQLiteForeignKeyAction.Cascade => "CASCADE",
            SQLiteForeignKeyAction.Restrict => "RESTRICT",
            SQLiteForeignKeyAction.SetNull => "SET NULL",
            SQLiteForeignKeyAction.SetDefault => "SET DEFAULT",
            _ => "NO ACTION"
        };
    }
}
