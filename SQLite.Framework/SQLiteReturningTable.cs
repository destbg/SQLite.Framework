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
    private ProjectionPlan? cachedPlan;

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
    /// auto-increment primary key back to <paramref name="item" /> when the projection
    /// materializes <typeparamref name="T" /> in full, the same as <see cref="SQLiteTable{T}.Add" />.
    /// </summary>
    public virtual TResult? Add(T item)
    {
        if (!Source.RunHooksInternal(Database.Options.AddHooks, item))
        {
            return default;
        }

        (TableColumn[] columns, string sql) = Source.GetAddInfoInternal();
        TableColumn? autoIncrement = Source.Table.Columns.FirstOrDefault(c => c.IsPrimaryKey && c.IsAutoIncrement);
        List<SQLiteParameter> parameters = BuildInsertParameters(columns, autoIncrement, item);

        List<TResult> rows = ExecuteWithReturning(sql, parameters);
        if (rows.Count == 0)
        {
            return default;
        }

        if (autoIncrement != null)
        {
            BackfillAutoIncrement(item, rows[0], autoIncrement);
        }

        return rows[0];
    }

    /// <summary>
    /// Inserts every item in <paramref name="collection" /> and returns the projected rows.
    /// Runs inside a transaction by default.
    /// </summary>
    public virtual List<TResult> AddRange(IEnumerable<T> collection, bool runInTransaction = true, bool separateConnection = false)
    {
        ArgumentNullException.ThrowIfNull(collection);

        (TableColumn[] columns, string sql) = Source.GetAddInfoInternal();
        TableColumn? autoIncrement = Source.Table.Columns.FirstOrDefault(c => c.IsPrimaryKey && c.IsAutoIncrement);

        return RunRangeWithReturning(
            collection,
            Database.Options.AddHooks,
            runInTransaction,
            separateConnection,
            item =>
            {
                List<SQLiteParameter> parameters = BuildInsertParameters(columns, autoIncrement, item);
                List<TResult> projected = ExecuteWithReturning(sql, parameters);
                if (autoIncrement != null && projected.Count > 0)
                {
                    BackfillAutoIncrement(item, projected[0], autoIncrement);
                }
                return projected;
            });
    }

    /// <summary>
    /// Updates the row identified by <paramref name="item" />'s primary key and returns the
    /// post-update row, projected. Returns <see langword="default" /> when no row matched
    /// or when an <c>OnUpdate</c> hook cancelled the write.
    /// </summary>
    public virtual TResult? Update(T item)
    {
        if (!Source.RunHooksInternal(Database.Options.UpdateHooks, item))
        {
            return default;
        }

        (TableColumn[] columns, TableColumn[] primaryColumns, string sql) = Source.GetUpdateInfoInternal();
        List<SQLiteParameter> parameters = BuildUpdateParameters(columns, primaryColumns, item);

        List<TResult> rows = ExecuteWithReturning(sql, parameters);
        return rows.Count == 0 ? default : rows[0];
    }

    /// <summary>
    /// Updates every item in <paramref name="collection" /> by primary key and returns the
    /// projected post-update rows. Runs inside a transaction by default.
    /// </summary>
    public virtual List<TResult> UpdateRange(IEnumerable<T> collection, bool runInTransaction = true, bool separateConnection = false)
    {
        ArgumentNullException.ThrowIfNull(collection);

        (TableColumn[] columns, TableColumn[] primaryColumns, string sql) = Source.GetUpdateInfoInternal();

        return RunRangeWithReturning(
            collection,
            Database.Options.UpdateHooks,
            runInTransaction,
            separateConnection,
            item =>
            {
                List<SQLiteParameter> parameters = BuildUpdateParameters(columns, primaryColumns, item);
                return ExecuteWithReturning(sql, parameters);
            });
    }

    /// <summary>
    /// Deletes the row identified by <paramref name="item" />'s primary key and returns the
    /// deleted row, projected. Returns <see langword="default" /> when no row matched or
    /// when an <c>OnRemove</c> hook cancelled the write.
    /// </summary>
    public virtual TResult? Remove(T item)
    {
        if (!Source.RunHooksInternal(Database.Options.RemoveHooks, item))
        {
            return default;
        }

        (TableColumn[] primaryColumns, string sql) = Source.GetRemoveInfoInternal();
        List<SQLiteParameter> parameters = BuildKeyParameters(primaryColumns, item);

        List<TResult> rows = ExecuteWithReturning(sql, parameters);
        return rows.Count == 0 ? default : rows[0];
    }

    /// <summary>
    /// Deletes every item in <paramref name="collection" /> by primary key and returns the
    /// projected deleted rows. Runs inside a transaction by default.
    /// </summary>
    public virtual List<TResult> RemoveRange(IEnumerable<T> collection, bool runInTransaction = true, bool separateConnection = false)
    {
        ArgumentNullException.ThrowIfNull(collection);

        (TableColumn[] primaryColumns, string sql) = Source.GetRemoveInfoInternal();

        return RunRangeWithReturning(
            collection,
            Database.Options.RemoveHooks,
            runInTransaction,
            separateConnection,
            item =>
            {
                List<SQLiteParameter> parameters = BuildKeyParameters(primaryColumns, item);
                return ExecuteWithReturning(sql, parameters);
            });
    }

    private List<TResult> ExecuteWithReturning(string entitySql, List<SQLiteParameter> entityParameters)
    {
        ProjectionPlan plan = cachedPlan ??= BuildProjectionPlan();

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
            ThrowOnMoreThanOne = false,
            ReflectedMethods = plan.Template.ReflectedMethods,
            ReflectedMethodInstances = plan.Template.ReflectedMethodInstances,
            CapturedValues = plan.Template.CapturedValues,
            ReflectedTypes = plan.Template.ReflectedTypes,
            ReflectedMembers = plan.Template.ReflectedMembers,
            ReflectedConstructors = plan.Template.ReflectedConstructors,
        };

        return Database.CreateCommand(finalSql, combinedParameters)
            .ExecuteQueryInternal<TResult>(query)
            .ToList();
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

    private static void BackfillAutoIncrement(T item, TResult row, TableColumn autoIncrement)
    {
        if (row is T projectedEntity)
        {
            autoIncrement.PropertyInfo.SetValue(item, autoIncrement.PropertyInfo.GetValue(projectedEntity));
        }
    }

    private List<TResult> RunRangeWithReturning(IEnumerable<T> collection, IReadOnlyDictionary<Type, IReadOnlyList<Delegate>> hooks, bool runInTransaction, bool separateConnection, Func<T, List<TResult>> writeOne)
    {
        List<TResult> results = [];

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

        return results;

        void Body()
        {
            foreach (T item in collection)
            {
                if (!Source.RunHooksInternal(hooks, item))
                {
                    continue;
                }
                results.AddRange(writeOne(item));
            }
        }
    }

    private static bool IsAutoIncrementUnset(object? value)
    {
        return Convert.ToInt64(value, CultureInfo.InvariantCulture) == 0L;
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
