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

        string methodName = node.Method.Name;
#if SQLITE_FRAMEWORK_OS_BUNDLED_SQLITE
        if (methodName is nameof(SQLiteJsonFunctions.ToJsonb) or nameof(SQLiteJsonFunctions.ExtractJsonb))
        {
            visitor.Database.Options.EnsureMinimumVersion(SQLiteMinimumVersion.V3_45, $"SQLiteJsonFunctions.{methodName}");
        }
        else
        {
            visitor.Database.Options.EnsureMinimumVersion(SQLiteMinimumVersion.V3_9, $"SQLiteJsonFunctions.{methodName}");
        }
#endif

        return methodName switch
        {
            nameof(SQLiteJsonFunctions.Extract) => Fn(visitor, node.Type, "json_extract", arguments[0], arguments[1], parameters),
            nameof(SQLiteJsonFunctions.Set) => Fn(visitor, node.Type, "json_set", arguments[0], arguments[1], arguments[2], parameters),
            nameof(SQLiteJsonFunctions.Insert) => Fn(visitor, node.Type, "json_insert", arguments[0], arguments[1], arguments[2], parameters),
            nameof(SQLiteJsonFunctions.Replace) => Fn(visitor, node.Type, "json_replace", arguments[0], arguments[1], arguments[2], parameters),
            nameof(SQLiteJsonFunctions.Remove) => Fn(visitor, node.Type, "json_remove", arguments[0], arguments[1], parameters),
            nameof(SQLiteJsonFunctions.Type) => Fn(visitor, node.Type, "json_type", arguments[0], arguments[1], parameters),
            nameof(SQLiteJsonFunctions.Valid) => Fn(visitor, node.Type, "json_valid", arguments[0], parameters),
            nameof(SQLiteJsonFunctions.Patch) => Fn(visitor, node.Type, "json_patch", arguments[0], arguments[1], parameters),
            nameof(SQLiteJsonFunctions.ArrayLength) when arguments.Count == 1 => Fn(visitor, node.Type, "json_array_length", arguments[0], parameters),
            nameof(SQLiteJsonFunctions.ArrayLength) => Fn(visitor, node.Type, "json_array_length", arguments[0], arguments[1], parameters),
            nameof(SQLiteJsonFunctions.Minify) => Fn(visitor, node.Type, "json", arguments[0], parameters),
            nameof(SQLiteJsonFunctions.ToJsonb) => Fn(visitor, node.Type, "jsonb", arguments[0], parameters),
            nameof(SQLiteJsonFunctions.ExtractJsonb) => Fn(visitor, node.Type, "jsonb_extract", arguments[0], arguments[1], parameters),
            _ => throw new NotSupportedException($"SQLiteJsonFunctions.{methodName} is not translatable to SQL."),
        };
    }

    private static SQLiteExpression Fn(SQLVisitor visitor, Type type, string fn, ResolvedModel a, SQLiteParameter[]? parameters)
    {
        return SQLiteExpression.Wrap(type, visitor.Counters.NextIdentifier(), $"{fn}(", a.SQLiteExpression!, ")", parameters);
    }

    private static SQLiteExpression Fn(SQLVisitor visitor, Type type, string fn, ResolvedModel a, ResolvedModel b, SQLiteParameter[]? parameters)
    {
        return SQLiteExpression.Binary(type, visitor.Counters.NextIdentifier(), $"{fn}(", a.SQLiteExpression!, ", ", b.SQLiteExpression!, ")", parameters);
    }

    private static SQLiteExpression Fn(SQLVisitor visitor, Type type, string fn, ResolvedModel a, ResolvedModel b, ResolvedModel c, SQLiteParameter[]? parameters)
    {
        return SQLiteExpression.Trinary(type, visitor.Counters.NextIdentifier(), $"{fn}(", a.SQLiteExpression!, ", ", b.SQLiteExpression!, ", ", c.SQLiteExpression!, ")", parameters);
    }
}
