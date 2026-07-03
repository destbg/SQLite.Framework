namespace SQLite.Framework;

/// <summary>
/// Declares the work one migration version performs. Reach an instance through the callback passed
/// to <see cref="SQLiteMigrationRunner.Version" />. Use <see cref="CreateTable{T}" /> for a new
/// table and <see cref="TableChanged{T}" /> to reconcile an existing one to the current model, plus
/// the explicit methods for renames, drops and raw SQL that a reconcile cannot work out on its own.
/// </summary>
/// <remarks>
/// Within a single run the runner does not apply these in the order written. It runs every
/// <see cref="RunBefore(Action{SQLiteMigrationContext})" /> callback first, then every rename,
/// then one reconcile per table, then drops, row inserts, raw SQL and
/// <see cref="Run(Action{SQLiteMigrationContext})" /> callbacks in the order declared. So a data
/// step runs against the final shape of the table, not an in-between shape. To move data out of
/// a column you are removing, read it in a <c>RunBefore</c> callback or keep the old column on
/// the model while you copy it, then remove it in a later version. A reconcile or a column
/// drop for a table that this same run created with <see cref="CreateTable{T}" /> is skipped,
/// since the new table already matches the model.
/// </remarks>
public sealed class SQLiteMigrationStep
{
    private readonly SQLiteDatabase database;
    private readonly int version;
    private readonly List<MigrationOperation> operations = [];

    internal SQLiteMigrationStep(SQLiteDatabase database, int version)
    {
        this.database = database;
        this.version = version;
    }

    internal IReadOnlyList<MigrationOperation> Operations => operations;

    /// <summary>
    /// Creates the table for <typeparamref name="T" /> from the model,
    /// with its declared indexes and triggers, if it does not already exist.
    /// </summary>
    public SQLiteMigrationStep CreateTable<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>()
    {
        TableMapping mapping = database.TableMapping<T>();
        operations.Add(new MigrationOperation
        {
            Kind = MigrationOperationKind.CreateTable,
            Description = $"create \"{mapping.TableName}\"",
            Mapping = mapping,
        });
        return this;
    }

    /// <summary>
    /// Reconciles the table for <typeparamref name="T" /> to the current model. New columns are
    /// added, dropped columns are removed and indexes and triggers are brought in line. Pass
    /// <paramref name="fill" /> to give new <c>NOT NULL</c> columns a value for existing rows. The
    /// runner unions the fills from every pending version before it reconciles, so a column added in
    /// a later version does not make an earlier version throw. By default the runner makes the change
    /// in place where it can and falls back to a rebuild otherwise. Set <paramref name="rebuild" /> to
    /// always rebuild, which works on any SQLite version.
    /// </summary>
    /// <param name="fill">An optional callback that sets values for columns while reconciling.</param>
    /// <param name="rebuild">Whether to always rebuild the table instead of trying in-place changes first.</param>
    public SQLiteMigrationStep TableChanged<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(Func<SQLiteMigrationBuilder<T>, SQLiteMigrationBuilder<T>>? fill = null, bool rebuild = false)
    {
        TableMapping mapping = database.TableMapping<T>();
        SQLiteMigrationBuilder<T> builder = new(database, mapping);
        fill?.Invoke(builder);

        string detail = rebuild ? " by rebuild" : string.Empty;
        string values = builder.Sets.Count > 0 ? $" with {builder.Sets.Count} value(s)" : string.Empty;
        operations.Add(new MigrationOperation
        {
            Kind = MigrationOperationKind.Reconcile,
            Description = $"reconcile \"{mapping.TableName}\"{detail}{values}",
            Mapping = mapping,
            Sets = builder.Sets,
            Rebuild = rebuild,
        });
        return this;
    }

    /// <summary>
    /// Renames the column <paramref name="fromColumn" /> to <paramref name="toColumn" /> on the table
    /// for <typeparamref name="T" />. Both names are SQLite column names. A reconcile cannot tell a
    /// rename from a drop plus an add, so use this when you rename a column to keep its data.
    /// </summary>
    public SQLiteMigrationStep RenameColumn<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(string fromColumn, string toColumn)
    {
        ArgumentException.ThrowIfNullOrEmpty(fromColumn);
        ArgumentException.ThrowIfNullOrEmpty(toColumn);

        TableMapping mapping = database.TableMapping<T>();
        operations.Add(new MigrationOperation
        {
            Kind = MigrationOperationKind.RenameColumn,
            Description = $"rename column \"{fromColumn}\" to \"{toColumn}\" on \"{mapping.TableName}\"",
            Mapping = mapping,
            FromColumn = fromColumn,
            ToColumn = toColumn,
        });
        return this;
    }

