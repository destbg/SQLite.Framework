namespace SQLite.Framework;

/// <summary>
/// Represents a base class for SQLite tables.
/// </summary>
public class SQLiteTable : BaseSQLiteTable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SQLiteTable{T}"/> class.
    /// </summary>
    public SQLiteTable(SQLiteDatabase database, TableMapping table)
        : base(database, table)
    {
    }

    /// <inheritdoc />
    public override Type ElementType => Table.Type;

    /// <inheritdoc />
    public override Expression Expression => Expression.Constant(this);

    /// <inheritdoc />
    public override IQueryProvider Provider => Database;

    /// <inheritdoc />
    public override IEnumerator GetEnumerator()
    {
        return Provider.Execute<IEnumerable>(Expression).GetEnumerator();
    }

    /// <summary>
    /// Performs a DELETE operation on the database table.
    /// </summary>
    /// <remarks>
    /// WARNING! This will delete all rows in the table.
    /// </remarks>
    public virtual int Clear()
    {
        string sql = $"DELETE FROM \"{Table.TableName}\"";
        return Database.CreateCommand(sql, []).ExecuteNonQuery();
    }
}

/// <summary>
/// Represents a table in the SQLite database.
/// </summary>
public class SQLiteTable<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T> : SQLiteTable, IQueryable<T>
{
    private bool? hasAnyDatabaseDefault;

    /// <summary>
    /// Initializes a new instance of the <see cref="SQLiteTable{T}"/> class.
    /// </summary>
    public SQLiteTable(SQLiteDatabase database, TableMapping table)
        : base(database, table)
    {
    }

    /// <summary>
    /// Initializes a table builder for creating the table.
    /// </summary>
    public virtual SQLiteTableSchema<T> Schema => Database.Schema.Table<T>();

    /// <summary>
    /// Extra columns written into the generated <c>INSERT</c> and <c>UPDATE</c>, as
    /// (column name, inlined value SQL) pairs. Empty by default. Set through
    /// <see cref="WithColumns" />, which returns a view that overrides this.
    /// </summary>
    internal virtual IReadOnlyList<(string Column, string ValueSql)> ExtraWriteColumns => [];

    /// <summary>
    /// True when a <see cref="WithColumns" /> value expression reads a column of the row, which is
    /// only valid on an <c>Update</c>. Used to reject such a value on an insert.
    /// </summary>
    internal virtual bool ExtraWriteColumnsReferenceRow => false;

    /// <summary>
    /// Wraps the provided SQL query and parameters into a queryable object.
    /// </summary>
    public virtual IQueryable<T> FromSql(string sql, params SQLiteParameter[] parameters)
    {
        return Database.FromSql<T>(sql, parameters);
    }

    /// <summary>
    /// Performs an INSERT operation on the database table using the row.
    /// </summary>
    public virtual int Add(T item)
    {
        if (HasColumnHooks(Database.Options.AddHooks))
        {
            Dictionary<string, object?> columns = [];
            if (!RunHooks(Database.Options.AddHooks, item, columns))
            {
                return 0;
            }

            SQLiteAction columnAction = RunActionHooks(item, SQLiteAction.Add);
            return columnAction == SQLiteAction.Add
                ? InsertWithExtraColumns(item, columns)
                : DispatchAction(columnAction, item);
        }

        if (!RunHooks(Database.Options.AddHooks, item))
        {
            return 0;
        }

        return DispatchAction(RunActionHooks(item, SQLiteAction.Add), item);
    }

    /// <summary>
    /// Performs an INSERT operation on the database table using the rows.
    /// </summary>
    public virtual int AddRange(IEnumerable<T> collection, bool runInTransaction = true)
    {
        if (HasColumnHooks(Database.Options.AddHooks))
        {
            return RunRangeWithColumns(Database.Options.AddHooks, collection, runInTransaction, SQLiteAction.Add);
        }

        if (Database.Options.OnActionHooks.Count == 0
            && !IsItemMethodOverridden(nameof(InsertItem))
            && !HasAnyDatabaseDefault())
        {
            (TableColumn[] columns, string sql) = GetAddInfo();
            TableColumn? autoIncrement = GetAutoIncrementColumn();
            SQLiteOptions options = Database.Options;
            Action<sqlite3_stmt, T> bindRow = ResolveInsertBindRow(columns, autoIncrement, options);
            return RunPreparedRange(sql, collection, Database.Options.AddHooks, runInTransaction, bindRow, autoIncrement);
        }

        return RunRange(Database.Options.AddHooks, collection, runInTransaction,
            item => DispatchAction(RunActionHooks(item, SQLiteAction.Add), item));
    }

    /// <summary>
    /// Performs an UPDATE operation on the database table using the row.
    /// </summary>
    public virtual int Update(T item)
    {
        if (HasColumnHooks(Database.Options.UpdateHooks))
        {
            Dictionary<string, object?> columns = [];
            if (!RunHooks(Database.Options.UpdateHooks, item, columns))
            {
                return 0;
            }

            SQLiteAction columnAction = RunActionHooks(item, SQLiteAction.Update);
            return columnAction == SQLiteAction.Update
                ? UpdateWithExtraColumns(item, columns)
                : DispatchAction(columnAction, item);
        }

        if (!RunHooks(Database.Options.UpdateHooks, item))
        {
            return 0;
        }

        return DispatchAction(RunActionHooks(item, SQLiteAction.Update), item);
    }

    /// <summary>
    /// Performs an UPDATE operation on the database table using the rows.
    /// </summary>
    public virtual int UpdateRange(IEnumerable<T> collection, bool runInTransaction = true)
    {
        if (HasColumnHooks(Database.Options.UpdateHooks))
        {
            return RunRangeWithColumns(Database.Options.UpdateHooks, collection, runInTransaction, SQLiteAction.Update);
        }

        if (Database.Options.OnActionHooks.Count == 0 && !IsItemMethodOverridden(nameof(UpdateItem)))
        {
            (TableColumn[] columns, TableColumn[] primaryKeyColumns, string sql) = GetUpdateInfo();
            SQLiteOptions options = Database.Options;
            Action<sqlite3_stmt, T> bindData = ResolveBindRow(columns, 0, options);
            Action<sqlite3_stmt, T> bindPk = ResolveBindRow(primaryKeyColumns, columns.Length, options);
            return RunPreparedRange(sql, collection, Database.Options.UpdateHooks, runInTransaction, BindRow);

            void BindRow(sqlite3_stmt stmt, T item)
            {
                bindData(stmt, item);
                bindPk(stmt, item);
            }
        }

        return RunRange(Database.Options.UpdateHooks, collection, runInTransaction,
            item => DispatchAction(RunActionHooks(item, SQLiteAction.Update), item));
    }

    /// <summary>
    /// Performs a DELETE operation on the database table using the row.
    /// </summary>
    public virtual int Remove(T item)
    {
        if (!RunHooks(Database.Options.RemoveHooks, item))
        {
            return 0;
        }

        return DispatchAction(RunActionHooks(item, SQLiteAction.Remove), item);
    }

    /// <summary>
    /// Performs a DELETE operation on the database table using the rows.
    /// </summary>
    public virtual int RemoveRange(IEnumerable<T> collection, bool runInTransaction = true)
    {
        if (Database.Options.OnActionHooks.Count == 0 && !IsItemMethodOverridden(nameof(AddOrRemoveItem)))
        {
            (TableColumn[] primaryKeyColumns, string sql) = GetRemoveInfo();
            SQLiteOptions options = Database.Options;
            Action<sqlite3_stmt, T> bindRow = ResolveBindRow(primaryKeyColumns, 0, options);
            return RunPreparedRange(sql, collection, Database.Options.RemoveHooks, runInTransaction, bindRow);
        }

        return RunRange(Database.Options.RemoveHooks, collection, runInTransaction,
            item => DispatchAction(RunActionHooks(item, SQLiteAction.Remove), item));
    }

