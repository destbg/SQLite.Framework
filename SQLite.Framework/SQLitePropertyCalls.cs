namespace SQLite.Framework;

/// <summary>
/// Represents a set of property calls for updating records in a database.
/// </summary>
public class SQLitePropertyCalls<T>
{
    private readonly SQLVisitor visitor;
    private readonly TableMapping tableMapping;

    internal SQLitePropertyCalls(SQLVisitor visitor, TableMapping tableMapping)
    {
        this.visitor = visitor;
        this.tableMapping = tableMapping;
    }

    internal List<(string, SQLiteExpression)> SetProperties { get; } = [];

    /// <summary>
    /// Sets the value of a specified property for update operations.
    /// </summary>
    /// <typeparam name="TValue">The type of the property value.</typeparam>
    /// <param name="propertyGetter">An expression that identifies the property to set.</param>
    /// <param name="value">The value to assign to the property.</param>
    /// <returns>The current <see cref="SQLitePropertyCalls{T}"/> instance for chaining.</returns>
    public SQLitePropertyCalls<T> Set<TValue>(Expression<Func<T, TValue>> propertyGetter, TValue value)
    {
        string propertyName = GetPropertyName(propertyGetter);
        MemberExpression member = (MemberExpression)propertyGetter.Body;
        SQLiteExpression expression = new(member.Type, visitor.Counters.IdentifierIndex++, $"@p{visitor.Counters.ParamIndex++}", value);

        SetProperties.Add((propertyName, expression));

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
        string propertyName = GetPropertyName(propertyGetter);
        visitor.MethodArguments[setter.Parameters[0]] = visitor.TableColumns;
        Expression expression = visitor.Visit(setter.Body);

        if (expression is not SQLiteExpression expr)
        {
            throw new ArgumentException($"Expression '{setter}' must evaluate to a SQL expression.", nameof(setter));
        }

        SetProperties.Add((propertyName, expr));

        return this;
    }

    private string GetPropertyName<TValue>(Expression<Func<T, TValue>> propertyGetter)
    {
        if (propertyGetter.Body is not MemberExpression member)
        {
            throw new ArgumentException($"Expression '{propertyGetter}' refers to a method, not a property.");
        }

        Expression target = member.Expression!;
        if (target is UnaryExpression { NodeType: ExpressionType.Convert } cast
            && cast.Type.IsAssignableFrom(cast.Operand.Type))
        {
            target = cast.Operand;
        }

        if (target is not ParameterExpression)
        {
            throw new ArgumentException("Invalid property expression.", nameof(propertyGetter));
        }

        if (member.Member is not PropertyInfo property)
        {
            throw new ArgumentException($"Expression '{propertyGetter}' refers to a field, not a property.");
        }

        return tableMapping.Columns.First(f => f.PropertyInfo.Name == property.Name).Name;
    }
}