    /// <summary>
    /// Drops the column with the given SQLite name from the table for <typeparamref name="T" />.
    /// </summary>
    public SQLiteMigrationStep DropColumn<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(string columnName)
    {
        ArgumentException.ThrowIfNullOrEmpty(columnName);

        TableMapping mapping = database.TableMapping<T>();
        operations.Add(new MigrationOperation
        {
            Kind = MigrationOperationKind.DropColumn,
            Description = $"drop column \"{columnName}\" on \"{mapping.TableName}\"",
            Mapping = mapping,
            ColumnName = columnName,
        });
        return this;
    }

    /// <summary>
    /// Drops the table for <typeparamref name="T" /> if it exists. For an FTS5 table with sync
    /// triggers, the triggers are dropped too, the same as <see cref="SQLiteSchema.DropTable{T}()" />.
    /// </summary>
    public SQLiteMigrationStep DropTable<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>()
    {
        TableMapping mapping = database.TableMapping<T>();
        operations.Add(new MigrationOperation
        {
            Kind = MigrationOperationKind.DropTable,
            Description = $"drop table \"{mapping.TableName}\"",
            Mapping = mapping,
            TableName = mapping.TableName,
        });
        return this;
    }

    /// <summary>
    /// Drops the table with the given SQLite name if it exists.
    /// </summary>
    public SQLiteMigrationStep DropTable(string tableName)
    {
        ArgumentException.ThrowIfNullOrEmpty(tableName);

        operations.Add(new MigrationOperation
        {
            Kind = MigrationOperationKind.DropTable,
            Description = $"drop table \"{tableName}\"",
            TableName = tableName,
        });
        return this;
    }

    /// <summary>
    /// Inserts the given rows into the table for <typeparamref name="T" />. The rows go through
    /// the same write pipeline as <see cref="SQLiteTable{T}.Add(T)" />, so storage modes,
    /// converters, write hooks and auto-increment key write-back all apply. Use this to seed the
    /// data that belongs to a schema version. The insert runs after the reconcile, against the
    /// final shape of the table.
    /// </summary>
    public SQLiteMigrationStep Insert<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(params T[] rows)
    {
        ArgumentNullException.ThrowIfNull(rows);
        if (rows.Length == 0)
        {
            throw new ArgumentException("Insert requires at least one row.", nameof(rows));
        }

        TableMapping mapping = database.TableMapping<T>();
        operations.Add(new MigrationOperation
        {
            Kind = MigrationOperationKind.InsertRows,
            Description = $"insert {rows.Length} row(s) into \"{mapping.TableName}\"",
            Mapping = mapping,
            InsertRows = db => db.Table<T>().AddRange(rows, runInTransaction: false),
        });
        return this;
    }

    /// <summary>
    /// Inserts the given rows into the table for <typeparamref name="T" />, skipping every row
    /// whose <paramref name="key" /> value is already in the table. Use this to seed rows that
    /// may already exist, for example because users can create rows with the same key themselves.
    /// The rows go through the same write pipeline as <see cref="SQLiteTable{T}.Add(T)" />, the
    /// same as <see cref="Insert{T}" />. The check and the insert run after the reconcile,
    /// against the final shape of the table. Rows in <paramref name="rows" /> are only checked
    /// against the table, not against each other.
    /// </summary>
    /// <param name="key">A mapped property on <typeparamref name="T" /> that identifies a row.</param>
    /// <param name="rows">The rows to insert when their key value is not in the table yet.</param>
    public SQLiteMigrationStep InsertIfMissing<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T, TKey>(Expression<Func<T, TKey>> key, params T[] rows)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(rows);
        if (rows.Length == 0)
        {
            throw new ArgumentException("InsertIfMissing requires at least one row.", nameof(rows));
        }

        TableMapping mapping = database.TableMapping<T>();
        Expression body = key.Body;
        if (body.NodeType == ExpressionType.Convert)
        {
            body = ((UnaryExpression)body).Operand;
        }

        if (body is not MemberExpression member
            || mapping.Columns.FirstOrDefault(c => c.PropertyInfo.Name == member.Member.Name) is not { } column)
        {
            throw new ArgumentException("The key must be a mapped property on the entity, like x => x.Name.", nameof(key));
        }

