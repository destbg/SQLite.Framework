namespace SQLite.Framework.Internals.Helpers;

/// <summary>
/// Resolves the database column name from a <c>Set</c> target expression. The target is either a
/// property on the entity or <see cref="SQLiteColumn.Of{TValue}" /> for a column with no CLR
/// property. Shared by the migration and write-column builders.
/// </summary>
internal static class ColumnTargetResolver
{
    public static string Resolve<T, TValue>(TableMapping mapping, Expression<Func<T, TValue>> column)
    {
        Expression body = column.Body;
        if (body.NodeType == ExpressionType.Convert)
        {
            body = ((UnaryExpression)body).Operand;
        }

        if (body is MethodCallExpression call && call.Method.DeclaringType == typeof(SQLiteColumn))
        {
            return (string)ExpressionHelpers.GetConstantValue(call.Arguments[1])!;
        }

        if (body is MemberExpression member
            && mapping.Columns.FirstOrDefault(c => c.PropertyInfo.Name == member.Member.Name) is { } mapped)
        {
            return mapped.Name;
        }

        throw new ArgumentException(
            "The Set target must be a property on the entity or SQLiteColumn.Of<TValue>(row, \"Name\").", nameof(column));
    }
}
