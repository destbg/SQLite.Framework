namespace SQLite.Framework.Internals.Visitors.Member;

internal static class WindowFunctionsMemberVisitor
{
    public static Expression HandleWindowFunctionMethod(SQLiteCallerContext ctx)
    {
        SQLVisitor visitor = ctx.Visitor;
        MethodCallExpression node = (MethodCallExpression)ctx.Node;
#if SQLITE_FRAMEWORK_VERSION_AWARE
        visitor.Database.Options.EnsureMinimumVersion(SQLiteMinimumVersion.V3_25, "Window functions");
#endif
        if (node.Method.DeclaringType == typeof(SQLiteFrameBoundary))
        {
            return HandleFrameBoundary(visitor, node);
        }

        return HandleWindowFunction(visitor, node);
    }

    private static SQLiteExpression HandleFrameBoundary(SQLVisitor visitor, MethodCallExpression node)
    {
        switch (node.Method.Name)
        {
            case nameof(SQLiteFrameBoundary.UnboundedPreceding):
                return SQLiteExpression.Leaf(node.Type, visitor.Counters.NextIdentifier(), "UNBOUNDED PRECEDING", null);
            case nameof(SQLiteFrameBoundary.CurrentRow):
                return SQLiteExpression.Leaf(node.Type, visitor.Counters.NextIdentifier(), "CURRENT ROW", null);
            case nameof(SQLiteFrameBoundary.UnboundedFollowing):
                return SQLiteExpression.Leaf(node.Type, visitor.Counters.NextIdentifier(), "UNBOUNDED FOLLOWING", null);
            case nameof(SQLiteFrameBoundary.Preceding):
            {
                ResolvedModel arg = visitor.ResolveExpression(node.Arguments[0]);
                return SQLiteExpression.Wrap(node.Type, visitor.Counters.NextIdentifier(), "", arg.SQLiteExpression!, " PRECEDING", arg.SQLiteExpression!.Parameters);
            }
            case nameof(SQLiteFrameBoundary.Following):
            {
                ResolvedModel arg = visitor.ResolveExpression(node.Arguments[0]);
                return SQLiteExpression.Wrap(node.Type, visitor.Counters.NextIdentifier(), "", arg.SQLiteExpression!, " FOLLOWING", arg.SQLiteExpression!.Parameters);
            }
            default:
                throw new NotSupportedException($"SQLiteFrameBoundary.{node.Method.Name} is not translatable to SQL.");
        }
    }

