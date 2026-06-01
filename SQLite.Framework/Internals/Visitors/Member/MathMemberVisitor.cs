namespace SQLite.Framework.Internals.Visitors.Member;

internal static class MathMemberVisitor
{
    private static readonly HashSet<string> MathExtensionFunctions = new(StringComparer.Ordinal)
    {
        nameof(Math.Ceiling), nameof(Math.Floor), nameof(Math.Truncate), nameof(Math.Pow),
        nameof(Math.Sqrt), nameof(Math.Exp), nameof(Math.Log), nameof(Math.Log10), nameof(Math.Log2),
        nameof(Math.Sin), nameof(Math.Cos), nameof(Math.Tan),
        nameof(Math.Asin), nameof(Math.Acos), nameof(Math.Atan), nameof(Math.Atan2),
        nameof(Math.Sinh), nameof(Math.Cosh), nameof(Math.Tanh),
        nameof(Math.Asinh), nameof(Math.Acosh), nameof(Math.Atanh),
        nameof(Math.Cbrt),
    };

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
        if (MathExtensionFunctions.Contains(node.Method.Name))
        {
            visitor.Database.Options.EnsureMinimumVersion(SQLiteMinimumVersion.V3_35, $"Math.{node.Method.Name}");
        }
#endif

        SQLiteParameter[]? parameters = ParameterHelpers.CombineParametersFromModels(arguments);

        Type returnType = node.Method.ReturnType;
        SQLiteExpression a0 = arguments[0].SQLiteExpression!;
        SQLiteExpression a1 = arguments.Count > 1 ? arguments[1].SQLiteExpression! : null!;
        SQLiteExpression a2 = arguments.Count > 2 ? arguments[2].SQLiteExpression! : null!;

        return node.Method.Name switch
        {
            nameof(Math.Min) => SQLiteExpression.Multi(returnType, visitor.Counters.NextIdentifier(), ["(CASE WHEN ", " < ", " THEN ", " ELSE ", " END)"], [a0, a1, a0, a1], parameters),
            nameof(Math.Max) => SQLiteExpression.Multi(returnType, visitor.Counters.NextIdentifier(), ["(CASE WHEN ", " > ", " THEN ", " ELSE ", " END)"], [a0, a1, a0, a1], parameters),
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
            nameof(Math.Cbrt) => SQLiteExpression.Trinary(returnType, visitor.Counters.NextIdentifier(), "(CASE WHEN ", a0, " >= 0 THEN POWER(", a0, ", 1.0/3.0) ELSE -POWER(-", a0, ", 1.0/3.0) END)", parameters),
            nameof(Math.Log2) => SQLiteExpression.Wrap(returnType, visitor.Counters.NextIdentifier(), "LOG2(", a0, ")", parameters),
            nameof(Math.Asinh) => SQLiteExpression.Wrap(returnType, visitor.Counters.NextIdentifier(), "ASINH(", a0, ")", parameters),
            nameof(Math.Acosh) => SQLiteExpression.Wrap(returnType, visitor.Counters.NextIdentifier(), "ACOSH(", a0, ")", parameters),
            nameof(Math.Atanh) => SQLiteExpression.Wrap(returnType, visitor.Counters.NextIdentifier(), "ATANH(", a0, ")", parameters),
            nameof(Math.Clamp) => SQLiteExpression.Multi(returnType, visitor.Counters.NextIdentifier(), ["(CASE WHEN ", " < ", " THEN ", " WHEN ", " > ", " THEN ", " ELSE ", " END)"], [a0, a1, a1, a0, a2, a2, a0], parameters),
            _ => throw new NotSupportedException($"Math.{node.Method.Name} is not translatable to SQL.")
        };
    }

    private static SQLiteExpression HandleRound(SQLVisitor visitor, MethodCallExpression node, List<ResolvedModel> arguments, SQLiteParameter[]? parameters)
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

    private static SQLiteExpression BuildRound(SQLVisitor visitor, MethodCallExpression node, ResolvedModel value, ResolvedModel? digits, MidpointRounding mode)
    {
        if (mode is not (MidpointRounding.AwayFromZero or MidpointRounding.ToEven))
        {
            throw new NotSupportedException(
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
