using System.Text.RegularExpressions;

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
    private static readonly Regex fullTextSearchContentRegex = new(
        """content\s*=\s*(?:"(?<name>[^"]*)"|'(?<name>[^']*)'|\[(?<name>[^\]]*)\]|`(?<name>[^`]*)`|(?<name>\w+))""",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly SQLiteSchema schema;
    private readonly SortedDictionary<int, Action<SQLiteMigrationStep>> versions = new();
    private Action<SQLiteMigrationProgress>? progress;
    private SQLiteMigrationActivator? migrationActivator;

    internal SQLiteMigrationRunner(SQLiteSchema schema)
    {
        this.schema = schema;
    }

    internal SQLiteDatabase Database => schema.Database;

    /// <summary>
    /// Declares the work for one schema version. Versions are applied in ascending order. Each
    /// version number must be one or more and may be declared only once. Never change a version
    /// that has shipped. Databases that already passed it will not run it again, so declare a
    /// new version instead. A fresh database runs the whole chain, so a table created in a late
    /// version exists on new installs just the same.
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
    /// <see cref="Version" /> apply. When the runner is created through the dependency injection
    /// package, the migration's constructor arguments are resolved from the service provider, so a
    /// migration class can take services. Otherwise the class is built through its public
    /// constructor with no arguments.
    /// </summary>
    /// <typeparam name="T">The migration type to register.</typeparam>
    public SQLiteMigrationRunner Add<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>() where T : ISQLiteMigration
    {
        return Version(T.Version, step =>
        {
            T migration = migrationActivator != null
                ? (T)migrationActivator(typeof(T))
                : Activator.CreateInstance<T>()!;
            migration.Apply(step);
        });
    }

    /// <summary>
    /// Registers a callback that is called once for each operation of a run, right before the
    /// operation is applied. Use it to show progress while a long migration runs or to log what
    /// the runner does. The callback fires during <see cref="Migrate" />, <c>MigrateAsync</c> and
    /// <see cref="Script" />. It runs inside the migration transaction, so a throw rolls the
    /// whole run back.
    /// </summary>
    /// <param name="callback">The callback that receives each progress update.</param>
    public SQLiteMigrationRunner Progress(Action<SQLiteMigrationProgress> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);
        progress = callback;
        return this;
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
            .SelectMany(v => BuildStep(v.Key, v.Value).Operations.Select(o => o.Description))
            .ToList();
        return new SQLiteMigrationPlan(currentVersion, targetVersion, operations);
    }

    /// <summary>
    /// Applies every declared version above the one the database records, in one transaction, then
    /// records the highest declared version. Returns the number of statements run, not counting the
    /// work done inside callbacks. Does nothing and returns zero when the database is already up to
    /// date. Throws when a pending version declares an async callback, which only
    /// <c>MigrateAsync</c> can await.
    /// </summary>
#if SQLITE_FRAMEWORK_OS_BUNDLED_SQLITE
    [UnsupportedOSPlatform("ios")]
    [SupportedOSPlatform("ios15.0")]
#endif
    public int Migrate()
    {
        using SQLiteTransaction transaction = schema.Database.BeginTransaction();
        ThrowIfMigrationInProgress();
        if (!TryGetPendingOperations(out int currentVersion, out int targetVersion, out List<MigrationOperation> operations))
        {
            return 0;
        }

        if (operations.Any(o => o.AsyncCallback != null))
        {
            throw new InvalidOperationException("A pending version declares an async callback. Call MigrateAsync instead of Migrate.");
        }

        SQLiteMigrationContext context = new(schema.Database, currentVersion, targetVersion, CancellationToken.None);
        int count = 0;
        int reported = 0;
        int total = operations.Count;

        schema.Database.MigrationInProgress = true;
        try
        {
            foreach (MigrationOperation operation in operations.Where(o => o.Kind == MigrationOperationKind.RunBefore))
            {
                ReportProgress(operation, ref reported, total);
                operation.Callback!(context);
            }

            count += ApplySchemaPhases(operations, total, ref reported, out HashSet<string> newlyCreated, out List<DeferredFill> deferredFills, out List<DeferredSchemaWork> deferredSchema);

            int nextSchema = 0;
            int nextFill = 0;
            foreach (MigrationOperation operation in operations.Where(o => IsDataPhase(o.Kind)))
            {
                count += ApplyDeferredSchemaThrough(deferredSchema, ref nextSchema, operation.Version, deferredFills, ref nextFill);
                count += ApplyDeferredFillsThrough(deferredFills, ref nextFill, operation.Version);
                ReportProgress(operation, ref reported, total);
                if (operation.Kind == MigrationOperationKind.Run)
                {
                    operation.Callback!(context);
                }
                else
                {
                    count += ApplyDataOperation(operation, newlyCreated);
                }
            }

            count += ApplyDeferredSchemaThrough(deferredSchema, ref nextSchema, int.MaxValue, deferredFills, ref nextFill);
            count += ApplyDeferredFillsThrough(deferredFills, ref nextFill, int.MaxValue);

            schema.Database.Pragmas.UserVersion = targetVersion;
        }
        finally
        {
            schema.Database.MigrationInProgress = false;
        }

        transaction.Commit();
        return count;
    }

    /// <summary>
    /// Runs every pending version inside a transaction, collects each SQL statement the run
    /// executes, then rolls the transaction back. The version and the schema are left as they
    /// were. Use it to review the exact statements a <see cref="Migrate" /> would run against
    /// this database. Rows are copied and rewritten just like a real run, so on a large database
    /// this takes as long as the migration itself, and rows passed to
    /// <see cref="SQLiteMigrationStep.Insert{T}" /> can get their auto-increment keys set.
    /// Callbacks declared with <c>Run</c> and <c>RunBefore</c> are not invoked. Each appears as a
    /// SQL comment in its place. Statement parameters are inlined, so every entry runs on its
    /// own. Returns an empty list when the database is up to date.
    /// </summary>
