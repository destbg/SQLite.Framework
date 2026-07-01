namespace SQLite.Framework;

/// <summary>
/// Runs ordered, versioned migrations. Reach an instance with <see cref="SQLiteSchema.Migrations" />.
/// Declare each schema version with <see cref="Version" />, then call <see cref="Migrate" /> to bring
/// the database from the version it records up to the highest declared version. The version is stored
/// in <c>PRAGMA user_version</c>, so already-applied versions are skipped on the next run.
/// </summary>
/// <remarks>
/// Migrations always move toward the current model. There is no path back to an older version and
/// no way to stop at a version below the highest one. Use the methods on <see cref="SQLiteSchema" />
/// directly if you need that. A whole run happens in one transaction, so a failure rolls the database
/// back to the version it started at and a re-run retries from there.
/// </remarks>
public sealed class SQLiteMigrationRunner
{
    private readonly SQLiteSchema schema;
    private readonly SortedDictionary<int, Action<SQLiteMigrationStep>> versions = new();

    internal SQLiteMigrationRunner(SQLiteSchema schema)
    {
        this.schema = schema;
    }

    internal SQLiteDatabase Database => schema.Database;

    /// <summary>
    /// Declares the work for one schema version. Versions are applied in ascending order. Each
    /// version number must be one or more and may be declared only once.
    /// </summary>
    /// <param name="version">The version number this step brings the database to.</param>
    /// <param name="build">A callback that declares the work, using the passed
    /// <see cref="SQLiteMigrationStep" />.</param>
    public SQLiteMigrationRunner Version(int version, Action<SQLiteMigrationStep> build)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(version, 1);
        ArgumentNullException.ThrowIfNull(build);
        if (versions.ContainsKey(version))
        {
            throw new InvalidOperationException($"Version {version} is declared more than once.");
        }

        versions.Add(version, build);
        return this;
    }

    /// <summary>
    /// Registers the migration <typeparamref name="T" /> under its
    /// <see cref="ISQLiteMigration.Version" />. The version number is read without creating an
    /// instance and the migration is constructed only when its version is applied, so a class that
    /// has already run is never loaded into memory. The same version rules as
    /// <see cref="Version" /> apply.
    /// </summary>
    /// <typeparam name="T">The migration type to register.</typeparam>
    public SQLiteMigrationRunner Add<T>() where T : ISQLiteMigration, new()
    {
        return Version(T.Version, static step => new T().Apply(step));
    }

    /// <summary>
    /// Reads the version recorded in the database and reports what a <see cref="Migrate" /> would do,
    /// without changing anything.
    /// </summary>
    public SQLiteMigrationPlan Plan()
    {
        int currentVersion = schema.Database.Pragmas.UserVersion;
        int targetVersion = versions.Count == 0 ? currentVersion : versions.Keys.Last();
        List<string> operations = versions
            .Where(v => v.Key > currentVersion)
            .SelectMany(v => BuildStep(v.Value).Operations.Select(o => o.Description))
            .ToList();
        return new SQLiteMigrationPlan(currentVersion, targetVersion, operations);
    }

    /// <summary>
    /// Applies every declared version above the one the database records, in one transaction, then
    /// records the highest declared version. Returns the number of statements run. Does nothing and
    /// returns zero when the database is already up to date.
    /// </summary>
#if SQLITE_FRAMEWORK_OS_BUNDLED_SQLITE
    [UnsupportedOSPlatform("ios")]
    [SupportedOSPlatform("ios15.0")]