        operations.Add(new MigrationOperation
        {
            Kind = MigrationOperationKind.InsertRows,
            Description = $"insert up to {rows.Length} row(s) into \"{mapping.TableName}\" missing by \"{column.Name}\"",
            Mapping = mapping,
            InsertRows = db => InsertMissingRows(db, key, rows),
        });
        return this;
    }

    /// <summary>
    /// Runs a raw SQL statement. Use this for data fixes and for changes the typed methods do not
    /// cover. To seed rows, prefer the typed <see cref="Insert{T}" />. The statement runs against
    /// the final shape of the tables, after the reconcile.
    /// </summary>
    public SQLiteMigrationStep Sql(string sql)
    {
        ArgumentException.ThrowIfNullOrEmpty(sql);

        operations.Add(new MigrationOperation
        {
            Kind = MigrationOperationKind.RawSql,
            Description = "run SQL",
            Sql = sql,
        });
        return this;
    }

    /// <summary>
    /// Runs a callback in the data phase, after every schema change, against the final shape of
    /// the tables. Use it for data work the other methods cannot express. The callback gets a
    /// <see cref="SQLiteMigrationContext" /> with the database and the version range of the run.
    /// It runs inside the migration transaction, so a throw rolls the whole run back. Work done
    /// inside the callback is not added to the count <see cref="SQLiteMigrationRunner.Migrate" />
    /// returns. For a callback that awaits async database methods use
    /// <see cref="RunAsync(Func{SQLiteMigrationContext, Task})" />.
    /// </summary>
    /// <param name="action">The callback to run.</param>
    public SQLiteMigrationStep Run(Action<SQLiteMigrationContext> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        operations.Add(new MigrationOperation
        {
            Kind = MigrationOperationKind.Run,
            Description = $"run callback at version {version}",
            Callback = action,
        });
        return this;
    }

    /// <summary>
    /// Runs a callback before any schema change of the run, against the old shape of the tables.
    /// Use it to read data that the schema changes would drop or rewrite. The model describes the
    /// new shape, so a typed query here can name columns that do not exist yet. Prefer raw SQL
    /// through <c>Query</c> and <c>Execute</c> and guard with <c>TableExists</c>, since on a fresh
    /// database the callback runs before any table exists. The same transaction and count rules
    /// as <see cref="Run(Action{SQLiteMigrationContext})" /> apply.
    /// </summary>
    /// <param name="action">The callback to run.</param>
    public SQLiteMigrationStep RunBefore(Action<SQLiteMigrationContext> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        operations.Add(new MigrationOperation
        {
            Kind = MigrationOperationKind.RunBefore,
            Description = $"run callback before schema changes at version {version}",
            Callback = action,
        });
        return this;
    }

    /// <summary>
    /// The same as <see cref="Run(Action{SQLiteMigrationContext})" /> for a callback that is
    /// awaited. Use the async database methods inside it and pass
    /// <see cref="SQLiteMigrationContext.CancellationToken" /> to them. Only <c>MigrateAsync</c>
    /// can await the callback, so <see cref="SQLiteMigrationRunner.Migrate" /> throws when a
    /// pending version declares one.
    /// </summary>
    /// <param name="action">The callback to await.</param>
    public SQLiteMigrationStep RunAsync(Func<SQLiteMigrationContext, Task> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        operations.Add(new MigrationOperation
        {
            Kind = MigrationOperationKind.Run,
            Description = $"run async callback at version {version}",
            AsyncCallback = action,
        });
        return this;
    }

    /// <summary>
    /// The same as <see cref="RunBefore(Action{SQLiteMigrationContext})" /> for a callback that is
    /// awaited. The same rules as <see cref="RunAsync(Func{SQLiteMigrationContext, Task})" />
    /// apply, so only <c>MigrateAsync</c> can await it.
    /// </summary>
    /// <param name="action">The callback to await.</param>
    public SQLiteMigrationStep RunBeforeAsync(Func<SQLiteMigrationContext, Task> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        operations.Add(new MigrationOperation
        {
            Kind = MigrationOperationKind.RunBefore,
            Description = $"run async callback before schema changes at version {version}",
            AsyncCallback = action,
        });
        return this;
    }

    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Compile falls back to the expression interpreter without dynamic code.")]
    private static int InsertMissingRows<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T, TKey>(SQLiteDatabase db, Expression<Func<T, TKey>> key, T[] rows)
    {
        Func<T, TKey> getKey = key.Compile();
        List<TKey> keys = new(rows.Length);
        foreach (T row in rows)
        {
            keys.Add(getKey(row));
        }

        Expression<Func<List<TKey>, TKey, bool>> containsTemplate = (list, value) => list.Contains(value);
        MethodInfo containsMethod = ((MethodCallExpression)containsTemplate.Body).Method;
        Expression<Func<T, bool>> predicate = Expression.Lambda<Func<T, bool>>(
            Expression.Call(Expression.Constant(keys), containsMethod, key.Body), key.Parameters);

        HashSet<TKey> existing = db.Table<T>().Where(predicate).Select(key).ToHashSet();
        List<T> missing = rows.Where(row => !existing.Contains(getKey(row))).ToList();
        return missing.Count == 0 ? 0 : db.Table<T>().AddRange(missing, runInTransaction: false);
    }
}
