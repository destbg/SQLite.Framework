namespace SQLite.Framework;

/// <summary>
/// Configures one entity's schema inside <see cref="SQLiteDatabase.OnModelCreating" />. This is the
/// single place to declare the table name, primary key, columns, computed columns, CHECK
/// constraints, indexes, foreign keys, defaults, STRICT, WITHOUT ROWID, and triggers. Everything the
/// mapping attributes can do is available here, plus columns that have no CLR property. The
/// configuration is written onto the model, so create, migrate, and validate all read the same
/// definition.
/// </summary>
public sealed class SQLiteEntityTypeBuilder<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>
{
    private readonly SQLiteDatabase database;
    private readonly TableMapping mapping;

    internal SQLiteEntityTypeBuilder(SQLiteDatabase database)
    {
        this.database = database;
        mapping = database.TableMapping<T>();
    }

    /// <summary>
    /// Sets the table name, the same as the <c>[Table]</c> attribute.
    /// </summary>
    /// <param name="name">The database table name.</param>
    public SQLiteEntityTypeBuilder<T> ToTable(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        IdentifierGuard.EnsureNoQuote(name, "Table");
        mapping.TableName = name;
        return this;
    }

    /// <summary>
    /// Marks the table as WITHOUT ROWID, the same as the <c>[WithoutRowId]</c> attribute. The table
    /// must have a primary key.
    /// </summary>
    public SQLiteEntityTypeBuilder<T> WithoutRowId()
    {
        mapping.WithoutRowId = true;
        return this;
    }

    /// <summary>
    /// Marks the table as STRICT, so SQLite enforces declared column types on every insert and
    /// update. Requires SQLite 3.37.0 or newer.
    /// </summary>
#if SQLITE_FRAMEWORK_OS_BUNDLED_SQLITE
    [UnsupportedOSPlatform("android")]
    [SupportedOSPlatform("android34.0")]
    [UnsupportedOSPlatform("ios")]
    [SupportedOSPlatform("ios16.0")]
#endif
    public SQLiteEntityTypeBuilder<T> Strict()
    {
        mapping.Strict = true;
        return this;
    }

    /// <summary>
    /// Sets the primary key, the same as the <c>[Key]</c> attribute. Pass a single property for a
    /// single-column key, or an anonymous object (<c>b =&gt; new { b.A, b.B }</c>) for a composite
    /// key. This replaces any primary key already declared on the columns.
    /// </summary>
    /// <param name="key">The key property or properties.</param>
    public SQLiteEntityTypeBuilder<T> HasKey<TKey>(Expression<Func<T, TKey>> key)
    {
        ArgumentNullException.ThrowIfNull(key);
        string[] names = ResolvePropertyNames(key.Body);

        foreach (TableColumn column in mapping.Columns)
        {
            column.IsPrimaryKey = false;
        }

        foreach (string name in names)
        {
            TableColumn column = mapping.Columns.FirstOrDefault(c => c.PropertyInfo.Name == name)
                ?? throw new ArgumentException($"Property '{name}' is not mapped on {typeof(T).Name}.", nameof(key));
            column.IsPrimaryKey = true;
            column.IsNullable = false;
        }

        return this;
    }

    /// <summary>
    /// Marks the column as an auto-incrementing primary key, the same as the
    /// <c>[AutoIncrement]</c> attribute.
    /// </summary>
    /// <param name="column">The key property.</param>
    public SQLiteEntityTypeBuilder<T> AutoIncrement<TValue>(Expression<Func<T, TValue>> column)
    {
        ArgumentNullException.ThrowIfNull(column);
        ResolveTargetColumn(column).IsAutoIncrement = true;
        return this;
    }

    /// <summary>
    /// Sets the database column name, the same as the <c>[Column]</c> attribute.
    /// </summary>
    /// <param name="column">The property to rename.</param>
    /// <param name="name">The database column name.</param>
    public SQLiteEntityTypeBuilder<T> HasColumnName<TValue>(Expression<Func<T, TValue>> column, string name)
    {
        ArgumentNullException.ThrowIfNull(column);
        ArgumentException.ThrowIfNullOrEmpty(name);
        IdentifierGuard.EnsureNoQuote(name, "Column");
        ResolveTargetColumn(column).Name = name;
        return this;
    }

