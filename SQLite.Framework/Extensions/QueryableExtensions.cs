namespace SQLite.Framework.Extensions;

/// <summary>
/// <see cref="Queryable"/> extensions for <see cref="IQueryable{T}"/>.
/// </summary>
public static class QueryableExtensions
{
    /// <summary>
    /// Skips every <see cref="SQLiteOptions.QueryFilters" /> entry for this query. Your own
    /// <c>Where</c> still runs. The opt-out applies to the whole subtree, including joined tables.
    /// </summary>
    public static IQueryable<T> IgnoreQueryFilters<T>(this IQueryable<T> source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return source.Provider.CreateQuery<T>(
            Expression.Call(
                null,
                new Func<IQueryable<T>, IQueryable<T>>(IgnoreQueryFilters).Method,
                source.Expression));
    }

    /// <summary>
    /// Adds a single <c>WHERE</c> clause built from one or more predicates joined with <c>AND</c>
    /// and <c>OR</c>. Use this when chaining <c>Where</c> would not capture the desired logic, for
    /// example when a row should match any of several predicates, or when predicates are added in
    /// a loop. An empty builder leaves the query unchanged.
    /// </summary>
    public static IQueryable<T> WhereBuilder<T>(this IQueryable<T> source, Action<SQLiteWhereBuilder<T>> build)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(build);

        SQLiteWhereBuilder<T> builder = new();
        build(builder);
        Expression<Func<T, bool>>? predicate = builder.Build();
        return predicate == null ? source : source.Where(predicate);
    }

    /// <summary>
    /// Orders the rows by <paramref name="keySelector" /> ascending and places nulls according to
    /// <paramref name="nulls" />, emitting <c>ORDER BY key ASC NULLS FIRST</c> or <c>... NULLS LAST</c>.
    /// Use this instead of a <c>CASE</c> on the sort key. <c>NULLS FIRST</c>/<c>LAST</c> requires
    /// SQLite 3.30.0 or newer.
    /// </summary>
#if SQLITE_FRAMEWORK_OS_BUNDLED_SQLITE
    [UnsupportedOSPlatform("android")]
    [SupportedOSPlatform("android31.0")]
    [UnsupportedOSPlatform("ios")]
    [SupportedOSPlatform("ios14.0")]
#endif
    public static IOrderedQueryable<T> OrderBy<T, TKey>(this IQueryable<T> source, Expression<Func<T, TKey>> keySelector, SQLiteNullsOrder nulls)
    {
        return ApplyOrderWithNulls(source, keySelector, nulls, new Func<IQueryable<T>, Expression<Func<T, TKey>>, SQLiteNullsOrder, IOrderedQueryable<T>>(OrderBy).Method);
    }

    /// <summary>
    /// Orders the rows by <paramref name="keySelector" /> descending and places nulls according to
    /// <paramref name="nulls" />, emitting <c>ORDER BY key DESC NULLS FIRST</c> or <c>... NULLS LAST</c>.
    /// <c>NULLS FIRST</c>/<c>LAST</c> requires SQLite 3.30.0 or newer.
    /// </summary>
#if SQLITE_FRAMEWORK_OS_BUNDLED_SQLITE
    [UnsupportedOSPlatform("android")]
    [SupportedOSPlatform("android31.0")]
    [UnsupportedOSPlatform("ios")]
    [SupportedOSPlatform("ios14.0")]
#endif
    public static IOrderedQueryable<T> OrderByDescending<T, TKey>(this IQueryable<T> source, Expression<Func<T, TKey>> keySelector, SQLiteNullsOrder nulls)
    {
        return ApplyOrderWithNulls(source, keySelector, nulls, new Func<IQueryable<T>, Expression<Func<T, TKey>>, SQLiteNullsOrder, IOrderedQueryable<T>>(OrderByDescending).Method);
    }

    /// <summary>
    /// Adds a secondary ascending sort key with the given null placement. Emits
    /// <c>key ASC NULLS FIRST</c> or <c>... NULLS LAST</c>. <c>NULLS FIRST</c>/<c>LAST</c> requires
    /// SQLite 3.30.0 or newer.
    /// </summary>
