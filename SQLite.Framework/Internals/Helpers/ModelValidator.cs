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

        ValidateColumns(table, mapping, dbColumns, issues);
        ValidateIndexes(database, table, mapping, issues);
        ValidateForeignKeys(database, table, mapping, issues);
        return issues;
    }

    private static void ValidateColumns(string table, TableMapping mapping, List<PragmaTableInfo> dbColumns, List<string> issues)
    {
        Dictionary<string, PragmaTableInfo> byName = dbColumns.ToDictionary(c => c.Name);

        foreach (TableColumn column in mapping.Columns)
        {
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

            if ((dbColumn.PrimaryKeyOrder > 0) != column.IsPrimaryKey)
            {
                issues.Add($"Column '{table}'.'{column.Name}' primary-key flag does not match the model.");
            }

            if (!column.IsPrimaryKey && dbColumn.IsNotNull == column.IsNullable)
            {
                issues.Add($"Column '{table}'.'{column.Name}' nullability does not match the model.");
            }
        }

        foreach (PragmaTableInfo dbColumn in dbColumns)
        {
            if (mapping.Columns.All(c => c.Name != dbColumn.Name))
            {
                issues.Add($"Column '{table}'.'{dbColumn.Name}' exists in the database but not in the model.");
            }
        }
    }

    private static void ValidateIndexes(SQLiteDatabase database, string table, TableMapping mapping, List<string> issues)
    {
        HashSet<string> dbIndexes = database.Pragmas.IndexList(table).Select(i => i.Name).ToHashSet();

        IEnumerable<string> expected = mapping.Columns
            .SelectMany(col => col.Indices.Select(idx => idx.Name ?? $"idx_{col.Name}_{idx.Order}"))
            .Distinct();

        foreach (string name in expected)
        {
            if (!dbIndexes.Contains(name))
            {
                issues.Add($"Index '{name}' on table '{table}' is missing in the database.");
            }
        }
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
        for (int i = 0; i < foreignKey.Columns.Count; i++)
        {
            string from = foreignKey.Columns[i];
            string to = foreignKey.TargetColumns[i];
            string target = foreignKey.TargetTable;

            if (!dbForeignKeys.Any(d => d.FromColumn == from && d.ToColumn == to && d.ReferencedTable == target))
            {
                issues.Add($"Foreign key '{table}'.'{from}' -> '{target}'.'{to}' is missing in the database.");
            }
        }
    }
}