#endif
    public int Migrate()
    {
        int currentVersion = schema.Database.Pragmas.UserVersion;
        List<KeyValuePair<int, Action<SQLiteMigrationStep>>> pending = versions.Where(v => v.Key > currentVersion).ToList();
        if (pending.Count == 0)
        {
            return 0;
        }

        int targetVersion = pending[^1].Key;
        List<MigrationOperation> operations = pending.SelectMany(v => BuildStep(v.Value).Operations).ToList();

        int count = 0;
        using SQLiteTransaction transaction = schema.Database.BeginTransaction();

        foreach (MigrationOperation operation in operations.Where(o => o.Kind == MigrationOperationKind.RenameColumn))
        {
            count += RenameColumnIfPresent(operation.Mapping!.TableName, operation.FromColumn!, operation.ToColumn!);
        }

        HashSet<string> newlyCreated = new(StringComparer.Ordinal);
        foreach (MigrationOperation operation in operations.Where(o => o.Kind == MigrationOperationKind.CreateTable))
        {
            if (!schema.TableExists(operation.Mapping!.TableName))
            {
                newlyCreated.Add(operation.Mapping.TableName);
            }

            count += schema.CreateTable(operation.Mapping!.Type);
        }

        foreach (IGrouping<string, MigrationOperation> group in operations
                     .Where(o => o.Kind == MigrationOperationKind.Reconcile)
                     .GroupBy(o => o.Mapping!.TableName))
        {
            if (newlyCreated.Contains(group.Key))
            {
                continue;
            }

            TableMapping mapping = group.First().Mapping!;
            bool rebuild = group.Any(o => o.Rebuild);
            IReadOnlyList<(string Column, string ValueSql)> sets = UnionSets(group);
            count += rebuild ? MigrateCore(mapping, sets) : MigrateInPlace(mapping, sets);
        }

        foreach (MigrationOperation operation in operations.Where(o => o.Kind is MigrationOperationKind.DropColumn or MigrationOperationKind.DropTable or MigrationOperationKind.RawSql))
        {
            if (operation.Kind == MigrationOperationKind.RawSql)
            {
                count += schema.Database.Execute(operation.Sql!);
            }
            else if (operation.Kind == MigrationOperationKind.DropColumn)
            {
                if (newlyCreated.Contains(operation.Mapping!.TableName))
                {
                    continue;
                }

                count += DropColumnIfRemovable(operation.Mapping!, operation.ColumnName!);
            }
            else if (operation.Mapping != null)
            {
                count += schema.DropTable(operation.Mapping.Type);
            }
            else
            {
                count += schema.DropTable(operation.TableName!);
            }
        }

        schema.Database.Pragmas.UserVersion = targetVersion;
        transaction.Commit();
        return count;
    }

    private SQLiteMigrationStep BuildStep(Action<SQLiteMigrationStep> build)
    {
        SQLiteMigrationStep step = new(schema.Database);
        build(step);
        return step;
    }

    private int RenameColumnIfPresent(string tableName, string fromColumn, string toColumn)
    {
        bool present = Database.Pragmas.TableInfo(tableName).ToList().Any(c => c.Name == fromColumn);
        return present ? schema.RenameColumnCore(tableName, fromColumn, toColumn) : 0;
    }

    private int DropColumnIfRemovable(TableMapping mapping, string columnName)
    {
        bool inModel = mapping.Columns.Any(c => c.Name == columnName)
            || mapping.ShadowColumns.Any(s => s.Name == columnName);
        if (inModel)
        {
            return 0;
        }

        bool present = Database.Pragmas.TableInfo(mapping.TableName).ToList().Any(c => c.Name == columnName);
        return present ? schema.DropColumnCore(mapping.TableName, columnName) : 0;
    }

    private int MigrateInPlace(TableMapping mapping, IReadOnlyList<(string Column, string ValueSql)> sets)
    {
#if SQLITE_FRAMEWORK_VERSION_AWARE
        Database.Options.EnsureMinimumVersion(SQLiteMinimumVersion.V3_35, "Migrate. Use MigrateByRebuild on older SQLite");
#endif

        if (mapping.IsFullTextSearch || mapping.IsRTree)
        {
            return schema.CreateTable(mapping.Type);
        }

        if (!schema.TableExists(mapping.TableName))
        {
            return schema.CreateTable(mapping.Type);
        }

        int count = 0;
        if (sets.Count == 0)
        {
            count += AlterColumnsInPlace(mapping);
        }

        string intended = SchemaSqlBuilder.BuildCreateTable(Database, mapping, mapping.TableName, ifNotExists: false);
        string? live = Database.ExecuteScalar<string?>($"SELECT sql FROM sqlite_master WHERE type = 'table' AND name = '{mapping.TableName.Replace("'", "''")}'");
        if (sets.Count > 0 || !string.Equals(StripWhitespace(intended), StripWhitespace(live!), StringComparison.Ordinal))
        {
            count += RebuildTable(mapping, sets);
        }

        count += ReconcileIndexes(mapping);
        count += ReconcileTriggers(mapping);
        return count;
    }

    private int AlterColumnsInPlace(TableMapping mapping)
    {
        int count = 0;
        List<PragmaTableInfo> liveInfo = Database.Pragmas.TableInfo(mapping.TableName).ToList();
        HashSet<string> liveColumns = liveInfo.Select(c => c.Name).ToHashSet();
        HashSet<string> computedColumns = mapping.ComputedColumns.Select(c => c.Column.Name).ToHashSet();
        HashSet<string> modelColumns = mapping.Columns.Select(c => c.Name)
            .Concat(mapping.ShadowColumns.Select(s => s.Name))
            .ToHashSet();

        int columnCount = liveColumns.Count;
        foreach (TableColumn column in mapping.Columns)
        {
            if (liveColumns.Contains(column.Name) || computedColumns.Contains(column.Name))
            {
                continue;
            }

            if (column.IsPrimaryKey || column.IsAutoIncrement || (!column.IsNullable && column.DefaultSql == null))
            {
                continue;
            }

            if (column.DefaultSql != null && column.DefaultSql.TrimStart().StartsWith('('))
            {
                continue;
            }

            string columnSql = CommonHelpers.GetCreateColumnSql(column, defaultOverride: null, emitForeignKey: column.DefaultSql == null);
            count += Database.CreateCommand($"ALTER TABLE \"{mapping.TableName}\" ADD COLUMN {columnSql}", []).ExecuteNonQuery();
            columnCount++;
        }

        string? createSql = Database.ExecuteScalar<string?>($"SELECT sql FROM sqlite_master WHERE type = 'table' AND name = '{mapping.TableName.Replace("'", "''")}'");
        foreach (string liveColumn in liveColumns)
        {
            if (modelColumns.Contains(liveColumn) || !IsAlterDroppable(mapping, liveInfo, createSql, liveColumn, columnCount))
            {
                continue;
            }

            count += Database.CreateCommand($"ALTER TABLE \"{mapping.TableName}\" DROP COLUMN \"{liveColumn.Replace("\"", "\"\"")}\"", []).ExecuteNonQuery();
            columnCount--;
        }

        return count;
    }

    private int MigrateCore(TableMapping mapping, IReadOnlyList<(string Column, string ValueSql)> sets)
    {
        if (mapping.IsFullTextSearch || mapping.IsRTree)
        {
            return schema.CreateTable(mapping.Type);
        }

        if (!schema.TableExists(mapping.TableName))
        {
            return schema.CreateTable(mapping.Type);
        }

        int count = 0;
        string intended = SchemaSqlBuilder.BuildCreateTable(Database, mapping, mapping.TableName, ifNotExists: false);
        string? live = Database.ExecuteScalar<string?>($"SELECT sql FROM sqlite_master WHERE type = 'table' AND name = '{mapping.TableName.Replace("'", "''")}'");
        if (sets.Count > 0 || !string.Equals(StripWhitespace(intended), StripWhitespace(live!), StringComparison.Ordinal))
        {
            count += RebuildTable(mapping, sets);
        }

        count += ReconcileIndexes(mapping);
        count += ReconcileTriggers(mapping);
        return count;
    }

    [UnconditionalSuppressMessage("AOT", "IL2026", Justification = "Querying built-in string rows keeps their public members reachable.")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Querying built-in string rows keeps their public members reachable.")]
    private bool IsAlterDroppable(TableMapping mapping, List<PragmaTableInfo> liveInfo, string? createSql, string columnName, int columnCount)
    {
        if (columnCount <= 1 || createSql == null)
        {
            return false;
        }

        PragmaTableInfo info = liveInfo.First(c => c.Name == columnName);
        if (info.PrimaryKeyOrder > 0)
        {
            return false;
        }

        string quoted = "\"" + columnName.Replace("\"", "\"\"") + "\"";
        int occurrences = 0;
        for (int i = createSql.IndexOf(quoted, StringComparison.Ordinal); i >= 0; i = createSql.IndexOf(quoted, i + quoted.Length, StringComparison.Ordinal))
        {
            occurrences++;
        }

        if (occurrences != 1)
        {
            return false;
        }

        if (ContainsUnquotedIdentifier(createSql, columnName))
        {
            return false;
        }

        bool inForeignKey = Database.Query<Dictionary<string, object?>>($"PRAGMA foreign_key_list(\"{mapping.TableName.Replace("\"", "\"\"")}\")")
            .Any(row => row["from"] as string == columnName);
        if (inForeignKey)
        {
            return false;
        }

        return !Database.Query<string>($"SELECT sql FROM sqlite_master WHERE type = 'index' AND tbl_name = '{mapping.TableName.Replace("'", "''")}' AND sql IS NOT NULL")
            .Any(sql => sql.Contains(quoted, StringComparison.Ordinal));
    }

    private int RebuildTable(TableMapping mapping, IReadOnlyList<(string Column, string ValueSql)> sets)
    {
        string table = mapping.TableName;
        string temp = table + "__sqlitefw_migrate";

        HashSet<string> liveColumns = Database.Pragmas.TableInfo(table).Select(c => c.Name).ToHashSet();
        HashSet<string> setColumns = sets.Select(s => s.Column).ToHashSet();
        HashSet<string> computedColumns = mapping.ComputedColumns.Select(c => c.Column.Name).ToHashSet();

        EnsureNoUnfilledNotNull(mapping, table, liveColumns, computedColumns, setColumns);

        List<string> copyColumns = mapping.Columns
            .Where(c => !computedColumns.Contains(c.Name))
            .Select(c => c.Name)
            .Concat(mapping.ShadowColumns.Select(s => s.Name))
            .Where(name => liveColumns.Contains(name) && !setColumns.Contains(name))
            .ToList();

        Dictionary<string, string> backfillDefaults = new();
        foreach (TableColumn column in mapping.Columns)
        {
            if (!computedColumns.Contains(column.Name) && !column.IsNullable && column.DefaultSql != null)
            {
                backfillDefaults[column.Name] = column.DefaultSql;
            }
        }
        foreach (ShadowColumnSpec shadow in mapping.ShadowColumns)
        {
            if (!shadow.IsNullable && shadow.DefaultSql != null)
            {
                backfillDefaults[shadow.Name] = shadow.DefaultSql;
            }
        }

        List<string> insertColumns = copyColumns.Concat(sets.Select(s => s.Column)).Select(IdentifierGuard.Quote).ToList();
        List<string> selectExpressions = copyColumns
            .Select(name => backfillDefaults.TryGetValue(name, out string? defaultSql)
                ? $"COALESCE({IdentifierGuard.Quote(name)}, {defaultSql})"
                : IdentifierGuard.Quote(name))
            .Concat(sets.Select(s => s.ValueSql))
            .ToList();

        IReadOnlyList<string> liveTriggers = Database.Query<string>(
            $"SELECT sql FROM sqlite_master WHERE type = 'trigger' AND tbl_name = '{table.Replace("'", "''")}' AND sql IS NOT NULL");

        long? autoIncrementSeq = ReadAutoIncrementSequence(mapping, table);

        long foreignKeys = Database.ExecuteScalar<long>("PRAGMA foreign_keys");
        bool foreignKeysEnforced = foreignKeys == 1;
        Database.Execute("PRAGMA foreign_keys = OFF");
        try
        {
            using SQLiteTransaction transaction = Database.BeginTransaction();

            List<SavedTable> dependents;
            if (foreignKeysEnforced)
            {
                Database.Execute("PRAGMA defer_foreign_keys = ON");
                dependents = EmptyReferencingTables(table, [], [table]);
            }
            else
            {
                dependents = [];
            }

            int count = Database.CreateCommand(SchemaSqlBuilder.BuildCreateTable(Database, mapping, temp, ifNotExists: false), []).ExecuteNonQuery();
            if (insertColumns.Count > 0)
            {
                Database.Execute($"INSERT INTO \"{temp}\" ({string.Join(", ", insertColumns)}) SELECT {string.Join(", ", selectExpressions)} FROM \"{table}\"");
            }
            Database.Execute($"DROP TABLE \"{table}\"");
            Database.Execute($"ALTER TABLE \"{temp}\" RENAME TO \"{table}\"");
            if (autoIncrementSeq.HasValue)
            {
                RestoreAutoIncrementSequence(table, autoIncrementSeq.Value);
            }
            foreach (string trigger in liveTriggers)
            {
                Database.Execute(trigger);
            }

            RestoreReferencingTables(dependents);
            transaction.Commit();
            return count;
        }
        finally
        {
            Database.Execute($"PRAGMA foreign_keys = {foreignKeys}");
        }
    }

    [UnconditionalSuppressMessage("AOT", "IL2026", Justification = "Querying built-in string and foreign key rows keeps their public members reachable.")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Querying built-in string and foreign key rows keeps their public members reachable.")]
    private List<SavedTable> EmptyReferencingTables(string table, List<SavedTable> saved, HashSet<string> visited)
    {
        string escaped = table.Replace("'", "''");
        List<string> allTables = Database.Query<string>(
            $"SELECT name FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite\\_%' ESCAPE '\\' AND name <> '{escaped}'");

        List<string> referencing = new();
        foreach (string candidate in allTables)
        {
            bool referencesTable = Database.Query<Dictionary<string, object?>>($"PRAGMA foreign_key_list(\"{candidate.Replace("\"", "\"\"")}\")")
                .Any(row => row["table"] as string == table);
            if (referencesTable)
            {
                referencing.Add(candidate);
            }
        }

        foreach (string child in referencing)
        {
            if (!visited.Add(child))
            {
                continue;
            }

            EmptyReferencingTables(child, saved, visited);

            string childEscaped = child.Replace("'", "''");
            List<Dictionary<string, object?>> triggers = Database.Query<Dictionary<string, object?>>(
                $"SELECT name, sql FROM sqlite_master WHERE type = 'trigger' AND tbl_name = '{childEscaped}' AND sql IS NOT NULL");

            List<string> triggerSql = new();
            foreach (Dictionary<string, object?> trigger in triggers)
            {
                triggerSql.Add((string)trigger["sql"]!);
                Database.Execute($"DROP TRIGGER \"{((string)trigger["name"]!).Replace("\"", "\"\"")}\"");
            }

            List<string> insertableColumns = Database
                .Query<Dictionary<string, object?>>($"PRAGMA table_xinfo(\"{childEscaped}\")")
                .Where(col => Convert.ToInt64(col["hidden"], CultureInfo.InvariantCulture) is not (2 or 3))
                .Select(col => (string)col["name"]!)
                .ToList();

            Database.Execute($"CREATE TABLE \"{child}__sqlitefw_hold\" AS SELECT * FROM \"{child}\"");
            Database.Execute($"DELETE FROM \"{child}\"");
            saved.Add(new SavedTable { Name = child, Triggers = triggerSql, InsertableColumns = insertableColumns });
        }

        return saved;
    }

    private void RestoreReferencingTables(List<SavedTable> saved)
    {
        for (int i = saved.Count - 1; i >= 0; i--)
        {
            SavedTable child = saved[i];
            string columnList = string.Join(", ", child.InsertableColumns.Select(c => $"\"{c.Replace("\"", "\"\"")}\""));
            Database.Execute($"INSERT INTO \"{child.Name}\" ({columnList}) SELECT {columnList} FROM \"{child.Name}__sqlitefw_hold\"");
            Database.Execute($"DROP TABLE \"{child.Name}__sqlitefw_hold\"");
            foreach (string trigger in child.Triggers)
            {
                Database.Execute(trigger);
            }
        }
    }

    private long? ReadAutoIncrementSequence(TableMapping mapping, string table)
    {
        if (!mapping.Columns.Any(c => c.IsPrimaryKey && c.IsAutoIncrement))
        {
            return null;
        }

        bool sequenceExists = Database.ExecuteScalar<long>(
            "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = 'sqlite_sequence'") > 0;
        if (!sequenceExists)
        {
            return null;
        }

        List<long> seq = Database.Query<long>($"SELECT seq FROM sqlite_sequence WHERE name = '{table.Replace("'", "''")}'");
        return seq.Count > 0 ? seq[0] : null;
    }

    private void RestoreAutoIncrementSequence(string table, long seq)
    {
        string escaped = table.Replace("'", "''");
        Database.Execute($"DELETE FROM sqlite_sequence WHERE name = '{escaped}'");
        Database.Execute($"INSERT INTO sqlite_sequence (name, seq) VALUES ('{escaped}', {seq})");
    }

    private void EnsureNoUnfilledNotNull(TableMapping mapping, string table, HashSet<string> liveColumns, HashSet<string> computedColumns, HashSet<string> setColumns)
    {
        List<(string Name, bool Nullable, bool HasDefault)> required = mapping.Columns
            .Where(c => !computedColumns.Contains(c.Name))
            .Select(c => (c.Name, c.IsNullable, c.DefaultSql != null))
            .Concat(mapping.ShadowColumns.Select(s => (s.Name, s.IsNullable, s.DefaultSql != null)))
            .Where(c => !c.Item2 && !c.Item3 && !liveColumns.Contains(c.Item1) && !setColumns.Contains(c.Item1))
            .ToList();

        if (required.Count == 0)
        {
            return;
        }

        if (Database.ExecuteScalar<long>($"SELECT COUNT(*) FROM \"{table}\"") == 0)
        {
            return;
        }

        (string name, _, _) = required[0];
        throw new InvalidOperationException(
            $"Cannot migrate table '{table}'. Column '{name}' is new and NOT NULL with no default, but the table has rows. " +
            "Give it a default in OnModelCreating, set a value with TableChanged(s => s.Set(...)) or make it nullable.");
    }

    private int ReconcileIndexes(TableMapping mapping)
    {
        int count = 0;
        List<(string Name, string Sql)> declared = SchemaSqlBuilder.BuildIndexes(mapping, mapping.TableName, ifNotExists: false);
        HashSet<string> declaredNames = declared.Select(d => d.Name).ToHashSet();

        foreach (PragmaIndexList live in Database.Pragmas.IndexList(mapping.TableName).ToList())
        {
            if (live.Origin == "c" && !declaredNames.Contains(live.Name))
            {
                count += Database.Execute($"DROP INDEX IF EXISTS {IdentifierGuard.Quote(live.Name)}");
            }
        }

        foreach ((string name, string sql) in declared)
        {
            string? liveSql = Database.ExecuteScalar<string?>($"SELECT sql FROM sqlite_master WHERE type = 'index' AND name = '{name.Replace("'", "''")}'");
            if (!string.Equals(sql, liveSql, StringComparison.Ordinal))
            {
                Database.Execute($"DROP INDEX IF EXISTS {IdentifierGuard.Quote(name)}");
                count += Database.CreateCommand(sql, []).ExecuteNonQuery();
            }
        }

        return count;
    }

    private int ReconcileTriggers(TableMapping mapping)
    {
        int count = 0;
        foreach ((string name, string sql) in SchemaSqlBuilder.BuildTriggers(mapping, mapping.TableName, ifNotExists: false))
        {
            string? liveSql = Database.ExecuteScalar<string?>($"SELECT sql FROM sqlite_master WHERE type = 'trigger' AND name = '{name.Replace("'", "''")}'");
            if (!string.Equals(sql, liveSql, StringComparison.Ordinal))
            {
                Database.Execute($"DROP TRIGGER IF EXISTS {IdentifierGuard.Quote(name)}");
                count += Database.CreateCommand(sql, []).ExecuteNonQuery();
            }
        }

        return count;
    }

    private static List<(string Column, string ValueSql)> UnionSets(IEnumerable<MigrationOperation> group)
    {
        Dictionary<string, int> index = new(StringComparer.Ordinal);
        List<(string Column, string ValueSql)> result = [];
        foreach (MigrationOperation operation in group)
        {
            foreach ((string column, string valueSql) in operation.Sets)
            {
                if (index.TryGetValue(column, out int existing))
                {
                    result[existing] = (column, valueSql);
                }
                else
                {
                    index[column] = result.Count;
                    result.Add((column, valueSql));
                }
            }
        }

        return result;
    }

    private static bool ContainsUnquotedIdentifier(string sql, string identifier)
    {
        string scan = sql + " ";
        bool inLiteral = false;
        bool inQuote = false;
        int i = 0;
        while (i < scan.Length)
        {
            char c = scan[i];
            if (inLiteral)
            {
                inLiteral = c != '\'';
                i++;
                continue;
            }

            if (inQuote)
            {
                inQuote = c != '"';
                i++;
                continue;
            }

            if (c == '\'')
            {
                inLiteral = true;
                i++;
                continue;
            }

            if (c == '"')
            {
                inQuote = true;
                i++;
                continue;
            }

            if (IsIdentifierChar(c))
            {
                int start = i;
                while (IsIdentifierChar(scan[i]))
                {
                    i++;
                }

                if (string.Equals(scan[start..i], identifier, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                continue;
            }

            i++;
        }

        return false;
    }

    private static bool IsIdentifierChar(char c)
    {
        return char.IsLetterOrDigit(c) || c == '_' || c == '$';
    }

    private static string StripWhitespace(string value)
    {
        StringBuilder builder = new(value.Length);
        bool inLiteral = false;
        bool inQuote = false;
        foreach (char c in value)
        {
            if (c == '\'' && !inQuote)
            {
                inLiteral = !inLiteral;
                builder.Append(c);
                continue;
            }

            if (c == '"' && !inLiteral)
            {
                inQuote = !inQuote;
                builder.Append(c);
                continue;
            }

            if (inLiteral || inQuote || !char.IsWhiteSpace(c))
            {
                builder.Append(c);
            }
        }

        return builder.ToString();
    }
}
