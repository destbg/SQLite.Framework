namespace SQLite.Framework.Internals.Visitors.Member;

internal static class GuidMemberVisitor
{
    public static Expression HandleGuidMethod(SQLiteCallerContext ctx)
    {

        SQLVisitor visitor = ctx.Visitor;
        MethodCallExpression node = (MethodCallExpression)ctx.Node;
        List<ResolvedModel> arguments = node.Arguments
            .Select(visitor.ResolveExpression)
            .ToList();

        if (node.Object == null)
        {
            if (arguments.Any(f => f.SQLiteExpression == null) || node.Method.Name == nameof(Guid.ToString))
            {
                return Expression.Call(node.Method, arguments.Select(f => f.Expression));
            }

            switch (node.Method.Name)
            {
                case nameof(Guid.NewGuid):
                {
                    return SQLiteExpression.Leaf(node.Method.ReturnType, visitor.Counters.NextIdentifier(),
                        "(LOWER(HEX(RANDOMBLOB(4))) || '-' || LOWER(HEX(RANDOMBLOB(2))) || '-4' || LOWER(SUBSTR(HEX(RANDOMBLOB(2)), 2)) || '-' || SUBSTR('89ab', ABS(RANDOM()) % 4 + 1, 1) || LOWER(SUBSTR(HEX(RANDOMBLOB(2)), 2)) || '-' || LOWER(HEX(RANDOMBLOB(6))))");
                }
            }
        }

        if (QueryableMemberVisitor.CheckConstantMethod<Guid>(visitor, node, arguments, out Expression? expression))
        {
            return expression;
        }

        return visitor.NotTranslatable(node, $"Guid.{node.Method.Name} is not translatable to SQL.");
    }
}