    /// <summary>
    /// Performs an <c>INSERT OR &lt;conflict&gt;</c> operation on the database table using the row.
    /// Defaults to <see cref="SQLiteConflict.Replace" /> for backward compatibility with the
    /// previous <c>INSERT OR REPLACE</c> behaviour.
    /// </summary>
    public virtual int AddOrUpdate(T item, SQLiteConflict conflict = SQLiteConflict.Replace)
    {
        if (!RunHooks(Database.Options.AddOrUpdateHooks, item))
        {
            return 0;
        }

        SQLiteAction final = RunActionHooks(item, SQLiteAction.AddOrUpdate);
        if (final == SQLiteAction.AddOrUpdate)
        {
            return DefaultAddOrUpdate(item, conflict);
        }

        return DispatchAction(final, item);
    }

    /// <summary>
    /// Performs an <c>INSERT OR &lt;conflict&gt;</c> operation on the database table using the rows.
    /// </summary>
    public virtual int AddOrUpdateRange(IEnumerable<T> collection, bool runInTransaction = true, SQLiteConflict conflict = SQLiteConflict.Replace)
    {
        if (Database.Options.OnActionHooks.Count == 0
            && !IsItemMethodOverridden(nameof(InsertItem))
            && !HasAnyDatabaseDefault())
        {
            (TableColumn[] columns, string sql) = GetAddOrUpdateInfo(conflict);
            TableColumn? autoIncrement = GetAutoIncrementColumn();
            SQLiteOptions options = Database.Options;
            Action<sqlite3_stmt, T> bindRow = ResolveInsertBindRow(columns, autoIncrement, options);
            return RunPreparedRange(sql, collection, Database.Options.AddOrUpdateHooks, runInTransaction, bindRow, autoIncrement);
        }

        return RunRange(Database.Options.AddOrUpdateHooks, collection, runInTransaction, item =>
        {
            SQLiteAction final = RunActionHooks(item, SQLiteAction.AddOrUpdate);
            return final == SQLiteAction.AddOrUpdate
                ? DefaultAddOrUpdate(item, conflict)
                : DispatchAction(final, item);
        });
    }

    /// <summary>
    /// Performs an <c>INSERT INTO ... ON CONFLICT (...) DO ...</c> upsert built through the
    /// <see cref="SQLiteUpsertBuilder{T}" /> DSL. Use this when <c>AddOrUpdate</c> with an
    /// <see cref="SQLiteConflict" /> value is not enough, for example to update only some
    /// columns or to do nothing on conflict. Requires SQLite 3.24.0 or newer.
    /// </summary>
#if SQLITE_FRAMEWORK_OS_BUNDLED_SQLITE
    [UnsupportedOSPlatform("android")]
    [SupportedOSPlatform("android30.0")]
    [UnsupportedOSPlatform("ios")]
    [SupportedOSPlatform("ios12.0")]
#endif
    public virtual int Upsert(T item, Action<SQLiteUpsertBuilder<T>> configure)
    {
        if (!RunHooks(Database.Options.AddOrUpdateHooks, item))
        {
            return 0;
        }

        SQLiteAction final = RunActionHooks(item, SQLiteAction.AddOrUpdate);
        if (final == SQLiteAction.AddOrUpdate)
        {
            return DefaultUpsert(item, configure);
        }

        return DispatchAction(final, item);
    }

    /// <summary>
    /// Range version of <see cref="Upsert" />. Runs hooks per row. Requires SQLite 3.24.0 or newer.
    /// </summary>
#if SQLITE_FRAMEWORK_OS_BUNDLED_SQLITE
    [UnsupportedOSPlatform("android")]
    [SupportedOSPlatform("android30.0")]
    [UnsupportedOSPlatform("ios")]
    [SupportedOSPlatform("ios12.0")]
#endif
    public virtual int UpsertRange(IEnumerable<T> collection, Action<SQLiteUpsertBuilder<T>> configure, bool runInTransaction = true)
    {
        if (Database.Options.OnActionHooks.Count == 0
            && !IsItemMethodOverridden(nameof(InsertItem))
            && !HasAnyDatabaseDefault())
        {
            (TableColumn[] columns, string sql) = GetUpsertInfo(configure);
            TableColumn? autoIncrement = GetAutoIncrementColumn();
            SQLiteOptions options = Database.Options;
            Action<sqlite3_stmt, T> bindRow = ResolveInsertBindRow(columns, autoIncrement, options);
            return RunPreparedRange(sql, collection, Database.Options.AddOrUpdateHooks, runInTransaction, bindRow, autoIncrement, detectInsertByRowIdChange: true);
        }

        return RunRange(Database.Options.AddOrUpdateHooks, collection, runInTransaction, item =>
        {
            SQLiteAction final = RunActionHooks(item, SQLiteAction.AddOrUpdate);
            return final == SQLiteAction.AddOrUpdate
                ? DefaultUpsert(item, configure)
                : DispatchAction(final, item);
        });
    }

    /// <summary>
    /// Wraps this table so the next entity write (<c>Add</c>, <c>Update</c>, or <c>Remove</c>)
    /// emits a SQLite <c>RETURNING *</c> clause and hands the written rows back to the caller.
    /// Useful when <c>INSERT</c>/<c>UPDATE</c>/<c>DELETE</c> triggers populate columns and you need
    /// to read the final row values atomically with the write.
    /// </summary>
    /// <remarks>
    /// <c>RETURNING</c> requires SQLite 3.35 or later.
    /// </remarks>
#if SQLITE_FRAMEWORK_OS_BUNDLED_SQLITE
    [UnsupportedOSPlatform("android")]
    [SupportedOSPlatform("android34.0")]
    [UnsupportedOSPlatform("ios")]
    [SupportedOSPlatform("ios15.0")]
#endif
    public virtual SQLiteReturningTable<T, T> Returning()
    {
        ParameterExpression parameter = Expression.Parameter(typeof(T), "x");
        Expression<Func<T, T>> identity = Expression.Lambda<Func<T, T>>(parameter, parameter);
        return new SQLiteReturningTable<T, T>(this, identity);
    }

