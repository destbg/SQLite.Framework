namespace SQLite.Framework.Internals.Visitors;

/// <summary>
/// Collects the database column names a <c>Set</c> value expression reads. Mapped property
/// accesses on the row parameter resolve to their column names and <c>SQLiteColumn.Of</c> calls
/// contribute the name they are given.
/// </summary>
internal sealed class SetReadColumnCollector : ExpressionVisitor
{
    private readonly TableMapping mapping;
    private readonly ParameterExpression row;
    private readonly List<string> columns = [];

    public SetReadColumnCollector(TableMapping mapping, ParameterExpression row)
    {
        this.mapping = mapping;
        this.row = row;
    }

    public IReadOnlyList<string> Columns => columns;

    protected override Expression VisitMember(MemberExpression node)
    {
        if (node.Expression == row
            && mapping.Columns.FirstOrDefault(c => c.PropertyInfo.Name == node.Member.Name) is { } column)
        {
            columns.Add(column.Name);
        }

        return base.VisitMember(node);
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (node.Method.DeclaringType == typeof(SQLiteColumn)
            && node.Arguments[1] is ConstantExpression { Value: string name })
        {
            columns.Add(name);
        }

        return base.VisitMethodCall(node);
    }
}
