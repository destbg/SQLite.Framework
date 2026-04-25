using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;

namespace SQLite.Framework.Internals.Visitors;

/// <summary>
/// Rebinds a filter lambda registered against an interface (or base type) so its parameter and
/// member accesses are typed against the concrete entity. Without the rewrite the SQL translator
/// would see <c>MemberAccess</c> nodes whose declaring type is the interface, and the column
/// lookup keyed on the entity's <c>TableMapping</c> would miss them.
/// </summary>
internal sealed class QueryFilterRebinder : ExpressionVisitor
{
    private readonly ParameterExpression oldParameter;
    private readonly ParameterExpression newParameter;

    private QueryFilterRebinder(ParameterExpression oldParameter, ParameterExpression newParameter)
    {
        this.oldParameter = oldParameter;
        this.newParameter = newParameter;
    }

    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Func<TEntity, bool> is constructed for entity types preserved by the user's Table<T>() reference.")]
    public static LambdaExpression Rebind(LambdaExpression source, Type entityType)
    {
        ParameterExpression oldP = source.Parameters[0];
        if (oldP.Type == entityType)
        {
            return source;
        }

        ParameterExpression newP = Expression.Parameter(entityType, oldP.Name);
        QueryFilterRebinder visitor = new(oldP, newP);
        Expression body = visitor.Visit(source.Body)!;
        Type funcType = typeof(Func<,>).MakeGenericType(entityType, typeof(bool));
        return Expression.Lambda(funcType, body, newP);
    }

    protected override Expression VisitParameter(ParameterExpression node)
    {
        if (node == oldParameter)
        {
            return newParameter;
        }

        return base.VisitParameter(node);
    }

    [UnconditionalSuppressMessage("AOT", "IL2075", Justification = "Entity types reach the rebinder via the user's Table<T>() reference, which preserves public properties and fields.")]
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
            if (concrete != null)
            {
                return Expression.MakeMemberAccess(expression, concrete);
            }
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
