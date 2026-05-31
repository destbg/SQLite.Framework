namespace SQLite.Framework;

/// <summary>
/// Collects column values to write while a table is rebuilt during <c>Migrate(...)</c>. Use it to
/// fill a new <c>NOT NULL</c> column that has no default, or to recompute a column from the old
/// row. Each value is read from the old row and inserted into the rebuilt table. A column not set
/// here is copied across unchanged when it still exists.
/// </summary>
public sealed class SQLiteMigrationBuilder<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>
{
    private readonly SQLiteDatabase database;
    private readonly TableMapping mapping;
    private readonly List<(string Column, string ValueSql)> sets = [];

    internal SQLiteMigrationBuilder(SQLiteDatabase database, TableMapping mapping)
    {
        this.database = database;
        this.mapping = mapping;
    }

    internal IReadOnlyList<(string Column, string ValueSql)> Sets => sets;

    /// <summary>
    /// Sets the target column to a constant value. The target is a property on
    /// <typeparamref name="T" /> or <c>row.Column&lt;TValue&gt;("Name")</c> for a column with no CLR
    /// property.
    /// </summary>
    public SQLiteMigrationBuilder<T> Set<TValue>(Expression<Func<T, TValue>> column, TValue value)
    {
        sets.Add((ResolveTarget(column), SqlLiteralHelper.FormatLiteral(value)));
        return this;
    }

    /// <summary>
    /// Sets the target column to an expression evaluated over the old row. The target is a property
    /// on <typeparamref name="T" /> or <c>row.Column&lt;TValue&gt;("Name")</c>. The value expression
    /// may read any column of the old row, including ones with no CLR property through
    /// <c>row.Column&lt;TValue&gt;("Name")</c>.
    /// </summary>
    public SQLiteMigrationBuilder<T> Set<TValue>(Expression<Func<T, TValue>> column, Expression<Func<T, TValue>> value)
    {
        sets.Add((ResolveTarget(column), BareSqlTranslator.Translate(database, mapping, value)));
        return this;
    }

    private string ResolveTarget<TValue>(Expression<Func<T, TValue>> column)
    {
        Expression body = column.Body;
        if (body.NodeType == ExpressionType.Convert)
        {
            body = ((UnaryExpression)body).Operand;
        }

        if (body is MethodCallExpression call && call.Method.DeclaringType == typeof(SQLiteColumnExtensions))
        {
            return (string)ExpressionHelpers.GetConstantValue(call.Arguments[1])!;
        }

        if (body is MemberExpression member
            && mapping.Columns.FirstOrDefault(c => c.PropertyInfo.Name == member.Member.Name) is { } mapped)
        {
            return mapped.Name;
        }

        throw new ArgumentException(
            "The Set target must be a property on the entity or row.Column<TValue>(\"Name\").", nameof(column));
    }
}
