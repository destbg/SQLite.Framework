namespace SQLite.Framework.Internals.Visitors;

internal static class MathMemberVisitor
{
    public static Expression HandleMathMethod(SQLiteCallerContext ctx)
    {

        SQLVisitor visitor = ctx.Visitor;
        MethodCallExpression node = (MethodCallExpression)ctx.Node;
        List<ResolvedModel> arguments = node.Arguments
            .Select(visitor.ResolveExpression)
            .ToList();

        if (QueryableMemberVisitor.CheckConstantMethod<double>(visitor, node, arguments, out Expression? expression))
        {
            return expression;
        }

        SQLiteParameter[]? parameters = ParameterHelpers.CombineParametersFromModels(arguments);

        return node.Method.Name switch
        {
            nameof(Math.Min) => new SQLiteExpression(
                node.Method.ReturnType,
                visitor.Counters.IdentifierIndex++,
                $"(CASE WHEN {arguments[0].Sql} < {arguments[1].Sql} THEN {arguments[0].Sql} ELSE {arguments[1].Sql} END)",
                parameters
            ),
            nameof(Math.Max) => new SQLiteExpression(
                node.Method.ReturnType,
                visitor.Counters.IdentifierIndex++,
                $"(CASE WHEN {arguments[0].Sql} > {arguments[1].Sql} THEN {arguments[0].Sql} ELSE {arguments[1].Sql} END)",
                parameters
            ),
            nameof(Math.Abs) => new SQLiteExpression(
                node.Method.ReturnType,
                visitor.Counters.IdentifierIndex++,
                $"ABS({arguments[0].Sql})",
                parameters
            ),
            nameof(Math.Round) => new SQLiteExpression(
                node.Method.ReturnType,
                visitor.Counters.IdentifierIndex++,
                arguments.Count == 2 ? $"ROUND({arguments[0].Sql}, {arguments[1].Sql})" : $"ROUND({arguments[0].Sql})",
                parameters
            ),
            nameof(Math.Ceiling) => new SQLiteExpression(
                node.Method.ReturnType,
                visitor.Counters.IdentifierIndex++,
                $"CEIL({arguments[0].Sql})",
                parameters
            ),
            nameof(Math.Floor) => new SQLiteExpression(
                node.Method.ReturnType,
                visitor.Counters.IdentifierIndex++,
                $"FLOOR({arguments[0].Sql})",
                parameters
            ),
            nameof(Math.Truncate) => new SQLiteExpression(
                node.Method.ReturnType,
                visitor.Counters.IdentifierIndex++,
                $"TRUNC({arguments[0].Sql})",
                parameters
            ),
            nameof(Math.Pow) => new SQLiteExpression(
                node.Method.ReturnType,
                visitor.Counters.IdentifierIndex++,
                $"POWER({arguments[0].Sql}, {arguments[1].Sql})",
                parameters
            ),
            nameof(Math.Sign) => new SQLiteExpression(
                node.Method.ReturnType,
                visitor.Counters.IdentifierIndex++,
                $"(CASE WHEN {arguments[0].Sql} > 0 THEN 1 WHEN {arguments[0].Sql} < 0 THEN -1 ELSE 0 END)",
                parameters
            ),
            nameof(Math.Sqrt) => new SQLiteExpression(
                node.Method.ReturnType,
                visitor.Counters.IdentifierIndex++,
                $"SQRT({arguments[0].Sql})",
                parameters
            ),
            nameof(Math.Exp) => new SQLiteExpression(
                node.Method.ReturnType,
                visitor.Counters.IdentifierIndex++,
                $"EXP({arguments[0].Sql})",
                parameters
            ),
            nameof(Math.Log) => new SQLiteExpression(
                node.Method.ReturnType,
                visitor.Counters.IdentifierIndex++,
                arguments.Count == 2 ? $"(LOG({arguments[0].Sql}) / LOG({arguments[1].Sql}))" : $"LOG({arguments[0].Sql})",
                parameters
            ),
            nameof(Math.Log10) => new SQLiteExpression(
                node.Method.ReturnType,
                visitor.Counters.IdentifierIndex++,
                $"LOG10({arguments[0].Sql})",
                parameters
            ),
            _ => throw new NotSupportedException($"Math.{node.Method.Name} is not translatable to SQL.")
        };
    }
}
