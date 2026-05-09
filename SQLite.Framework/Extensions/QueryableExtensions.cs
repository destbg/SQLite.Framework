namespace SQLite.Framework.Extensions;

/// <summary>
/// <see cref="Queryable"/> extensions for <see cref="IQueryable{T}"/>.
/// </summary>
public static class QueryableExtensions
{
    /// <summary>
    /// Tells the framework to skip every <see cref="SQLiteOptions.QueryFilters" /> entry that would
    /// otherwise apply to this query. Combine with <c>Where</c> as usual: the user's <c>Where</c>
    /// still runs; only the framework-injected filters are dropped. The opt-out applies to the
    /// entire wrapped subtree, including <c>Join</c>-ed tables.
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
    /// Executes the query and deletes the records from the database.
    /// </summary>
    public static int ExecuteDelete<T>(this IQueryable<T> source)
    {
        if (source is not BaseSQLiteQueryable table)
        {
            throw new InvalidOperationException($"Queryable must be of type {typeof(BaseSQLiteQueryable)}.");
        }

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
    public static int ExecuteUpdate<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(this IQueryable<T> source, Func<SQLitePropertyCalls<T>, SQLitePropertyCalls<T>> setters)
    {
        if (source is not BaseSQLiteQueryable table)
        {
            throw new InvalidOperationException($"Queryable must be of type {typeof(BaseSQLiteQueryable)}.");
        }

        SQLTranslator translator = new(table.Database)
        {
            QueryType = QueryType.Update,
        };
        translator.Visit(source.Expression);

        SQLitePropertyCalls<T> propertyCalls = new(translator.Visitor, table.Database.TableMapping<T>());
        translator.SetProperties = setters(propertyCalls).SetProperties;

        SQLQuery query = translator.Translate(null);

        return table.Database.CreateCommand(query.Sql, query.Parameters).ExecuteNonQuery();
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
}
