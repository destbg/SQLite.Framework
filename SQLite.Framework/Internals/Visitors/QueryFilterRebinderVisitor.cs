namespace SQLite.Framework.Internals.Visitors;

/// <summary>
/// Rebinds a filter lambda registered against an interface (or base type) so its parameter and
/// member accesses are typed against the concrete entity. Without the rewrite the SQL translator
/// would see <c>MemberAccess</c> nodes whose declaring type is the interface and the column
/// lookup keyed on the entity's <c>TableMapping</c> would miss them.
/// </summary>
internal sealed class QueryFilterRebinderVisitor : ExpressionVisitor
{
    private readonly ParameterExpression oldParameter;
    private readonly ParameterExpression newParameter;

    public QueryFilterRebinderVisitor(ParameterExpression oldParameter, ParameterExpression newParameter)
    {
        this.oldParameter = oldParameter;
        this.newParameter = newParameter;
    }

    protected override Expression VisitParameter(ParameterExpression node)
    {
        if (node == oldParameter)
        {
            return newParameter;
        }

        return base.VisitParameter(node);
    }

    [UnconditionalSuppressMessage("AOT", "IL2075", Justification = "Entity types are preserved by the user Table<T>().")]
    protected override Expression VisitMember(MemberExpression node)
    {
        Expression? expression = node.Expression != null ? Visit(node.Expression) : null;

        if (expression != null
            && node.Expression != null
            && expression.Type != node.Expression.Type
            && node.Member.DeclaringType != null
            && node.Member.DeclaringType.IsAssignableFrom(expression.Type))
        {
            MemberInfo? concrete = FindConcreteMember(expression.Type, node.Member);
            if (concrete == null)
            {
                throw new NotSupportedException(
                    $"The query filter registered for '{node.Member.DeclaringType.Name}' reads '{node.Member.Name}', " +
                    $"but the entity '{expression.Type.Name}' has no public member with that name, so the filter cannot map it to a column. " +
                    $"Implement '{node.Member.Name}' as a public mapped property on '{expression.Type.Name}' " +
                    $"instead of an explicit interface implementation.");
            }

            return Expression.MakeMemberAccess(expression, concrete);
        }

        return node.Update(expression);
    }

    [UnconditionalSuppressMessage("AOT", "IL2070", Justification = "Entity types are preserved via DynamicallyAccessedMembers on Table<T>().")]
    private static MemberInfo? FindConcreteMember(Type type, MemberInfo abstractMember)
    {
        if (abstractMember is PropertyInfo)
        {
            return type.GetProperty(abstractMember.Name, BindingFlags.Public | BindingFlags.Instance);
        }

        return type.GetField(abstractMember.Name, BindingFlags.Public | BindingFlags.Instance);
    }
}
