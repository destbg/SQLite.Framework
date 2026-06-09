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

#if SQLITE_FRAMEWORK_VERSION_AWARE
        if (TranslationPatterns.IsMathExtensionFunction(node.Method.Name)
            && !visitor.Database.Options.OverMinimumVersion(SQLiteMinimumVersion.V3_35))
        {
            return visitor.NotTranslatableBelowVersion(node, SQLiteMinimumVersion.V3_35, $"Math.{node.Method.Name}");
        }
#endif

        SQLiteParameter[]? parameters = ParameterHelpers.CombineParametersFromModels(arguments);

        Type returnType = node.Method.ReturnType;
        SQLiteExpression a0 = arguments[0].SQLiteExpression!;
        SQLiteExpression a1 = arguments.Count > 1 ? arguments[1].SQLiteExpression! : null!;
        SQLiteExpression a2 = arguments.Count > 2 ? arguments[2].SQLiteExpression! : null!;

        return node.Method.Name switch
        {
            nameof(Math.Min) => returnType == typeof(ulong)
                ? BuildUnsignedMinMax(visitor, isMax: false, returnType, a0, a1, parameters)
                : SQLiteExpression.Binary(returnType, visitor.Counters.NextIdentifier(), "MIN(", a0, ", ", a1, ")", parameters),
            nameof(Math.Max) => returnType == typeof(ulong)
                ? BuildUnsignedMinMax(visitor, isMax: true, returnType, a0, a1, parameters)
                : SQLiteExpression.Binary(returnType, visitor.Counters.NextIdentifier(), "MAX(", a0, ", ", a1, ")", parameters),
            nameof(Math.Abs) => SQLiteExpression.Wrap(returnType, visitor.Counters.NextIdentifier(), "ABS(", a0, ")", parameters),
            nameof(Math.Round) => HandleRound(visitor, node, arguments, parameters),
            nameof(Math.Ceiling) => SQLiteExpression.Wrap(returnType, visitor.Counters.NextIdentifier(), "CEIL(", a0, ")", parameters),
            nameof(Math.Floor) => SQLiteExpression.Wrap(returnType, visitor.Counters.NextIdentifier(), "FLOOR(", a0, ")", parameters),
            nameof(Math.Truncate) => SQLiteExpression.Wrap(returnType, visitor.Counters.NextIdentifier(), "TRUNC(", a0, ")", parameters),
            nameof(Math.Pow) => SQLiteExpression.Binary(returnType, visitor.Counters.NextIdentifier(), "POWER(", a0, ", ", a1, ")", parameters),
            nameof(Math.Sign) => SQLiteExpression.Binary(returnType, visitor.Counters.NextIdentifier(), "(CASE WHEN ", a0, " > 0 THEN 1 WHEN ", a0, " < 0 THEN -1 ELSE 0 END)", parameters),
            nameof(Math.Sqrt) => SQLiteExpression.Wrap(returnType, visitor.Counters.NextIdentifier(), "SQRT(", a0, ")", parameters),
            nameof(Math.Exp) => SQLiteExpression.Wrap(returnType, visitor.Counters.NextIdentifier(), "EXP(", a0, ")", parameters),
            nameof(Math.Log) when arguments.Count == 2 => SQLiteExpression.Binary(returnType, visitor.Counters.NextIdentifier(), "(LOG(", a0, ") / LOG(", a1, "))", parameters),
            nameof(Math.Log) => SQLiteExpression.Wrap(returnType, visitor.Counters.NextIdentifier(), "LN(", a0, ")", parameters),
            nameof(Math.Log10) => SQLiteExpression.Wrap(returnType, visitor.Counters.NextIdentifier(), "LOG10(", a0, ")", parameters),
            nameof(Math.Sin) => SQLiteExpression.Wrap(returnType, visitor.Counters.NextIdentifier(), "SIN(", a0, ")", parameters),
            nameof(Math.Cos) => SQLiteExpression.Wrap(returnType, visitor.Counters.NextIdentifier(), "COS(", a0, ")", parameters),
            nameof(Math.Tan) => SQLiteExpression.Wrap(returnType, visitor.Counters.NextIdentifier(), "TAN(", a0, ")", parameters),
            nameof(Math.Asin) => SQLiteExpression.Wrap(returnType, visitor.Counters.NextIdentifier(), "ASIN(", a0, ")", parameters),
            nameof(Math.Acos) => SQLiteExpression.Wrap(returnType, visitor.Counters.NextIdentifier(), "ACOS(", a0, ")", parameters),
            nameof(Math.Atan) => SQLiteExpression.Wrap(returnType, visitor.Counters.NextIdentifier(), "ATAN(", a0, ")", parameters),
            nameof(Math.Atan2) => SQLiteExpression.Binary(returnType, visitor.Counters.NextIdentifier(), "ATAN2(", a0, ", ", a1, ")", parameters),
            nameof(Math.Sinh) => SQLiteExpression.Wrap(returnType, visitor.Counters.NextIdentifier(), "SINH(", a0, ")", parameters),
            nameof(Math.Cosh) => SQLiteExpression.Wrap(returnType, visitor.Counters.NextIdentifier(), "COSH(", a0, ")", parameters),
            nameof(Math.Tanh) => SQLiteExpression.Wrap(returnType, visitor.Counters.NextIdentifier(), "TANH(", a0, ")", parameters),
            nameof(Math.Cbrt) => SubSelectBuilder.EvaluateOnce(visitor.Counters, returnType, [a0], v =>
                SQLiteExpression.Trinary(returnType, visitor.Counters.NextIdentifier(), "(CASE WHEN ", v[0], " >= 0 THEN POWER(", v[0], ", 1.0/3.0) ELSE -POWER(-", v[0], ", 1.0/3.0) END)", null)),
            nameof(Math.Log2) => SQLiteExpression.Wrap(returnType, visitor.Counters.NextIdentifier(), "LOG2(", a0, ")", parameters),
            nameof(Math.Asinh) => SQLiteExpression.Wrap(returnType, visitor.Counters.NextIdentifier(), "ASINH(", a0, ")", parameters),
            nameof(Math.Acosh) => SQLiteExpression.Wrap(returnType, visitor.Counters.NextIdentifier(), "ACOSH(", a0, ")", parameters),
            nameof(Math.Atanh) => SQLiteExpression.Wrap(returnType, visitor.Counters.NextIdentifier(), "ATANH(", a0, ")", parameters),
            nameof(Math.Clamp) => BuildClamp(visitor, node, returnType, arguments, a0, a1, a2, parameters),
            _ => visitor.NotTranslatable(node, $"Math.{node.Method.Name} is not translatable to SQL.")
        };
    }

    private static Expression BuildClamp(SQLVisitor visitor, MethodCallExpression node, Type returnType, List<ResolvedModel> arguments, SQLiteExpression a0, SQLiteExpression a1, SQLiteExpression a2, SQLiteParameter[]? parameters)
    {
        if (arguments[1].IsConstant && arguments[2].IsConstant
            && Convert.ToDouble(arguments[1].Constant) > Convert.ToDouble(arguments[2].Constant))
        {
            return visitor.NotTranslatable(node, "Math.Clamp requires the minimum to be less than or equal to the maximum.");
        }

        if (returnType == typeof(ulong))
        {
            SQLiteExpression inner = BuildUnsignedMinMax(visitor, isMax: false, returnType, a0, a2, parameters);
            return BuildUnsignedMinMax(visitor, isMax: true, returnType, a1, inner, parameters);
        }

        return SQLiteExpression.Multi(returnType, visitor.Counters.NextIdentifier(),
            ["MAX(", ", MIN(", ", ", "))"],
            [a1, a0, a2], parameters);
    }

    private static SQLiteExpression BuildUnsignedMinMax(SQLVisitor visitor, bool isMax, Type returnType, SQLiteExpression a, SQLiteExpression b, SQLiteParameter[]? parameters)
    {
        string cmpOp = isMax ? " >= " : " <= ";
        SQLiteExpression elseSide = isMax ? a : b;

        return SQLiteExpression.Multi(returnType, visitor.Counters.NextIdentifier(),
            ["(CASE WHEN (CASE WHEN ((", " < 0) = (", " < 0)) THEN ", cmpOp, " ELSE ", " < 0 END) THEN ", " ELSE ", " END)"],
            [a, b, a, b, elseSide, a, b],
            parameters);
    }

    private static Expression HandleRound(SQLVisitor visitor, MethodCallExpression node, List<ResolvedModel> arguments, SQLiteParameter[]? parameters)
    {
        if (arguments.Count == 1)
        {
            return BuildRound(visitor, node, arguments[0], digits: null, MidpointRounding.ToEven);
        }

        if (arguments.Count == 3)
        {
            return BuildRound(visitor, node, arguments[0], arguments[1], (MidpointRounding)arguments[2].Constant!);
        }

        if (arguments[1].Constant is MidpointRounding mode2)
        {
            return BuildRound(visitor, node, arguments[0], digits: null, mode2);
        }

        return BuildRound(visitor, node, arguments[0], arguments[1], MidpointRounding.ToEven);
    }

    private static Expression BuildRound(SQLVisitor visitor, MethodCallExpression node, ResolvedModel value, ResolvedModel? digits, MidpointRounding mode)
    {
        if (mode is not (MidpointRounding.AwayFromZero or MidpointRounding.ToEven))
        {
            return visitor.NotTranslatable(node,
                $"Math.Round with MidpointRounding.{mode} is not translatable to SQL. " +
                "SQLite supports round-half-away-from-zero (MidpointRounding.AwayFromZero) " +
                "and round-half-to-even (MidpointRounding.ToEven, the .NET default).");
        }

        SQLiteParameter[]? parameters = digits is null
            ? value.SQLiteExpression!.Parameters
            : ParameterHelpers.CombineParameters(value.SQLiteExpression!, digits.Value.SQLiteExpression!);

        SQLiteExpression valueExpr = value.SQLiteExpression!;
        SQLiteExpression? digitsExpr = digits?.SQLiteExpression;
        Type returnType = node.Method.ReturnType;

        if (mode == MidpointRounding.AwayFromZero)
        {
            return digitsExpr is null
                ? SQLiteExpression.Wrap(returnType, visitor.Counters.NextIdentifier(), "ROUND(", valueExpr, ")", parameters)
                : SQLiteExpression.Binary(returnType, visitor.Counters.NextIdentifier(), "ROUND(", valueExpr, ", ", digitsExpr, ")", parameters);
        }

        string v = valueExpr.ToString();
        string scaled = digitsExpr is null ? v : $"({v} * POWER(10, {digitsExpr}))";
        string toEven = $"(CASE WHEN ABS({scaled} - ROUND({scaled})) = 0.5 THEN 2 * ROUND({scaled} / 2) ELSE ROUND({scaled}) END)";
        string sql = digitsExpr is null ? toEven : $"({toEven} / POWER(10, {digitsExpr}))";

        return SQLiteExpression.Leaf(returnType, visitor.Counters.NextIdentifier(), sql, parameters);
    }
}
