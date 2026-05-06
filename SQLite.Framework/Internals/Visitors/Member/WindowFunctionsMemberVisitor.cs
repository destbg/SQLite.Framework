namespace SQLite.Framework.Internals.Visitors.Member;

internal static class WindowFunctionsMemberVisitor
{
    public static Expression HandleWindowFunctionMethod(SQLiteCallerContext ctx)
    {

        SQLVisitor visitor = ctx.Visitor;
        MethodCallExpression node = (MethodCallExpression)ctx.Node;
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
                return new SQLiteExpression(node.Type, visitor.Counters.IdentifierIndex++, "UNBOUNDED PRECEDING", null);
            case nameof(SQLiteFrameBoundary.CurrentRow):
                return new SQLiteExpression(node.Type, visitor.Counters.IdentifierIndex++, "CURRENT ROW", null);
            case nameof(SQLiteFrameBoundary.UnboundedFollowing):
                return new SQLiteExpression(node.Type, visitor.Counters.IdentifierIndex++, "UNBOUNDED FOLLOWING", null);
            case nameof(SQLiteFrameBoundary.Preceding):
            {
                ResolvedModel arg = visitor.ResolveExpression(node.Arguments[0]);
                return new SQLiteExpression(node.Type, visitor.Counters.IdentifierIndex++, $"{arg.Sql} PRECEDING", arg.SQLiteExpression!.Parameters);
            }
            case nameof(SQLiteFrameBoundary.Following):
            {
                ResolvedModel arg = visitor.ResolveExpression(node.Arguments[0]);
                return new SQLiteExpression(node.Type, visitor.Counters.IdentifierIndex++, $"{arg.Sql} FOLLOWING", arg.SQLiteExpression!.Parameters);
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

        string sql = node.Method.Name switch
        {
            nameof(SQLiteWindowFunctions.Sum) => $"SUM({arguments[0].Sql}) OVER ()",
            nameof(SQLiteWindowFunctions.Avg) => $"AVG({arguments[0].Sql}) OVER ()",
            nameof(SQLiteWindowFunctions.Min) => $"MIN({arguments[0].Sql}) OVER ()",
            nameof(SQLiteWindowFunctions.Max) => $"MAX({arguments[0].Sql}) OVER ()",
            nameof(SQLiteWindowFunctions.Count) when arguments.Count == 0 => "COUNT(*) OVER ()",
            nameof(SQLiteWindowFunctions.Count) => $"COUNT({arguments[0].Sql}) OVER ()",
            nameof(SQLiteWindowFunctions.RowNumber) => "ROW_NUMBER() OVER ()",
            nameof(SQLiteWindowFunctions.Rank) => "RANK() OVER ()",
            nameof(SQLiteWindowFunctions.DenseRank) => "DENSE_RANK() OVER ()",
            nameof(SQLiteWindowFunctions.PercentRank) => "PERCENT_RANK() OVER ()",
            nameof(SQLiteWindowFunctions.CumeDist) => "CUME_DIST() OVER ()",
            nameof(SQLiteWindowFunctions.NTile) => $"NTILE({arguments[0].Sql}) OVER ()",
            nameof(SQLiteWindowFunctions.Lag) when arguments.Count == 1 => $"LAG({arguments[0].Sql}) OVER ()",
            nameof(SQLiteWindowFunctions.Lag) when arguments.Count == 2 => $"LAG({arguments[0].Sql}, {arguments[1].Sql}) OVER ()",
            nameof(SQLiteWindowFunctions.Lag) => $"LAG({arguments[0].Sql}, {arguments[1].Sql}, {arguments[2].Sql}) OVER ()",
            nameof(SQLiteWindowFunctions.Lead) when arguments.Count == 1 => $"LEAD({arguments[0].Sql}) OVER ()",
            nameof(SQLiteWindowFunctions.Lead) when arguments.Count == 2 => $"LEAD({arguments[0].Sql}, {arguments[1].Sql}) OVER ()",
            nameof(SQLiteWindowFunctions.Lead) => $"LEAD({arguments[0].Sql}, {arguments[1].Sql}, {arguments[2].Sql}) OVER ()",
            nameof(SQLiteWindowFunctions.FirstValue) => $"FIRST_VALUE({arguments[0].Sql}) OVER ()",
            nameof(SQLiteWindowFunctions.LastValue) => $"LAST_VALUE({arguments[0].Sql}) OVER ()",
            nameof(SQLiteWindowFunctions.NthValue) => $"NTH_VALUE({arguments[0].Sql}, {arguments[1].Sql}) OVER ()",
            nameof(SQLiteWindow<>.AsValue) => arguments[0].Sql!,
            nameof(SQLiteWindow<>.Over) => arguments[0].Sql!,
            nameof(SQLiteWindow<>.PartitionBy) => $"{CustomMemberVisitor.TrimClose(arguments[0].Sql!)} PARTITION BY {arguments[1].Sql})",
            nameof(SQLiteWindow<>.ThenPartitionBy) => $"{CustomMemberVisitor.TrimClose(arguments[0].Sql!)}, {arguments[1].Sql})",
            nameof(SQLiteWindow<>.OrderBy) => $"{CustomMemberVisitor.TrimClose(arguments[0].Sql!)} ORDER BY {arguments[1].Sql} ASC)",
            nameof(SQLiteWindow<>.OrderByDescending) => $"{CustomMemberVisitor.TrimClose(arguments[0].Sql!)} ORDER BY {arguments[1].Sql} DESC)",
            nameof(SQLiteWindow<>.ThenOrderBy) => $"{CustomMemberVisitor.TrimClose(arguments[0].Sql!)}, {arguments[1].Sql} ASC)",
            nameof(SQLiteWindow<>.ThenOrderByDescending) => $"{CustomMemberVisitor.TrimClose(arguments[0].Sql!)}, {arguments[1].Sql} DESC)",
            nameof(SQLiteWindow<>.Rows) => $"{CustomMemberVisitor.TrimClose(arguments[0].Sql!)} ROWS BETWEEN {arguments[1].Sql} AND {arguments[2].Sql})",
            nameof(SQLiteWindow<>.Range) => $"{CustomMemberVisitor.TrimClose(arguments[0].Sql!)} RANGE BETWEEN {arguments[1].Sql} AND {arguments[2].Sql})",
            nameof(SQLiteWindow<>.Groups) => $"{CustomMemberVisitor.TrimClose(arguments[0].Sql!)} GROUPS BETWEEN {arguments[1].Sql} AND {arguments[2].Sql})",
            _ => throw new NotSupportedException($"{node.Method.DeclaringType!.Name}.{node.Method.Name} is not translatable to SQL."),
        };

        return new SQLiteExpression(node.Type, visitor.Counters.IdentifierIndex++, sql, parameters);
    }
}