#if SQLITE_FRAMEWORK_OS_BUNDLED_SQLITE
    [UnsupportedOSPlatform("android")]
    [SupportedOSPlatform("android31.0")]
    [UnsupportedOSPlatform("ios")]
    [SupportedOSPlatform("ios14.0")]
#endif
    public static IOrderedQueryable<T> ThenBy<T, TKey>(this IOrderedQueryable<T> source, Expression<Func<T, TKey>> keySelector, SQLiteNullsOrder nulls)
    {
        return ApplyOrderWithNulls(source, keySelector, nulls, new Func<IOrderedQueryable<T>, Expression<Func<T, TKey>>, SQLiteNullsOrder, IOrderedQueryable<T>>(ThenBy).Method);
    }

    /// <summary>
    /// Adds a secondary descending sort key with the given null placement. Emits
    /// <c>key DESC NULLS FIRST</c> or <c>... NULLS LAST</c>. <c>NULLS FIRST</c>/<c>LAST</c> requires
    /// SQLite 3.30.0 or newer.
    /// </summary>
#if SQLITE_FRAMEWORK_OS_BUNDLED_SQLITE
    [UnsupportedOSPlatform("android")]
    [SupportedOSPlatform("android31.0")]
    [UnsupportedOSPlatform("ios")]
    [SupportedOSPlatform("ios14.0")]
#endif
    public static IOrderedQueryable<T> ThenByDescending<T, TKey>(this IOrderedQueryable<T> source, Expression<Func<T, TKey>> keySelector, SQLiteNullsOrder nulls)
    {
        return ApplyOrderWithNulls(source, keySelector, nulls, new Func<IOrderedQueryable<T>, Expression<Func<T, TKey>>, SQLiteNullsOrder, IOrderedQueryable<T>>(ThenByDescending).Method);
    }

    /// <summary>
    /// Joins <paramref name="outer" /> and <paramref name="inner" /> on matching keys and keeps the
    /// unmatched rows from both sides, emitting a <c>FULL OUTER JOIN</c>. The
    /// <paramref name="resultSelector" /> receives the outer row (null when only an inner row
    /// matched) and the inner row (null when only an outer row matched). .NET has no built-in
    /// <c>FullOuterJoin</c>, so this is the framework's own operator. Requires SQLite 3.39.0 or newer.
    /// </summary>
#if SQLITE_FRAMEWORK_OS_BUNDLED_SQLITE
    [UnsupportedOSPlatform("android")]
    [SupportedOSPlatform("android34.0")]
    [UnsupportedOSPlatform("ios")]
    [SupportedOSPlatform("ios16.0")]