    /// <summary>
    /// Sets the SQLite storage type of the column, overriding the type inferred from the property.
    /// </summary>
    /// <param name="column">The property whose column type to set.</param>
    /// <param name="type">The SQLite column type.</param>
    public SQLiteEntityTypeBuilder<T> HasColumnType<TValue>(Expression<Func<T, TValue>> column, SQLiteColumnType type)
    {
        ArgumentNullException.ThrowIfNull(column);
        ResolveTargetColumn(column).ColumnType = type;
        return this;
    }

    /// <summary>
    /// Sets whether the column allows NULL. <c>IsRequired()</c> (the default) makes it NOT NULL, the
    /// same as the <c>[Required]</c> attribute. Pass <see langword="false" /> to allow NULL.
    /// </summary>
    /// <param name="column">The property whose nullability to set.</param>
    /// <param name="required">When <see langword="true" /> (the default), the column is NOT NULL.</param>
    public SQLiteEntityTypeBuilder<T> IsRequired<TValue>(Expression<Func<T, TValue>> column, bool required = true)
    {
        ArgumentNullException.ThrowIfNull(column);
        ResolveTargetColumn(column).IsNullable = !required;
        return this;
    }

    /// <summary>
    /// Removes the property from the model so it maps to no column, the same as the
    /// <c>[NotMapped]</c> attribute.
    /// </summary>
    /// <param name="column">The property to drop from the model.</param>
    public SQLiteEntityTypeBuilder<T> Ignore<TValue>(Expression<Func<T, TValue>> column)
    {
        ArgumentNullException.ThrowIfNull(column);
        mapping.RemoveColumn(ResolveTargetColumn(column));
        return this;
    }

    /// <summary>
    /// Adds a column that has no CLR property. The framework creates it and keeps it across a
    /// migrate rebuild, but never reads or writes it. Use it for columns driven by defaults,
    /// triggers, or other consumers. A NOT NULL column needs a default so inserts of the model
    /// succeed.
    /// </summary>
    /// <param name="name">The column name.</param>
    /// <param name="type">The SQLite column type.</param>
    /// <param name="nullable">When <see langword="true" /> (the default), the column allows NULL.</param>
    /// <param name="defaultSql">Optional raw SQL written as the column <c>DEFAULT</c>.</param>
    public SQLiteEntityTypeBuilder<T> Column(string name, SQLiteColumnType type, bool nullable = true, string? defaultSql = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        IdentifierGuard.EnsureNoQuote(name, "Column");
        mapping.AddShadowColumn(new ShadowColumnSpec(name, type, nullable, defaultSql));
        return this;
    }

    /// <summary>
    /// Adds a generated (computed) column. The column is computed from <paramref name="sql" /> on
    /// every read when <paramref name="stored" /> is <see langword="false" /> (the default), or
    /// stored on disk when it is <see langword="true" />. Requires SQLite 3.31.0 or newer.
    /// </summary>
    /// <param name="column">The property that maps to the computed column.</param>
    /// <param name="sql">Expression that produces the column value, translated to SQL.</param>
    /// <param name="stored">When <see langword="true" />, the column is stored on disk.</param>
#if SQLITE_FRAMEWORK_OS_BUNDLED_SQLITE
    [UnsupportedOSPlatform("android")]
    [SupportedOSPlatform("android31.0")]
    [UnsupportedOSPlatform("ios")]
    [SupportedOSPlatform("ios14.0")]
#endif
    public SQLiteEntityTypeBuilder<T> Computed<TValue>(Expression<Func<T, TValue>> column, Expression<Func<T, TValue>> sql, bool stored = false)
    {
        ArgumentNullException.ThrowIfNull(column);
        ArgumentNullException.ThrowIfNull(sql);
#if SQLITE_FRAMEWORK_VERSION_AWARE
        database.Options.EnsureMinimumVersion(SQLiteMinimumVersion.V3_31, "Computed columns");
#endif

        TableColumn target = ResolveTargetColumn(column);
        string expressionSql = TranslateBareSql(sql);

        mapping.AddComputedColumn(new ComputedColumnSpec(target, expressionSql, stored));
        return this;
    }

