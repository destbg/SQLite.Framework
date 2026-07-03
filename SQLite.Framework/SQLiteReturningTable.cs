namespace SQLite.Framework;

/// <summary>
/// Wraps a <see cref="SQLiteTable{T}" /> with a projection. The entity-level write methods
/// (<see cref="Add" />, <see cref="Update" />, <see cref="Remove" /> and their range variants)
/// issue an <c>INSERT</c>/<c>UPDATE</c>/<c>DELETE ... RETURNING</c> and hand back the projected
/// rows. The projection goes through the regular <c>Select</c> pipeline, so a matching
/// source-generated materializer is reused.
/// </summary>
/// <remarks>
/// <c>RETURNING</c> requires SQLite 3.35 or later.
/// </remarks>
public class SQLiteReturningTable<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] TResult>
{
    /// <summary>
    /// Initializes a new wrapper around <paramref name="source" /> with the given <paramref name="projection" />.
    /// </summary>
    public SQLiteReturningTable(SQLiteTable<T> source, Expression<Func<T, TResult>> projection)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(projection);

        Source = source;
        Projection = projection;
    }

    /// <summary>
    /// The underlying table that the wrapper writes to.
    /// </summary>
    public SQLiteTable<T> Source { get; }

    /// <summary>
    /// The projection emitted after the <c>RETURNING</c> clause.
    /// </summary>
    public Expression<Func<T, TResult>> Projection { get; }

    /// <summary>
    /// The database that <see cref="Source" /> targets.
    /// </summary>
    public SQLiteDatabase Database => Source.Database;

    /// <summary>
    /// Inserts <paramref name="item" /> and returns the projected inserted row. Returns
    /// <see langword="default" /> when an <c>OnAdd</c> hook cancels the write. Copies an
    /// auto-increment primary key back to <paramref name="item" />, the same as
    /// <see cref="SQLiteTable{T}.Add" />.
    /// </summary>
    public virtual TResult? Add(T item)
    {
        if (CommonHelpers.HasColumnHooks<T>(Database.Options.AddHooks))
        {
            List<TResult> hookedRows = RunWithColumnHooks(item, Database.Options.AddHooks, SQLiteAction.Add, insert: true);
            return hookedRows.Count == 0 ? default : hookedRows[0];
        }

        if (!Source.RunHooks(Database.Options.AddHooks, item))
        {
            return default;
        }

        SQLiteAction action = Source.RunActionHooks(item, SQLiteAction.Add);
        List<TResult> rows = RunResolvedAction(action, item);
        return rows.Count == 0 ? default : rows[0];
    }

    /// <summary>
    /// Inserts every item in <paramref name="collection" /> and returns the projected rows.
    /// Runs inside a transaction by default.
    /// </summary>
    public virtual List<TResult> AddRange(IEnumerable<T> collection, bool runInTransaction = true)
    {
        ArgumentNullException.ThrowIfNull(collection);

        TableColumn? autoIncrement = Source.GetAutoIncrementColumn();

        return RunRangeWithReturning(
            collection,
            Database.Options.AddHooks,
            SQLiteAction.Add,
            runInTransaction,
            item =>
            {
                (TableColumn[] columns, string sql) = Source.GetAddInfoForItemInternal(item);
                List<SQLiteParameter> parameters = BuildInsertParameters(columns, autoIncrement, item);
                List<TResult> projected = ExecuteWithReturning(sql, parameters);
                if (autoIncrement != null && projected.Count > 0)
                {
                    BackfillAutoIncrement(item, autoIncrement);
                }
                return projected;
            },
            columnInsert: true);
    }

    /// <summary>
    /// Updates the row identified by <paramref name="item" />'s primary key and returns the
    /// post-update row, projected. Returns <see langword="default" /> when no row matched
    /// or when an <c>OnUpdate</c> hook cancelled the write.
    /// </summary>
    public virtual TResult? Update(T item)
    {
        if (CommonHelpers.HasColumnHooks<T>(Database.Options.UpdateHooks))
        {
            List<TResult> hookedRows = RunWithColumnHooks(item, Database.Options.UpdateHooks, SQLiteAction.Update, insert: false);
            return hookedRows.Count == 0 ? default : hookedRows[0];
        }

        if (!Source.RunHooks(Database.Options.UpdateHooks, item))
        {
            return default;
        }

        SQLiteAction action = Source.RunActionHooks(item, SQLiteAction.Update);
        List<TResult> rows = RunResolvedAction(action, item);
        return rows.Count == 0 ? default : rows[0];
    }

    /// <summary>
    /// Updates every item in <paramref name="collection" /> by primary key and returns the
    /// projected post-update rows. Runs inside a transaction by default.
    /// </summary>
    public virtual List<TResult> UpdateRange(IEnumerable<T> collection, bool runInTransaction = true)
    {
        ArgumentNullException.ThrowIfNull(collection);

        (TableColumn[] columns, TableColumn[] primaryColumns, string sql) = Source.GetUpdateInfo();

        return RunRangeWithReturning(
            collection,
            Database.Options.UpdateHooks,
            SQLiteAction.Update,
            runInTransaction,
            item =>
            {
                List<SQLiteParameter> parameters = BuildUpdateParameters(columns, primaryColumns, item);
                return ExecuteWithReturning(sql, parameters);
            },
            columnInsert: false);
    }

    /// <summary>
    /// Deletes the row identified by <paramref name="item" />'s primary key and returns the
    /// deleted row, projected. Returns <see langword="default" /> when no row matched or
    /// when an <c>OnRemove</c> hook cancelled the write.
    /// </summary>
    public virtual TResult? Remove(T item)
    {
        if (!Source.RunHooks(Database.Options.RemoveHooks, item))
        {
            return default;
        }

        SQLiteAction action = Source.RunActionHooks(item, SQLiteAction.Remove);
        List<TResult> rows = RunResolvedAction(action, item);
        return rows.Count == 0 ? default : rows[0];
    }

    /// <summary>
    /// Deletes every item in <paramref name="collection" /> by primary key and returns the
    /// projected deleted rows. Runs inside a transaction by default.
    /// </summary>
    public virtual List<TResult> RemoveRange(IEnumerable<T> collection, bool runInTransaction = true)
    {
        ArgumentNullException.ThrowIfNull(collection);

        (TableColumn[] primaryColumns, string sql) = Source.GetRemoveInfo();

        return RunRangeWithReturning(
            collection,
            Database.Options.RemoveHooks,
            SQLiteAction.Remove,
            runInTransaction,
            item =>
            {
                List<SQLiteParameter> parameters = BuildKeyParameters(primaryColumns, item);
                return ExecuteWithReturning(sql, parameters);
            });
    }

    /// <summary>
    /// Performs an <c>INSERT INTO ... ON CONFLICT (...) DO ...</c> upsert built through the
    /// <see cref="SQLiteUpsertBuilder{T}" /> DSL and returns the written row, projected. Returns
    /// <see langword="default" /> when the conflict resolves to no write (a <c>DO NOTHING</c> or a
    /// <c>DO UPDATE ... WHERE</c> guard that fails) or when an <c>OnAddOrUpdate</c> hook cancels the write.
    /// </summary>
    public virtual TResult? Upsert(T item, Action<SQLiteUpsertBuilder<T>> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        if (!Source.RunHooks(Database.Options.AddOrUpdateHooks, item))
        {
            return default;
        }

        SQLiteAction action = Source.RunActionHooks(item, SQLiteAction.AddOrUpdate);
        List<TResult> rows = action == SQLiteAction.AddOrUpdate
            ? RunConfiguredUpsert(configure, item)
            : RunResolvedAction(action, item);
        return rows.Count == 0 ? default : rows[0];
    }

    /// <summary>
    /// Performs the configured upsert for every item in <paramref name="collection" /> and returns
    /// the written rows, projected. Rows whose conflict resolves to no write contribute nothing to
    /// the result. Runs inside a transaction by default.
    /// </summary>
    public virtual List<TResult> UpsertRange(IEnumerable<T> collection, Action<SQLiteUpsertBuilder<T>> configure, bool runInTransaction = true)
    {
        ArgumentNullException.ThrowIfNull(collection);
        ArgumentNullException.ThrowIfNull(configure);

        (TableColumn[] baseColumns, string baseSql) = Source.GetUpsertInfo(configure);
        TableColumn? autoIncrement = Source.GetAutoIncrementColumn();

        return RunRangeWithReturning(
            collection,
            Database.Options.AddOrUpdateHooks,
            SQLiteAction.AddOrUpdate,
            runInTransaction,
            item =>
            {
                (TableColumn[] columns, string sql) = Source.FilterUpsertInfoForItemInternal(configure, item, baseColumns, baseSql);
                List<SQLiteParameter> parameters = BuildInsertParameters(columns, autoIncrement, item);
                return UpsertWithReturning(sql, parameters, autoIncrement, item);
            });
    }

    private List<TResult> RunConfiguredUpsert(Action<SQLiteUpsertBuilder<T>> configure, T item)
    {
        (TableColumn[] baseColumns, string baseSql) = Source.GetUpsertInfo(configure);
        (TableColumn[] columns, string sql) = Source.FilterUpsertInfoForItemInternal(configure, item, baseColumns, baseSql);
        TableColumn? autoIncrement = Source.GetAutoIncrementColumn();
        List<SQLiteParameter> parameters = BuildInsertParameters(columns, autoIncrement, item);
        return UpsertWithReturning(sql, parameters, autoIncrement, item);
    }

    private List<TResult> RunResolvedAction(SQLiteAction action, T item)
    {
        switch (action)
        {
            case SQLiteAction.Skip:
                return [];
            case SQLiteAction.Add:
            {
                (TableColumn[] columns, string sql) = Source.GetAddInfoForItemInternal(item);
                List<SQLiteParameter> parameters = BuildInsertParameters(columns, Source.GetAutoIncrementColumn(), item);
                return ExecuteInsertReturning(sql, parameters, item);
            }
            case SQLiteAction.Update:
            {
                (TableColumn[] columns, TableColumn[] primaryColumns, string sql) = Source.GetUpdateInfo();
                List<SQLiteParameter> parameters = BuildUpdateParameters(columns, primaryColumns, item);
                return ExecuteWithReturning(sql, parameters);
            }
            case SQLiteAction.Remove:
            {
                (TableColumn[] primaryColumns, string sql) = Source.GetRemoveInfo();
                List<SQLiteParameter> parameters = BuildKeyParameters(primaryColumns, item);
                return ExecuteWithReturning(sql, parameters);
            }
            case SQLiteAction.AddOrUpdate:
            {
                (TableColumn[] columns, string sql) = Source.GetAddOrUpdateInfoForItemInternal(item, SQLiteConflict.Replace);
                TableColumn? autoIncrement = Source.GetAutoIncrementColumn();
                List<SQLiteParameter> parameters = BuildInsertParameters(columns, autoIncrement, item);
                return UpsertWithReturning(sql, parameters, autoIncrement, item);
            }
            default:
                throw new InvalidOperationException($"Unsupported SQLiteAction value: {action}");
        }
    }

    private List<TResult> ExecuteWithReturning(string entitySql, List<SQLiteParameter> entityParameters)
    {
#if SQLITE_FRAMEWORK_VERSION_AWARE
        Database.Options.EnsureMinimumVersion(SQLiteMinimumVersion.V3_35, "RETURNING");
#endif
        ProjectionPlan plan = BuildProjectionPlan();

        string finalSql = entitySql + " RETURNING " + plan.ColumnsSql;

        List<SQLiteParameter> combinedParameters = new(entityParameters.Count + plan.Parameters.Count);
        combinedParameters.AddRange(entityParameters);
        combinedParameters.AddRange(plan.Parameters);

        SQLQuery query = new()
        {
            Sql = finalSql,
            Parameters = combinedParameters,
            CreateObject = plan.Template.CreateObject,
            Reverse = false,
            ThrowOnEmpty = false,
            ElementAtSemantic = false,
            ThrowOnMoreThanOne = false,
            ReflectedMethods = plan.Template.ReflectedMethods,
            ReflectedMethodInstances = plan.Template.ReflectedMethodInstances,
            CapturedValues = plan.Template.CapturedValues,
            ReflectedTypes = plan.Template.ReflectedTypes,
            ReflectedMembers = plan.Template.ReflectedMembers,
            ReflectedConstructors = plan.Template.ReflectedConstructors,
        };

        using IDisposable _ = Database.Lock();
        return Database.CreateCommand(finalSql, combinedParameters)
            .ExecuteQueryInternal<TResult>(query)
            .ToList();
    }

    private List<TResult> UpsertWithReturning(string sql, List<SQLiteParameter> parameters, TableColumn? autoIncrement, T item)
    {
        long lastRowIdBefore = raw.sqlite3_last_insert_rowid(Database.GetActiveHandle());
        List<TResult> projected = ExecuteWithReturning(sql, parameters);

        bool inserted = raw.sqlite3_last_insert_rowid(Database.GetActiveHandle()) != lastRowIdBefore;
        if (inserted && autoIncrement != null)
        {
            BackfillAutoIncrement(item, autoIncrement);
        }

        return projected;
    }

    private ProjectionPlan BuildProjectionPlan()
    {
        SQLiteCounters counters = new("@rp");
        SQLTranslator translator = new(Database, counters, 0, false)
        {
            QueryType = QueryType.Select,
            OmitTableAlias = true,
        };

        IQueryable<TResult> projected = Source.Select(Projection);
        SQLQuery template = translator.Translate(projected.Expression);

        StringBuilder sb = new();
        IReadOnlyList<SQLiteExpression> selects = translator.Selects;
        for (int i = 0; i < selects.Count; i++)
        {
            if (i > 0)
            {
                sb.Append(", ");
            }
            selects[i].WriteSqlTo(sb);
            sb.Append(" AS \"");
            sb.Append(selects[i].IdentifierText);
            sb.Append('"');
        }

        return new ProjectionPlan
        {
            ColumnsSql = sb.ToString(),
            Parameters = template.Parameters,
            Template = template,
        };
    }

    private List<TResult> RunRangeWithReturning(IEnumerable<T> collection, IReadOnlyDictionary<Type, IReadOnlyList<Delegate>> hooks, SQLiteAction startingAction, bool runInTransaction, Func<T, List<TResult>> writeUnchanged, bool? columnInsert = null)
    {
        List<TResult> results = [];
        bool useColumns = columnInsert.HasValue && CommonHelpers.HasColumnHooks<T>(hooks);

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

        return results;

        void Body()
        {
            foreach (T item in collection)
            {
                if (useColumns)
                {
                    results.AddRange(RunWithColumnHooks(item, hooks, startingAction, columnInsert!.Value));
                    continue;
                }

                if (!Source.RunHooks(hooks, item))
                {
                    continue;
                }

                SQLiteAction action = Source.RunActionHooks(item, startingAction);
                results.AddRange(action == startingAction
                    ? writeUnchanged(item)
                    : RunResolvedAction(action, item));
            }
        }
    }

    private List<TResult> RunWithColumnHooks(T item, IReadOnlyDictionary<Type, IReadOnlyList<Delegate>> hooks, SQLiteAction startingAction, bool insert)
    {
        Dictionary<string, object?> columns = [];
        if (!Source.RunHooks(hooks, item, columns))
        {
            return [];
        }

        SQLiteAction action = Source.RunActionHooks(item, startingAction);
        if (action != startingAction)
        {
            return RunResolvedAction(action, item, columns);
        }

        if (insert)
        {
            (string sql, List<SQLiteParameter> parameters) = Source.BuildInsertWithExtraColumns(item, columns);
            return ExecuteInsertReturning(sql, parameters, item);
        }

        (string updateSql, List<SQLiteParameter> updateParameters) = Source.BuildUpdateWithExtraColumns(item, columns);
        return ExecuteWithReturning(updateSql, updateParameters);
    }

    private List<TResult> RunResolvedAction(SQLiteAction action, T item, IDictionary<string, object?> columns)
    {
        if (columns.Count == 0)
        {
            return RunResolvedAction(action, item);
        }

        switch (action)
        {
            case SQLiteAction.Skip:
                return [];
            case SQLiteAction.Add:
            case SQLiteAction.AddOrUpdate:
            {
                string insertVerb = action == SQLiteAction.AddOrUpdate ? "INSERT OR REPLACE" : "INSERT";
                (string sql, List<SQLiteParameter> parameters) = Source.BuildInsertWithExtraColumns(item, columns, insertVerb);
                return ExecuteInsertReturning(sql, parameters, item);
            }
            case SQLiteAction.Update:
            {
                (string sql, List<SQLiteParameter> parameters) = Source.BuildUpdateWithExtraColumns(item, columns);
                return ExecuteWithReturning(sql, parameters);
            }
            default:
                return RunResolvedAction(action, item);
        }
    }

    private List<TResult> ExecuteInsertReturning(string sql, List<SQLiteParameter> parameters, T item)
    {
        TableColumn? autoIncrement = Source.GetAutoIncrementColumn();
        List<TResult> rows = ExecuteWithReturning(sql, parameters);
        if (autoIncrement != null && rows.Count > 0)
        {
            BackfillAutoIncrement(item, autoIncrement);
        }

        return rows;
    }

    private void BackfillAutoIncrement(T item, TableColumn autoIncrement)
    {
        long rowId = raw.sqlite3_last_insert_rowid(Database.GetActiveHandle());
        autoIncrement.PropertyInfo.SetValue(item, SQLiteTable<T>.ConvertRowIdToType(rowId, autoIncrement.PropertyType));
    }

    private static bool IsAutoIncrementUnset(object? value)
    {
        return value is ulong unsignedValue
            ? unsignedValue == 0UL
            : Convert.ToInt64(value, CultureInfo.InvariantCulture) == 0L;
    }

    private static List<SQLiteParameter> BuildInsertParameters(TableColumn[] columns, TableColumn? autoIncrement, T item)
    {
        List<SQLiteParameter> parameters = new(columns.Length);
        for (int i = 0; i < columns.Length; i++)
        {
            TableColumn column = columns[i];
            object? value = column.PropertyInfo.GetValue(item);
            if (column == autoIncrement && IsAutoIncrementUnset(value))
            {
                value = null;
            }
            parameters.Add(new SQLiteParameter { Name = $"@p{i}", Value = value });
        }
        return parameters;
    }

    private static List<SQLiteParameter> BuildUpdateParameters(TableColumn[] columns, TableColumn[] primaryColumns, T item)
    {
        List<SQLiteParameter> parameters = new(columns.Length + primaryColumns.Length);
        for (int i = 0; i < columns.Length; i++)
        {
            parameters.Add(new SQLiteParameter
            {
                Name = $"@p{i}",
                Value = columns[i].PropertyInfo.GetValue(item),
            });
        }
        for (int i = 0; i < primaryColumns.Length; i++)
        {
            parameters.Add(new SQLiteParameter
            {
                Name = $"@p{i + columns.Length}",
                Value = primaryColumns[i].PropertyInfo.GetValue(item),
            });
        }
        return parameters;
    }

    private static List<SQLiteParameter> BuildKeyParameters(TableColumn[] primaryColumns, T item)
    {
        List<SQLiteParameter> parameters = new(primaryColumns.Length);
        for (int i = 0; i < primaryColumns.Length; i++)
        {
            parameters.Add(new SQLiteParameter
            {
                Name = $"@p{i}",
                Value = primaryColumns[i].PropertyInfo.GetValue(item),
            });
        }
        return parameters;
    }

    private sealed class ProjectionPlan
    {
        public required string ColumnsSql { get; init; }
        public required IReadOnlyList<SQLiteParameter> Parameters { get; init; }
        public required SQLQuery Template { get; init; }
    }
}
