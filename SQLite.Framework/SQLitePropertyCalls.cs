namespace SQLite.Framework;

/// <summary>
/// Represents a set of property calls for updating records in a database.
/// </summary>
public class SQLitePropertyCalls<T>
{
    private readonly SQLVisitor visitor;
    private readonly TableMapping targetMapping;
    private readonly List<LambdaExpression>? recordedSetters;

    internal SQLitePropertyCalls(SQLVisitor visitor, TableMapping targetMapping)
    {
        this.visitor = visitor;
        this.targetMapping = targetMapping;
    }

    internal SQLitePropertyCalls()
    {
        visitor = null!;
        targetMapping = null!;
        recordedSetters = [];
    }

    internal List<(string, SQLiteExpression)> SetProperties { get; } = [];

    /// <summary>
    /// The value lambdas declared through <c>Set</c> while the instance records instead of
    /// translating. Null on a translating instance.
    /// </summary>
    internal IReadOnlyList<LambdaExpression>? RecordedSetters => recordedSetters;

    /// <summary>
    /// Sets the value of a specified property for update operations.
    /// </summary>
    /// <typeparam name="TValue">The type of the property value.</typeparam>
    /// <param name="propertyGetter">An expression that identifies the property to set.</param>
    /// <param name="value">The value to assign to the property.</param>
    /// <returns>The current <see cref="SQLitePropertyCalls{T}"/> instance for chaining.</returns>
    public SQLitePropertyCalls<T> Set<TValue>(Expression<Func<T, TValue>> propertyGetter, TValue value)
    {
        if (recordedSetters != null)
        {
            return this;
        }

        TableColumn targetColumn = GetTargetColumn(propertyGetter);
        if (IsComputedColumn(targetColumn))
        {
            return this;
        }

        MemberExpression member = (MemberExpression)propertyGetter.Body;
        string paramName = visitor.Counters.NextParamName();
        string sql = ConverterSql.WrapParameter(paramName, member.Type, visitor.Database.Options);

        SQLiteExpression expression = SQLiteExpression.Leaf(member.Type, visitor.Counters.NextIdentifier(), sql,
            [new SQLiteParameter { Name = paramName, Value = value }]);

        SetProperties.Add((targetColumn.Name, expression));

        return this;
    }

    /// <summary>
    /// Sets the value of a specified property for update operations.
    /// </summary>
    /// <typeparam name="TValue">The type of the property value.</typeparam>
    /// <param name="propertyGetter">An expression that identifies the property to set.</param>
    /// <param name="setter">An expression that provides the value to assign to the property.</param>
    /// <returns>The current <see cref="SQLitePropertyCalls{T}"/> instance for chaining.</returns>
    public SQLitePropertyCalls<T> Set<TValue>(Expression<Func<T, TValue>> propertyGetter, Expression<Func<T, TValue>> setter)
    {
        if (recordedSetters != null)
        {
            recordedSetters.Add(setter);
            return this;
        }

        TableColumn targetColumn = GetTargetColumn(propertyGetter);
        if (IsComputedColumn(targetColumn))
        {
            return this;
        }

        visitor.MethodArguments[setter.Parameters[0]] = visitor.TableColumns;
        Expression setterBody = CommonHelpers.Inline(setter.Body);
        bool ignoreAll = visitor.Counters.IgnoreQueryFilters || QueryFilterInjector.ShouldIgnoreAll(setterBody, visitor.Database);
        setterBody = QueryFilterInjector.Inject(setterBody, visitor.Database.Options, ignoreAll);
        if (visitor.Visit(setterBody) is not SQLiteExpression expr)
        {
            throw new NotSupportedException(
                $"The value for '{targetColumn.PropertyInfo.Name}' cannot be translated to SQL. " +
                "An ExecuteUpdate setter runs inside the database, so it cannot call methods that run in memory. " +
                "Compute the value first and pass it as a constant.");
        }

        if (expr.IsDayOfWeekInteger && visitor.Database.Options.EnumStorage == EnumStorageMode.Text)
        {
            expr = EnumMemberVisitor.BuildEnumToNameText(visitor, targetColumn.PropertyType, expr);
        }

        if (ExpressionHelpers.IsConstant(setter.Body))
        {
            string sql = expr.ToString();
            string wrapped = ConverterSql.WrapParameter(sql, ((MemberExpression)propertyGetter.Body).Type, visitor.Database.Options);
            if (wrapped != sql)
            {
                expr = SQLiteExpression.Leaf(expr.Type, expr.Identifier, wrapped, expr.Parameters);
            }
        }
        else
        {
            SQLiteExpression? stored = TryReadStoredColumn(setterBody, targetColumn);
            if (stored != null)
            {
                expr = stored;
            }
            else if (WrapConverterValue(expr, targetColumn) is { } wrappedValue)
            {
                expr = wrappedValue;
            }
        }

        SetProperties.Add((targetColumn.Name, expr));

        return this;
    }

