namespace SQLite.Framework.Internals.Visitors;

/// <summary>
/// Rewrites an operand of a client-evaluated projection so that only leaf table columns become
/// SQL (read back as columns), while every operation is kept as a C# expression and computed at
/// materialization time. This keeps the SELECT list to raw leaf columns, which is the same shape
/// the source generator reads, so reflection and generated materializers agree.
/// </summary>
internal sealed class ClientLeafRewriter : ExpressionVisitor
{
    private readonly SQLVisitor owner;

    public ClientLeafRewriter(SQLVisitor owner)
    {
        this.owner = owner;
    }

    [return: NotNullIfNotNull(nameof(node))]
    public override Expression? Visit(Expression? node)
    {
        if (node is null || ExpressionHelpers.IsConstant(node))
        {
            return node;
        }

        if (node is MemberExpression or ParameterExpression && owner.TryResolveColumnLeaf(node) is { } leaf)
        {
            if (node.Type != leaf.Type && Nullable.GetUnderlyingType(node.Type) == leaf.Type)
            {
                return Expression.Convert(leaf, node.Type);
            }

            return leaf;
        }

        if (node is MemberExpression or ParameterExpression && owner.TryMaterializeEntityLeaves(node) is { } entity)
        {
            return entity;
        }

        if (node is MemberExpression unmaterializable && owner.IsUnmaterializableRowMember(unmaterializable))
        {
            throw new NotSupportedException(
                $"The entity '{unmaterializable.Member.DeclaringType!.Name}' cannot be read on its own to compute " +
                $"'{unmaterializable.Member.Name}' in this projection, since it has no parameterless constructor to " +
                "build it from its columns or it is only partly available here. Project the columns you need instead.");
        }

        if (node is BinaryExpression { NodeType: ExpressionType.Equal or ExpressionType.NotEqual } binary
            && owner.TryResolveEntityNullCheck(binary) is { } entityNullCheck)
        {
            return entityNullCheck;
        }

        if (node is MethodCallExpression { Object: null, Arguments.Count: > 0 } groupCall
            && (IsDirectGroupingCall(groupCall) || IsGroupingConcatCall(groupCall))
            && owner.Visit(node) is SQLiteExpression groupAggregate)
        {
            return groupAggregate;
        }

        return base.Visit(node);
    }

    private static bool IsDirectGroupingCall(MethodCallExpression call)
    {
        return call.Arguments[0].Type.IsGenericType
            && call.Arguments[0].Type.GetGenericTypeDefinition() == typeof(IGrouping<,>);
    }

    private static bool IsGroupingConcatCall(MethodCallExpression call)
    {
        return call.Method.DeclaringType == typeof(string)
            && call.Method.Name is nameof(string.Join) or nameof(string.Concat)
            && QueryableMemberVisitor.IsGroupingRooted(call.Arguments[^1]);
    }
}
