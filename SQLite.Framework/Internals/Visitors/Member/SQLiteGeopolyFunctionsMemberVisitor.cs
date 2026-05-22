namespace SQLite.Framework.Internals.Visitors.Member;

internal static class SQLiteGeopolyFunctionsMemberVisitor
{
    private static readonly Dictionary<string, string> SqlFunctionNames = new(StringComparer.Ordinal)
    {
        [nameof(SQLiteGeopolyFunctions.Overlap)] = "geopoly_overlap",
        [nameof(SQLiteGeopolyFunctions.Within)] = "geopoly_within",
        [nameof(SQLiteGeopolyFunctions.Area)] = "geopoly_area",
        [nameof(SQLiteGeopolyFunctions.ContainsPoint)] = "geopoly_contains_point",
        [nameof(SQLiteGeopolyFunctions.BoundingBox)] = "geopoly_bbox",
        [nameof(SQLiteGeopolyFunctions.Blob)] = "geopoly_blob",
        [nameof(SQLiteGeopolyFunctions.Json)] = "geopoly_json",
        [nameof(SQLiteGeopolyFunctions.Svg)] = "geopoly_svg",
        [nameof(SQLiteGeopolyFunctions.CounterClockwise)] = "geopoly_ccw",
        [nameof(SQLiteGeopolyFunctions.Regular)] = "geopoly_regular",
        [nameof(SQLiteGeopolyFunctions.Transform)] = "geopoly_xform",
    };

    public static Expression HandleSQLiteGeopolyFunctionsMethod(SQLiteCallerContext ctx)
    {
        SQLVisitor visitor = ctx.Visitor;
        MethodCallExpression node = (MethodCallExpression)ctx.Node;
#if SQLITE_FRAMEWORK_OS_BUNDLED_SQLITE
        visitor.Database.Options.EnsureMinimumVersion(SQLiteMinimumVersion.V3_27, $"SQLiteGeopolyFunctions.{node.Method.Name}");
#endif

        List<ResolvedModel> arguments = node.Arguments.Select(visitor.ResolveExpression).ToList();
        SQLiteExpression[] args = arguments.Select(a => a.SQLiteExpression!).ToArray();
        SQLiteParameter[]? parameters = ParameterHelpers.CombineParameters(args);
        Type returnType = node.Method.ReturnType;
        int id = visitor.Counters.NextIdentifier();

        string sqlName = SqlFunctionNames[node.Method.Name];
        return SQLiteExpression.Variadic(returnType, id, $"{sqlName}(", args, ", ", ")", parameters);
    }
}