    private SQLiteExpression? WrapConverterValue(SQLiteExpression value, TableColumn target)
    {
        if (!visitor.Database.Options.TypeConverters.TryGetValue(target.PropertyType, out ISQLiteTypeConverter? converter)
            || converter.ParameterSqlExpression is not { } writeWrap
            || converter.ColumnSqlExpression == null)
        {
            return null;
        }

        string wrapped = string.Format(CultureInfo.InvariantCulture, writeWrap, value.ToString());
        return SQLiteExpression.Leaf(value.Type, visitor.Counters.NextIdentifier(), wrapped, value.Parameters);
    }

    private SQLiteExpression? TryReadStoredColumn(Expression setterBody, TableColumn target)
    {
        if (setterBody is not MemberExpression { Expression: ParameterExpression } member)
        {
            return null;
        }

        TableColumn? source = targetMapping.Columns.FirstOrDefault(c => c.PropertyInfo.Name == member.Member.Name);
        if (source == null || source.PropertyType != target.PropertyType)
        {
            return null;
        }

        if (!visitor.Database.Options.TypeConverters.TryGetValue(source.PropertyType, out ISQLiteTypeConverter? converter)
            || converter.ColumnSqlExpression == null)
        {
            return null;
        }

        return SQLiteExpression.Leaf(source.PropertyType, visitor.Counters.NextIdentifier(), IdentifierGuard.Quote(source.Name));
    }

    private bool IsComputedColumn(TableColumn column)
    {
        return targetMapping.ComputedColumns.Any(c => string.Equals(c.Column.Name, column.Name, StringComparison.OrdinalIgnoreCase));
    }

    private TableColumn GetTargetColumn<TValue>(Expression<Func<T, TValue>> propertyGetter)
    {
        if (propertyGetter.Body is not MemberExpression member)
        {
            throw new ArgumentException($"Expression '{propertyGetter}' refers to a method, not a property.");
        }

        Expression target = ExpressionHelpers.StripUpcast(member.Expression!);

        if (target is not ParameterExpression && target is not MemberExpression)
        {
            throw new ArgumentException("Invalid property expression.", nameof(propertyGetter));
        }

        if (member.Member is not PropertyInfo property)
        {
            throw new ArgumentException($"Expression '{propertyGetter}' refers to a field, not a property.");
        }

        if (target is MemberExpression && !property.DeclaringType!.IsAssignableFrom(targetMapping.Type))
        {
            throw new ArgumentException(
                $"Expression '{propertyGetter}' references '{property.DeclaringType.Name}.{property.Name}', " +
                $"but the target table is '{targetMapping.Type.Name}'. " +
                $"In an UPDATE FROM, only columns of the target table can appear on the left side of Set.",
                nameof(propertyGetter));
        }

        return targetMapping.Columns.First(f => f.PropertyInfo.Name == property.Name);
    }
}
