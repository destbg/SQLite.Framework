namespace SQLite.Framework.Models;

/// <summary>
/// Fluent builder for creating a table and its associated indexes, CHECK constraints, and
/// computed columns at the same time. Reach an instance with
/// <see cref="SQLiteSchema.Table{T}" />.
/// </summary>
public sealed class SQLiteTableBuilder<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>
{
    private readonly SQLiteDatabase database;
    private readonly TableMapping mapping;
    private readonly List<ComputedColumnSpec> computed = [];
    private readonly List<CheckConstraintSpec> checks = [];
    private readonly List<IndexSpec> indexes = [];

    internal SQLiteTableBuilder(SQLiteSchema schema)
    {
        database = schema.Database;
        mapping = database.TableMapping<T>();
    }

    internal SQLiteDatabase Database => database;

    /// <summary>
    /// Adds a generated (computed) column. The column is computed from <paramref name="sql" /> on
    /// every read when <paramref name="stored" /> is <see langword="false" /> (the default), or
    /// stored on disk when <paramref name="stored" /> is <see langword="true" />.
    /// Requires SQLite 3.31.0 or newer.
    /// </summary>
    /// <param name="column">The property on the entity that maps to the computed column.</param>
    /// <param name="sql">Expression that produces the column value, translated to SQL.</param>
    /// <param name="stored">When <see langword="true" />, the column is stored on disk. Default is virtual.</param>
#if SQLITE_FRAMEWORK_OS_BUNDLED_SQLITE
    [UnsupportedOSPlatform("android")]
    [SupportedOSPlatform("android31.0")]
    [UnsupportedOSPlatform("ios")]
    [SupportedOSPlatform("ios14.0")]
#endif
    public SQLiteTableBuilder<T> Computed<TValue>(Expression<Func<T, TValue>> column, Expression<Func<T, TValue>> sql, bool stored = false)
    {
        ArgumentNullException.ThrowIfNull(column);
        ArgumentNullException.ThrowIfNull(sql);
#if SQLITE_FRAMEWORK_VERSION_AWARE
        database.Options.EnsureMinimumVersion(SQLiteMinimumVersion.V3_31, "Computed columns");
#endif

        TableColumn target = ResolveTargetColumn(column);
        string expressionSql = TranslateBareSql(sql);

        computed.Add(new ComputedColumnSpec(target, expressionSql, stored));
        return this;
    }

    /// <summary>
    /// Adds a table-level CHECK constraint. <paramref name="predicate" /> is translated to SQL the
    /// same way <c>Where</c> clauses are.
    /// </summary>
    /// <param name="predicate">The condition every row must satisfy.</param>
    /// <param name="name">Optional constraint name. When set, emits
    /// <c>CONSTRAINT &lt;name&gt; CHECK (...)</c>. Otherwise emits a bare <c>CHECK (...)</c>.</param>
    public SQLiteTableBuilder<T> Check(Expression<Func<T, bool>> predicate, string? name = null)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        string sql = TranslateBareSql(predicate);
        checks.Add(new CheckConstraintSpec(sql, name));
        return this;
    }

    /// <summary>
    /// Adds an index. Pass a single property to create a single-column index, an anonymous
    /// object (<c>b =&gt; new { b.A, b.B }</c>) to create a composite index, or any expression
    /// (<c>b =&gt; b.Title.ToLower()</c>) to create an expression index. Optionally limit the
    /// index to rows matching <paramref name="filter" /> for a partial index. Use
    /// <paramref name="direction" /> or <paramref name="directions" /> to store one or more
    /// slots in descending order, which lets the planner skip the sort step for matching
    /// <c>ORDER BY x DESC</c> queries.
    /// </summary>
    /// <param name="column">Column, columns, or expression to index. Composite indexes can mix
    /// plain properties and expressions.</param>
    /// <param name="name">Optional index name. The default is <c>idx_{TableName}_{ColumnName}</c>
    /// for plain-property indexes. Required when the body contains any expression that is not a
    /// plain property access.</param>
    /// <param name="unique">Whether the index is unique.</param>
    /// <param name="filter">Optional predicate that produces a partial index (<c>WHERE</c> clause).</param>
    /// <param name="collation">Collation applied to every slot of the index. The default
    /// <see cref="SQLiteCollation.Inherit" /> emits no clause so the column's declared collation
    /// wins. Use the <paramref name="collations" /> parameter for per-slot collations on a
    /// composite index.</param>
    /// <param name="collations">Per-slot collations for a composite index. Must match the
    /// number of slots. When set, takes precedence over <paramref name="collation" />.</param>
    /// <param name="direction">Sort direction applied to every slot of the index. The default
    /// <see cref="SQLiteIndexDirection.Inherit" /> emits no clause. Use the
    /// <paramref name="directions" /> parameter for per-slot directions on a composite index.</param>
    /// <param name="directions">Per-slot sort directions for a composite index. Must match the
    /// number of slots. When set, takes precedence over <paramref name="direction" />.</param>