#endif
    public static IQueryable<TResult> FullOuterJoin<TOuter, TInner, TKey, TResult>(this IQueryable<TOuter> outer, IQueryable<TInner> inner, Expression<Func<TOuter, TKey>> outerKeySelector, Expression<Func<TInner, TKey>> innerKeySelector, Expression<Func<TOuter?, TInner?, TResult>> resultSelector)
    {
        ArgumentNullException.ThrowIfNull(outer);
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(outerKeySelector);
        ArgumentNullException.ThrowIfNull(innerKeySelector);
        ArgumentNullException.ThrowIfNull(resultSelector);

        MethodInfo method = new Func<IQueryable<TOuter>, IQueryable<TInner>, Expression<Func<TOuter, TKey>>, Expression<Func<TInner, TKey>>, Expression<Func<TOuter?, TInner?, TResult>>, IQueryable<TResult>>(FullOuterJoin).Method;

        return outer.Provider.CreateQuery<TResult>(
            Expression.Call(
                null,
                method,
                outer.Expression,
                inner.Expression,
                Expression.Quote(outerKeySelector),
                Expression.Quote(innerKeySelector),
                Expression.Quote(resultSelector)));
    }

    /// <summary>
    /// Executes the query and deletes the records from the database.
    /// </summary>
    public static int ExecuteDelete<T>(this IQueryable<T> source)
    {
        if (source is not BaseSQLiteQueryable table)
        {
            throw new InvalidOperationException($"Queryable must be of type {typeof(BaseSQLiteQueryable)}.");
        }

        EnsureWritable(source.Expression);

        SQLTranslator translator = new(table.Database)
        {
            QueryType = QueryType.Delete,
        };
        SQLQuery query = translator.Translate(source.Expression);

        return table.Database.CreateCommand(query.Sql, query.Parameters).ExecuteNonQuery();
    }

    /// <summary>
    /// Executes the query and deletes the records from the database.
    /// </summary>
    public static int ExecuteDelete<T>(this IQueryable<T> source, Expression<Func<T, bool>> predicate)
    {
        if (source is not BaseSQLiteQueryable table)
        {
            throw new InvalidOperationException($"Queryable must be of type {typeof(BaseSQLiteQueryable)}.");
        }

        EnsureWritable(source.Expression);

        SQLTranslator translator = new(table.Database)
        {
            QueryType = QueryType.Delete,
        };
        SQLQuery query = translator.Translate(source.Where(predicate).Expression);

        return table.Database.CreateCommand(query.Sql, query.Parameters).ExecuteNonQuery();
    }

    /// <summary>
    /// Executes the query and updates the records in the database.
    /// </summary>
    [UnconditionalSuppressMessage("AOT", "IL2072", Justification = "From type is rooted by the user Table<T>().")]
    public static int ExecuteUpdate<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(this IQueryable<T> source, Func<SQLitePropertyCalls<T>, SQLitePropertyCalls<T>> setters)
    {
        if (source is not BaseSQLiteQueryable table)
        {
            throw new InvalidOperationException($"Queryable must be of type {typeof(BaseSQLiteQueryable)}.");
        }

        EnsureWritable(source.Expression);

        SQLTranslator translator = new(table.Database)
        {
            QueryType = QueryType.Update,
        };
        translator.Visit(source.Expression);

        TableMapping targetMapping = table.Database.TableMapping(translator.Visitor.From!.Type);
        SQLitePropertyCalls<T> propertyCalls = new(translator.Visitor, targetMapping);
        translator.SetProperties = setters(propertyCalls).SetProperties;

        SQLQuery query = translator.Translate(null);

        return table.Database.CreateCommand(query.Sql, query.Parameters).ExecuteNonQuery();
    }

    /// <summary>
    /// Wraps <paramref name="source" /> so that the next <c>ExecuteDelete</c> or
    /// <c>ExecuteUpdate</c> call emits a SQLite <c>RETURNING *</c> clause and returns the
    /// projected (full entity) rows instead of the row count.
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
    public static SQLiteReturningQueryable<T, T> Returning<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this IQueryable<T> source)
    {
        ArgumentNullException.ThrowIfNull(source);
        ParameterExpression parameter = Expression.Parameter(typeof(T), "x");
        Expression<Func<T, T>> identity = Expression.Lambda<Func<T, T>>(parameter, parameter);
        return new SQLiteReturningQueryable<T, T>(source, identity);
    }

    /// <summary>
    /// Wraps <paramref name="source" /> with a projection so that the next <c>ExecuteDelete</c>
    /// or <c>ExecuteUpdate</c> call emits a SQLite <c>RETURNING <em>projection</em></c> clause
    /// and returns the projected rows instead of the row count.
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
    public static SQLiteReturningQueryable<T, TResult> Returning<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] TResult>(this IQueryable<T> source, Expression<Func<T, TResult>> projection)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(projection);
        return new SQLiteReturningQueryable<T, TResult>(source, projection);
    }

    /// <summary>
    /// Executes a <c>DELETE ... RETURNING <em>projection</em></c> against the wrapped source and
    /// returns the deleted rows projected through the wrapper's projection lambda. The projection
    /// goes through the same pipeline as <see cref="Queryable.Select{TSource, TResult}(IQueryable{TSource}, Expression{Func{TSource, TResult}})" />,
    /// so a matching source-generated materializer is used when one is registered.
    /// </summary>
    public static List<TResult> ExecuteDelete<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] TResult>(this SQLiteReturningQueryable<T, TResult> returning)
    {
        ArgumentNullException.ThrowIfNull(returning);
        EnsureWritable(returning.Source.Expression);
#if SQLITE_FRAMEWORK_VERSION_AWARE
        returning.Database.Options.EnsureMinimumVersion(SQLiteMinimumVersion.V3_35, "RETURNING");
#endif

        SQLTranslator translator = new(returning.Database)
        {
            QueryType = QueryType.Delete,
            EmitReturning = true,
            OmitTableAlias = true,
        };
        IQueryable<TResult> projected = returning.Source.Select(returning.Projection);
        SQLQuery query = translator.Translate(projected.Expression);

        return returning.Database.CreateCommand(query.Sql, query.Parameters)
            .ExecuteQueryInternal<TResult>(query)
            .ToList();
    }

    /// <summary>
    /// Executes an <c>UPDATE ... RETURNING <em>projection</em></c> against the wrapped source
    /// and returns the post-update rows projected through the wrapper's projection lambda. The
    /// projection goes through the same pipeline as <c>Select</c>, so a matching
    /// source-generated materializer is used when one is registered.
    /// </summary>
    [UnconditionalSuppressMessage("AOT", "IL2072", Justification = "From type is rooted by the user Table<T>().")]
    public static List<TResult> ExecuteUpdate<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] TResult>(this SQLiteReturningQueryable<T, TResult> returning, Func<SQLitePropertyCalls<T>, SQLitePropertyCalls<T>> setters)
    {
        ArgumentNullException.ThrowIfNull(returning);
        EnsureWritable(returning.Source.Expression);
        ArgumentNullException.ThrowIfNull(setters);
#if SQLITE_FRAMEWORK_VERSION_AWARE
        returning.Database.Options.EnsureMinimumVersion(SQLiteMinimumVersion.V3_35, "RETURNING");
#endif

        SQLTranslator translator = new(returning.Database)
        {
            QueryType = QueryType.Update,
            EmitReturning = true,
            OmitTableAlias = true,
        };
        IQueryable<TResult> projected = returning.Source.Select(returning.Projection);
        translator.Visit(projected.Expression);

        TableMapping targetMapping = returning.Database.TableMapping(translator.Visitor.From!.Type);
        SQLitePropertyCalls<T> propertyCalls = new(translator.Visitor, targetMapping);
        translator.SetProperties = setters(propertyCalls).SetProperties;

        SQLQuery query = translator.Translate(null);

        return returning.Database.CreateCommand(query.Sql, query.Parameters)
            .ExecuteQueryInternal<TResult>(query)
            .ToList();
    }

    /// <summary>
    /// Converts the <see cref="IQueryable{T}"/> to a <see cref="SQLiteCommand"/>.
    /// </summary>
    public static SQLiteCommand ToSqlCommand<T>(this IQueryable<T> queryable)
    {
        BaseSQLiteQueryable table = (BaseSQLiteQueryable)queryable;
        SQLTranslator translator = new(table.Database);
        SQLQuery query = translator.Translate(queryable.Expression);

        return table.Database.CreateCommand(query.Sql, query.Parameters);
    }

    /// <summary>
    /// Converts the <see cref="IQueryable{T}"/> to a SQL string.
    /// </summary>
    public static string ToSql<T>(this IQueryable<T> queryable)
    {
        BaseSQLiteQueryable table = (BaseSQLiteQueryable)queryable;
        SQLTranslator translator = new(table.Database);
        SQLQuery query = translator.Translate(queryable.Expression);

        return query.Sql;
    }

    /// <summary>
    /// Runs <c>EXPLAIN QUERY PLAN</c> for <paramref name="source" /> and returns the result
    /// as a tree. Use <see cref="SQLiteQueryPlan.ToString" /> to get a printable text version.
    /// Requires SQLite 3.24.0 or newer for the four-column <c>(id, parent, notused, detail)</c>
    /// row format the helper parses.
    /// </summary>
