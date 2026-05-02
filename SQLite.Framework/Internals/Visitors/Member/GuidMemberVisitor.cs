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
                    Guid guid = Guid.NewGuid();
                    string pName = $"@p{visitor.Counters.ParamIndex++}";

                    return new SQLiteExpression(node.Method.ReturnType, visitor.Counters.IdentifierIndex++, pName, guid);
                }
            }
        }

        if (QueryableMemberVisitor.CheckConstantMethod<Guid>(visitor, node, arguments, out Expression? expression))
        {
            return expression;
        }

        throw new NotSupportedException($"Guid.{node.Method.Name} is not translatable to SQL.");
    }
}