    /// <summary>
    /// Adds a table-level CHECK constraint. The predicate is translated to SQL the same way
    /// <c>Where</c> clauses are. Every row must satisfy it.
    /// </summary>
    /// <param name="predicate">The condition every row must satisfy.</param>
    /// <param name="name">Optional constraint name. When set, emits a named CONSTRAINT.</param>
    public SQLiteEntityTypeBuilder<T> Check(Expression<Func<T, bool>> predicate, string? name = null)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        string sql = TranslateBareSql(predicate);
        mapping.AddCheck(new CheckConstraintSpec(sql, name));
        return this;
    }

    /// <summary>
    /// Adds an index. Pass a single property for a single-column index, an anonymous object
    /// (<c>b =&gt; new { b.A, b.B }</c>) for a composite index, or any expression for an expression
    /// index. Optionally limit it to rows matching <paramref name="filter" /> for a partial index,
    /// and set collation or sort direction per slot.
    /// </summary>
    /// <param name="column">Column, columns, or expression to index.</param>
    /// <param name="name">Optional index name. Required when the body is not a plain property.</param>
    /// <param name="unique">Whether the index is unique.</param>
    /// <param name="filter">Optional predicate that produces a partial index.</param>
    /// <param name="collation">Collation applied to every slot.</param>
    /// <param name="collations">Per-slot collations for a composite index.</param>
    /// <param name="direction">Sort direction applied to every slot.</param>
    /// <param name="directions">Per-slot sort directions for a composite index.</param>
#if SQLITE_FRAMEWORK_OS_BUNDLED_SQLITE
    [UnsupportedOSPlatform("android")]
    [SupportedOSPlatform("android24.0")]
    [UnsupportedOSPlatform("ios")]
    [SupportedOSPlatform("ios10.0")]
