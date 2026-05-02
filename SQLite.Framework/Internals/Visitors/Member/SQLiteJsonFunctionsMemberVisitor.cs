namespace SQLite.Framework.Internals.Visitors.Member;

internal static class SQLiteJsonFunctionsMemberVisitor
{
    public static Expression HandleSQLiteJsonFunctionsMethod(SQLiteCallerContext ctx)
    {

        SQLVisitor visitor = ctx.Visitor;
        MethodCallExpression node = (MethodCallExpression)ctx.Node;
        List<ResolvedModel> arguments = node.Arguments.Select(visitor.ResolveExpression).ToList();
        SQLiteParameter[]? parameters = ParameterHelpers.CombineParameters(arguments
            .Select(a => a.SQLiteExpression)
            .Where(s => s != null)
            .Cast<SQLiteExpression>()
            .ToArray());

        string sql = node.Method.Name switch
        {
            nameof(SQLiteJsonFunctions.Extract) => $"json_extract({arguments[0].Sql}, {arguments[1].Sql})",
            nameof(SQLiteJsonFunctions.Set) => $"json_set({arguments[0].Sql}, {arguments[1].Sql}, {arguments[2].Sql})",
            nameof(SQLiteJsonFunctions.Insert) => $"json_insert({arguments[0].Sql}, {arguments[1].Sql}, {arguments[2].Sql})",
            nameof(SQLiteJsonFunctions.Replace) => $"json_replace({arguments[0].Sql}, {arguments[1].Sql}, {arguments[2].Sql})",
            nameof(SQLiteJsonFunctions.Remove) => $"json_remove({arguments[0].Sql}, {arguments[1].Sql})",
            nameof(SQLiteJsonFunctions.Type) => $"json_type({arguments[0].Sql}, {arguments[1].Sql})",
            nameof(SQLiteJsonFunctions.Valid) => $"json_valid({arguments[0].Sql})",
            nameof(SQLiteJsonFunctions.Patch) => $"json_patch({arguments[0].Sql}, {arguments[1].Sql})",
            nameof(SQLiteJsonFunctions.ArrayLength) when arguments.Count == 1 => $"json_array_length({arguments[0].Sql})",
            nameof(SQLiteJsonFunctions.ArrayLength) => $"json_array_length({arguments[0].Sql}, {arguments[1].Sql})",
            nameof(SQLiteJsonFunctions.Minify) => $"json({arguments[0].Sql})",
            nameof(SQLiteJsonFunctions.ToJsonb) => $"jsonb({arguments[0].Sql})",
            nameof(SQLiteJsonFunctions.ExtractJsonb) => $"jsonb_extract({arguments[0].Sql}, {arguments[1].Sql})",
            _ => throw new NotSupportedException($"SQLiteJsonFunctions.{node.Method.Name} is not translatable to SQL."),
        };

        return new SQLiteExpression(node.Type, visitor.Counters.IdentifierIndex++, sql, parameters);
    }
}
