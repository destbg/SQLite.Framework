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

        IEnumerable<Expression> resolvable = IsFrameMethod(node.Method.Name)
            ? node.Arguments.Take(node.Arguments.Count - 1)
            : node.Arguments;
        arguments.AddRange(resolvable.Select(visitor.ResolveExpression));

        SQLiteParameter[]? parameters = ParameterHelpers.CombineParameters(arguments
            .Select(a => a.SQLiteExpression)
            .Where(s => s != null)
            .Cast<SQLiteExpression>()
            .ToArray());

        if (node.Method.Name is nameof(SQLiteWindow<>.ThenPartitionBy) or nameof(SQLiteWindow<>.ThenOrderBy) or nameof(SQLiteWindow<>.ThenOrderByDescending))
        {
            RequireWindowChainPredecessor(node);
        }

        if (node.Method.Name is nameof(SQLiteWindow<>.PartitionBy))
        {
            RequirePartitionByFirst(node);
        }

        Type t = node.Type;
        int id = visitor.Counters.NextIdentifier();
        return node.Method.Name switch
        {
            nameof(SQLiteWindowFunctions.Sum) => SQLiteExpression.Wrap(t, id, "SUM(COALESCE(", arguments[0].SQLiteExpression!, ", 0)) OVER ()", parameters),
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
            nameof(SQLiteWindow<>.Filter) => Filter(visitor, t, id, arguments[0], arguments[1], parameters),
            nameof(SQLiteWindow<>.PartitionBy) => SQLiteExpression.Lambda(t, id, sb => WriteOverChain(sb, arguments[0], " PARTITION BY ", arguments[1]), parameters),
            nameof(SQLiteWindow<>.ThenPartitionBy) => SQLiteExpression.Lambda(t, id, sb => WriteOverChain(sb, arguments[0], ", ", arguments[1]), parameters),
            nameof(SQLiteWindow<>.OrderBy) => SQLiteExpression.Lambda(t, id, sb => WriteOverChainOrderBy(sb, arguments[0], " ORDER BY ", arguments[1], " ASC"), parameters),
            nameof(SQLiteWindow<>.OrderByDescending) => SQLiteExpression.Lambda(t, id, sb => WriteOverChainOrderBy(sb, arguments[0], " ORDER BY ", arguments[1], " DESC"), parameters),
            nameof(SQLiteWindow<>.ThenOrderBy) => SQLiteExpression.Lambda(t, id, sb => WriteOverChainOrderBy(sb, arguments[0], ", ", arguments[1], " ASC"), parameters),
            nameof(SQLiteWindow<>.ThenOrderByDescending) => SQLiteExpression.Lambda(t, id, sb => WriteOverChainOrderBy(sb, arguments[0], ", ", arguments[1], " DESC"), parameters),
            nameof(SQLiteWindow<>.Rows) => Frame(visitor, t, id, arguments, " ROWS BETWEEN ", node, parameters),
            nameof(SQLiteWindow<>.Range) => Frame(visitor, t, id, arguments, " RANGE BETWEEN ", node, parameters),
            nameof(SQLiteWindow<>.Groups) => Frame(visitor, t, id, arguments, " GROUPS BETWEEN ", node, parameters),
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

    private static SQLiteExpression Filter(SQLVisitor visitor, Type t, int id, ResolvedModel prev, ResolvedModel predicate, SQLiteParameter[]? parameters)
    {
#if SQLITE_FRAMEWORK_VERSION_AWARE
        visitor.Database.Options.EnsureMinimumVersion(SQLiteMinimumVersion.V3_30, "FILTER (WHERE ...) on window aggregates");
#endif
        return SQLiteExpression.Lambda(t, id, sb => WriteFilter(sb, prev, predicate), parameters);
    }

    private static SQLiteExpression Frame(SQLVisitor visitor, Type t, int id, List<ResolvedModel> arguments, string keyword, MethodCallExpression node, SQLiteParameter[]? parameters)
    {
#if SQLITE_FRAMEWORK_VERSION_AWARE
        if (keyword.Contains("GROUPS"))
        {
            visitor.Database.Options.EnsureMinimumVersion(SQLiteMinimumVersion.V3_28, "GROUPS frame type");
        }
#endif
        string exclude = ExcludeKeyword(visitor, node);
        ResolvedModel prev = arguments[0];
        ResolvedModel start = arguments[1];
        ResolvedModel end = arguments[2];
        return SQLiteExpression.Lambda(t, id, sb => WriteFrame(sb, prev, keyword, start, end, exclude), parameters);
    }

    private static string ExcludeKeyword(SQLVisitor visitor, MethodCallExpression node)
    {
        SQLiteFrameExclude exclude = (SQLiteFrameExclude)ExpressionHelpers.GetConstantValue(node.Arguments[^1])!;
        string keyword;
        switch (exclude)
        {
            case SQLiteFrameExclude.NoOthers:
                return "";
            case SQLiteFrameExclude.CurrentRow:
                keyword = " EXCLUDE CURRENT ROW";
                break;
            case SQLiteFrameExclude.Group:
                keyword = " EXCLUDE GROUP";
                break;
            case SQLiteFrameExclude.Ties:
                keyword = " EXCLUDE TIES";
                break;
            default:
                throw new NotSupportedException($"SQLiteFrameExclude.{exclude} is not translatable to SQL.");
        }
#if SQLITE_FRAMEWORK_VERSION_AWARE
        visitor.Database.Options.EnsureMinimumVersion(SQLiteMinimumVersion.V3_28, "EXCLUDE in window frames");
#endif
        return keyword;
    }

    private static bool IsFrameMethod(string name)
    {
        return name is nameof(SQLiteWindow<>.Rows) or nameof(SQLiteWindow<>.Range) or nameof(SQLiteWindow<>.Groups);
    }

    private static void RequirePartitionByFirst(MethodCallExpression node)
    {
        MethodCallExpression call = (MethodCallExpression)node.Object!;
        while (call.Method.Name is nameof(SQLiteWindow<>.Over) or nameof(SQLiteWindow<>.Filter))
        {
            if (call.Object is not MethodCallExpression inner)
            {
                return;
            }

            call = inner;
        }

        if (call.Method.Name is nameof(SQLiteWindow<>.PartitionBy) or nameof(SQLiteWindow<>.ThenPartitionBy)
            or nameof(SQLiteWindow<>.OrderBy) or nameof(SQLiteWindow<>.OrderByDescending)
            or nameof(SQLiteWindow<>.ThenOrderBy) or nameof(SQLiteWindow<>.ThenOrderByDescending))
        {
            throw new NotSupportedException(
                "PartitionBy must come right after Over. Use ThenPartitionBy to add more partition columns.");
        }
    }

    private static void RequireWindowChainPredecessor(MethodCallExpression node)
    {
        bool orderChain = node.Method.Name is nameof(SQLiteWindow<>.ThenOrderBy) or nameof(SQLiteWindow<>.ThenOrderByDescending);

        MethodCallExpression call = (MethodCallExpression)node.Object!;
        while (call.Method.Name is nameof(SQLiteWindow<>.Over) or nameof(SQLiteWindow<>.Filter))
        {
            call = (MethodCallExpression)call.Object!;
        }

        bool satisfied = orderChain
            ? call.Method.Name is nameof(SQLiteWindow<>.OrderBy) or nameof(SQLiteWindow<>.OrderByDescending) or nameof(SQLiteWindow<>.ThenOrderBy) or nameof(SQLiteWindow<>.ThenOrderByDescending)
            : call.Method.Name is nameof(SQLiteWindow<>.PartitionBy) or nameof(SQLiteWindow<>.ThenPartitionBy);

        if (satisfied)
        {
            return;
        }

        string required = orderChain
            ? "OrderBy, OrderByDescending, ThenOrderBy, or ThenOrderByDescending"
            : "PartitionBy or ThenPartitionBy";
        throw new NotSupportedException($"{node.Method.Name} must come right after {required} in the window chain.");
    }

    private static SQLiteExpression RequireKeyExpression(ResolvedModel arg)
    {
        return arg.SQLiteExpression
            ?? throw new NotSupportedException(
                "A window PARTITION BY or ORDER BY key must be a single column or expression. " +
                "To partition or order by several keys, chain ThenPartitionBy or ThenOrderBy.");
    }

    private static void WriteOverChain(StringBuilder sb, ResolvedModel prev, string sep, ResolvedModel arg)
    {
        prev.SQLiteExpression!.WriteSqlTo(sb);
        sb.Length--;
        sb.Append(sep);
        RequireKeyExpression(arg).WriteSqlTo(sb);
        sb.Append(')');
    }

    private static void WriteOverChainOrderBy(StringBuilder sb, ResolvedModel prev, string sep, ResolvedModel arg, string direction)
    {
        prev.SQLiteExpression!.WriteSqlTo(sb);
        sb.Length--;
        sb.Append(sep);
        RequireKeyExpression(arg).WriteSqlTo(sb);
        sb.Append(direction);
        sb.Append(')');
    }

    private static void WriteFrame(StringBuilder sb, ResolvedModel prev, string keyword, ResolvedModel start, ResolvedModel end, string exclude)
    {
        prev.SQLiteExpression!.WriteSqlTo(sb);
        sb.Length--;
        sb.Append(keyword);
        start.SQLiteExpression!.WriteSqlTo(sb);
        sb.Append(" AND ");
        end.SQLiteExpression!.WriteSqlTo(sb);
        sb.Append(exclude);
        sb.Append(')');
    }

    private static void WriteFilter(StringBuilder sb, ResolvedModel prev, ResolvedModel predicate)
    {
        int start = sb.Length;
        prev.SQLiteExpression!.WriteSqlTo(sb);
        int overIndex = start + sb.ToString(start, sb.Length - start).IndexOf(" OVER (", StringComparison.Ordinal);

        StringBuilder clause = StringBuilderPool.Rent();
        clause.Append(" FILTER (WHERE ");
        predicate.SQLiteExpression!.WriteSqlTo(clause);
        clause.Append(')');
        sb.Insert(overIndex, StringBuilderPool.ToStringAndReturn(clause));
    }
}
