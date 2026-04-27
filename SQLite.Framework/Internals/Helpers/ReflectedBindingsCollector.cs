using System.Linq.Expressions;
using System.Reflection;

namespace SQLite.Framework.Internals.Helpers;

/// <summary>
/// Walks the Select lambda tree once and fills the lists that the generated code reads by index.
/// </summary>
internal sealed class ReflectedBindingsCollector : ExpressionVisitor
{
    public List<MethodInfo> Methods { get; } = [];
    public List<object?> Instances { get; } = [];
    public List<object?> CapturedValues { get; } = [];
    public List<Type> Types { get; } = [];
    public List<MemberInfo> Members { get; } = [];
    public List<ConstructorInfo> Constructors { get; } = [];

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (IsNonPublic(node.Method))
        {
            Methods.Add(node.Method);
            Instances.Add(node.Object != null && CommonHelpers.IsConstant(node.Object)
                ? CommonHelpers.GetConstantValue(node.Object)
                : null);
        }

        return base.VisitMethodCall(node);
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        if (CommonHelpers.IsConstant(node))
        {
            CapturedValues.Add(CommonHelpers.GetConstantValue(node));
            return node;
        }

        return base.VisitMember(node);
    }

    protected override Expression VisitListInit(ListInitExpression node)
    {
        if (CommonHelpers.IsConstant(node))
        {
            CapturedValues.Add(CommonHelpers.GetConstantValue(node));
            return node;
        }

        return base.VisitListInit(node);
    }

    protected override Expression VisitMemberInit(MemberInitExpression node)
    {
        if (!node.NewExpression.Type.IsVisible)
        {
            Types.Add(node.NewExpression.Type);
            CollectBindingMembers(node.Bindings);
        }

        return base.VisitMemberInit(node);
    }

    protected override Expression VisitNew(NewExpression node)
    {
        if (node.Constructor != null
            && IsAnonymousType(node.Type)
            && node.Arguments.Any(arg => !arg.Type.IsVisible))
        {
            Constructors.Add(node.Constructor);
            foreach (Expression arg in node.Arguments)
            {
                if (!arg.Type.IsVisible)
                {
                    Types.Add(arg.Type);
                }
            }
        }

        return base.VisitNew(node);
    }

    private void CollectBindingMembers(IReadOnlyCollection<MemberBinding> bindings)
    {
        foreach (MemberBinding binding in bindings)
        {
            Members.Add(binding.Member);
            if (binding is MemberMemberBinding mmb)
            {
                CollectBindingMembers(mmb.Bindings);
            }
        }
    }

    private static bool IsAnonymousType(Type type)
    {
        return type.Name.StartsWith("<>f__AnonymousType", StringComparison.Ordinal)
            || type.Name.StartsWith("<>h__TransparentIdentifier", StringComparison.Ordinal);
    }

    private static bool IsNonPublic(MethodInfo method)
    {
        if (!method.IsPublic)
        {
            return true;
        }

        Type? declaring = method.DeclaringType;
        while (declaring != null)
        {
            if (!declaring.IsVisible)
            {
                return true;
            }
            declaring = declaring.DeclaringType;
        }

        return false;
    }
}
