namespace SQLite.Framework.Internals.Visitors.Member;

internal static class NumericMemberVisitor
{
    public static Expression HandleIntegerMethod(SQLiteCallerContext ctx)
    {

        SQLVisitor visitor = ctx.Visitor;
        MethodCallExpression node = (MethodCallExpression)ctx.Node;
        List<ResolvedModel> arguments = node.Arguments
            .Select(visitor.ResolveExpression)
            .ToList();

        if (node.Object != null)
        {
            ResolvedModel obj = visitor.ResolveExpression(node.Object);

            if (obj.SQLiteExpression == null || arguments.Any(f => f.SQLiteExpression == null))
            {
                return Expression.Call(obj.Expression, node.Method, arguments.Select(f => f.Expression));
            }

            if (node.Method.Name == nameof(int.ToString))
            {
                return SQLiteExpression.Wrap(node.Method.ReturnType, visitor.Counters.NextIdentifier(), "CAST(", obj.SQLiteExpression!, " AS TEXT)", obj.Parameters);
            }
        }

        if (QueryableMemberVisitor.CheckConstantMethod<long>(visitor, node, arguments, out Expression? expression))
        {
            return expression;
        }

        if (node.Method.Name == "Parse")
        {
            return SQLiteExpression.Wrap(node.Method.ReturnType, visitor.Counters.NextIdentifier(), "CAST(", arguments[0].SQLiteExpression!, " AS INTEGER)", arguments[0].Parameters);
        }

        throw new NotSupportedException($"{node.Method.DeclaringType!.Name}.{node.Method.Name} is not translatable to SQL.");
    }

    public static Expression HandleFloatingPointMethod(SQLiteCallerContext ctx)
    {

        SQLVisitor visitor = ctx.Visitor;
        MethodCallExpression node = (MethodCallExpression)ctx.Node;
        List<ResolvedModel> arguments = node.Arguments
            .Select(visitor.ResolveExpression)
            .ToList();

        if (node.Object != null)
        {
            ResolvedModel obj = visitor.ResolveExpression(node.Object);

            if (obj.SQLiteExpression == null || arguments.Any(f => f.SQLiteExpression == null))
            {
                return Expression.Call(obj.Expression, node.Method, arguments.Select(f => f.Expression));
            }

            if (node.Method.Name == nameof(double.ToString))
            {
                return SQLiteExpression.Wrap(node.Method.ReturnType, visitor.Counters.NextIdentifier(), "CAST(", obj.SQLiteExpression!, " AS TEXT)", obj.Parameters);
            }
        }

        if (QueryableMemberVisitor.CheckConstantMethod<double>(visitor, node, arguments, out Expression? expression))
        {
            return expression;
        }

        if (node.Method.Name == "Parse")
        {
            return SQLiteExpression.Wrap(node.Method.ReturnType, visitor.Counters.NextIdentifier(), "CAST(", arguments[0].SQLiteExpression!, " AS REAL)", arguments[0].Parameters);
        }

        throw new NotSupportedException($"{node.Method.DeclaringType!.Name}.{node.Method.Name} is not translatable to SQL.");
    }
}
