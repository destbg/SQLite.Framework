namespace SQLite.Framework.Internals.Visitors.Member;

/// <summary>
/// Translates <see cref="Convert"/> methods. Only <see cref="Convert.ToInt32(double)"/> and
/// <see cref="Convert.ToInt64(double)"/> of a floating-point value map to SQL, as
/// <c>CAST(ROUND(value) AS INTEGER)</c>. Every other <see cref="Convert"/> call runs in memory in a
/// <c>Select</c> and is not translatable in a <c>Where</c>.
/// </summary>
internal static class ConvertMemberVisitor
{
    public static Expression HandleConvertMethod(SQLiteCallerContext ctx)
    {
        SQLVisitor visitor = ctx.Visitor;
        MethodCallExpression node = (MethodCallExpression)ctx.Node;

        List<ResolvedModel> arguments = node.Arguments
            .Select(visitor.ResolveExpression)
            .ToList();

        bool isIntegerResult = node.Method.Name == nameof(Convert.ToInt32) || node.Method.Name == nameof(Convert.ToInt64);
        if (isIntegerResult
            && IsFloatingParameter(node.Method)
            && arguments[0].SQLiteExpression != null)
        {
            SQLiteExpression inner = arguments[0].SQLiteExpression!;
            return SQLiteExpression.Wrap(node.Method.ReturnType, visitor.Counters.NextIdentifier(), "CAST(ROUND(", inner, ") AS INTEGER)", inner.Parameters);
        }

        return Expression.Call(node.Method, CoerceArguments(visitor, node.Method, arguments));
    }

    private static bool IsFloatingParameter(MethodInfo method)
    {
        Type type = method.GetParameters()[0].ParameterType;
        return type == typeof(double) || type == typeof(float);
    }

    private static Expression[] CoerceArguments(SQLVisitor visitor, MethodInfo method, List<ResolvedModel> arguments)
    {
        ParameterInfo[] parameters = method.GetParameters();
        Expression[] result = new Expression[arguments.Count];
        for (int i = 0; i < arguments.Count; i++)
        {
            result[i] = visitor.CoerceClientExpression(arguments[i].Expression, parameters[i].ParameterType);
        }

        return result;
    }
}
