namespace SQLite.Framework.Internals.Visitors.SQL;

internal partial class SQLVisitor
{
    public Expression NotTranslatable(Expression node, string message)
    {
        if (!ClientEvalAllowed)
        {
            throw new NotSupportedException(message);
        }

        ClientEvalUsed = true;
        return BuildClientEvalFallback(node);
    }

    public Expression NotTranslatableBelowVersion(Expression node, SQLiteMinimumVersion requiredVersion, string featureName)
    {
        if (!ClientEvalAllowed)
        {
            Database.Options.ThrowMinimumVersionNotSupported(requiredVersion, featureName);
        }

        ClientEvalUsed = true;
        return BuildClientEvalFallback(node);
    }

    public SQLiteExpression? TryResolveColumnLeaf(Expression node)
    {
        (string path, ParameterExpression? pe) = ExpressionHelpers.ResolveNullableParameterPath(node);
        if (pe != null
            && MethodArguments.TryGetValue(pe, out Dictionary<string, Expression>? columns)
            && IsSingleLeafColumn(columns, path))
        {
            return (SQLiteExpression)Visit(node);
        }

        return null;
    }

    public Expression ToClientExpression(Expression node)
    {
        return new ClientLeafRewriter(this).Visit(node);
    }

    private Expression BuildClientEvalFallback(Expression node)
    {
        if (node is MethodCallExpression methodCall)
        {
            Expression[] arguments = methodCall.Arguments.Select(ToClientExpression).ToArray();
            return methodCall.Object == null
                ? Expression.Call(methodCall.Method, arguments)
                : Expression.Call(ToClientExpression(methodCall.Object), methodCall.Method, arguments);
        }

        if (node is UnaryExpression unary)
        {
            return Expression.MakeUnary(unary.NodeType, ToClientExpression(unary.Operand), unary.Type);
        }

        if (node is TypeBinaryExpression typeBinary)
        {
            return typeBinary.NodeType == ExpressionType.TypeIs
                ? Expression.TypeIs(ToClientExpression(typeBinary.Expression), typeBinary.TypeOperand)
                : Expression.TypeEqual(ToClientExpression(typeBinary.Expression), typeBinary.TypeOperand);
        }

        MemberExpression memberExpression = (MemberExpression)node;
        return Expression.MakeMemberAccess(ToClientExpression(memberExpression.Expression!), memberExpression.Member);
    }

    private static bool IsSingleLeafColumn(Dictionary<string, Expression> columns, string path)
    {
        if (columns.TryGetValue(path, out Expression? column))
        {
            return column is SQLiteExpression;
        }

        return path.Length == 0 && columns.Count == 1 && columns.Values.First() is SQLiteExpression;
    }
}