#if SQLITE_FRAMEWORK_OS_BUNDLED_SQLITE
    [UnsupportedOSPlatform("ios")]
    [SupportedOSPlatform("ios15.0")]
#endif
    public IReadOnlyList<string> Script()
    {
        List<string> statements = [];
        using SQLiteTransaction transaction = schema.Database.BeginTransaction();
        ThrowIfMigrationInProgress();
        if (!TryGetPendingOperations(out _, out int targetVersion, out List<MigrationOperation> operations))
        {
            return statements;
        }

        IReadOnlyList<ISQLiteCommandInterceptor> original = schema.Database.EffectiveCommandInterceptors;
        schema.Database.EffectiveCommandInterceptors = [.. original, new MigrationScriptCapture(schema.Database.Options, statements)];
        schema.Database.MigrationInProgress = true;
        try
        {
            int reported = 0;
            int total = operations.Count;

            foreach (MigrationOperation operation in operations.Where(o => o.Kind == MigrationOperationKind.RunBefore))
            {
                ReportProgress(operation, ref reported, total);
                statements.Add("-- " + operation.Description);
            }

            ApplySchemaPhases(operations, total, ref reported, out HashSet<string> newlyCreated, out List<DeferredFill> deferredFills, out List<DeferredSchemaWork> deferredSchema);

            int nextSchema = 0;
            int nextFill = 0;
            foreach (MigrationOperation operation in operations.Where(o => IsDataPhase(o.Kind)))
            {
                ApplyDeferredSchemaThrough(deferredSchema, ref nextSchema, operation.Version, deferredFills, ref nextFill);
                ApplyDeferredFillsThrough(deferredFills, ref nextFill, operation.Version);
                ReportProgress(operation, ref reported, total);
                if (operation.Kind == MigrationOperationKind.Run)
                {
                    statements.Add("-- " + operation.Description);
                }
                else
                {
                    ApplyDataOperation(operation, newlyCreated);
                }
            }

            ApplyDeferredSchemaThrough(deferredSchema, ref nextSchema, int.MaxValue, deferredFills, ref nextFill);
            ApplyDeferredFillsThrough(deferredFills, ref nextFill, int.MaxValue);

            schema.Database.Pragmas.UserVersion = targetVersion;
        }
        finally
        {
            schema.Database.MigrationInProgress = false;
            schema.Database.EffectiveCommandInterceptors = original;
        }

        return statements;
    }

    internal void UseMigrationActivator(SQLiteMigrationActivator activator)
    {
        migrationActivator = activator;
    }

    internal async Task<int> MigratePending(CancellationToken cancellationToken)
    {
        using SQLiteTransaction transaction = schema.Database.BeginTransaction();
        ThrowIfMigrationInProgress();
        if (!TryGetPendingOperations(out int currentVersion, out int targetVersion, out List<MigrationOperation> operations))
        {
            return 0;
        }

        SQLiteMigrationContext context = new(schema.Database, currentVersion, targetVersion, cancellationToken);
        int count = 0;
        int reported = 0;
        int total = operations.Count;

        schema.Database.MigrationInProgress = true;
        try
        {
            foreach (MigrationOperation operation in operations.Where(o => o.Kind == MigrationOperationKind.RunBefore))
            {
                ReportProgress(operation, ref reported, total);
                if (operation.AsyncCallback != null)
                {
                    await operation.AsyncCallback(context);
                }
                else
                {
                    operation.Callback!(context);
                }
            }

            count += ApplySchemaPhases(operations, total, ref reported, out HashSet<string> newlyCreated, out List<DeferredFill> deferredFills, out List<DeferredSchemaWork> deferredSchema);

            int nextSchema = 0;
            int nextFill = 0;
            foreach (MigrationOperation operation in operations.Where(o => IsDataPhase(o.Kind)))
            {
                count += ApplyDeferredSchemaThrough(deferredSchema, ref nextSchema, operation.Version, deferredFills, ref nextFill);
                count += ApplyDeferredFillsThrough(deferredFills, ref nextFill, operation.Version);
                ReportProgress(operation, ref reported, total);
                if (operation.Kind == MigrationOperationKind.Run)
                {
                    if (operation.AsyncCallback != null)
                    {
                        await operation.AsyncCallback(context);
                    }
                    else
                    {
                        operation.Callback!(context);
                    }
                }
                else
                {
                    count += ApplyDataOperation(operation, newlyCreated);
                }
            }

            count += ApplyDeferredSchemaThrough(deferredSchema, ref nextSchema, int.MaxValue, deferredFills, ref nextFill);
            count += ApplyDeferredFillsThrough(deferredFills, ref nextFill, int.MaxValue);

            schema.Database.Pragmas.UserVersion = targetVersion;
        }
        finally
        {
            schema.Database.MigrationInProgress = false;
        }

        transaction.Commit();
        return count;
    }

    private bool TryGetPendingOperations(out int currentVersion, out int targetVersion, out List<MigrationOperation> operations)
    {
        int recorded = schema.Database.Pragmas.UserVersion;
        currentVersion = recorded;
        if (versions.Count > 0 && recorded > versions.Keys.Last())
        {
            throw new InvalidOperationException(
                $"The database records version {recorded} but the highest declared version is {versions.Keys.Last()}. " +
                "A newer app version created this database. Add the missing versions or open it with the newer app.");
        }

        List<KeyValuePair<int, Action<SQLiteMigrationStep>>> pending = versions.Where(v => v.Key > recorded).ToList();
        if (pending.Count == 0)
        {
            targetVersion = recorded;
            operations = [];
            return false;
        }

        targetVersion = pending[^1].Key;
        operations = pending.SelectMany(v => BuildStep(v.Key, v.Value).Operations).ToList();
        RemoveDropsSupersededByCreate(operations);
        return true;
    }

    private int ApplySchemaPhases(List<MigrationOperation> operations, int total, ref int reported, out HashSet<string> newlyCreated, out List<DeferredFill> deferredFills, out List<DeferredSchemaWork> deferredSchema)
    {
        int count = 0;
        int firstOpaqueVersion = FirstOpaqueVersion(operations);
        List<DeferredSchemaWork> deferred = [];
        foreach (MigrationOperation operation in operations.Where(o => o.Kind == MigrationOperationKind.RenameTable))
        {
            ReportProgress(operation, ref reported, total);
            string fromTable = operation.FromTable!;
            string toTable = operation.Mapping!.TableName;
            if (operation.Version > firstOpaqueVersion && !schema.TableExists(fromTable))
            {
                deferred.Add(new DeferredSchemaWork { Version = operation.Version, Order = 0, Apply = () => (RenameTableIfPresent(fromTable, toTable), []) });
            }
            else
            {
                count += RenameTableIfPresent(fromTable, toTable);
            }
        }

        foreach (MigrationOperation operation in operations.Where(o => o.Kind == MigrationOperationKind.RenameColumn))
        {
            ReportProgress(operation, ref reported, total);
            string tableName = operation.Mapping!.TableName;
            string fromColumn = operation.FromColumn!;
            string toColumn = operation.ToColumn!;
            if (operation.Version > firstOpaqueVersion && !schema.TableExists(tableName))
            {
                deferred.Add(new DeferredSchemaWork { Version = operation.Version, Order = 1, Apply = () => (RenameColumnIfPresent(tableName, fromColumn, toColumn), []) });
            }
            else
            {
                count += RenameColumnIfPresent(tableName, fromColumn, toColumn);
            }
        }

        HashSet<string> created = new(StringComparer.OrdinalIgnoreCase);
        newlyCreated = created;
        foreach (MigrationOperation operation in operations.Where(o => o.Kind == MigrationOperationKind.CreateTable))
        {
            ReportProgress(operation, ref reported, total);
            if (operation.DropTableFirst && schema.TableExists(operation.Mapping!.TableName))
            {
                count += schema.DropTable(operation.Mapping.Type);
            }

            if (!schema.TableExists(operation.Mapping!.TableName))
            {
                created.Add(operation.Mapping.TableName);
            }

            count += schema.CreateTable(operation.Mapping!.Type);
        }

        deferredFills = [];
        foreach (IGrouping<string, MigrationOperation> group in operations
                     .Where(o => o.Kind == MigrationOperationKind.Reconcile)
                     .GroupBy(o => o.Mapping!.TableName, StringComparer.OrdinalIgnoreCase))
        {
            foreach (MigrationOperation operation in group)
            {
                ReportProgress(operation, ref reported, total);
            }

            TableMapping mapping = group.First().Mapping!;
            List<MigrationOperation> groupOps = group.ToList();
            int groupVersion = groupOps.Min(o => o.Version);
            if (groupVersion > firstOpaqueVersion && !created.Contains(group.Key) && !schema.TableExists(mapping.TableName))
            {
                deferred.Add(new DeferredSchemaWork { Version = groupVersion, Order = 2, Apply = () => ProcessReconcileGroup(mapping, groupOps, created) });
            }
            else
            {
                (int groupCount, List<DeferredFill> groupFills) = ProcessReconcileGroup(mapping, groupOps, created);
                count += groupCount;
                deferredFills.AddRange(groupFills);
            }
        }

        deferredFills.Sort((a, b) => a.Version.CompareTo(b.Version));
        deferred.Sort((a, b) => a.Version != b.Version ? a.Version.CompareTo(b.Version) : a.Order.CompareTo(b.Order));
        deferredSchema = deferred;
        return count;
    }

    private (int Count, List<DeferredFill> Fills) ProcessReconcileGroup(TableMapping mapping, List<MigrationOperation> group, HashSet<string> newlyCreated)
    {
        int count = 0;
        bool rebuild = group.Any(o => o.Rebuild);
        bool isNew = newlyCreated.Contains(mapping.TableName) || !schema.TableExists(mapping.TableName);
        Dictionary<string, (int Version, MigrationSetValue Set)> schemaWinners = new(StringComparer.OrdinalIgnoreCase);
        if (isNew)
        {
            if (!newlyCreated.Contains(mapping.TableName))
            {
                newlyCreated.Add(mapping.TableName);
                count += rebuild ? MigrateCore(mapping, []) : MigrateInPlace(mapping, []);
            }
        }
        else
        {
            List<(int Version, MigrationSetValue Set, MigrationSetValue ApplySet)> schemaSets = mapping.IsFullTextSearch || mapping.IsRTree
                ? []
                : SchemaPhaseSets(mapping, group);
            List<MigrationSetValue> schemaSetValues = schemaSets.Select(s => s.ApplySet).ToList();
            count += rebuild
                ? MigrateCore(mapping, schemaSetValues)
                : MigrateInPlace(mapping, schemaSetValues);
            foreach ((int version, MigrationSetValue set, _) in schemaSets)
            {
                schemaWinners[set.Column] = (version, set);
            }
        }

        List<DeferredFill> fills = [];
        foreach (IGrouping<int, MigrationOperation> versionGroup in group.GroupBy(o => o.Version))
        {
            int version = versionGroup.Key;
            List<MigrationSetValue> versionSets = UnionSets(versionGroup.SelectMany(o => o.Sets))
                .Where(s => !ReadsOutsideModel(mapping, s)
                    && !IsSettledInSchemaPhase(mapping, s, version, schemaWinners))
                .ToList();
            if (versionSets.Count > 0)
            {
                fills.Add(new DeferredFill { Version = version, Mapping = mapping, Sets = versionSets });
            }
        }

        return (count, fills);
    }

    private List<(int Version, MigrationSetValue Set, MigrationSetValue ApplySet)> SchemaPhaseSets(TableMapping mapping, IEnumerable<MigrationOperation> group)
    {
        List<PragmaTableInfo> liveInfo = Database.Pragmas.TableInfo(mapping.TableName).ToList();
        HashSet<string> liveColumns = liveInfo.Select(c => c.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        HashSet<string> liveNullable = liveInfo.Where(c => !c.IsNotNull).Select(c => c.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        HashSet<string> notNullModel = mapping.Columns.Where(c => !c.IsNullable).Select(c => c.Name)
            .Concat(mapping.ShadowColumns.Where(s => !s.IsNullable).Select(s => s.Name))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        List<(int Version, MigrationSetValue Set)> perVersionWinners = [];
        foreach (IGrouping<int, MigrationOperation> versionGroup in group.GroupBy(o => o.Version))
        {
            IEnumerable<MigrationSetValue> qualifying = versionGroup
                .SelectMany(o => o.Sets)
                .Where(s => s.RunInRebuild
                    || ReadsOutsideModel(mapping, s)
                    || !liveColumns.Contains(s.Column)
                    || (notNullModel.Contains(s.Column) && liveNullable.Contains(s.Column)))
                .Where(s => s.ReadColumns.All(liveColumns.Contains));
            foreach (MigrationSetValue set in UnionSets(qualifying))
            {
                perVersionWinners.Add((versionGroup.Key, set));
            }
        }

        List<(int Version, MigrationSetValue Set, MigrationSetValue ApplySet)> result = [];
        foreach (IGrouping<string, (int Version, MigrationSetValue Set)> columnGroup in perVersionWinners
                     .GroupBy(w => w.Set.Column, StringComparer.OrdinalIgnoreCase))
        {
            List<(int Version, MigrationSetValue Set)> outsideModel = columnGroup
                .Where(w => ReadsOutsideModel(mapping, w.Set))
                .ToList();
            (int version, MigrationSetValue winner) = outsideModel.Count > 0 ? outsideModel[^1] : columnGroup.First();
            result.Add((version, winner, BuildSchemaApplySet(mapping, winner, liveColumns)));
        }

        return result;
    }

    private int ApplyDataOperation(MigrationOperation operation, HashSet<string> newlyCreated)
    {
        if (operation.Kind == MigrationOperationKind.RawSql)
        {
            return schema.Database.Execute(operation.Sql!, operation.SqlParameters);
        }

        if (operation.Execute != null)
        {
            return operation.Execute(schema.Database);
        }

        if (operation.Kind == MigrationOperationKind.DropColumn)
        {
            if (newlyCreated.Contains(operation.Mapping!.TableName))
            {
                return 0;
            }

            return DropColumnIfRemovable(operation.Mapping!, operation.ColumnName!);
        }

        int count = operation.Mapping != null
            ? schema.DropTable(operation.Mapping.Type)
            : schema.DropTable(operation.TableName!);
        if (operation.RecreateMapping != null)
        {
            count += schema.CreateTable(operation.RecreateMapping.Type);
        }

        return count;
    }

    private int ApplyDeferredFillsThrough(List<DeferredFill> deferredFills, ref int nextFill, int version)
    {
        int count = 0;
        while (nextFill < deferredFills.Count && deferredFills[nextFill].Version <= version)
        {
            count += ApplyFill(deferredFills[nextFill].Mapping, deferredFills[nextFill].Sets);
            nextFill++;
        }

        return count;
    }

    private int ApplyFill(TableMapping mapping, IReadOnlyList<MigrationSetValue> sets)
    {
        string assignments = string.Join(", ", sets.Select(s => $"{IdentifierGuard.Quote(s.Column)} = {s.ValueSql}"));
        return Database.Execute($"UPDATE \"{mapping.TableName.Replace("\"", "\"\"")}\" SET {assignments}");
    }

    private SQLiteMigrationStep BuildStep(int version, Action<SQLiteMigrationStep> build)
    {
        SQLiteMigrationStep step = new(schema.Database, version);
        build(step);
        foreach (MigrationOperation operation in step.Operations)
        {
            operation.Version = version;
        }

        return step;
    }

    private void ThrowIfMigrationInProgress()
    {
        if (schema.Database.MigrationInProgress)
        {
            throw new InvalidOperationException(
                "Migrate cannot run inside a migration callback. Remove the nested Migrate or Script call.");
        }
    }

    private void ReportProgress(MigrationOperation operation, ref int reported, int total)
    {
        reported++;
        progress?.Invoke(new SQLiteMigrationProgress(operation.Version, operation.Description, reported, total));
    }

    private int RenameTableIfPresent(string fromTable, string toTable)
    {
        if (string.Equals(fromTable, toTable, StringComparison.Ordinal) || !schema.TableExists(fromTable))
        {
            return 0;
        }

        int count;
        if (string.Equals(fromTable, toTable, StringComparison.OrdinalIgnoreCase))
        {
            string temp = toTable + "__sqlitefw_case";
            count = schema.RenameTableCore(fromTable, temp) + schema.RenameTableCore(temp, toTable);
        }
        else
        {
            count = schema.RenameTableCore(fromTable, toTable);
        }

        return count + RebuildFullTextSearchReferencing(fromTable, toTable);
    }

    [UnconditionalSuppressMessage("AOT", "IL2026", Justification = "Querying built-in string rows keeps their public members reachable.")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Querying built-in string rows keeps their public members reachable.")]
    private int RebuildFullTextSearchReferencing(string fromTable, string toTable)
    {
        int count = 0;
        List<Dictionary<string, object?>> virtualTables = Database.Query<Dictionary<string, object?>>(
            "SELECT name, sql FROM sqlite_master WHERE type = 'table' AND sql LIKE 'CREATE VIRTUAL TABLE%'");
        foreach (Dictionary<string, object?> row in virtualTables)
        {
            string sql = (string)row["sql"]!;
            Match content = fullTextSearchContentRegex.Match(sql);
            if (!content.Success || !string.Equals(content.Groups["name"].Value, fromTable, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string name = (string)row["name"]!;
            string quotedName = "\"" + name.Replace("\"", "\"\"") + "\"";
            string newSql = string.Concat(
                sql.AsSpan(0, content.Index),
                $"content=\"{toTable.Replace("\"", "\"\"")}\"",
                sql.AsSpan(content.Index + content.Length));
            count += Database.Execute($"DROP TABLE {quotedName}");
            count += Database.Execute(newSql);
            count += Database.Execute($"INSERT INTO {quotedName}({quotedName}) VALUES('rebuild')");
        }

        return count;
    }

    private int RenameColumnIfPresent(string tableName, string fromColumn, string toColumn)
    {
        bool present = Database.Pragmas.TableInfo(tableName).ToList()
            .Any(c => string.Equals(c.Name, fromColumn, StringComparison.OrdinalIgnoreCase));
        if (!present || string.Equals(fromColumn, toColumn, StringComparison.Ordinal))
        {
            return 0;
        }

        if (string.Equals(fromColumn, toColumn, StringComparison.OrdinalIgnoreCase))
        {
            string temp = toColumn + "__sqlitefw_case";
            return schema.RenameColumnCore(tableName, fromColumn, temp) + schema.RenameColumnCore(tableName, temp, toColumn);
        }

        return schema.RenameColumnCore(tableName, fromColumn, toColumn);
    }

    private int DropColumnIfRemovable(TableMapping mapping, string columnName)
    {
        bool inModel = mapping.Columns.Any(c => string.Equals(c.Name, columnName, StringComparison.OrdinalIgnoreCase))
            || mapping.ShadowColumns.Any(s => string.Equals(s.Name, columnName, StringComparison.OrdinalIgnoreCase));
        if (inModel)
        {
            return 0;
        }

        List<PragmaTableInfo> liveInfo = Database.Pragmas.TableInfo(mapping.TableName).ToList();
        PragmaTableInfo? liveColumn = liveInfo.FirstOrDefault(c => string.Equals(c.Name, columnName, StringComparison.OrdinalIgnoreCase));
        if (liveColumn == null)
        {
            return 0;
        }

        columnName = liveColumn.Name;
        string? createSql = Database.ExecuteScalar<string?>($"SELECT sql FROM sqlite_master WHERE type = 'table' AND name = '{mapping.TableName.Replace("'", "''")}' COLLATE NOCASE");
        if (IsAlterDroppable(mapping, liveInfo, createSql, columnName, liveInfo.Count))
        {
            return schema.DropColumnCore(mapping.TableName, columnName);
        }

        int count = DropObjectsReferencingColumn(mapping.TableName, columnName);
        if (IsAlterDroppable(mapping, liveInfo, createSql, columnName, liveInfo.Count))
        {
            return count + schema.DropColumnCore(mapping.TableName, columnName);
        }

        count += RebuildTable(mapping, []);
        count += ReconcileIndexes(mapping);
        count += ReconcileTriggers(mapping);
        return count;
    }

    private int DropObjectsReferencingColumn(string tableName, string columnName)
    {
        int count = 0;
        string quoted = "\"" + columnName.Replace("\"", "\"\"") + "\"";
        string escapedTable = tableName.Replace("'", "''");
        List<Dictionary<string, object?>> indexes = Database.Query<Dictionary<string, object?>>(
            $"SELECT name, sql FROM sqlite_master WHERE type = 'index' AND tbl_name = '{escapedTable}' AND sql IS NOT NULL");
        foreach (Dictionary<string, object?> index in indexes)
        {
            string sql = (string)index["sql"]!;
            if (sql.Contains(quoted, StringComparison.OrdinalIgnoreCase) || ContainsUnquotedIdentifier(sql, columnName))
            {
                count += Database.Execute($"DROP INDEX \"{((string)index["name"]!).Replace("\"", "\"\"")}\"");
            }
        }

        List<Dictionary<string, object?>> triggers = Database.Query<Dictionary<string, object?>>(
            $"SELECT name, sql FROM sqlite_master WHERE type = 'trigger' AND tbl_name = '{escapedTable}' AND sql IS NOT NULL");
        foreach (Dictionary<string, object?> trigger in triggers)
        {
            string sql = (string)trigger["sql"]!;
            if (sql.Contains(quoted, StringComparison.OrdinalIgnoreCase) || ContainsUnquotedIdentifier(sql, columnName))
            {
                count += Database.Execute($"DROP TRIGGER \"{((string)trigger["name"]!).Replace("\"", "\"\"")}\"");
            }
        }

        return count;
    }

    private int MigrateInPlace(TableMapping mapping, IReadOnlyList<MigrationSetValue> sets)
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
        string? live = Database.ExecuteScalar<string?>($"SELECT sql FROM sqlite_master WHERE type = 'table' AND name = '{mapping.TableName.Replace("'", "''")}' COLLATE NOCASE");
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
        HashSet<string> liveColumns = liveInfo.Select(c => c.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        HashSet<string> computedColumns = mapping.ComputedColumns.Select(c => c.Column.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        HashSet<string> modelColumns = mapping.Columns.Select(c => c.Name)
            .Concat(mapping.ShadowColumns.Select(s => s.Name))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

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

        string? createSql = Database.ExecuteScalar<string?>($"SELECT sql FROM sqlite_master WHERE type = 'table' AND name = '{mapping.TableName.Replace("'", "''")}' COLLATE NOCASE");
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

    private int MigrateCore(TableMapping mapping, IReadOnlyList<MigrationSetValue> sets)
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
        string? live = Database.ExecuteScalar<string?>($"SELECT sql FROM sqlite_master WHERE type = 'table' AND name = '{mapping.TableName.Replace("'", "''")}' COLLATE NOCASE");
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

        PragmaTableInfo info = liveInfo.First(c => string.Equals(c.Name, columnName, StringComparison.OrdinalIgnoreCase));
        if (info.PrimaryKeyOrder > 0)
        {
            return false;
        }

        string quoted = "\"" + columnName.Replace("\"", "\"\"") + "\"";
        int occurrences = 0;
        for (int i = createSql.IndexOf(quoted, StringComparison.OrdinalIgnoreCase); i >= 0; i = createSql.IndexOf(quoted, i + quoted.Length, StringComparison.OrdinalIgnoreCase))
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
            .Any(row => string.Equals(row["from"] as string, columnName, StringComparison.OrdinalIgnoreCase));
        if (inForeignKey)
        {
            return false;
        }

        if (Database.Query<string>($"SELECT sql FROM sqlite_master WHERE type = 'index' AND tbl_name = '{mapping.TableName.Replace("'", "''")}' AND sql IS NOT NULL")
            .Any(sql => sql.Contains(quoted, StringComparison.OrdinalIgnoreCase) || ContainsUnquotedIdentifier(sql, columnName)))
        {
            return false;
        }

        if (IsColumnIndexed(mapping.TableName, columnName))
        {
            return false;
        }

        return !Database.Query<string>("SELECT sql FROM sqlite_master WHERE type IN ('trigger', 'view') AND sql IS NOT NULL")
            .Any(sql => sql.Contains(quoted, StringComparison.OrdinalIgnoreCase) || ContainsUnquotedIdentifier(sql, columnName));
    }

    [UnconditionalSuppressMessage("AOT", "IL2026", Justification = "Querying built-in index rows keeps their public members reachable.")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Querying built-in index rows keeps their public members reachable.")]
    private bool IsColumnIndexed(string tableName, string columnName)
    {
        foreach (PragmaIndexList index in Database.Pragmas.IndexList(tableName).ToList())
        {
            bool inIndex = Database.Query<Dictionary<string, object?>>($"PRAGMA index_info(\"{index.Name.Replace("\"", "\"\"")}\")")
                .Any(row => string.Equals(row["name"] as string, columnName, StringComparison.OrdinalIgnoreCase));
            if (inIndex)
            {
                return true;
            }
        }

        return false;
    }

    private int RebuildTable(TableMapping mapping, IReadOnlyList<MigrationSetValue> sets)
    {
        string table = mapping.TableName;
        string temp = table + "__sqlitefw_migrate";

        List<PragmaTableInfo> liveInfo = Database.Pragmas.TableInfo(table).ToList();
        HashSet<string> liveColumns = liveInfo.Select(c => c.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        HashSet<string> setColumns = sets.Select(s => s.Column).ToHashSet(StringComparer.OrdinalIgnoreCase);
        HashSet<string> computedColumns = mapping.ComputedColumns.Select(c => c.Column.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        EnsureNoUnfilledNotNull(mapping, table, liveColumns, computedColumns, setColumns);

        List<string> copyColumns = mapping.Columns
            .Where(c => !computedColumns.Contains(c.Name))
            .Select(c => c.Name)
            .Concat(mapping.ShadowColumns.Select(s => s.Name))
            .Where(name => liveColumns.Contains(name) && !setColumns.Contains(name))
            .ToList();

        EnsureNoUncoveredStorageChange(mapping, table, liveInfo, copyColumns);

        Dictionary<string, string> backfillDefaults = new(StringComparer.OrdinalIgnoreCase);
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

        bool copyRowId = !HasIntegerKeyAlias(mapping) && !IsWithoutRowId(table);
        List<string> insertColumns = copyColumns.Concat(sets.Select(s => s.Column)).Select(IdentifierGuard.Quote).ToList();
        List<string> selectExpressions = copyColumns
            .Select(name => backfillDefaults.TryGetValue(name, out string? defaultSql)
                ? $"COALESCE({IdentifierGuard.Quote(name)}, {defaultSql})"
                : IdentifierGuard.Quote(name))
            .Concat(sets.Select(s => s.ValueSql))
            .ToList();
        if (copyRowId && insertColumns.Count > 0)
        {
            insertColumns.Insert(0, "rowid");
            selectExpressions.Insert(0, "\"__sqlitefw_rowid\"");
        }

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

            if (copyRowId)
            {
                Database.Execute($"CREATE TABLE \"{temp}\" AS SELECT rowid AS \"__sqlitefw_rowid\", * FROM \"{table}\"");
            }
            else
            {
                Database.Execute($"CREATE TABLE \"{temp}\" AS SELECT * FROM \"{table}\"");
            }
            Database.Execute($"DROP TABLE \"{table}\"");
            int count = Database.CreateCommand(SchemaSqlBuilder.BuildCreateTable(Database, mapping, table, ifNotExists: false), []).ExecuteNonQuery();
            ReconcileIndexes(mapping);
            if (insertColumns.Count > 0)
            {
                Database.Execute($"INSERT INTO \"{table}\" ({string.Join(", ", insertColumns)}) SELECT {string.Join(", ", selectExpressions)} FROM \"{temp}\"");
            }
            Database.Execute($"DROP TABLE \"{temp}\"");
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
                .Any(row => string.Equals(row["table"] as string, table, StringComparison.OrdinalIgnoreCase));
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

            List<Dictionary<string, object?>> childInfo = Database
                .Query<Dictionary<string, object?>>($"PRAGMA table_xinfo(\"{childEscaped}\")")
                .Where(col => Convert.ToInt64(col["hidden"], CultureInfo.InvariantCulture) is not (2 or 3))
                .ToList();
            List<string> insertableColumns = childInfo.Select(col => (string)col["name"]!).ToList();
            List<Dictionary<string, object?>> keyColumns = childInfo
                .Where(col => Convert.ToInt64(col["pk"], CultureInfo.InvariantCulture) > 0)
                .ToList();
            bool copyRowId = !IsWithoutRowId(child)
                && !(keyColumns.Count == 1 && string.Equals((string?)keyColumns[0]["type"], "INTEGER", StringComparison.OrdinalIgnoreCase));

            if (copyRowId)
            {
                Database.Execute($"CREATE TABLE \"{child}__sqlitefw_hold\" AS SELECT rowid AS \"__sqlitefw_rowid\", * FROM \"{child}\"");
            }
            else
            {
                Database.Execute($"CREATE TABLE \"{child}__sqlitefw_hold\" AS SELECT * FROM \"{child}\"");
            }
            Database.Execute($"DELETE FROM \"{child}\"");
            saved.Add(new SavedTable { Name = child, Triggers = triggerSql, InsertableColumns = insertableColumns, CopyRowId = copyRowId });
        }

        return saved;
    }

    private void RestoreReferencingTables(List<SavedTable> saved)
    {
        for (int i = saved.Count - 1; i >= 0; i--)
        {
            SavedTable child = saved[i];
            string columnList = string.Join(", ", child.InsertableColumns.Select(c => $"\"{c.Replace("\"", "\"\"")}\""));
            string insertList = child.CopyRowId ? "rowid, " + columnList : columnList;
            string selectList = child.CopyRowId ? "\"__sqlitefw_rowid\", " + columnList : columnList;
            Database.Execute($"INSERT INTO \"{child.Name}\" ({insertList}) SELECT {selectList} FROM \"{child.Name}__sqlitefw_hold\"");
            Database.Execute($"DROP TABLE \"{child.Name}__sqlitefw_hold\"");
            foreach (string trigger in child.Triggers)
            {
                Database.Execute(trigger);
            }
        }
    }

    private bool HasIntegerKeyAlias(TableMapping mapping)
    {
        List<TableColumn> keys = mapping.Columns.Where(c => c.IsPrimaryKey).ToList();
        return keys.Count == 1
            && TypeHelpers.TypeToSQLiteType(keys[0].PropertyType, Database.Options) == SQLiteColumnType.Integer;
    }

    private bool IsWithoutRowId(string table)
    {
        string? createSql = Database.ExecuteScalar<string?>(
            $"SELECT sql FROM sqlite_master WHERE type = 'table' AND name = '{table.Replace("'", "''")}' COLLATE NOCASE");
        return CreateTableInspector.HasWithoutRowIdClause(createSql!);
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

        List<long> seq = Database.Query<long>($"SELECT seq FROM sqlite_sequence WHERE name = '{table.Replace("'", "''")}' COLLATE NOCASE");
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

    private void EnsureNoUncoveredStorageChange(TableMapping mapping, string table, List<PragmaTableInfo> liveInfo, List<string> copyColumns)
    {
        if (!mapping.Strict)
        {
            return;
        }

        HashSet<string> copySet = copyColumns.ToHashSet(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, string> liveTypes = liveInfo.ToDictionary(c => c.Name, c => c.Type, StringComparer.OrdinalIgnoreCase);
        foreach (TableColumn column in mapping.Columns)
        {
            if (!copySet.Contains(column.Name))
            {
                continue;
            }

            bool intendedBlob = column.ColumnType == SQLiteColumnType.Blob;
            bool liveBlob = string.Equals(liveTypes[column.Name], "BLOB", StringComparison.OrdinalIgnoreCase);
            if (intendedBlob == liveBlob)
            {
                continue;
            }

            if (Database.ExecuteScalar<long>($"SELECT COUNT(*) FROM \"{table}\"") == 0)
            {
                return;
            }

            throw new InvalidOperationException(
                $"Cannot migrate STRICT table '{table}'. Column '{column.Name}' changes storage class from " +
                $"{liveTypes[column.Name].ToUpperInvariant()} to {column.ColumnType.ToString().ToUpperInvariant()}, but the migration does not rewrite its data. " +
                "SQLite will not store the old value in the new column. Re-encode it with TableChanged(s => s.Reconvert(...)) or set a value with Set(...).");
        }
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

    private static bool ReadsOutsideModel(TableMapping mapping, MigrationSetValue set)
    {
        if (set.ReadColumns.Count == 0)
        {
            return false;
        }

        HashSet<string> modelColumns = mapping.Columns.Select(c => c.Name)
            .Concat(mapping.ShadowColumns.Select(s => s.Name))
            .Concat(mapping.ComputedColumns.Select(c => c.Column.Name))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        return set.ReadColumns.Any(read => !modelColumns.Contains(read));
    }

    private static bool ReadsOwnColumn(MigrationSetValue set)
    {
        return set.ReadColumns.Any(read => string.Equals(read, set.Column, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsSettledInSchemaPhase(TableMapping mapping, MigrationSetValue set, int version, Dictionary<string, (int Version, MigrationSetValue Set)> schemaWinners)
    {
        if (!schemaWinners.TryGetValue(set.Column, out (int Version, MigrationSetValue Set) winner))
        {
            return false;
        }

        if (ReadsOutsideModel(mapping, winner.Set))
        {
            return version <= winner.Version;
        }

        return ReferenceEquals(set, winner.Set) && ReadsOwnColumn(set);
    }

    private static int FirstOpaqueVersion(List<MigrationOperation> operations)
    {
        int first = int.MaxValue;
        foreach (MigrationOperation operation in operations)
        {
            if (operation.Kind is MigrationOperationKind.RawSql or MigrationOperationKind.Run && operation.Version < first)
            {
                first = operation.Version;
            }
        }

        return first;
    }

    private static MigrationSetValue BuildSchemaApplySet(TableMapping mapping, MigrationSetValue winner, HashSet<string> liveColumns)
    {
        if (ReadsOwnColumn(winner) || ReadsOutsideModel(mapping, winner) || !liveColumns.Contains(winner.Column))
        {
            return winner;
        }

        return new MigrationSetValue
        {
            Column = winner.Column,
            ValueSql = $"COALESCE({IdentifierGuard.Quote(winner.Column)}, {winner.ValueSql})",
            ReadColumns = [.. winner.ReadColumns, winner.Column],
            RunInRebuild = winner.RunInRebuild,
        };
    }

    private static int ApplyDeferredSchemaThrough(List<DeferredSchemaWork> deferredSchema, ref int nextSchema, int version, List<DeferredFill> deferredFills, ref int nextFill)
    {
        int count = 0;
        while (nextSchema < deferredSchema.Count && deferredSchema[nextSchema].Version <= version)
        {
            (int applied, List<DeferredFill> fills) = deferredSchema[nextSchema].Apply();
            count += applied;
            foreach (DeferredFill fill in fills)
            {
                int insertAt = nextFill;
                while (insertAt < deferredFills.Count && deferredFills[insertAt].Version <= fill.Version)
                {
                    insertAt++;
                }

                deferredFills.Insert(insertAt, fill);
            }

            nextSchema++;
        }

        return count;
    }

    private static bool IsDataPhase(MigrationOperationKind kind)
    {
        return kind is MigrationOperationKind.DropColumn
            or MigrationOperationKind.DropTable
            or MigrationOperationKind.RawSql
            or MigrationOperationKind.InsertRows
            or MigrationOperationKind.UpdateRows
            or MigrationOperationKind.DeleteRows
            or MigrationOperationKind.CreateView
            or MigrationOperationKind.DropView
            or MigrationOperationKind.RebuildFullTextSearch
            or MigrationOperationKind.Run;
    }

    private static void RemoveDropsSupersededByCreate(List<MigrationOperation> operations)
    {
        for (int i = operations.Count - 1; i >= 0; i--)
        {
            MigrationOperation operation = operations[i];
            if (operation.Kind != MigrationOperationKind.DropTable)
            {
                continue;
            }

            string tableName = operation.Mapping?.TableName ?? operation.TableName!;
            bool superseded = false;
            for (int j = i + 1; j < operations.Count; j++)
            {
                if (operations[j].Kind == MigrationOperationKind.CreateTable
                    && string.Equals(operations[j].Mapping!.TableName, tableName, StringComparison.OrdinalIgnoreCase))
                {
                    operations[j].DropTableFirst = true;
                    operation.RecreateMapping = operations[j].Mapping;
                    superseded = true;
                    break;
                }
            }

            if (!superseded)
            {
                continue;
            }

            for (int j = i - 1; j >= 0; j--)
            {
                if (IsDiscardedByLaterDrop(operations[j].Kind)
                    && string.Equals(operations[j].Mapping!.TableName, tableName, StringComparison.OrdinalIgnoreCase))
                {
                    operations.RemoveAt(j);
                    i--;
                }
            }
        }
    }

    private static bool IsDiscardedByLaterDrop(MigrationOperationKind kind)
    {
        return kind is MigrationOperationKind.InsertRows
            or MigrationOperationKind.UpdateRows
            or MigrationOperationKind.DeleteRows
            or MigrationOperationKind.Reconcile
            or MigrationOperationKind.DropColumn
            or MigrationOperationKind.RebuildFullTextSearch;
    }

    private static List<MigrationSetValue> UnionSets(IEnumerable<MigrationSetValue> sets)
    {
        Dictionary<string, int> index = new(StringComparer.OrdinalIgnoreCase);
        List<MigrationSetValue> result = [];
        foreach (MigrationSetValue set in sets)
        {
            if (index.TryGetValue(set.Column, out int existing))
            {
                result[existing] = set;
            }
            else
            {
                index[set.Column] = result.Count;
                result.Add(set);
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