#if SQLITE_FRAMEWORK_OS_BUNDLED_SQLITE
    [UnsupportedOSPlatform("android")]
    [SupportedOSPlatform("android30.0")]
    [UnsupportedOSPlatform("ios")]
    [SupportedOSPlatform("ios12.0")]
#endif
    public static SQLiteQueryPlan ExplainQueryPlan<T>(this IQueryable<T> source)
    {
        if (source is not BaseSQLiteQueryable table)
        {
            throw new InvalidOperationException($"Queryable must be of type {typeof(BaseSQLiteQueryable)}.");
        }

#if SQLITE_FRAMEWORK_VERSION_AWARE
        table.Database.Options.EnsureMinimumVersion(SQLiteMinimumVersion.V3_24, "EXPLAIN QUERY PLAN (4-column row format)");
#endif
        SQLTranslator translator = new(table.Database);
        SQLQuery query = translator.Translate(source.Expression);

        SQLiteCommand command = table.Database.CreateCommand("EXPLAIN QUERY PLAN " + query.Sql, query.Parameters);
        using SQLiteDataReader reader = command.ExecuteReader();

        List<(int Id, int ParentId, string Detail)> rows = [];
        while (reader.Read())
        {
            int id = (int)(long)reader.GetValue(0, reader.GetColumnType(0), typeof(long))!;
            int parentId = (int)(long)reader.GetValue(1, reader.GetColumnType(1), typeof(long))!;
            string detail = (string)reader.GetValue(3, reader.GetColumnType(3), typeof(string))!;
            rows.Add((id, parentId, detail));
        }

        return BuildPlan(rows);
    }

    /// <summary>
    /// Overrides the global source-generated materializer for this specific query and uses
    /// runtime reflection to materialize results instead.
    /// </summary>
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Method reference only builds an Expression tree.")]
    public static IQueryable<T> UseReflectionMaterializer<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this IQueryable<T> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return source.Provider.CreateQuery<T>(
            Expression.Call(
                null,
                new Func<IQueryable<T>, IQueryable<T>>(UseReflectionMaterializer).Method,
                source.Expression));
    }

    /// <summary>
    /// Concatenates the projected values of <paramref name="source" /> into one string, separated
    /// by <paramref name="separator" />. Emits a single
    /// <c>SELECT group_concat(column, separator) FROM ...</c> SQL query. Returns an empty string
    /// when the source has no rows, matching <see cref="string.Join(string, IEnumerable{string})" />.
    /// </summary>
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "The marker method is only used to build an Expression tree, it is never invoked.")]
    public static string StringJoin<T>(this IQueryable<T> source, string separator)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(separator);

        if (source is not BaseSQLiteQueryable sqliteSource)
        {
            throw new InvalidOperationException($"Queryable must be of type {typeof(BaseSQLiteQueryable)}.");
        }

        MethodInfo marker = typeof(QueryableExtensions)
            .GetMethod(nameof(GroupConcatMarker), BindingFlags.Static | BindingFlags.NonPublic)!
            .MakeGenericMethod(typeof(T));
        Expression callExpression = Expression.Call(marker, source.Expression, Expression.Constant(separator));

        return sqliteSource.Provider.Execute<string>(callExpression);
    }

    /// <summary>
    /// Computes the sum of the projected values as a <see cref="double" />. Emits a single
    /// <c>SELECT total(column) FROM ...</c> SQL query. Always returns a value: <c>0.0</c> when
    /// the source has no rows or every projected value is <see langword="null" />. Unlike
    /// <see cref="Queryable.Sum(IQueryable{double})" /> which delegates the empty case to LINQ,
    /// <c>Total</c> gets the zero default from SQLite itself.
    /// </summary>
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "The marker method is only used to build an Expression tree, it is never invoked.")]
    public static double Total<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, double>> selector)
    {
        return InvokeTotalMarker(source, selector);
    }

    /// <summary>
    /// Computes the sum of the projected decimal values as a <see cref="double" />. Emits a single
    /// <c>SELECT total(column) FROM ...</c> SQL query. Always returns a value: <c>0.0</c> when
    /// the source has no rows or every projected value is <see langword="null" />.
    /// </summary>
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "The marker method is only used to build an Expression tree, it is never invoked.")]
    public static double Total<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, decimal>> selector)
    {
        return InvokeTotalMarker(source, selector);
    }

    /// <summary>
    /// Computes the sum of the projected integer values as a <see cref="double" />. Emits a single
    /// <c>SELECT total(column) FROM ...</c> SQL query. Always returns a value: <c>0.0</c> when
    /// the source has no rows or every projected value is <see langword="null" />.
    /// </summary>
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "The marker method is only used to build an Expression tree, it is never invoked.")]
    public static double Total<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, int>> selector)
    {
        return InvokeTotalMarker(source, selector);
    }

    /// <summary>
    /// Computes the sum of the projected long values as a <see cref="double" />. Emits a single
    /// <c>SELECT total(column) FROM ...</c> SQL query. Always returns a value: <c>0.0</c> when
    /// the source has no rows or every projected value is <see langword="null" />.
    /// </summary>
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "The marker method is only used to build an Expression tree, it is never invoked.")]
    public static double Total<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, long>> selector)
    {
        return InvokeTotalMarker(source, selector);
    }

    /// <summary>
    /// Marker method that the SQL translator rewrites into SQLite's <c>group_concat</c> aggregate.
    /// Invoked indirectly when <see cref="string.Join(string, IEnumerable{string})" /> is called
    /// with an <see cref="IQueryable{T}" /> as the source inside a query expression, or by
    /// <see cref="StringJoin{T}" /> at the root. Throws <see cref="InvalidOperationException" />
    /// when called directly.
    /// </summary>
    internal static string GroupConcatMarker<T>(IQueryable<T> source, string separator)
    {
        throw new InvalidOperationException(
            "GroupConcatMarker is a marker for the SQL translator. " +
            "Use string.Join(separator, queryable) inside a query expression, or call queryable.StringJoin(separator).");
    }

    /// <summary>
    /// Marker method that the SQL translator rewrites into SQLite's <c>total</c> aggregate. Invoked
    /// indirectly by <see cref="Total{TSource}(IQueryable{TSource}, Expression{Func{TSource, double}})" />
    /// and its overloads. Throws <see cref="InvalidOperationException" /> when called directly.
    /// </summary>
    internal static double TotalMarker<TSource, TValue>(IQueryable<TSource> source, Expression<Func<TSource, TValue>> selector)
    {
        throw new InvalidOperationException(
            "TotalMarker is a marker for the SQL translator. " +
            "Call queryable.Total(selector) instead.");
    }

    private static IOrderedQueryable<T> ApplyOrderWithNulls<T, TKey>(IQueryable<T> source, Expression<Func<T, TKey>> keySelector, SQLiteNullsOrder nulls, MethodInfo method)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(keySelector);

        return (IOrderedQueryable<T>)source.Provider.CreateQuery<T>(
            Expression.Call(
                null,
                method,
                source.Expression,
                Expression.Quote(keySelector),
                Expression.Constant(nulls)));
    }

    private static SQLiteQueryPlan BuildPlan(List<(int Id, int ParentId, string Detail)> rows)
    {
        Dictionary<int, List<SQLiteQueryPlanNode>> childrenById = [];
        foreach ((int id, int _, string _) in rows)
        {
            childrenById[id] = [];
        }

        List<SQLiteQueryPlanNode> roots = [];
        foreach ((int id, int parentId, string detail) in rows)
        {
            SQLiteQueryPlanNode node = new()
            {
                Id = id,
                ParentId = parentId,
                Detail = detail,
                Children = childrenById[id],
            };

            if (parentId == 0 || !childrenById.TryGetValue(parentId, out List<SQLiteQueryPlanNode>? siblings))
            {
                roots.Add(node);
            }
            else
            {
                siblings.Add(node);
            }
        }

        return new SQLiteQueryPlan { Roots = roots };
    }

    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "The marker method is only used to build an Expression tree, it is never invoked.")]
    private static double InvokeTotalMarker<TSource, TValue>(IQueryable<TSource> source, Expression<Func<TSource, TValue>> selector)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(selector);

        if (source is not BaseSQLiteQueryable sqliteSource)
        {
            throw new InvalidOperationException($"Queryable must be of type {typeof(BaseSQLiteQueryable)}.");
        }

        MethodInfo marker = typeof(QueryableExtensions)
            .GetMethod(nameof(TotalMarker), BindingFlags.Static | BindingFlags.NonPublic)!
            .MakeGenericMethod(typeof(TSource), typeof(TValue));
        Expression callExpression = Expression.Call(marker, source.Expression, selector);

        return sqliteSource.Provider.Execute<double>(callExpression);
    }

    private static void EnsureWritable(Expression expression)
    {
        Expression current = expression;
        while (current is MethodCallExpression { Arguments.Count: > 0 } call)
        {
            current = call.Arguments[0];
        }

        if (current is ConstantExpression { Value: BaseSQLiteTable and not SQLiteTable })
        {
            throw new NotSupportedException(
                "ExecuteUpdate and ExecuteDelete are not supported on a read-only table, such as one returned by " +
                "Table<T>(schema) or ReadOnlyTable<T>(). Writes to an attached database use raw SQL.");
        }
    }
}
