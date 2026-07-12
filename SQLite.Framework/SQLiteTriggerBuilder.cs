namespace SQLite.Framework;

/// <summary>
/// Builds the body of a LINQ-typed trigger. Reference the row that fired the trigger through
/// <see cref="Old" /> and <see cref="New" /> inside the statement and <c>When</c> expressions, the
/// same way SQLite's <c>OLD</c> and <c>NEW</c> rows work. Each <c>Update</c>, <c>Insert</c> or
/// <c>Delete</c> call adds one statement to the body.
/// </summary>
public sealed class SQLiteTriggerBuilder<T>
{
    private readonly SQLiteDatabase database;
    private readonly TableMapping triggerMapping;
    private readonly ParameterExpression oldRow;
    private readonly ParameterExpression newRow;
    private readonly List<string> statements = [];

    internal SQLiteTriggerBuilder(SQLiteDatabase database, TableMapping triggerMapping)
    {
        this.database = database;
        this.triggerMapping = triggerMapping;
        oldRow = Expression.Parameter(typeof(T), "old");
        newRow = Expression.Parameter(typeof(T), "new");
        Old = default!;
        New = default!;
    }

    /// <summary>
    /// The row as it was before the change. Populated for <c>UPDATE</c> and <c>DELETE</c> triggers.
    /// Only valid inside the expressions passed to this builder, where it maps to SQLite's
    /// <c>OLD</c> row.
    /// </summary>
    public T Old { get; }

    /// <summary>
    /// The row as it is after the change. Populated for <c>INSERT</c> and <c>UPDATE</c> triggers.
    /// Only valid inside the expressions passed to this builder, where it maps to SQLite's
    /// <c>NEW</c> row.
    /// </summary>
    public T New { get; }

    internal string? WhenSql { get; private set; }

    internal IReadOnlyList<string> Statements => statements;

    /// <summary>
    /// Sets the trigger's <c>WHEN</c> guard. The body runs only for rows where the predicate is
    /// true, as in <c>When(() =&gt; t.Old.Price != t.New.Price)</c>.
    /// </summary>
    public SQLiteTriggerBuilder<T> When(Expression<Func<bool>> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        if (WhenSql != null)
        {
            throw new InvalidOperationException("When was already called for this trigger.");
        }

        WhenSql = Translate(predicate, targetMapping: null);
        return this;
    }

    /// <summary>
    /// Adds an <c>UPDATE</c> statement to the body. The predicate and setter expressions can read
    /// the target row and the trigger's <see cref="Old" /> and <see cref="New" /> rows.
    /// </summary>
    public SQLiteTriggerBuilder<T> Update<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] TTarget>(SQLiteTable<TTarget> target, Expression<Func<TTarget, bool>> predicate, Action<SQLiteTriggerSetBuilder<TTarget>> setters)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(setters);

        SQLiteTriggerSetBuilder<TTarget> set = new();
        setters(set);
        if (set.Setters.Count == 0)
        {
            throw new ArgumentException("Update requires at least one Set(...) call.", nameof(setters));
        }

        TableMapping mapping = target.Table;
        StringBuilder sql = new();
        sql.Append("UPDATE \"").Append(mapping.TableName).Append("\" SET ");
        for (int i = 0; i < set.Setters.Count; i++)
        {
            if (i > 0)
            {
                sql.Append(", ");
            }
            (string column, LambdaExpression value) = set.Setters[i];
            sql.Append(IdentifierGuard.Quote(ResolveColumn(mapping, column)));
            sql.Append(" = ");
            sql.Append(Translate(value, mapping, SetterColumn(mapping, column)));
        }
        sql.Append(" WHERE ").Append(Translate(predicate, mapping));

        statements.Add(sql.ToString());
        return this;
    }

    /// <summary>
    /// Adds an <c>INSERT</c> statement to the body. Each setter pairs a target column with a value
    /// expression, which typically reads the trigger's <see cref="Old" /> and <see cref="New" /> rows.
    /// </summary>
    public SQLiteTriggerBuilder<T> Insert<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] TTarget>(SQLiteTable<TTarget> target, Action<SQLiteTriggerSetBuilder<TTarget>> values)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(values);

        SQLiteTriggerSetBuilder<TTarget> set = new();
        values(set);
        if (set.Setters.Count == 0)
        {
            throw new ArgumentException("Insert requires at least one Set(...) call.", nameof(values));
        }

        TableMapping mapping = target.Table;
        string columns = string.Join(", ", set.Setters.Select(s => IdentifierGuard.Quote(ResolveColumn(mapping, s.Column))));
        string valueList = string.Join(", ", set.Setters.Select(s => Translate(s.Value, mapping, SetterColumn(mapping, s.Column))));

        statements.Add($"INSERT INTO \"{mapping.TableName}\" ({columns}) VALUES ({valueList})");
        return this;
    }

    /// <summary>
    /// Adds a <c>DELETE</c> statement to the body. The predicate can read the target row and the
    /// trigger's <see cref="Old" /> and <see cref="New" /> rows.
    /// </summary>
    public SQLiteTriggerBuilder<T> Delete<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] TTarget>(SQLiteTable<TTarget> target, Expression<Func<TTarget, bool>> predicate)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(predicate);

        statements.Add($"DELETE FROM \"{target.Table.TableName}\" WHERE {Translate(predicate, target.Table)}");
        return this;
    }

    private string Translate(LambdaExpression lambda, TableMapping? targetMapping, TableColumn? wrapColumn = null)
    {
        Expression body = new TriggerRowRewriter(oldRow, newRow, GetType()).Visit(lambda.Body)!;

        List<(ParameterExpression, TableMapping, string?)> rows = [];
        if (targetMapping != null)
        {
            rows.Add((lambda.Parameters[0], targetMapping, null));
        }
        rows.Add((oldRow, triggerMapping, "OLD."));
        rows.Add((newRow, triggerMapping, "NEW."));

        string sql = BareSqlTranslator.TranslateTrigger(database, body, rows.ToArray(), wrapConverterReads: wrapColumn == null);

        if (wrapColumn != null && ExpressionHelpers.IsConstant(body))
        {
            sql = ConverterSql.WrapParameter(sql, wrapColumn.PropertyType, database.Options);
        }

        return sql;
    }

    private static TableColumn SetterColumn(TableMapping mapping, string propertyName)
    {
        return mapping.Columns.First(c => c.PropertyInfo.Name == propertyName);
    }

    private static string ResolveColumn(TableMapping mapping, string propertyName)
    {
        TableColumn? column = mapping.Columns.FirstOrDefault(c => c.PropertyInfo.Name == propertyName);
        return column != null
            ? column.Name
            : throw new InvalidOperationException($"Trigger statement references property '{propertyName}' which is not a mapped column on '{mapping.TableName}'.");
    }
}
