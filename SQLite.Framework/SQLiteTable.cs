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
        : base(database)
    {
        Table = table;
    }

    /// <summary>
    /// The mapping of the database table to the class.
    /// </summary>
    public virtual TableMapping Table { get; }

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
    /// Creates the table in the database if it does not exist. Forwards to
    /// <see cref="SQLiteSchema.CreateTable(Type)" />.
    /// </summary>
    [Obsolete("Use Schema.CreateTable() instead.")]
    [UnconditionalSuppressMessage("AOT", "IL2072", Justification = "Table.Type comes from a TableMapping that already preserves public properties.")]
    public virtual int CreateTable()
    {
        return Database.Schema.CreateTable(Table.Type);
    }

    /// <summary>
    /// Deletes the table from the database. Forwards to <see cref="SQLiteSchema.DropTable(Type)" />.
    /// </summary>
    [Obsolete("Use Database.Schema.DropTable<T>() instead.")]
    [UnconditionalSuppressMessage("AOT", "IL2072", Justification = "Table.Type comes from a TableMapping that already preserves public properties.")]
    public virtual int DropTable()
    {
        return Database.Schema.DropTable(Table.Type);
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
    public virtual SQLiteTableBuilder<T> Schema => Database.Schema.Table<T>();

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
        if (!RunHooks(Database.Options.AddHooks, item))
        {
            return 0;
        }

        return DispatchAction(RunActionHooks(item, SQLiteAction.Add), item);
    }

    /// <summary>
    /// Performs an INSERT operation on the database table using the rows.
    /// </summary>
    public virtual int AddRange(IEnumerable<T> collection, bool runInTransaction = true, bool separateConnection = false)
    {
        if (Database.Options.OnActionHooks.Count == 0)
        {
            (TableColumn[] columns, string sql) = GetAddInfo();
            return RunRange(Database.Options.AddHooks, collection, runInTransaction, separateConnection, item => AddOrRemoveItem(columns, sql, item));
        }

        return RunRange(Database.Options.AddHooks, collection, runInTransaction, separateConnection,
            item => DispatchAction(RunActionHooks(item, SQLiteAction.Add), item));
    }

    /// <summary>
    /// Performs an UPDATE operation on the database table using the row.
    /// </summary>
    public virtual int Update(T item)
    {
        if (!RunHooks(Database.Options.UpdateHooks, item))
        {
            return 0;
        }

        return DispatchAction(RunActionHooks(item, SQLiteAction.Update), item);
    }

    /// <summary>
    /// Performs an UPDATE operation on the database table using the rows.
    /// </summary>
    public virtual int UpdateRange(IEnumerable<T> collection, bool runInTransaction = true, bool separateConnection = false)
    {
        if (Database.Options.OnActionHooks.Count == 0)
        {
            (TableColumn[] columns, TableColumn[] primaryKeyColumns, string sql) = GetUpdateInfo();
            return RunRange(Database.Options.UpdateHooks, collection, runInTransaction, separateConnection, item => UpdateItem(columns, primaryKeyColumns, sql, item));
        }

        return RunRange(Database.Options.UpdateHooks, collection, runInTransaction, separateConnection,
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
    public virtual int RemoveRange(IEnumerable<T> collection, bool runInTransaction = true, bool separateConnection = false)
    {
        if (Database.Options.OnActionHooks.Count == 0)
        {
            (TableColumn[] primaryKeyColumns, string sql) = GetRemoveInfo();
            return RunRange(Database.Options.RemoveHooks, collection, runInTransaction, separateConnection, item => AddOrRemoveItem(primaryKeyColumns, sql, item));
        }

        return RunRange(Database.Options.RemoveHooks, collection, runInTransaction, separateConnection,
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
    public virtual int AddOrUpdateRange(IEnumerable<T> collection, bool runInTransaction = true, bool separateConnection = false, SQLiteConflict conflict = SQLiteConflict.Replace)
    {
        if (Database.Options.OnActionHooks.Count == 0)
        {
            (TableColumn[] columns, string sql) = GetAddOrUpdateInfo(conflict);
            return RunRange(Database.Options.AddOrUpdateHooks, collection, runInTransaction, separateConnection, item => AddOrRemoveItem(columns, sql, item));
        }

        return RunRange(Database.Options.AddOrUpdateHooks, collection, runInTransaction, separateConnection, item =>
        {
            SQLiteAction final = RunActionHooks(item, SQLiteAction.AddOrUpdate);
            return final == SQLiteAction.AddOrUpdate
                ? DefaultAddOrUpdate(item, conflict)
                : DispatchAction(final, item);
        });
    }

    /// <summary>
    /// Performs an <c>INSERT INTO ... ON CONFLICT (...) DO ...</c> upsert built through the
    /// <see cref="UpsertBuilder{T}" /> DSL. Use this when <c>AddOrUpdate</c> with an
    /// <see cref="SQLiteConflict" /> value is not enough, for example to update only some
    /// columns or to do nothing on conflict.
    /// </summary>
    public virtual int Upsert(T item, Action<UpsertBuilder<T>> configure)
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
    /// Range version of <see cref="Upsert" />. Runs hooks per row.
    /// </summary>
    public virtual int UpsertRange(IEnumerable<T> collection, Action<UpsertBuilder<T>> configure, bool runInTransaction = true, bool separateConnection = false)
    {
        if (Database.Options.OnActionHooks.Count == 0)
        {
            (TableColumn[] columns, string sql) = GetUpsertInfo(configure);
            return RunRange(Database.Options.AddOrUpdateHooks, collection, runInTransaction, separateConnection, item => AddOrRemoveItem(columns, sql, item));
        }

        return RunRange(Database.Options.AddOrUpdateHooks, collection, runInTransaction, separateConnection, item =>
        {
            SQLiteAction final = RunActionHooks(item, SQLiteAction.AddOrUpdate);
            return final == SQLiteAction.AddOrUpdate
                ? DefaultUpsert(item, configure)
                : DispatchAction(final, item);
        });
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

        if (source is not BaseSQLiteTable sourceTable)
        {
            throw new InvalidOperationException($"Source must be a framework queryable (a {nameof(SQLiteTable)} or a LINQ chain over one).");
        }

        if (sourceTable.Database != Database)
        {
            throw new InvalidOperationException("Source must belong to the same database as the target.");
        }

        SQLTranslator translator = new(Database);
        SQLQuery sourceQuery = translator.Translate(source.Expression);

        string columnList = string.Join(", ", Table.Columns.Select(c => c.Name));
        string sql = $"INSERT INTO \"{Table.TableName}\" ({columnList}){Environment.NewLine}{sourceQuery.Sql}";

        return Database.CreateCommand(sql, sourceQuery.Parameters).ExecuteNonQuery();
    }

    IEnumerator<T> IEnumerable<T>.GetEnumerator()
    {
        return Database.ExecuteSequenceQuery<T>(Expression).GetEnumerator();
    }

    /// <inheritdoc />
    public override IEnumerator GetEnumerator()
    {
        return ((IEnumerable<T>)this).GetEnumerator();
    }

    /// <summary>
    /// Returns the columns to bind and the <c>INSERT INTO</c> SQL used by <see cref="Add" /> and
    /// <see cref="AddRange" />. Auto-increment primary keys are excluded from the binding set so
    /// that SQLite can populate them. Override to change the SQL shape (for example, to use
    /// <c>INSERT OR IGNORE</c>) or to filter the column list.
    /// </summary>
    protected virtual (TableColumn[] Columns, string Sql) GetAddInfo()
    {
        TableColumn[] columns = Table.Columns
            .Where(f => !f.IsPrimaryKey || !f.IsAutoIncrement)
            .ToArray();

        string columnsString = string.Join(", ", columns.Select(c => c.Name));
        string parametersString = string.Join(", ", columns.Select((c, i) => WrapParam($"@p{i}", c)));

        string sql = $"INSERT INTO \"{Table.TableName}\" ({columnsString}) VALUES ({parametersString})";

        return (columns, sql);
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

        TableColumn[] primaryKeyColumns = Table.Columns
            .Where(f => f.IsPrimaryKey)
            .ToArray();

        string setClause = string.Join(", ", columns.Select((c, i) => $"{c.Name} = {WrapParam($"@p{i}", c)}"));
        string primaryKeyClause = string.Join(" AND ",
            primaryKeyColumns.Select((c, i) => $"{c.Name} = @p{i + columns.Length}")
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
            throw new Exception("Cannot perform a delete operation without a primary key.");
        }

        string primaryKeyClause = string.Join(" AND ",
            primaryKeyColumns.Select((c, i) => $"{c.Name} = @p{i}")
        );
        string sql = $"DELETE FROM \"{Table.TableName}\" WHERE {primaryKeyClause}";

        return (primaryKeyColumns, sql);
    }

    /// <summary>
    /// Returns the columns to bind and the <c>INSERT OR REPLACE INTO</c> SQL used by
    /// <see cref="AddOrUpdate" /> and <see cref="AddOrUpdateRange" />. Override to change the
    /// upsert SQL, for example to use SQLite's <c>ON CONFLICT</c> syntax instead.
    /// </summary>
    protected virtual (TableColumn[] Columns, string Sql) GetAddOrUpdateInfo(SQLiteConflict conflict)
    {
        TableColumn[] columns = Table.Columns
            .Where(f => !f.IsPrimaryKey || !f.IsAutoIncrement)
            .ToArray();

        string columnsString = string.Join(", ", columns.Select(c => c.Name));
        string parametersString = string.Join(", ", columns.Select((c, i) => WrapParam($"@p{i}", c)));

        string action = conflict switch
        {
            SQLiteConflict.Replace => "OR REPLACE ",
            SQLiteConflict.Ignore => "OR IGNORE ",
            SQLiteConflict.Abort => "OR ABORT ",
            SQLiteConflict.Fail => "OR FAIL ",
            SQLiteConflict.Rollback => "OR ROLLBACK ",
            _ => "OR REPLACE ",
        };

        string sql = $"INSERT {action}INTO \"{Table.TableName}\" ({columnsString}) VALUES ({parametersString})";

        return (columns, sql);
    }

    /// <summary>
    /// Returns the columns to bind and the <c>INSERT INTO ... ON CONFLICT (...) DO ...</c> SQL
    /// produced by configuring an <see cref="UpsertBuilder{T}" />. Override to change the SQL shape
    /// produced by <see cref="Upsert" /> and <see cref="UpsertRange" />.
    /// </summary>
    protected virtual (TableColumn[] Columns, string Sql) GetUpsertInfo(Action<UpsertBuilder<T>> configure)
    {
        UpsertBuilder<T> builder = new();
        configure(builder);
        UpsertConflictTarget<T> target = builder.Build();
        return UpsertSqlBuilder.Build(Table, target, (c, p) => WrapParam(p, c));
    }

    /// <summary>
    /// Runs the per-entity hooks for <typeparamref name="T" /> stored on the database options.
    /// Each hook can mutate <paramref name="item" />. Returns <see langword="false" /> when any
    /// hook returns <see langword="false" />, signalling that the default operation should be skipped.
    /// </summary>
    protected virtual bool RunHooks(IReadOnlyDictionary<Type, IReadOnlyList<Delegate>> hooks, T item)
    {
        if (!hooks.TryGetValue(typeof(T), out IReadOnlyList<Delegate>? list))
        {
            return true;
        }

        foreach (Delegate hook in list)
        {
            Func<SQLiteDatabase, T, bool> typed = (Func<SQLiteDatabase, T, bool>)hook;
            if (!typed(Database, item))
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
    protected virtual int RunRange(IReadOnlyDictionary<Type, IReadOnlyList<Delegate>> hooks, IEnumerable<T> collection, bool runInTransaction, bool separateConnection, Func<T, int> execute)
    {
        int count = 0;

        if (runInTransaction)
        {
            using SQLiteTransaction transaction = Database.BeginTransaction(separateConnection);
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
    /// returns <c>0</c> without touching the database; the other values map to the standard
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
        (TableColumn[] columns, string sql) = GetAddInfo();
        return AddOrRemoveItem(columns, sql, item);
    }

    /// <summary>
    /// Runs the default UPDATE for <paramref name="item" />. Used by
    /// <see cref="DispatchAction" /> when the action hook chain settles on
    /// <see cref="SQLiteAction.Update" />.
    /// </summary>
    protected virtual int DefaultUpdate(T item)
    {
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
        (TableColumn[] columns, string sql) = GetAddOrUpdateInfo(conflict);
        return AddOrRemoveItem(columns, sql, item);
    }

    /// <summary>
    /// Runs the configured <c>Upsert</c> for <paramref name="item" />. Used by <c>Upsert</c> and
    /// <c>UpsertRange</c> when the action hook chain leaves the action as
    /// <see cref="SQLiteAction.AddOrUpdate" /> so the configured <c>ON CONFLICT</c> shape is preserved.
    /// </summary>
    protected virtual int DefaultUpsert(T item, Action<UpsertBuilder<T>> configure)
    {
        (TableColumn[] columns, string sql) = GetUpsertInfo(configure);
        return AddOrRemoveItem(columns, sql, item);
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
    /// Binds the values of <paramref name="columns" /> on <paramref name="item" /> and executes
    /// <paramref name="sql" />. Used by <see cref="Add" />, <see cref="AddRange" />,
    /// <see cref="Remove" />, <see cref="RemoveRange" />, <see cref="AddOrUpdate" />, and
    /// <see cref="AddOrUpdateRange" />. Override to mutate the entity right before binding (for
    /// example, to stamp <c>CreatedAt</c>) or to log every write.
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
}