    private static SQLiteExpression HandleWindowFunction(SQLVisitor visitor, MethodCallExpression node)
    {
        List<ResolvedModel> arguments = node.Object != null
            ? new List<ResolvedModel> { visitor.ResolveExpression(node.Object) }
            : new List<ResolvedModel>();
        arguments.AddRange(node.Arguments.Select(visitor.ResolveExpression));

        SQLiteParameter[]? parameters = ParameterHelpers.CombineParameters(arguments
            .Select(a => a.SQLiteExpression)
            .Where(s => s != null)
            .Cast<SQLiteExpression>()
            .ToArray());

        Type t = node.Type;
        int id = visitor.Counters.NextIdentifier();
        return node.Method.Name switch
        {
            nameof(SQLiteWindowFunctions.Sum) => FnOver(t, id, "SUM", arguments[0], parameters),
            nameof(SQLiteWindowFunctions.Avg) => FnOver(t, id, "AVG", arguments[0], parameters),
            nameof(SQLiteWindowFunctions.Min) => FnOver(t, id, "MIN", arguments[0], parameters),
            nameof(SQLiteWindowFunctions.Max) => FnOver(t, id, "MAX", arguments[0], parameters),
            nameof(SQLiteWindowFunctions.Count) when arguments.Count == 0 => SQLiteExpression.Leaf(t, id, "COUNT(*) OVER ()", parameters),
            nameof(SQLiteWindowFunctions.Count) => FnOver(t, id, "COUNT", arguments[0], parameters),
            nameof(SQLiteWindowFunctions.RowNumber) => SQLiteExpression.Leaf(t, id, "ROW_NUMBER() OVER ()", parameters),
            nameof(SQLiteWindowFunctions.Rank) => SQLiteExpression.Leaf(t, id, "RANK() OVER ()", parameters),
            nameof(SQLiteWindowFunctions.DenseRank) => SQLiteExpression.Leaf(t, id, "DENSE_RANK() OVER ()", parameters),
            nameof(SQLiteWindowFunctions.PercentRank) => SQLiteExpression.Leaf(t, id, "PERCENT_RANK() OVER ()", parameters),
            nameof(SQLiteWindowFunctions.CumeDist) => SQLiteExpression.Leaf(t, id, "CUME_DIST() OVER ()", parameters),
            nameof(SQLiteWindowFunctions.NTile) => FnOver(t, id, "NTILE", arguments[0], parameters),
            nameof(SQLiteWindowFunctions.Lag) when arguments.Count == 1 => FnOver(t, id, "LAG", arguments[0], parameters),
            nameof(SQLiteWindowFunctions.Lag) when arguments.Count == 2 => FnOver(t, id, "LAG", arguments[0], arguments[1], parameters),
            nameof(SQLiteWindowFunctions.Lag) => FnOver(t, id, "LAG", arguments[0], arguments[1], arguments[2], parameters),
            nameof(SQLiteWindowFunctions.Lead) when arguments.Count == 1 => FnOver(t, id, "LEAD", arguments[0], parameters),
            nameof(SQLiteWindowFunctions.Lead) when arguments.Count == 2 => FnOver(t, id, "LEAD", arguments[0], arguments[1], parameters),
            nameof(SQLiteWindowFunctions.Lead) => FnOver(t, id, "LEAD", arguments[0], arguments[1], arguments[2], parameters),
            nameof(SQLiteWindowFunctions.FirstValue) => FnOver(t, id, "FIRST_VALUE", arguments[0], parameters),
            nameof(SQLiteWindowFunctions.LastValue) => FnOver(t, id, "LAST_VALUE", arguments[0], parameters),
            nameof(SQLiteWindowFunctions.NthValue) => FnOver(t, id, "NTH_VALUE", arguments[0], arguments[1], parameters),
            nameof(SQLiteWindow<>.AsValue) => SQLiteExpression.Alias(t, id, arguments[0].SQLiteExpression!, parameters),
            nameof(SQLiteWindow<>.Over) => SQLiteExpression.Alias(t, id, arguments[0].SQLiteExpression!, parameters),
            nameof(SQLiteWindow<>.PartitionBy) => SQLiteExpression.Lambda(t, id, sb => WriteOverChain(sb, arguments[0], " PARTITION BY ", arguments[1]), parameters),
            nameof(SQLiteWindow<>.ThenPartitionBy) => SQLiteExpression.Lambda(t, id, sb => WriteOverChain(sb, arguments[0], ", ", arguments[1]), parameters),
            nameof(SQLiteWindow<>.OrderBy) => SQLiteExpression.Lambda(t, id, sb => WriteOverChainOrderBy(sb, arguments[0], " ORDER BY ", arguments[1], " ASC"), parameters),
            nameof(SQLiteWindow<>.OrderByDescending) => SQLiteExpression.Lambda(t, id, sb => WriteOverChainOrderBy(sb, arguments[0], " ORDER BY ", arguments[1], " DESC"), parameters),
            nameof(SQLiteWindow<>.ThenOrderBy) => SQLiteExpression.Lambda(t, id, sb => WriteOverChainOrderBy(sb, arguments[0], ", ", arguments[1], " ASC"), parameters),
            nameof(SQLiteWindow<>.ThenOrderByDescending) => SQLiteExpression.Lambda(t, id, sb => WriteOverChainOrderBy(sb, arguments[0], ", ", arguments[1], " DESC"), parameters),
            nameof(SQLiteWindow<>.Rows) => SQLiteExpression.Lambda(t, id, sb => WriteFrame(sb, arguments[0], " ROWS BETWEEN ", arguments[1], arguments[2]), parameters),
            nameof(SQLiteWindow<>.Range) => SQLiteExpression.Lambda(t, id, sb => WriteFrame(sb, arguments[0], " RANGE BETWEEN ", arguments[1], arguments[2]), parameters),
            nameof(SQLiteWindow<>.Groups) => SQLiteExpression.Lambda(t, id, sb => WriteFrame(sb, arguments[0], " GROUPS BETWEEN ", arguments[1], arguments[2]), parameters),
            _ => throw new NotSupportedException($"{node.Method.DeclaringType!.Name}.{node.Method.Name} is not translatable to SQL."),
        };
    }

    private static SQLiteExpression FnOver(Type t, int id, string fn, ResolvedModel a, SQLiteParameter[]? parameters)
    {
        return SQLiteExpression.Wrap(t, id, $"{fn}(", a.SQLiteExpression!, ") OVER ()", parameters);
    }

    private static SQLiteExpression FnOver(Type t, int id, string fn, ResolvedModel a, ResolvedModel b, SQLiteParameter[]? parameters)
    {
        return SQLiteExpression.Binary(t, id, $"{fn}(", a.SQLiteExpression!, ", ", b.SQLiteExpression!, ") OVER ()", parameters);
    }

    private static SQLiteExpression FnOver(Type t, int id, string fn, ResolvedModel a, ResolvedModel b, ResolvedModel c, SQLiteParameter[]? parameters)
    {
        return SQLiteExpression.Trinary(t, id, $"{fn}(", a.SQLiteExpression!, ", ", b.SQLiteExpression!, ", ", c.SQLiteExpression!, ") OVER ()", parameters);
    }

    private static void WriteOverChain(StringBuilder sb, ResolvedModel prev, string sep, ResolvedModel arg)
    {
        prev.SQLiteExpression!.WriteSqlTo(sb);
        sb.Length--;
        sb.Append(sep);
        arg.SQLiteExpression!.WriteSqlTo(sb);
        sb.Append(')');
    }

    private static void WriteOverChainOrderBy(StringBuilder sb, ResolvedModel prev, string sep, ResolvedModel arg, string direction)
    {
        prev.SQLiteExpression!.WriteSqlTo(sb);
        sb.Length--;
        sb.Append(sep);
        arg.SQLiteExpression!.WriteSqlTo(sb);
        sb.Append(direction);
        sb.Append(')');
    }

    private static void WriteFrame(StringBuilder sb, ResolvedModel prev, string keyword, ResolvedModel start, ResolvedModel end)
    {
        prev.SQLiteExpression!.WriteSqlTo(sb);
        sb.Length--;
        sb.Append(keyword);
        start.SQLiteExpression!.WriteSqlTo(sb);
        sb.Append(" AND ");
        end.SQLiteExpression!.WriteSqlTo(sb);
        sb.Append(')');
    }
}
