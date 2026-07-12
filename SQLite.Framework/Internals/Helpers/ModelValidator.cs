namespace SQLite.Framework.Internals.Helpers;

/// <summary>
/// Compares an entity <see cref="TableMapping" /> against the live database schema using the
/// <c>pragma_table_info</c>, <c>pragma_index_list</c> and <c>pragma_foreign_key_list</c>
/// introspection functions and collects each difference as a message.
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
        Dictionary<string, PragmaTableInfo> byName = dbColumns.ToDictionary(c => c.Name, StringComparer.OrdinalIgnoreCase);
        HashSet<string> computedNames = mapping.ComputedColumns.Select(c => c.Column.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        HashSet<string>? generatedColumnNames = null;

        List<TableColumn> modelKey = mapping.Columns.Where(c => c.IsPrimaryKey).OrderBy(c => c.PrimaryKeyOrder).ToList();
        Dictionary<string, int> modelKeyRank = new(StringComparer.OrdinalIgnoreCase);
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
            if (TypeAffinityResolver.Resolve(dbColumn.Type) != TypeAffinityResolver.Resolve(expectedType))
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

        foreach (ShadowColumnSpec shadow in mapping.ShadowColumns)
        {
            if (!byName.ContainsKey(shadow.Name))
            {
                issues.Add($"Column '{table}'.'{shadow.Name}' is missing in the database.");
            }
        }

        foreach (PragmaTableInfo dbColumn in dbColumns)
        {
            if (mapping.Columns.All(c => !string.Equals(c.Name, dbColumn.Name, StringComparison.OrdinalIgnoreCase))
                && mapping.ShadowColumns.All(s => !string.Equals(s.Name, dbColumn.Name, StringComparison.OrdinalIgnoreCase)))
            {
                issues.Add($"Column '{table}'.'{dbColumn.Name}' exists in the database but not in the model.");
            }
        }
    }

    private static void ValidateIndexes(SQLiteDatabase database, string table, TableMapping mapping, List<string> issues)
    {
        Dictionary<string, (IReadOnlyList<string> Columns, bool Unique)> expected = BuildExpectedIndexes(mapping, table);
        Dictionary<string, PragmaIndexList> dbIndexes = database.Pragmas.IndexList(table).ToDictionary(i => i.Name, StringComparer.OrdinalIgnoreCase);
        Dictionary<string, string> declaredSql = SchemaSqlBuilder.BuildIndexes(mapping, table, ifNotExists: false)
            .ToDictionary(x => x.Name, x => x.Sql, StringComparer.OrdinalIgnoreCase);

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

            IReadOnlyList<string> dbColumns = ReadIndexColumns(database, dbIndex.Name);
            if (!dbColumns.SequenceEqual(columns, StringComparer.OrdinalIgnoreCase))
            {
                issues.Add($"Index '{name}' on table '{table}' columns do not match the model.");
                mismatchReported = true;
            }

            if (!mismatchReported)
            {
                string? liveSql = database.ExecuteScalar<string?>(
                    $"SELECT sql FROM sqlite_master WHERE type = 'index' AND name = '{dbIndex.Name.Replace("'", "''")}'");
                if (!SchemaSqlNormalizer.AreEquivalent(declaredSql[name], liveSql))
                {
                    issues.Add($"Index '{name}' on table '{table}' definition does not match the model (such as a partial-index filter or column direction).");
                }
            }
        }
    }

    private static Dictionary<string, (IReadOnlyList<string> Columns, bool Unique)> BuildExpectedIndexes(TableMapping mapping, string table)
    {
        Dictionary<string, (IReadOnlyList<string>, bool)> expected = new(StringComparer.OrdinalIgnoreCase);

        IEnumerable<IGrouping<string, (string Name, string Column, int Order, bool Unique)>> groups = mapping.Columns
            .SelectMany(col => col.Indices.Select(idx => (
                Name: idx.Name ?? $"idx_{table}_{col.Name}",
                Column: col.Name,
                idx.Order,
                Unique: idx.IsUnique)))
            .GroupBy(x => x.Name, StringComparer.OrdinalIgnoreCase);

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
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
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
        Dictionary<string, IReadOnlyList<string>> targetKeys = new(StringComparer.OrdinalIgnoreCase);

        foreach (TableColumn column in mapping.Columns)
        {
            if (column.ForeignKey != null)
            {
                CheckForeignKey(database, column.ForeignKey, dbForeignKeys, table, targetKeys, issues);
            }
        }

        foreach (ForeignKeyInfo foreignKey in mapping.CompositeForeignKeys)
        {
            CheckForeignKey(database, foreignKey, dbForeignKeys, table, targetKeys, issues);
        }
    }

    private static void CheckForeignKey(SQLiteDatabase database, ForeignKeyInfo foreignKey, List<PragmaForeignKey> dbForeignKeys, string table, Dictionary<string, IReadOnlyList<string>> targetKeys, List<string> issues)
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
                string? target = rows[i].ToColumn ?? ResolveImplicitTarget(database, rows[i].ReferencedTable, i, targetKeys);
                if (!string.Equals(rows[i].FromColumn, foreignKey.Columns[i], StringComparison.OrdinalIgnoreCase)
                    || !string.Equals(target, foreignKey.TargetColumns[i], StringComparison.OrdinalIgnoreCase)
                    || !string.Equals(rows[i].ReferencedTable, foreignKey.TargetTable, StringComparison.OrdinalIgnoreCase))
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

    private static string? ResolveImplicitTarget(SQLiteDatabase database, string targetTable, int position, Dictionary<string, IReadOnlyList<string>> targetKeys)
    {
        if (!targetKeys.TryGetValue(targetTable, out IReadOnlyList<string>? key))
        {
            key = database.Pragmas.TableInfo(targetTable).ToList()
                .Where(c => c.PrimaryKeyOrder > 0)
                .OrderBy(c => c.PrimaryKeyOrder)
                .Select(c => c.Name)
                .ToList();
            targetKeys.Add(targetTable, key);
        }

        return position < key.Count ? key[position] : null;
    }

    private static void ValidateTriggers(SQLiteDatabase database, string table, TableMapping mapping, List<string> issues)
    {
        foreach ((string name, string sql) in SchemaSqlBuilder.BuildTriggers(mapping, mapping.TableName, ifNotExists: false))
        {
            string escaped = name.Replace("'", "''");
            string? live = database.ExecuteScalar<string?>(
                $"SELECT sql FROM sqlite_master WHERE type = 'trigger' AND name = '{escaped}' COLLATE NOCASE");
            if (live == null)
            {
                issues.Add($"Trigger '{name}' on table '{table}' is missing in the database.");
            }
            else if (!SchemaSqlNormalizer.AreEquivalent(sql, live))
            {
                issues.Add($"Trigger '{name}' on table '{table}' does not match the model definition.");
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