#if SQLITE_FRAMEWORK_OS_BUNDLED_SQLITE
    [UnsupportedOSPlatform("android")]
    [SupportedOSPlatform("android24.0")]
    [UnsupportedOSPlatform("ios")]
    [SupportedOSPlatform("ios10.0")]
#endif
    public SQLiteTableBuilder<T> Index<TKey>(Expression<Func<T, TKey>> column, string? name = null, bool unique = false, Expression<Func<T, bool>>? filter = null, SQLiteCollation collation = SQLiteCollation.Inherit, SQLiteCollation[]? collations = null, SQLiteIndexDirection direction = SQLiteIndexDirection.Inherit, SQLiteIndexDirection[]? directions = null)
    {
        ArgumentNullException.ThrowIfNull(column);

        Expression body = column.Body;
        if (body is UnaryExpression unary) body = unary.Operand;

        ParameterExpression rowParameter = column.Parameters[0];
        string[] items;
        string[]? plainColumnNames;

        if (body is NewExpression newExpr)
        {
            items = new string[newExpr.Arguments.Count];
            string[] memberNames = new string[newExpr.Arguments.Count];
            bool allPlain = true;
            for (int i = 0; i < newExpr.Arguments.Count; i++)
            {
                Expression arg = newExpr.Arguments[i];
                if (arg is UnaryExpression u) arg = u.Operand;

                if (IsPlainRowMember(arg, rowParameter, out MemberExpression? mem))
                {
                    TableColumn col = mapping.Columns.FirstOrDefault(c => c.PropertyInfo.Name == mem.Member.Name)
                        ?? throw new ArgumentException($"Property '{mem.Member.Name}' is not mapped on {typeof(T).Name}.", nameof(column));
                    items[i] = col.Name;
                    memberNames[i] = col.Name;
                }
                else
                {
#if SQLITE_FRAMEWORK_VERSION_AWARE
                    database.Options.EnsureMinimumVersion(SQLiteMinimumVersion.V3_9, "Expression indexes");
#endif
                    items[i] = "(" + TranslateBareSql(rowParameter, arg) + ")";
                    allPlain = false;
                }
            }
            plainColumnNames = allPlain ? memberNames : null;
        }
        else if (IsPlainRowMember(body, rowParameter, out MemberExpression? plainMember))
        {
            TableColumn target = mapping.Columns.FirstOrDefault(c => c.PropertyInfo.Name == plainMember.Member.Name)
                ?? throw new ArgumentException($"Property '{plainMember.Member.Name}' is not mapped on {typeof(T).Name}.", nameof(column));
            items = [target.Name];
            plainColumnNames = [target.Name];
        }
        else
        {
#if SQLITE_FRAMEWORK_VERSION_AWARE
            database.Options.EnsureMinimumVersion(SQLiteMinimumVersion.V3_9, "Expression indexes");
#endif
            items = ["(" + TranslateBareSql(rowParameter, body) + ")"];
            plainColumnNames = null;
        }

        SQLiteCollation[] columnCollations;
        if (collations != null)
        {
            if (collations.Length != items.Length)
                throw new ArgumentException($"Expected {items.Length} collation(s) to match the number of indexed columns, got {collations.Length}.", nameof(collations));
            columnCollations = collations;
        }
        else
        {
            columnCollations = new SQLiteCollation[items.Length];
            Array.Fill(columnCollations, collation);
        }

        SQLiteIndexDirection[] columnDirections;
        if (directions != null)
        {
            if (directions.Length != items.Length)
            {
                throw new ArgumentException($"Expected {items.Length} direction(s) to match the number of indexed columns, got {directions.Length}.", nameof(directions));
            }

            columnDirections = directions;
        }
        else
        {
            columnDirections = new SQLiteIndexDirection[items.Length];
            Array.Fill(columnDirections, direction);
        }

        string indexName;
        if (name != null)
        {
            indexName = name;
        }
        else if (plainColumnNames != null)
        {
            indexName = $"idx_{mapping.TableName}_{string.Join("_", plainColumnNames)}";
        }
        else
        {
            throw new ArgumentException("Expression indexes require an explicit 'name'. The framework cannot derive a stable default name from a translated SQL expression.", nameof(name));
        }

        string? filterSql = filter == null ? null : TranslateBareSql(filter);

        indexes.Add(new IndexSpec(items, columnCollations, columnDirections, indexName, unique, filterSql));
        return this;
    }

    private static bool IsPlainRowMember(Expression expr, ParameterExpression rowParameter, [NotNullWhen(true)] out MemberExpression? member)
    {
        if (expr is MemberExpression me && me.Expression == rowParameter)
        {
            member = me;
            return true;
        }

        member = null;
        return false;
    }

    /// <summary>
    /// Adds a foreign key from <paramref name="column" /> to the primary key of
    /// <typeparamref name="TParent" />. Use the overload that takes a target column selector for
    /// non-PK targets or for composite keys.
    /// </summary>
    public SQLiteTableBuilder<T> ForeignKey<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TParent>(Expression<Func<T, object?>> column, SQLiteForeignKeyAction onDelete = SQLiteForeignKeyAction.NoAction, SQLiteForeignKeyAction onUpdate = SQLiteForeignKeyAction.NoAction, bool deferred = false)
    {
        ArgumentNullException.ThrowIfNull(column);
        return ForeignKeyCore<TParent>(column, target: null, onDelete, onUpdate, deferred);
    }

    /// <summary>
    /// Adds a foreign key from <paramref name="column" /> to <paramref name="targetColumn" /> on
    /// <typeparamref name="TParent" />. Supports composite foreign keys when both selectors
    /// project an anonymous tuple of the same arity (for example
    /// <c>l =&gt; new { l.A, l.B }</c>).
    /// </summary>
    public SQLiteTableBuilder<T> ForeignKey<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TParent>(Expression<Func<T, object?>> column, Expression<Func<TParent, object?>> targetColumn, SQLiteForeignKeyAction onDelete = SQLiteForeignKeyAction.NoAction, SQLiteForeignKeyAction onUpdate = SQLiteForeignKeyAction.NoAction, bool deferred = false)
    {
        ArgumentNullException.ThrowIfNull(column);
        ArgumentNullException.ThrowIfNull(targetColumn);
        return ForeignKeyCore(column, targetColumn, onDelete, onUpdate, deferred);
    }

    /// <summary>
    /// Marks the table as a SQLite <c>STRICT</c> table. SQLite enforces declared column types on
    /// every insert and update instead of accepting any type. Requires SQLite 3.37.0 or newer.
    /// </summary>