    /// <summary>
    /// Wraps this table with a projection so the next entity write emits a
    /// <c>RETURNING *</c> clause and the result rows are projected through
    /// <paramref name="projection" /> client-side before being returned.
    /// </summary>
    /// <remarks>
    /// <c>RETURNING</c> requires SQLite 3.35 or later.
    /// </remarks>
#if SQLITE_FRAMEWORK_OS_BUNDLED_SQLITE
    [UnsupportedOSPlatform("android")]
    [SupportedOSPlatform("android34.0")]
    [UnsupportedOSPlatform("ios")]
    [SupportedOSPlatform("ios15.0")]
#endif
    public virtual SQLiteReturningTable<T, TResult> Returning<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] TResult>(Expression<Func<T, TResult>> projection)
    {
        ArgumentNullException.ThrowIfNull(projection);
        return new SQLiteReturningTable<T, TResult>(this, projection);
    }

    /// <summary>
    /// Wraps this table so the next write also writes the columns declared in <paramref name="build" />.
    /// This covers <c>Add</c>, <c>AddRange</c>, <c>Update</c>, <c>UpdateRange</c>, <c>AddOrUpdate</c>,
    /// <c>Upsert</c> (the inserted row), and the same writes through <see cref="Returning()" />. Use it
    /// to fill a column that has no CLR property, such as a shadow column declared with
    /// <see cref="SQLiteEntityTypeBuilder{T}.Column" />, or to override a mapped column with a database
    /// expression. The values are inlined into the generated SQL.
    /// </summary>
    public virtual SQLiteTable<T> WithColumns(Action<SQLiteWriteColumnsBuilder<T>> build)
    {
        ArgumentNullException.ThrowIfNull(build);
        SQLiteWriteColumnsBuilder<T> builder = new(Database, Table);
        build(builder);
        return new SQLiteWriteColumnsTable<T>(Database, Table, builder.Columns, builder.ReferencesRow);
    }

    /// <summary>
    /// Copies rows from <paramref name="source" /> into this table using a single
    /// <c>INSERT INTO ... SELECT</c> statement, so the data never round-trips through your code.
    /// The source must be a queryable from the same database (a table or a LINQ chain over one).
    /// All columns are inserted in the table's column order, so primary keys from the source are
    /// preserved. No <c>OnAdd</c> hooks fire for the inserted rows.
    /// </summary>
    /// <param name="source">The query whose rows will be inserted into this table.</param>
    /// <returns>The number of rows inserted.</returns>
    public virtual int InsertFromQuery(IQueryable<T> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (source is not BaseSQLiteQueryable sourceTable)
        {
            throw new InvalidOperationException($"Source must be a framework queryable (a {nameof(SQLiteTable)} or a LINQ chain over one).");
        }

        if (sourceTable.Database != Database)
        {
            throw new InvalidOperationException("Source must belong to the same database as the target.");
        }

        SQLTranslator translator = new(Database);
        SQLQuery sourceQuery = translator.Translate(source.Expression);

        IReadOnlyList<SQLiteExpression> selects = translator.Selects;
        IEnumerable<string> targetColumns = selects.Select(s =>
        {
            TableColumn column = Table.Columns.First(c => c.PropertyInfo.Name == s.IdentifierText);
            return IdentifierGuard.Quote(column.Name);
        });

        string columnList = string.Join(", ", targetColumns);
        string sql = $"INSERT INTO \"{Table.TableName}\" ({columnList}){Environment.NewLine}{sourceQuery.Sql}";

        return Database.CreateCommand(sql, sourceQuery.Parameters).ExecuteNonQuery();
    }

    /// <inheritdoc />
    public override IEnumerator GetEnumerator()
    {
        return ((IEnumerable<T>)this).GetEnumerator();
    }

    internal (TableColumn[] Columns, string Sql) GetAddInfoForItemInternal(T item)
    {
        (TableColumn[] columns, string sql) = GetAddInfo();
        if (!IsItemMethodOverridden(nameof(GetAddInfo)))
        {
            columns = FilterColumnsForDefaults(columns, item);
            sql = BuildAddSql(columns);
        }

        return (columns, sql);
    }

    internal (TableColumn[] Columns, TableColumn[] PrimaryColumns, string Sql) GetUpdateInfoInternal() => GetUpdateInfo();
    internal (TableColumn[] PrimaryColumns, string Sql) GetRemoveInfoInternal() => GetRemoveInfo();
    internal (TableColumn[] Columns, string Sql) GetUpsertInfoInternal(Action<SQLiteUpsertBuilder<T>> configure) => GetUpsertInfo(configure);
    internal (TableColumn[] Columns, string Sql) GetAddOrUpdateInfoInternal(SQLiteConflict conflict) => GetAddOrUpdateInfo(conflict);
    internal bool RunHooksInternal(IReadOnlyDictionary<Type, IReadOnlyList<Delegate>> hooks, T item) => RunHooks(hooks, item);

    internal SQLiteAction RunActionHooksInternal(T item, SQLiteAction startingAction) => RunActionHooks(item, startingAction);

    internal (TableColumn[] Columns, string Sql) FilterUpsertInfoForItemInternal(Action<SQLiteUpsertBuilder<T>> configure, T item, TableColumn[] baseColumns, string baseSql)
    {
        if (IsItemMethodOverridden(nameof(GetUpsertInfo)) || UpsertHasDoUpdate(configure))
        {
            return (baseColumns, baseSql);
        }

        TableColumn[] filtered = FilterColumnsForDefaults(baseColumns, item);
        return filtered.Length != baseColumns.Length
            ? (filtered, BuildUpsertSql(configure, filtered))
            : (baseColumns, baseSql);
    }

    internal TableColumn? GetAutoIncrementColumn()
    {
        return Table.Columns.FirstOrDefault(c => c.IsPrimaryKey && c.IsAutoIncrement);
    }

    /// <summary>
    /// Prepares <paramref name="sql" /> once, then loops over <paramref name="items" /> and binds /
    /// steps / resets per row. The connection lock and (optional) transaction are held for the whole
    /// loop so the prepared statement is reused without re-parsing. When <paramref name="autoIncrement" />
    /// is supplied, <c>last_insert_rowid</c> is read after every row that affected at least one row
    /// and written back to the entity.
    /// </summary>
    protected virtual int RunPreparedRange(string sql, IEnumerable<T> items, IReadOnlyDictionary<Type, IReadOnlyList<Delegate>> hooks, bool runInTransaction, Action<sqlite3_stmt, T> bindRow, TableColumn? autoIncrement = null, bool detectInsertByRowIdChange = false)
    {
        int count = 0;

        if (runInTransaction)
        {
            using SQLiteTransaction transaction = Database.BeginTransaction();
            Body();
            transaction.Commit();
        }
        else
        {
            Body();
        }

        return count;

        void Body()
        {
            Database.OpenConnection();
            using IDisposable _ = Database.Lock();

            SQLiteCommand command = Database.CreateCommand(sql, []);
            command.NotifyExecuting();

            sqlite3 handle = Database.GetActiveHandle();
            SQLiteResult prepareResult = (SQLiteResult)raw.sqlite3_prepare_v2(handle, sql, out sqlite3_stmt? stmt);
            if (prepareResult != SQLiteResult.OK)
            {
                SQLiteException prepareException = new(prepareResult, raw.sqlite3_errmsg(handle).utf8_to_string(), sql);
                command.NotifyFailed(prepareException);
                throw prepareException;
            }

            try
            {
                foreach (T item in items)
                {
                    if (!RunHooks(hooks, item))
                    {
                        continue;
                    }

                    bindRow(stmt, item);

                    long rowIdBefore = autoIncrement != null && detectInsertByRowIdChange
                        ? raw.sqlite3_last_insert_rowid(handle)
                        : 0L;

                    SQLiteResult stepResult = (SQLiteResult)raw.sqlite3_step(stmt);
                    if (stepResult != SQLiteResult.Done)
                    {
                        throw new SQLiteException(stepResult, raw.sqlite3_errmsg(handle).utf8_to_string(), sql);
                    }

                    int changes = raw.sqlite3_changes(handle);
                    count += changes;

                    if (autoIncrement != null)
                    {
                        long rowId = raw.sqlite3_last_insert_rowid(handle);
                        bool inserted = detectInsertByRowIdChange ? rowId != rowIdBefore : changes > 0;
                        if (inserted)
                        {
                            autoIncrement.PropertyInfo.SetValue(item, ConvertRowIdToType(rowId, autoIncrement.PropertyType));
                        }
                    }

                    raw.sqlite3_reset(stmt);
                }

                command.NotifyExecuted(count);
            }
            catch (Exception exception)
            {
                command.NotifyFailed(exception);
                throw;
            }
            finally
            {
                raw.sqlite3_finalize(stmt);
            }
        }
    }

    /// <summary>
    /// Returns the columns to bind and the <c>INSERT INTO</c> SQL used by <see cref="Add" /> and
    /// <see cref="AddRange" />. Auto-increment primary keys are excluded from the binding set so
    /// that SQLite can populate them, unless
    /// <see cref="SQLiteOptions.ExplicitAutoIncrementKeysPreserved" /> is <see langword="true" />,
    /// in which case the column is included and a non-default value on the entity is used directly.
    /// Override to change the SQL shape (for example, to use <c>INSERT OR IGNORE</c>) or to filter
    /// the column list.
    /// </summary>
    protected virtual (TableColumn[] Columns, string Sql) GetAddInfo()
    {
        TableColumn[] columns = Database.Options.ExplicitAutoIncrementKeysPreserved
            ? Table.Columns.ToArray()
            : Table.Columns.Where(f => !f.IsPrimaryKey || !f.IsAutoIncrement).ToArray();
        columns = ExcludeComputedColumns(columns);
        columns = ExcludeOverriddenColumns(columns);

        return (columns, BuildAddSql(columns));
    }

    /// <summary>
    /// Returns the columns to update, the primary-key columns to match by, and the
    /// <c>UPDATE</c> SQL used by <see cref="Update" /> and <see cref="UpdateRange" />. Override
    /// to add columns to the <c>SET</c> clause (for example, an <c>UpdatedAt</c> shadow column)
    /// or to change the WHERE shape.
    /// </summary>
    protected virtual (TableColumn[] Columns, TableColumn[] PrimaryColumns, string Sql) GetUpdateInfo()
    {
        TableColumn[] columns = Table.Columns
            .Where(f => !f.IsPrimaryKey || !f.IsAutoIncrement)
            .ToArray();
        columns = ExcludeComputedColumns(columns);
        columns = ExcludeOverriddenColumns(columns);

        TableColumn[] primaryKeyColumns = Table.Columns
            .Where(f => f.IsPrimaryKey)
            .ToArray();

        if (primaryKeyColumns.Length == 0)
        {
            throw new NotSupportedException("Cannot perform an update operation without a primary key, use ExecuteUpdate instead.");
        }

        string setClause = string.Join(", ", columns.Select((c, i) => $"{IdentifierGuard.Quote(c.Name)} = {WrapParam($"@p{i}", c)}"));

        IReadOnlyList<(string Column, string ValueSql)> extra = ExtraWriteColumns;
        if (extra.Count > 0)
        {
            string extraClause = string.Join(", ", extra.Select(e => $"{IdentifierGuard.Quote(e.Column)} = {e.ValueSql}"));
            setClause = setClause.Length == 0 ? extraClause : setClause + ", " + extraClause;
        }

        if (setClause.Length == 0)
        {
            string[] selfAssignments = new string[primaryKeyColumns.Length];
            for (int i = 0; i < primaryKeyColumns.Length; i++)
            {
                string quoted = IdentifierGuard.Quote(primaryKeyColumns[i].Name);
                selfAssignments[i] = quoted + " = " + quoted;
            }

            setClause = string.Join(", ", selfAssignments);
        }

        string primaryKeyClause = string.Join(" AND ",
            primaryKeyColumns.Select((c, i) => $"{IdentifierGuard.Quote(c.Name)} = @p{i + columns.Length}")
        );
        string sql = $"UPDATE \"{Table.TableName}\" SET {setClause} WHERE {primaryKeyClause}";

        return (columns, primaryKeyColumns, sql);
    }

    /// <summary>
    /// Returns the primary-key columns and the <c>DELETE FROM</c> SQL used by
    /// <see cref="Remove" /> and <see cref="RemoveRange" />. Override to implement soft delete
    /// (return an <c>UPDATE</c> statement that sets a <c>Deleted</c> flag instead) or to match
    /// rows by a different column set.
    /// </summary>
    protected virtual (TableColumn[] PrimaryColumns, string Sql) GetRemoveInfo()
    {
        TableColumn[] primaryKeyColumns = Table.Columns
            .Where(f => f.IsPrimaryKey)
            .ToArray();

        if (primaryKeyColumns.Length == 0)
        {
            throw new NotSupportedException("Cannot perform a delete operation without a primary key, use ExecuteDelete instead.");
        }

        string primaryKeyClause = string.Join(" AND ",
            primaryKeyColumns.Select((c, i) => $"{IdentifierGuard.Quote(c.Name)} = @p{i}")
        );
        string sql = $"DELETE FROM \"{Table.TableName}\" WHERE {primaryKeyClause}";

        return (primaryKeyColumns, sql);
    }

    /// <summary>
    /// Returns the columns to bind and the <c>INSERT OR REPLACE INTO</c> SQL used by
    /// <see cref="AddOrUpdate" /> and <see cref="AddOrUpdateRange" />. The auto-increment primary
    /// key (when present) is included in the column list so the caller can either supply an
    /// explicit value (replacing the matching row, or inserting at that key) or leave it at the
    /// type default to let SQLite assign one. Override to change the upsert SQL, for example to
    /// use SQLite's <c>ON CONFLICT</c> syntax instead.
    /// </summary>
    protected virtual (TableColumn[] Columns, string Sql) GetAddOrUpdateInfo(SQLiteConflict conflict)
    {
        TableColumn[] columns = ExcludeOverriddenColumns(ExcludeComputedColumns(Table.Columns.ToArray()));
        string sql = BuildAddOrUpdateSql(columns, conflict);

        return (columns, sql);
    }

    /// <summary>
    /// Returns the columns to bind and the <c>INSERT INTO ... ON CONFLICT (...) DO ...</c> SQL
    /// produced by configuring an <see cref="SQLiteUpsertBuilder{T}" />. Override to change the SQL shape
    /// produced by <see cref="Upsert" /> and <see cref="UpsertRange" />.
    /// </summary>
    protected virtual (TableColumn[] Columns, string Sql) GetUpsertInfo(Action<SQLiteUpsertBuilder<T>> configure)
    {
#if SQLITE_FRAMEWORK_VERSION_AWARE
        Database.Options.EnsureMinimumVersion(SQLiteMinimumVersion.V3_24, "UPSERT (INSERT ... ON CONFLICT ... DO ...)");
#endif
        SQLiteUpsertBuilder<T> builder = new();
        configure(builder);
        SQLiteUpsertConflictTarget<T> target = builder.Build();
        if (ExtraWriteColumns.Count > 0)
        {
            ThrowIfExtraWriteColumnsReferenceRowOnInsert();
        }
        return UpsertSqlBuilder.Build(Database, Table, target, (c, p) => WrapParam(p, c), ExtraWriteColumns);
    }

    /// <summary>
    /// Runs the per-entity hooks for <typeparamref name="T" /> stored on the database options.
    /// Each hook can mutate <paramref name="item" />. Returns <see langword="false" /> when any
    /// hook returns <see langword="false" />, signalling that the default operation should be skipped.
    /// </summary>
    protected virtual bool RunHooks(IReadOnlyDictionary<Type, IReadOnlyList<Delegate>> hooks, T item)
    {
        if (hooks.Count == 0)
        {
            return true;
        }

        return RunHooks(hooks, item, new Dictionary<string, object?>());
    }

    /// <summary>
    /// Runs the per-entity hooks for <typeparamref name="T" />, passing <paramref name="columns" />
    /// to the hooks that accept a column collector so they can set values for columns that have no
    /// CLR property. Returns <see langword="false" /> when a hook cancels the operation.
    /// </summary>
    protected virtual bool RunHooks(IReadOnlyDictionary<Type, IReadOnlyList<Delegate>> hooks, T item, IDictionary<string, object?> columns)
    {
        if (!hooks.TryGetValue(typeof(T), out IReadOnlyList<Delegate>? list))
        {
            return true;
        }

        foreach (Delegate hook in list)
        {
            bool keep = hook is Func<SQLiteDatabase, T, IDictionary<string, object?>, bool> columnHook
                ? columnHook(Database, item, columns)
                : ((Func<SQLiteDatabase, T, bool>)hook)(Database, item);
            if (!keep)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Loops over <paramref name="collection" />, runs the per-row <paramref name="hooks" />, and
    /// invokes <paramref name="execute" /> for every item that the hooks did not cancel. Wraps the
    /// loop in a transaction when <paramref name="runInTransaction" /> is set.
    /// </summary>
    protected virtual int RunRange(IReadOnlyDictionary<Type, IReadOnlyList<Delegate>> hooks, IEnumerable<T> collection, bool runInTransaction, Func<T, int> execute)
    {
        int count = 0;

        if (runInTransaction)
        {
            using SQLiteTransaction transaction = Database.BeginTransaction();
            Body();
            transaction.Commit();
        }
        else
        {
            Body();
        }

        return count;

        void Body()
        {
            foreach (T item in collection)
            {
                if (!RunHooks(hooks, item))
                {
                    continue;
                }

                count += execute(item);
            }
        }
    }

    /// <summary>
    /// Runs the cross-cutting <see cref="SQLiteOptions.OnActionHooks" /> for <paramref name="item" />,
    /// starting from <paramref name="startingAction" />. Each hook receives the action returned by
    /// the previous one. Returns the final action chosen by the chain.
    /// </summary>
    protected virtual SQLiteAction RunActionHooks(T item, SQLiteAction startingAction)
    {
        IReadOnlyList<SQLiteActionHook> hooks = Database.Options.OnActionHooks;
        SQLiteAction action = startingAction;
        foreach (SQLiteActionHook hook in hooks)
        {
            action = hook(Database, item!, action);
        }

        return action;
    }

    /// <summary>
    /// Runs the default work for the given <paramref name="action" />. <see cref="SQLiteAction.Skip" />
    /// returns <c>0</c> without touching the database. The other values map to the standard
    /// <c>INSERT</c>, <c>UPDATE</c>, <c>DELETE</c>, or <c>INSERT OR REPLACE</c> paths.
    /// </summary>
    protected virtual int DispatchAction(SQLiteAction action, T item)
    {
        return action switch
        {
            SQLiteAction.Skip => 0,
            SQLiteAction.Add => DefaultAdd(item),
            SQLiteAction.Update => DefaultUpdate(item),
            SQLiteAction.Remove => DefaultRemove(item),
            SQLiteAction.AddOrUpdate => DefaultAddOrUpdate(item, SQLiteConflict.Replace),
            _ => throw new InvalidOperationException($"Unsupported SQLiteAction value: {action}"),
        };
    }

    /// <summary>
    /// Runs the default INSERT for <paramref name="item" />. Used by
    /// <see cref="DispatchAction" /> when the action hook chain settles on
    /// <see cref="SQLiteAction.Add" />. Override to change how <c>Add</c> resolves once dispatch
    /// has decided on it.
    /// </summary>
    protected virtual int DefaultAdd(T item)
    {
        TableWriteCache<T>? cache = ResolveWriteCache();
        if (cache != null)
        {
            TableWriteCacheEntry<T> entry = cache.Add ??= BuildAddEntry();
            TableColumn[] filtered = FilterColumnsForDefaults(entry.Columns, item);
            return ReferenceEquals(filtered, entry.Columns)
                ? ExecutePreparedWrite(entry.Sql, entry.BindRow, item, entry.AutoIncrement)
                : InsertItem(filtered, BuildAddSql(filtered), item);
        }

        (TableColumn[] columns, string sql) = GetAddInfo();
        if (!IsItemMethodOverridden(nameof(GetAddInfo)))
        {
            columns = FilterColumnsForDefaults(columns, item);
            sql = BuildAddSql(columns);
        }
        return InsertItem(columns, sql, item);
    }

    /// <summary>
    /// Runs the default UPDATE for <paramref name="item" />. Used by
    /// <see cref="DispatchAction" /> when the action hook chain settles on
    /// <see cref="SQLiteAction.Update" />.
    /// </summary>
    protected virtual int DefaultUpdate(T item)
    {
        TableWriteCache<T>? cache = ResolveWriteCache();
        if (cache != null)
        {
            TableWriteCacheEntry<T> entry = cache.Update ??= BuildUpdateEntry();
            return ExecutePreparedWrite(entry.Sql, entry.BindRow, item, null);
        }

        (TableColumn[] columns, TableColumn[] primaryKeyColumns, string sql) = GetUpdateInfo();
        return UpdateItem(columns, primaryKeyColumns, sql, item);
    }

    /// <summary>
    /// Runs the default DELETE for <paramref name="item" />. Used by
    /// <see cref="DispatchAction" /> when the action hook chain settles on
    /// <see cref="SQLiteAction.Remove" />.
    /// </summary>
    protected virtual int DefaultRemove(T item)
    {
        TableWriteCache<T>? cache = ResolveWriteCache();
        if (cache != null)
        {
            TableWriteCacheEntry<T> entry = cache.Remove ??= BuildRemoveEntry();
            return ExecutePreparedWrite(entry.Sql, entry.BindRow, item, null);
        }

        (TableColumn[] primaryKeyColumns, string sql) = GetRemoveInfo();
        return AddOrRemoveItem(primaryKeyColumns, sql, item);
    }

    /// <summary>
    /// Runs the default <c>INSERT OR &lt;conflict&gt;</c> for <paramref name="item" />. Used by
    /// <see cref="DispatchAction" /> when the action hook chain settles on
    /// <see cref="SQLiteAction.AddOrUpdate" />.
    /// </summary>
    protected virtual int DefaultAddOrUpdate(T item, SQLiteConflict conflict)
    {
        TableWriteCache<T>? cache = ResolveWriteCache();
        if (cache != null)
        {
            int slot = conflict >= SQLiteConflict.Replace && conflict <= SQLiteConflict.Rollback
                ? (int)conflict
                : (int)SQLiteConflict.Replace;
            TableWriteCacheEntry<T> entry = cache.AddOrUpdate[slot] ??= BuildAddOrUpdateEntry(conflict);
            TableColumn[] filtered = FilterColumnsForDefaults(entry.Columns, item);
            return ReferenceEquals(filtered, entry.Columns)
                ? ExecutePreparedWrite(entry.Sql, entry.BindRow, item, entry.AutoIncrement)
                : InsertItem(filtered, BuildAddOrUpdateSql(filtered, conflict), item);
        }

        (TableColumn[] columns, string sql) = GetAddOrUpdateInfo(conflict);
        if (!IsItemMethodOverridden(nameof(GetAddOrUpdateInfo)))
        {
            columns = FilterColumnsForDefaults(columns, item);
            sql = BuildAddOrUpdateSql(columns, conflict);
        }
        return InsertItem(columns, sql, item);
    }

    /// <summary>
    /// Runs the configured <c>Upsert</c> for <paramref name="item" />. Used by <c>Upsert</c> and
    /// <c>UpsertRange</c> when the action hook chain leaves the action as
    /// <see cref="SQLiteAction.AddOrUpdate" /> so the configured <c>ON CONFLICT</c> shape is preserved.
    /// </summary>
    protected virtual int DefaultUpsert(T item, Action<SQLiteUpsertBuilder<T>> configure)
    {
        (TableColumn[] columns, string sql) = GetUpsertInfo(configure);
        if (!IsItemMethodOverridden(nameof(GetUpsertInfo)) && !UpsertHasDoUpdate(configure))
        {
            TableColumn[] filtered = FilterColumnsForDefaults(columns, item);
            if (filtered.Length != columns.Length)
            {
                columns = filtered;
                sql = BuildUpsertSql(configure, filtered);
            }
        }
        return InsertItem(columns, sql, item, detectInsertByRowIdChange: true);
    }

    /// <summary>
    /// Returns the SQL fragment used as the right-hand side of a column binding for the given
    /// placeholder. By default this is the placeholder itself, unless the column has a registered
    /// <see cref="ISQLiteTypeConverter" /> with a custom <c>ParameterSqlExpression</c>. Override
    /// to wrap parameters with custom SQL functions (for example, <c>jsonb(@p0)</c> or
    /// <c>CAST(@p0 AS BLOB)</c>).
    /// </summary>
    protected virtual string WrapParam(string placeholder, TableColumn column)
    {
        if (Database.Options.TypeConverters.TryGetValue(column.PropertyType, out ISQLiteTypeConverter? conv)
            && conv.ParameterSqlExpression is { } paramExpr)
        {
            return string.Format(paramExpr, placeholder);
        }

        return placeholder;
    }

    /// <summary>
    /// Binds the values for <paramref name="item" /> and runs an INSERT. When the table has an
    /// auto-increment primary key column, the assigned rowid is read on the same connection and
    /// written back to <paramref name="item" /> so the caller sees the new key. If the
    /// auto-increment primary key column is part of <paramref name="columns" /> and the entity's
    /// value for it is the type default (e.g. <c>0</c>), it is bound as <c>NULL</c> so SQLite
    /// generates a fresh key.
    /// </summary>
    protected virtual int InsertItem(TableColumn[] columns, string sql, T item, bool detectInsertByRowIdChange = false)
    {
        TableColumn? autoIncrement = GetAutoIncrementColumn();

        List<SQLiteParameter> parameters = columns
            .Select((c, i) =>
            {
                object? value = c.PropertyInfo.GetValue(item);
                if (c == autoIncrement && IsAutoIncrementUnset(value))
                {
                    value = null;
                }
                return new SQLiteParameter { Name = $"@p{i}", Value = value };
            })
            .ToList();

        if (autoIncrement == null)
        {
            return Database.CreateCommand(sql, parameters).ExecuteNonQuery();
        }

        (int changes, long rowId, bool rowIdChanged) = Database.CreateCommand(sql, parameters).ExecuteWithInsertDetection();
        bool inserted = detectInsertByRowIdChange ? rowIdChanged : changes > 0;
        if (inserted)
        {
            autoIncrement.PropertyInfo.SetValue(item, ConvertRowIdToType(rowId, autoIncrement.PropertyType));
        }

        return changes;
    }

    /// <summary>
    /// Binds the values of <paramref name="columns" /> on <paramref name="item" /> and executes
    /// <paramref name="sql" />. Used by <see cref="Remove" /> and <see cref="RemoveRange" />.
    /// Override to mutate the entity right before binding or to log every write.
    /// </summary>
    protected virtual int AddOrRemoveItem(TableColumn[] columns, string sql, T item)
    {
        List<SQLiteParameter> parameters = columns
            .Select((c, i) => new SQLiteParameter
            {
                Name = $"@p{i}",
                Value = c.PropertyInfo.GetValue(item)
            })
            .ToList();

        return Database.CreateCommand(sql, parameters).ExecuteNonQuery();
    }

    /// <summary>
    /// Binds the SET-clause values from <paramref name="columns" /> followed by the WHERE-clause
    /// values from <paramref name="primaryColumns" /> on <paramref name="item" /> and executes
    /// <paramref name="sql" />. Used by <see cref="Update" /> and <see cref="UpdateRange" />.
    /// Override to mutate the entity right before binding (for example, to stamp <c>UpdatedAt</c>).
    /// </summary>
    protected virtual int UpdateItem(TableColumn[] columns, TableColumn[] primaryColumns, string sql, T item)
    {
        IEnumerable<SQLiteParameter> primaryParameters = primaryColumns
            .Select((c, i) => new SQLiteParameter
            {
                Name = $"@p{i + columns.Length}",
                Value = c.PropertyInfo.GetValue(item)
            });

        List<SQLiteParameter> parameters = columns
            .Select((c, i) => new SQLiteParameter
            {
                Name = $"@p{i}",
                Value = c.PropertyInfo.GetValue(item)
            })
            .Concat(primaryParameters)
            .ToList();

        return Database.CreateCommand(sql, parameters).ExecuteNonQuery();
    }

    IEnumerator<T> IEnumerable<T>.GetEnumerator()
    {
        return Database.ExecuteSequenceQuery<T>(Expression).GetEnumerator();
    }

    private void ThrowIfExtraWriteColumnsReferenceRowOnInsert()
    {
        if (ExtraWriteColumnsReferenceRow)
        {
            throw new NotSupportedException(
                "A WithColumns value expression reads a column of the row, which an Add cannot do " +
                "because the row does not exist yet. Use a constant or a function such as " +
                "'_ => SQLiteFunctions.UnixEpoch()', or set the value on an Update instead.");
        }
    }

    [UnconditionalSuppressMessage("AOT", "IL2075", Justification = "Reflects the subclass only to detect a protected override.")]
    private bool IsItemMethodOverridden(string methodName)
    {
        Type runtime = GetType();
        if (runtime == typeof(SQLiteTable<T>))
        {
            return false;
        }

        MethodInfo method = runtime.GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)!;
        return method.DeclaringType != typeof(SQLiteTable<T>);
    }

    private TableWriteCache<T>? ResolveWriteCache()
    {
        if (GetType() != typeof(SQLiteTable<T>)
            || !Database.ModelFrozen
            || Database.Options.CommandInterceptors.Count != 0)
        {
            return null;
        }

        if (Table.SingleWriteCache is TableWriteCache<T> cache && ReferenceEquals(cache.Options, Database.Options))
        {
            return cache;
        }

        TableWriteCache<T> created = new(Database.Options);
        Table.SingleWriteCache = created;
        return created;
    }

    private TableWriteCacheEntry<T> BuildAddEntry()
    {
        (TableColumn[] columns, string sql) = GetAddInfo();
        TableColumn? autoIncrement = GetAutoIncrementColumn();
        return new TableWriteCacheEntry<T>
        {
            Columns = columns,
            Sql = sql,
            BindRow = ResolveInsertBindRow(columns, autoIncrement, Database.Options),
            AutoIncrement = autoIncrement,
        };
    }

    private TableWriteCacheEntry<T> BuildAddOrUpdateEntry(SQLiteConflict conflict)
    {
        (TableColumn[] columns, string sql) = GetAddOrUpdateInfo(conflict);
        TableColumn? autoIncrement = GetAutoIncrementColumn();
        return new TableWriteCacheEntry<T>
        {
            Columns = columns,
            Sql = sql,
            BindRow = ResolveInsertBindRow(columns, autoIncrement, Database.Options),
            AutoIncrement = autoIncrement,
        };
    }

    private TableWriteCacheEntry<T> BuildUpdateEntry()
    {
        (TableColumn[] columns, TableColumn[] primaryKeyColumns, string sql) = GetUpdateInfo();
        Action<sqlite3_stmt, T> bindData = ResolveBindRow(columns, 0, Database.Options);
        Action<sqlite3_stmt, T> bindKeys = ResolveBindRow(primaryKeyColumns, columns.Length, Database.Options);
        return new TableWriteCacheEntry<T>
        {
            Columns = columns,
            Sql = sql,
            BindRow = (stmt, item) =>
            {
                bindData(stmt, item);
                bindKeys(stmt, item);
            },
        };
    }

    private TableWriteCacheEntry<T> BuildRemoveEntry()
    {
        (TableColumn[] primaryKeyColumns, string sql) = GetRemoveInfo();
        return new TableWriteCacheEntry<T>
        {
            Columns = primaryKeyColumns,
            Sql = sql,
            BindRow = ResolveBindRow(primaryKeyColumns, 0, Database.Options),
        };
    }

    private int ExecutePreparedWrite(string sql, Action<sqlite3_stmt, T> bindRow, T item, TableColumn? autoIncrement)
    {
        Database.OpenConnection();
        using IDisposable _ = Database.Lock();

        sqlite3 handle = Database.GetActiveHandle();
        sqlite3_stmt statement = Database.RentStatement(sql);
        try
        {
            bindRow(statement, item);

            SQLiteResult stepResult = (SQLiteResult)raw.sqlite3_step(statement);
            if (stepResult != SQLiteResult.Done)
            {
                throw new SQLiteException(stepResult, raw.sqlite3_errmsg(handle).utf8_to_string(), sql);
            }

            int changes = raw.sqlite3_changes(handle);
            if (autoIncrement != null && changes > 0)
            {
                long rowId = raw.sqlite3_last_insert_rowid(handle);
                autoIncrement.PropertyInfo.SetValue(item, ConvertRowIdToType(rowId, autoIncrement.PropertyType));
            }

            return changes;
        }
        finally
        {
            Database.ReturnStatement(sql, statement);
        }
    }

    private bool HasAnyDatabaseDefault()
    {
        return hasAnyDatabaseDefault ??= Table.Columns.Any(c => c.HasDatabaseDefault);
    }

    private TableColumn[] FilterColumnsForDefaults(TableColumn[] baseColumns, T item)
    {
        if (!HasAnyDatabaseDefault())
        {
            return baseColumns;
        }

        List<TableColumn>? filtered = null;
        for (int i = 0; i < baseColumns.Length; i++)
        {
            TableColumn col = baseColumns[i];
            bool skip = col.HasDatabaseDefault && Equals(col.ClrDefaultBox, col.PropertyInfo.GetValue(item));
            if (!skip)
            {
                filtered?.Add(col);
                continue;
            }

            if (filtered == null)
            {
                filtered = new List<TableColumn>(baseColumns.Length - 1);
                for (int j = 0; j < i; j++)
                {
                    filtered.Add(baseColumns[j]);
                }
            }
        }

        return filtered?.ToArray() ?? baseColumns;
    }

    private TableColumn[] ExcludeOverriddenColumns(TableColumn[] columns)
    {
        IReadOnlyList<(string Column, string ValueSql)> extra = ExtraWriteColumns;
        if (extra.Count == 0)
        {
            return columns;
        }

        HashSet<string> overridden = extra.Select(e => e.Column).ToHashSet();
        return columns.Where(c => !overridden.Contains(c.Name)).ToArray();
    }

    private TableColumn[] ExcludeComputedColumns(TableColumn[] columns)
    {
        IReadOnlyList<ComputedColumnSpec> computed = Table.ComputedColumns;
        if (computed.Count == 0)
        {
            return columns;
        }

        HashSet<string> computedNames = computed.Select(c => c.Column.Name).ToHashSet();
        return columns.Where(c => !computedNames.Contains(c.Name)).ToArray();
    }

    private (string ColumnList, string ValueList) BuildWriteLists(TableColumn[] columns)
    {
        ThrowIfExtraWriteColumnsReferenceRowOnInsert();

        IEnumerable<string> names = columns.Select(c => IdentifierGuard.Quote(c.Name));
        IEnumerable<string> values = columns.Select((c, i) => WrapParam($"@p{i}", c));

        IReadOnlyList<(string Column, string ValueSql)> extra = ExtraWriteColumns;
        if (extra.Count > 0)
        {
            names = names.Concat(extra.Select(e => IdentifierGuard.Quote(e.Column)));
            values = values.Concat(extra.Select(e => e.ValueSql));
        }

        return (string.Join(", ", names), string.Join(", ", values));
    }

    private string BuildAddSql(TableColumn[] columns)
    {
        (string columnList, string paramList) = BuildWriteLists(columns);
        if (columnList.Length == 0)
        {
            return $"INSERT INTO \"{Table.TableName}\" DEFAULT VALUES";
        }

        return $"INSERT INTO \"{Table.TableName}\" ({columnList}) VALUES ({paramList})";
    }

    private string BuildAddOrUpdateSql(TableColumn[] columns, SQLiteConflict conflict)
    {
        string action = conflict switch
        {
            SQLiteConflict.Replace => "OR REPLACE ",
            SQLiteConflict.Ignore => "OR IGNORE ",
            SQLiteConflict.Abort => "OR ABORT ",
            SQLiteConflict.Fail => "OR FAIL ",
            SQLiteConflict.Rollback => "OR ROLLBACK ",
            _ => "OR REPLACE ",
        };
        (string columnList, string paramList) = BuildWriteLists(columns);
        if (columnList.Length == 0)
        {
            return $"INSERT {action}INTO \"{Table.TableName}\" DEFAULT VALUES";
        }

        return $"INSERT {action}INTO \"{Table.TableName}\" ({columnList}) VALUES ({paramList})";
    }

    private int RunRangeWithColumns(IReadOnlyDictionary<Type, IReadOnlyList<Delegate>> hooks, IEnumerable<T> collection, bool runInTransaction, SQLiteAction defaultAction)
    {
        int count = 0;

        if (runInTransaction)
        {
            using SQLiteTransaction transaction = Database.BeginTransaction();
            Body();
            transaction.Commit();
        }
        else
        {
            Body();
        }

        return count;

        void Body()
        {
            foreach (T item in collection)
            {
                Dictionary<string, object?> columns = [];
                if (!RunHooks(hooks, item, columns))
                {
                    continue;
                }

                SQLiteAction action = RunActionHooks(item, defaultAction);
                if (action != defaultAction)
                {
                    count += DispatchAction(action, item);
                }
                else
                {
                    count += defaultAction == SQLiteAction.Add
                        ? InsertWithExtraColumns(item, columns)
                        : UpdateWithExtraColumns(item, columns);
                }
            }
        }
    }

    private int InsertWithExtraColumns(T item, IDictionary<string, object?> extra)
    {
        (TableColumn[] baseColumns, _) = GetAddInfo();
        baseColumns = FilterColumnsForDefaults(baseColumns, item);

        TableColumn? autoIncrement = GetAutoIncrementColumn();
        HashSet<string> overridden = extra.Keys.ToHashSet();
        TableColumn[] entityColumns = baseColumns.Where(c => !overridden.Contains(c.Name)).ToArray();

        List<SQLiteParameter> parameters = new(entityColumns.Length + extra.Count);
        List<string> names = new(entityColumns.Length + extra.Count);
        List<string> placeholders = new(entityColumns.Length + extra.Count);

        for (int i = 0; i < entityColumns.Length; i++)
        {
            TableColumn column = entityColumns[i];
            object? value = column.PropertyInfo.GetValue(item);
            if (column == autoIncrement && IsAutoIncrementUnset(value))
            {
                value = null;
            }
            string placeholder = $"@p{i}";
            parameters.Add(new SQLiteParameter { Name = placeholder, Value = value });
            names.Add(IdentifierGuard.Quote(column.Name));
            placeholders.Add(WrapParam(placeholder, column));
        }

        int next = entityColumns.Length;
        foreach (KeyValuePair<string, object?> entry in extra)
        {
            string placeholder = $"@p{next++}";
            parameters.Add(new SQLiteParameter { Name = placeholder, Value = entry.Value });
            names.Add(IdentifierGuard.Quote(entry.Key));
            placeholders.Add(placeholder);
        }

        IReadOnlyList<(string Column, string ValueSql)> withColumns = ExtraWriteColumns;
        if (withColumns.Count > 0)
        {
            ThrowIfExtraWriteColumnsReferenceRowOnInsert();
            foreach ((string column, string valueSql) in withColumns)
            {
                if (overridden.Contains(column))
                {
                    continue;
                }

                names.Add(IdentifierGuard.Quote(column));
                placeholders.Add(valueSql);
            }
        }

        string sql = names.Count == 0
            ? $"INSERT INTO \"{Table.TableName}\" DEFAULT VALUES"
            : $"INSERT INTO \"{Table.TableName}\" ({string.Join(", ", names)}) VALUES ({string.Join(", ", placeholders)})";

        if (autoIncrement == null)
        {
            return Database.CreateCommand(sql, parameters).ExecuteNonQuery();
        }

        (int changes, long rowId) = Database.CreateCommand(sql, parameters).ExecuteWithLastRowId();
        if (changes > 0)
        {
            autoIncrement.PropertyInfo.SetValue(item, ConvertRowIdToType(rowId, autoIncrement.PropertyType));
        }

        return changes;
    }

    private int UpdateWithExtraColumns(T item, IDictionary<string, object?> extra)
    {
        (TableColumn[] baseColumns, TableColumn[] primaryColumns, _) = GetUpdateInfo();
        HashSet<string> overridden = extra.Keys.ToHashSet();
        TableColumn[] setColumns = baseColumns.Where(c => !overridden.Contains(c.Name)).ToArray();

        List<SQLiteParameter> parameters = [];
        List<string> setClauses = [];

        for (int i = 0; i < setColumns.Length; i++)
        {
            TableColumn column = setColumns[i];
            string placeholder = $"@p{i}";
            parameters.Add(new SQLiteParameter { Name = placeholder, Value = column.PropertyInfo.GetValue(item) });
            setClauses.Add($"{IdentifierGuard.Quote(column.Name)} = {WrapParam(placeholder, column)}");
        }

        int next = setColumns.Length;
        foreach (KeyValuePair<string, object?> entry in extra)
        {
            string placeholder = $"@p{next++}";
            parameters.Add(new SQLiteParameter { Name = placeholder, Value = entry.Value });
            setClauses.Add($"{IdentifierGuard.Quote(entry.Key)} = {placeholder}");
        }

        foreach ((string column, string valueSql) in ExtraWriteColumns)
        {
            if (overridden.Contains(column))
            {
                continue;
            }

            setClauses.Add($"{IdentifierGuard.Quote(column)} = {valueSql}");
        }

        if (setClauses.Count == 0)
        {
            foreach (TableColumn primaryColumn in primaryColumns)
            {
                string quoted = IdentifierGuard.Quote(primaryColumn.Name);
                setClauses.Add($"{quoted} = {quoted}");
            }
        }

        List<string> primaryKeyClauses = [];
        for (int i = 0; i < primaryColumns.Length; i++)
        {
            string placeholder = $"@p{next++}";
            parameters.Add(new SQLiteParameter { Name = placeholder, Value = primaryColumns[i].PropertyInfo.GetValue(item) });
            primaryKeyClauses.Add($"{IdentifierGuard.Quote(primaryColumns[i].Name)} = {placeholder}");
        }

        string sql = $"UPDATE \"{Table.TableName}\" SET {string.Join(", ", setClauses)} WHERE {string.Join(" AND ", primaryKeyClauses)}";
        return Database.CreateCommand(sql, parameters).ExecuteNonQuery();
    }

    private string BuildUpsertSql(Action<SQLiteUpsertBuilder<T>> configure, TableColumn[] insertOverride)
    {
        SQLiteUpsertBuilder<T> builder = new();
        configure(builder);
        SQLiteUpsertConflictTarget<T> target = builder.Build();
        return UpsertSqlBuilder.Build(Database, Table, target, (c, p) => WrapParam(p, c), ExtraWriteColumns, insertOverride).Sql;
    }

    private static bool UpsertHasDoUpdate(Action<SQLiteUpsertBuilder<T>> configure)
    {
        SQLiteUpsertBuilder<T> builder = new();
        configure(builder);
        return builder.Build().ResolvedAction.Kind != UpsertActionKind.DoNothing;
    }

    private static bool HasColumnHooks(IReadOnlyDictionary<Type, IReadOnlyList<Delegate>> hooks)
    {
        return hooks.TryGetValue(typeof(T), out IReadOnlyList<Delegate>? list)
            && list.Any(h => h is Func<SQLiteDatabase, T, IDictionary<string, object?>, bool>);
    }

    private static Action<sqlite3_stmt, T> ResolveBindRow(TableColumn[] columns, int firstParameterIndex, SQLiteOptions options)
    {
        if (options.EntityWriters.TryGetValue(typeof(T), out IReadOnlyDictionary<string, SQLiteEntityColumnWriter>? writers))
        {
            SQLiteEntityColumnWriter[] resolved = new SQLiteEntityColumnWriter[columns.Length];
            bool allResolved = true;
            for (int i = 0; i < columns.Length; i++)
            {
                if (!writers.TryGetValue(columns[i].PropertyInfo.Name, out SQLiteEntityColumnWriter? writer))
                {
                    allResolved = false;
                    break;
                }
                resolved[i] = writer;
            }

            if (allResolved)
            {
                return (stmt, item) =>
                {
                    for (int i = 0; i < resolved.Length; i++)
                    {
                        resolved[i](stmt, firstParameterIndex + i + 1, item!, options);
                    }
                };
            }
        }

        return (stmt, item) =>
        {
            for (int i = 0; i < columns.Length; i++)
            {
                CommandHelpers.BindParameterByIndex(stmt, firstParameterIndex + i + 1, columns[i].PropertyInfo.GetValue(item), options);
            }
        };
    }

    private static Action<sqlite3_stmt, T> ResolveInsertBindRow(TableColumn[] columns, TableColumn? autoIncrement, SQLiteOptions options)
    {
        if (autoIncrement == null || Array.IndexOf(columns, autoIncrement) < 0)
        {
            return ResolveBindRow(columns, 0, options);
        }

        return (stmt, item) =>
        {
            for (int i = 0; i < columns.Length; i++)
            {
                TableColumn column = columns[i];
                object? value = column.PropertyInfo.GetValue(item);
                if (column == autoIncrement && IsAutoIncrementUnset(value))
                {
                    value = null;
                }
                CommandHelpers.BindParameterByIndex(stmt, i + 1, value, options);
            }
        };
    }

    private static bool IsAutoIncrementUnset(object? value)
    {
        return value is ulong unsignedValue
            ? unsignedValue == 0UL
            : Convert.ToInt64(value, CultureInfo.InvariantCulture) == 0L;
    }

    private static object ConvertRowIdToType(long rowId, Type type)
    {
        return type == typeof(long) ? rowId
            : type == typeof(int) ? checked((int)rowId)
            : type == typeof(short) ? checked((short)rowId)
            : type == typeof(byte) ? checked((byte)rowId)
            : type == typeof(sbyte) ? checked((sbyte)rowId)
            : type == typeof(uint) ? checked((uint)rowId)
            : type == typeof(ulong) ? unchecked((ulong)rowId)
            : Convert.ChangeType(rowId, type, CultureInfo.InvariantCulture);
    }
}
