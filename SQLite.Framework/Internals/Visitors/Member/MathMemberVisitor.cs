namespace SQLite.Framework.Internals.Visitors.Member;

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
            nameof(Math.Round) => HandleRound(visitor, node, arguments, parameters),
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
            nameof(Math.Sin) => new SQLiteExpression(
                node.Method.ReturnType,
                visitor.Counters.IdentifierIndex++,
                $"SIN({arguments[0].Sql})",
                parameters
            ),
            nameof(Math.Cos) => new SQLiteExpression(
                node.Method.ReturnType,
                visitor.Counters.IdentifierIndex++,
                $"COS({arguments[0].Sql})",
                parameters
            ),
            nameof(Math.Tan) => new SQLiteExpression(
                node.Method.ReturnType,
                visitor.Counters.IdentifierIndex++,
                $"TAN({arguments[0].Sql})",
                parameters
            ),
            nameof(Math.Asin) => new SQLiteExpression(
                node.Method.ReturnType,
                visitor.Counters.IdentifierIndex++,
                $"ASIN({arguments[0].Sql})",
                parameters
            ),
            nameof(Math.Acos) => new SQLiteExpression(
                node.Method.ReturnType,
                visitor.Counters.IdentifierIndex++,
                $"ACOS({arguments[0].Sql})",
                parameters
            ),
            nameof(Math.Atan) => new SQLiteExpression(
                node.Method.ReturnType,
                visitor.Counters.IdentifierIndex++,
                $"ATAN({arguments[0].Sql})",
                parameters
            ),
            nameof(Math.Atan2) => new SQLiteExpression(
                node.Method.ReturnType,
                visitor.Counters.IdentifierIndex++,
                $"ATAN2({arguments[0].Sql}, {arguments[1].Sql})",
                parameters
            ),
            nameof(Math.Sinh) => new SQLiteExpression(
                node.Method.ReturnType,
                visitor.Counters.IdentifierIndex++,
                $"SINH({arguments[0].Sql})",
                parameters
            ),
            nameof(Math.Cosh) => new SQLiteExpression(
                node.Method.ReturnType,
                visitor.Counters.IdentifierIndex++,
                $"COSH({arguments[0].Sql})",
                parameters
            ),
            nameof(Math.Tanh) => new SQLiteExpression(
                node.Method.ReturnType,
                visitor.Counters.IdentifierIndex++,
                $"TANH({arguments[0].Sql})",
                parameters
            ),
            nameof(Math.Cbrt) => new SQLiteExpression(
                node.Method.ReturnType,
                visitor.Counters.IdentifierIndex++,
                $"(CASE WHEN {arguments[0].Sql} >= 0 THEN POWER({arguments[0].Sql}, 1.0/3.0) ELSE -POWER(-{arguments[0].Sql}, 1.0/3.0) END)",
                parameters
            ),
            nameof(Math.Log2) => new SQLiteExpression(
                node.Method.ReturnType,
                visitor.Counters.IdentifierIndex++,
                $"LOG2({arguments[0].Sql})",
                parameters
            ),
            nameof(Math.Asinh) => new SQLiteExpression(
                node.Method.ReturnType,
                visitor.Counters.IdentifierIndex++,
                $"ASINH({arguments[0].Sql})",
                parameters
            ),
            nameof(Math.Acosh) => new SQLiteExpression(
                node.Method.ReturnType,
                visitor.Counters.IdentifierIndex++,
                $"ACOSH({arguments[0].Sql})",
                parameters
            ),
            nameof(Math.Atanh) => new SQLiteExpression(
                node.Method.ReturnType,
                visitor.Counters.IdentifierIndex++,
                $"ATANH({arguments[0].Sql})",
                parameters
            ),
            nameof(Math.Clamp) => new SQLiteExpression(
                node.Method.ReturnType,
                visitor.Counters.IdentifierIndex++,
                $"(CASE WHEN {arguments[0].Sql} < {arguments[1].Sql} THEN {arguments[1].Sql} WHEN {arguments[0].Sql} > {arguments[2].Sql} THEN {arguments[2].Sql} ELSE {arguments[0].Sql} END)",
                parameters
            ),
            _ => throw new NotSupportedException($"Math.{node.Method.Name} is not translatable to SQL.")
        };
    }

    private static SQLiteExpression HandleRound(SQLVisitor visitor, MethodCallExpression node, List<ResolvedModel> arguments, SQLiteParameter[]? parameters)
    {
        if (arguments.Count == 1)
        {
            return new SQLiteExpression(node.Method.ReturnType, visitor.Counters.IdentifierIndex++, $"ROUND({arguments[0].Sql})", parameters);
        }

        if (arguments.Count == 3)
        {
            return BuildRound(visitor, node, arguments[0], arguments[1], (MidpointRounding)arguments[2].Constant!);
        }

        if (arguments[1].Constant is MidpointRounding mode2)
        {
            return BuildRound(visitor, node, arguments[0], digits: null, mode2);
        }

        return new SQLiteExpression(node.Method.ReturnType, visitor.Counters.IdentifierIndex++, $"ROUND({arguments[0].Sql}, {arguments[1].Sql})", parameters);
    }

    private static SQLiteExpression BuildRound(SQLVisitor visitor, MethodCallExpression node, ResolvedModel value, ResolvedModel? digits, MidpointRounding mode)
    {
        if (mode != MidpointRounding.AwayFromZero)
        {
            throw new NotSupportedException(
                $"Math.Round with MidpointRounding.{mode} is not translatable to SQL. " +
                "SQLite's ROUND uses round-half-away-from-zero, so only MidpointRounding.AwayFromZero is supported.");
        }

        SQLiteParameter[]? parameters = digits is null
            ? value.SQLiteExpression!.Parameters
            : ParameterHelpers.CombineParameters(value.SQLiteExpression!, digits.Value.SQLiteExpression!);

        string sql = digits is null
            ? $"ROUND({value.Sql})"
            : $"ROUND({value.Sql}, {digits.Value.Sql})";

        return new SQLiteExpression(node.Method.ReturnType, visitor.Counters.IdentifierIndex++, sql, parameters);
    }
}