#if SQLITE_FRAMEWORK_OS_BUNDLED_SQLITE
    [UnsupportedOSPlatform("android")]
    [SupportedOSPlatform("android34.0")]
    [UnsupportedOSPlatform("ios")]
    [SupportedOSPlatform("ios16.0")]
#endif
    public SQLiteTableBuilder<T> Strict()
    {
        mapping.Strict = true;
        return this;
    }

    /// <summary>
    /// Sets a literal <c>DEFAULT</c> for the column. <c>CreateTable</c> writes it into the column
    /// definition, and the framework omits the column from <c>Add</c>/<c>AddRange</c> inserts when
    /// the CLR value equals <c>default(TValue)</c>, so SQLite applies the default. To insert the
    /// CLR-default value explicitly, the user must use raw SQL or change the property type.
    /// </summary>
    public SQLiteTableBuilder<T> Default<TValue>(Expression<Func<T, TValue>> column, TValue value)
    {
        ArgumentNullException.ThrowIfNull(column);
        TableColumn col = ResolveTargetColumn(column);
        col.DefaultSql = SqlLiteralHelper.FormatLiteral(value);
        return this;
    }

    /// <summary>
    /// Sets the column's <c>DEFAULT</c> to the SQL produced by translating
    /// <paramref name="defaultExpression" />. The body must be a parameterless lambda; constants
    /// are inlined as SQL literals. SQLite requires the resulting expression to be deterministic
    /// and to not reference any row.
    /// </summary>
    public SQLiteTableBuilder<T> Default<TValue>(Expression<Func<T, TValue>> column, Expression<Func<TValue>> defaultExpression)
    {
        ArgumentNullException.ThrowIfNull(column);
        ArgumentNullException.ThrowIfNull(defaultExpression);
#if SQLITE_FRAMEWORK_VERSION_AWARE
        database.Options.EnsureMinimumVersion(SQLiteMinimumVersion.V3_31, "Column DEFAULT with computed expression");
#endif
        TableColumn col = ResolveTargetColumn(column);
        col.DefaultSql = "(" + DefaultExpressionTranslator.Translate(database, defaultExpression, nameof(defaultExpression)) + ")";
        return this;
    }

    private SQLiteTableBuilder<T> ForeignKeyCore<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TParent>(Expression<Func<T, object?>> column, Expression<Func<TParent, object?>>? target, SQLiteForeignKeyAction onDelete, SQLiteForeignKeyAction onUpdate, bool deferred)
    {
        string[] sourcePropertyNames = ResolvePropertyNames(column.Body);
        bool[] sourceNullability = new bool[sourcePropertyNames.Length];
        string[] sourceColumnNames = new string[sourcePropertyNames.Length];
        for (int i = 0; i < sourcePropertyNames.Length; i++)
        {
            TableColumn col = mapping.Columns.FirstOrDefault(c => c.PropertyInfo.Name == sourcePropertyNames[i])
                ?? throw new ArgumentException($"Property '{sourcePropertyNames[i]}' is not mapped on {typeof(T).Name}.", nameof(column));
            sourceColumnNames[i] = col.Name;
            sourceNullability[i] = col.IsNullable;
        }

        string[]? targetPropertyNames = target == null ? null : ResolvePropertyNames(target.Body);
        (string targetTable, string[] targetColumns) = ForeignKeyResolver.ResolveTargets(
            sourceTable: mapping.TableName,
            sourceColumns: sourceColumnNames,
            typeof(TParent),
            targetPropertyNames);

        ForeignKeyResolver.ValidateSetNullCompatibility(
            sourceTable: mapping.TableName,
            sourceColumns: sourceColumnNames,
            sourceNullability: sourceNullability,
            onDelete,
            onUpdate);

        ForeignKeyInfo info = new(
            columns: sourceColumnNames,
            targetTable: targetTable,
            targetColumns: targetColumns,
            onDelete: onDelete,
            onUpdate: onUpdate,
            deferred: deferred);

        if (sourceColumnNames.Length == 1)
        {
            TableColumn column0 = mapping.Columns.First(c => c.Name == sourceColumnNames[0]);
            if (column0.ForeignKey != null)
            {
                throw new InvalidOperationException(
                    $"Column \"{mapping.TableName}\".\"{column0.Name}\" already has a foreign key declared via [ForeignKey].");
            }
            column0.ForeignKey = info;
        }
        else
        {
            mapping.AddCompositeForeignKey(info);
        }

        return this;
    }

    /// <summary>
    /// Emits the <c>CREATE TABLE IF NOT EXISTS</c> statement plus any indexes recorded on the
    /// builder. Returns the total number of statements run.
    /// </summary>
    public int CreateTable()
    {
        if (mapping.IsFullTextSearch)
        {
            if (computed.Count > 0 || checks.Count > 0 || indexes.Count > 0)
            {
                throw new InvalidOperationException($"FTS5 entity '{typeof(T).Name}' does not support Computed, Check, or Index from the fluent builder. Remove those calls.");
            }

            return database.Schema.CreateTable<T>();
        }

        TableColumn[] primaryKeyColumns = mapping.Columns.Where(c => c.IsPrimaryKey).ToArray();
        bool hasCompositePrimaryKey = primaryKeyColumns.Length > 1;

        StringBuilder sb = new();
        sb.Append("CREATE TABLE IF NOT EXISTS \"");
        sb.Append(mapping.TableName);
        sb.Append("\" (");

        bool first = true;
        foreach (TableColumn col in mapping.Columns)
        {
            if (!first) sb.Append(", ");
            first = false;

            ComputedColumnSpec? cc = computed.FirstOrDefault(c => c.Column.Name == col.Name);
            if (cc != null)
            {
                sb.Append(col.Name);
                sb.Append(' ');
                sb.Append(col.ColumnType.ToString().ToUpperInvariant());
                sb.Append(" GENERATED ALWAYS AS (");
                sb.Append(cc.ExpressionSql);
                sb.Append(") ");
                sb.Append(cc.Stored ? "STORED" : "VIRTUAL");
            }
            else
            {
                sb.Append(col.GetCreateColumnSql(!hasCompositePrimaryKey));
            }
        }

        if (hasCompositePrimaryKey)
        {
            sb.Append(", PRIMARY KEY (");
            for (int i = 0; i < primaryKeyColumns.Length; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append('"');
                sb.Append(primaryKeyColumns[i].Name);
                sb.Append('"');
            }
            sb.Append(')');
        }

        foreach (CheckConstraintSpec check in checks)
        {
            sb.Append(", ");
            if (!string.IsNullOrEmpty(check.Name))
            {
                sb.Append("CONSTRAINT \"");
                sb.Append(check.Name.Replace("\"", "\"\""));
                sb.Append("\" ");
            }
            sb.Append("CHECK (");
            sb.Append(check.Sql);
            sb.Append(')');
        }

        foreach (ForeignKeyInfo composite in mapping.CompositeForeignKeys)
        {
            sb.Append(", ");
            composite.WriteSql(sb, inline: false);
        }

        sb.Append(')');

        if (mapping.WithoutRowId)
        {
            sb.Append(" WITHOUT ROWID");
        }

        if (mapping.Strict)
        {
#if SQLITE_FRAMEWORK_VERSION_AWARE
            database.Options.EnsureMinimumVersion(SQLiteMinimumVersion.V3_37, "STRICT tables");
#endif
            sb.Append(mapping.WithoutRowId ? ", STRICT" : " STRICT");
        }

        int count = database.CreateCommand(sb.ToString(), []).ExecuteNonQuery();

        var indexGroups = mapping.Columns
            .SelectMany(col => col.Indices.Select(idx => (
                Name: idx.Name ?? ("idx_" + col.Name + "_" + idx.Order),
                Column: col.Name,
                Order: idx.Order,
                IsUnique: idx.IsUnique,
                Collation: idx.Collation,
                Direction: idx.Direction)))
            .GroupBy(x => x.Name);

        foreach (var group in indexGroups)
        {
            var ordered = group.OrderBy(x => x.Order).ToArray();
            string uniqueClause = group.Any(x => x.IsUnique) ? "UNIQUE " : string.Empty;
            string columnList = string.Join(", ", ordered.Select(x => x.Column + CollationHelper.Clause(x.Collation) + IndexDirectionHelper.Clause(x.Direction)));
            string sql = $"CREATE {uniqueClause}INDEX IF NOT EXISTS \"{group.Key}\" ON \"{mapping.TableName}\" ({columnList})";
            count += database.CreateCommand(sql, []).ExecuteNonQuery();
        }

        foreach (IndexSpec index in indexes)
        {
            string uniqueClause = index.Unique ? "UNIQUE " : string.Empty;
            string columnList = string.Join(", ", index.Columns.Select((c, i) => c + CollationHelper.Clause(index.Collations[i]) + IndexDirectionHelper.Clause(index.Directions[i])));
            string where = index.FilterSql == null ? string.Empty : $" WHERE {index.FilterSql}";
            string sql = $"CREATE {uniqueClause}INDEX IF NOT EXISTS \"{index.Name.Replace("\"", "\"\"")}\" ON \"{mapping.TableName}\" ({columnList}){where}";
            count += database.CreateCommand(sql, []).ExecuteNonQuery();
        }

        return count;
    }

    private TableColumn ResolveTargetColumn<TKey>(Expression<Func<T, TKey>> column)
    {
        if (column.Body is not MemberExpression member)
        {
            throw new ArgumentException("Expected a property access expression on the entity, like b => b.Title.", nameof(column));
        }

        TableColumn col = mapping.Columns.FirstOrDefault(c => c.PropertyInfo.Name == member.Member.Name)
            ?? throw new ArgumentException($"Property '{member.Member.Name}' is not mapped on {typeof(T).Name}.", nameof(column));

        return col;
    }

    private string TranslateBareSql(LambdaExpression lambda)
    {
        return TranslateBareSql(lambda.Parameters[0], lambda.Body);
    }

    private string TranslateBareSql(ParameterExpression rowParameter, Expression body)
    {
        SQLVisitor visitor = new(database, new SQLiteCounters(), 0);

        Dictionary<string, Expression> columnExpressions = mapping.Columns.ToDictionary(
            c => c.PropertyInfo.Name,
            Expression (c) => SQLiteExpression.Leaf(c.PropertyType, visitor.Counters.NextIdentifier(), c.Name));

        visitor.MethodArguments[rowParameter] = columnExpressions;

        Expression result = visitor.Visit(body);
        if (result is not SQLiteExpression sqlExpr)
        {
            throw new ArgumentException($"Expression '{body}' could not be translated to SQL.", nameof(body));
        }

        return SqlLiteralHelper.InlineParameters(sqlExpr.ToString(), sqlExpr.Parameters ?? []);
    }

    private static string[] ResolvePropertyNames(Expression body)
    {
        if (body is UnaryExpression unary)
        {
            body = unary.Operand;
        }

        if (body is NewExpression newExpr)
        {
            string[] names = new string[newExpr.Arguments.Count];
            for (int i = 0; i < newExpr.Arguments.Count; i++)
            {
                Expression arg = newExpr.Arguments[i];
                if (arg is UnaryExpression u)
                {
                    arg = u.Operand;
                }
                if (arg is not MemberExpression mem)
                {
                    throw new ArgumentException("Foreign key tuple expressions must be plain property accesses like b => new { b.A, b.B }.");
                }
                names[i] = mem.Member.Name;
            }
            return names;
        }

        if (body is MemberExpression member)
        {
            return [member.Member.Name];
        }

        throw new ArgumentException("Expected a property access expression like b => b.Id, or an anonymous tuple like b => new { b.A, b.B }.");
    }
}