#endif
    public SQLiteEntityTypeBuilder<T> Index<TKey>(Expression<Func<T, TKey>> column, string? name = null, bool unique = false, Expression<Func<T, bool>>? filter = null, SQLiteCollation collation = SQLiteCollation.Inherit, SQLiteCollation[]? collations = null, SQLiteIndexDirection direction = SQLiteIndexDirection.Inherit, SQLiteIndexDirection[]? directions = null)
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
                    items[i] = IdentifierGuard.Quote(col.Name);
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
            items = [IdentifierGuard.Quote(target.Name)];
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

        mapping.AddIndex(new IndexSpec(items, columnCollations, columnDirections, indexName, unique, filterSql));
        return this;
    }

    /// <summary>
    /// Declares a single-column foreign key from <paramref name="column" /> to the primary key of
    /// <typeparamref name="TParent" />, the same as the <c>[ReferencesTable]</c> attribute. Use the
    /// overload that takes a target selector for non-PK targets or composite keys.
    /// </summary>
    /// <param name="column">The source property.</param>
    /// <param name="onDelete">Action on delete of the parent row.</param>
    /// <param name="onUpdate">Action on update of the parent key.</param>
    /// <param name="deferred">When <see langword="true" />, the constraint is deferred.</param>
    public SQLiteEntityTypeBuilder<T> ForeignKey<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TParent>(Expression<Func<T, object?>> column, SQLiteForeignKeyAction onDelete = SQLiteForeignKeyAction.NoAction, SQLiteForeignKeyAction onUpdate = SQLiteForeignKeyAction.NoAction, bool deferred = false)
    {
        ArgumentNullException.ThrowIfNull(column);
        ForeignKeyCore<TParent>(column, target: null, onDelete, onUpdate, deferred);
        return this;
    }

    /// <summary>
    /// Declares a foreign key from <paramref name="column" /> to <paramref name="targetColumn" /> on
    /// <typeparamref name="TParent" />. Supports composite foreign keys when both selectors project
    /// an anonymous tuple of the same arity (for example <c>l =&gt; new { l.A, l.B }</c>).
    /// </summary>
    /// <param name="column">The source property, or an anonymous tuple.</param>
    /// <param name="targetColumn">The parent property, or an anonymous tuple, to point at.</param>
    /// <param name="onDelete">Action on delete of the parent row.</param>
    /// <param name="onUpdate">Action on update of the parent key.</param>
    /// <param name="deferred">When <see langword="true" />, the constraint is deferred.</param>
    public SQLiteEntityTypeBuilder<T> ForeignKey<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TParent>(Expression<Func<T, object?>> column, Expression<Func<TParent, object?>> targetColumn, SQLiteForeignKeyAction onDelete = SQLiteForeignKeyAction.NoAction, SQLiteForeignKeyAction onUpdate = SQLiteForeignKeyAction.NoAction, bool deferred = false)
    {
        ArgumentNullException.ThrowIfNull(column);
        ArgumentNullException.ThrowIfNull(targetColumn);
        ForeignKeyCore(column, targetColumn, onDelete, onUpdate, deferred);
        return this;
    }

    /// <summary>
    /// Sets a literal <c>DEFAULT</c> for the column. The framework omits the column from inserts when
    /// the CLR value equals <c>default(TValue)</c>, so SQLite applies the default.
    /// </summary>
    /// <param name="column">The property whose default to set.</param>
    /// <param name="value">The default value, written as a SQL literal.</param>
    public SQLiteEntityTypeBuilder<T> Default<TValue>(Expression<Func<T, TValue>> column, TValue value)
    {
        ArgumentNullException.ThrowIfNull(column);
        ResolveTargetColumn(column).DefaultSql = SqlLiteralHelper.FormatLiteral(value);
        return this;
    }

    /// <summary>
    /// Sets the column's <c>DEFAULT</c> to the SQL produced by translating
    /// <paramref name="defaultExpression" />. The body must be a parameterless lambda and must be
    /// deterministic and not reference any row.
    /// </summary>
    /// <param name="column">The property whose default to set.</param>
    /// <param name="defaultExpression">Expression that produces the default value.</param>
    public SQLiteEntityTypeBuilder<T> Default<TValue>(Expression<Func<T, TValue>> column, Expression<Func<TValue>> defaultExpression)
    {
        ArgumentNullException.ThrowIfNull(column);
        ArgumentNullException.ThrowIfNull(defaultExpression);
#if SQLITE_FRAMEWORK_VERSION_AWARE
        database.Options.EnsureMinimumVersion(SQLiteMinimumVersion.V3_31, "Column DEFAULT with computed expression");
#endif
        ResolveTargetColumn(column).DefaultSql = "(" + DefaultExpressionTranslator.Translate(database, defaultExpression, nameof(defaultExpression)) + ")";
        return this;
    }

    /// <summary>
    /// Declares a trigger whose body is built from typed LINQ statements. The trigger becomes part
    /// of the model, so <c>CreateTable</c> creates it and <c>Migrate</c> creates it when missing and
    /// recreates it when its body changes. Reference target tables through the database's own
    /// <c>Table&lt;TTarget&gt;()</c>, which is in scope inside <c>OnModelCreating</c>.
    /// </summary>
    /// <param name="name">The trigger name.</param>
    /// <param name="timing">When the trigger fires, relative to the row change.</param>
    /// <param name="event">The row change that fires the trigger.</param>
    /// <param name="build">Builds the body and the optional <c>When</c> guard.</param>
    /// <param name="forEachRow">When <see langword="true" /> (the default), it fires once per row.</param>
    public SQLiteEntityTypeBuilder<T> Trigger(string name, SQLiteTriggerTiming timing, SQLiteTriggerEvent @event, Action<SQLiteTriggerBuilder<T>> build, bool forEachRow = true)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(build);

        SQLiteTriggerBuilder<T> triggerBuilder = new(database, mapping);
        build(triggerBuilder);
        if (triggerBuilder.Statements.Count == 0)
        {
            throw new ArgumentException("The trigger body must contain at least one Update, Insert, or Delete statement.", nameof(build));
        }

        string body = string.Join("; ", triggerBuilder.Statements);
        mapping.AddTrigger(new TriggerSpec(name, timing, @event, forEachRow, triggerBuilder.WhenSql, body));
        return this;
    }

    private void ForeignKeyCore<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TParent>(Expression<Func<T, object?>> column, Expression<Func<TParent, object?>>? target, SQLiteForeignKeyAction onDelete, SQLiteForeignKeyAction onUpdate, bool deferred)
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
        return BareSqlTranslator.Translate(database, mapping, lambda);
    }

    private string TranslateBareSql(ParameterExpression rowParameter, Expression body)
    {
        return BareSqlTranslator.Translate(database, mapping, rowParameter, body);
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
