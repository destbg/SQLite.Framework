namespace SQLite.Framework.Internals.Visitors;

internal static class SQLiteFunctionsMemberVisitor
{
    public static Expression HandleSQLiteFunctionsMethod(SQLiteCallerContext ctx)
    {

        SQLVisitor visitor = ctx.Visitor;
        MethodCallExpression node = (MethodCallExpression)ctx.Node;
        return node.Method.Name switch
        {
            nameof(SQLiteFunctions.Random) => new SQLiteExpression(typeof(double), visitor.Counters.IdentifierIndex++, "RANDOM()", null),
            nameof(SQLiteFunctions.RandomBlob) => HandleFunctionsRandomBlob(visitor, node),
            nameof(SQLiteFunctions.Glob) => HandleFunctionsGlob(visitor, node),
            nameof(SQLiteFunctions.UnixEpoch) => HandleFunctionsUnixEpoch(visitor, node),
            nameof(SQLiteFunctions.Printf) => HandleFunctionsPrintf(visitor, node),
            nameof(SQLiteFunctions.Regexp) => HandleFunctionsRegexp(visitor, node),
            nameof(SQLiteFunctions.Between) => HandleFunctionsBetween(visitor, node),
            nameof(SQLiteFunctions.In) => HandleFunctionsIn(visitor, node),
            nameof(SQLiteFunctions.Coalesce) => HandleFunctionsVariadic(visitor, node, "coalesce", node.Method.ReturnType),
            nameof(SQLiteFunctions.Nullif) => HandleFunctionsNullif(visitor, node),
            nameof(SQLiteFunctions.Typeof) => HandleFunctionsUnaryFn(visitor, node, "typeof", typeof(string)),
            nameof(SQLiteFunctions.Hex) => HandleFunctionsUnaryFn(visitor, node, "hex", typeof(string)),
            nameof(SQLiteFunctions.Quote) => HandleFunctionsUnaryFn(visitor, node, "quote", typeof(string)),
            nameof(SQLiteFunctions.Zeroblob) => HandleFunctionsUnaryFn(visitor, node, "zeroblob", typeof(byte[])),
            nameof(SQLiteFunctions.Instr) => HandleFunctionsInstr(visitor, node),
            nameof(SQLiteFunctions.LastInsertRowId) => new SQLiteExpression(typeof(long), visitor.Counters.IdentifierIndex++, "last_insert_rowid()", null),
            nameof(SQLiteFunctions.SqliteVersion) => new SQLiteExpression(typeof(string), visitor.Counters.IdentifierIndex++, "sqlite_version()", null),
            nameof(SQLiteFunctions.Min) => HandleFunctionsVariadic(visitor, node, "min", node.Method.ReturnType),
            nameof(SQLiteFunctions.Max) => HandleFunctionsVariadic(visitor, node, "max", node.Method.ReturnType),
            nameof(SQLiteFunctions.Changes) => new SQLiteExpression(typeof(long), visitor.Counters.IdentifierIndex++, "changes()", null),
            nameof(SQLiteFunctions.TotalChanges) => new SQLiteExpression(typeof(long), visitor.Counters.IdentifierIndex++, "total_changes()", null),
            _ => throw new NotSupportedException($"SQLiteFunctions.{node.Method.Name} is not translatable to SQL."),
        };
    }

    private static SQLiteExpression HandleFunctionsRandomBlob(SQLVisitor visitor, MethodCallExpression node)
    {
        ResolvedModel arg = visitor.ResolveExpression(node.Arguments[0]);
        return new SQLiteExpression(
            typeof(byte[]),
            visitor.Counters.IdentifierIndex++,
            $"RANDOMBLOB({arg.Sql})",
            arg.Parameters);
    }

    private static SQLiteExpression HandleFunctionsGlob(SQLVisitor visitor, MethodCallExpression node)
    {
        ResolvedModel pattern = visitor.ResolveExpression(node.Arguments[0]);
        ResolvedModel value = visitor.ResolveExpression(node.Arguments[1]);
        return new SQLiteExpression(
            typeof(bool),
            visitor.Counters.IdentifierIndex++,
            $"({value.Sql} GLOB {pattern.Sql})",
            ParameterHelpers.CombineParameters(value.SQLiteExpression!, pattern.SQLiteExpression!));
    }

    private static SQLiteExpression HandleFunctionsBetween(SQLVisitor visitor, MethodCallExpression node)
    {
        ResolvedModel value = visitor.ResolveExpression(node.Arguments[0]);
        ResolvedModel low = visitor.ResolveExpression(node.Arguments[1]);
        ResolvedModel high = visitor.ResolveExpression(node.Arguments[2]);
        return new SQLiteExpression(
            typeof(bool),
            visitor.Counters.IdentifierIndex++,
            $"({value.Sql} BETWEEN {low.Sql} AND {high.Sql})",
            ParameterHelpers.CombineParameters(value.SQLiteExpression!, low.SQLiteExpression!, high.SQLiteExpression!));
    }

    private static SQLiteExpression HandleFunctionsIn(SQLVisitor visitor, MethodCallExpression node)
    {
        ResolvedModel value = visitor.ResolveExpression(node.Arguments[0]);
        List<ResolvedModel> items = ResolveVariadic(visitor, node.Arguments[1]);

        string itemsSql = string.Join(", ", items.Select(r => r.Sql));
        SQLiteExpression[] parts = [value.SQLiteExpression!, .. items.Select(r => r.SQLiteExpression!)];
        return new SQLiteExpression(
            typeof(bool),
            visitor.Counters.IdentifierIndex++,
            $"({value.Sql} IN ({itemsSql}))",
            ParameterHelpers.CombineParameters(parts));
    }

    private static SQLiteExpression HandleFunctionsVariadic(SQLVisitor visitor, MethodCallExpression node, string sqlFunction, Type returnType)
    {
        List<ResolvedModel> items = ResolveVariadic(visitor, node.Arguments[0]);
        string argsSql = string.Join(", ", items.Select(r => r.Sql));
        SQLiteExpression[] parts = items.Select(r => r.SQLiteExpression!).ToArray();
        return new SQLiteExpression(
            returnType,
            visitor.Counters.IdentifierIndex++,
            $"{sqlFunction}({argsSql})",
            ParameterHelpers.CombineParameters(parts));
    }

    private static SQLiteExpression HandleFunctionsNullif(SQLVisitor visitor, MethodCallExpression node)
    {
        ResolvedModel a = visitor.ResolveExpression(node.Arguments[0]);
        ResolvedModel b = visitor.ResolveExpression(node.Arguments[1]);
        return new SQLiteExpression(
            node.Method.ReturnType,
            visitor.Counters.IdentifierIndex++,
            $"nullif({a.Sql}, {b.Sql})",
            ParameterHelpers.CombineParameters(a.SQLiteExpression!, b.SQLiteExpression!));
    }

    private static SQLiteExpression HandleFunctionsUnaryFn(SQLVisitor visitor, MethodCallExpression node, string sqlFunction, Type returnType)
    {
        ResolvedModel arg = visitor.ResolveExpression(node.Arguments[0]);
        return new SQLiteExpression(
            returnType,
            visitor.Counters.IdentifierIndex++,
            $"{sqlFunction}({arg.Sql})",
            arg.Parameters);
    }

    private static SQLiteExpression HandleFunctionsInstr(SQLVisitor visitor, MethodCallExpression node)
    {
        ResolvedModel haystack = visitor.ResolveExpression(node.Arguments[0]);
        ResolvedModel needle = visitor.ResolveExpression(node.Arguments[1]);
        return new SQLiteExpression(
            typeof(int),
            visitor.Counters.IdentifierIndex++,
            $"instr({haystack.Sql}, {needle.Sql})",
            ParameterHelpers.CombineParameters(haystack.SQLiteExpression!, needle.SQLiteExpression!));
    }

    private static List<ResolvedModel> ResolveVariadic(SQLVisitor visitor, Expression argument)
    {
        List<ResolvedModel> resolved = [];
        if (argument is NewArrayExpression arrayExpr)
        {
            foreach (Expression e in arrayExpr.Expressions)
            {
                resolved.Add(visitor.ResolveExpression(e));
            }
        }
        else
        {
            Array array = (Array)ExpressionHelpers.GetConstantValue(argument)!;
            Type elementType = argument.Type.GetElementType()!;
            foreach (object? item in array)
            {
                resolved.Add(visitor.ResolveExpression(Expression.Constant(item, elementType)));
            }
        }
        return resolved;
    }

    private static SQLiteExpression HandleFunctionsUnixEpoch(SQLVisitor visitor, MethodCallExpression node)
    {
        if (node.Arguments.Count == 0)
        {
            return new SQLiteExpression(typeof(long), visitor.Counters.IdentifierIndex++, "unixepoch()", null);
        }

        ResolvedModel arg = visitor.ResolveExpression(node.Arguments[0]);
        return new SQLiteExpression(
            typeof(long),
            visitor.Counters.IdentifierIndex++,
            $"unixepoch({arg.Sql})",
            arg.Parameters);
    }

    private static SQLiteExpression HandleFunctionsPrintf(SQLVisitor visitor, MethodCallExpression node)
    {
        ResolvedModel format = visitor.ResolveExpression(node.Arguments[0]);

        List<ResolvedModel> rest = [];
        if (node.Arguments[1] is NewArrayExpression arrayExpr)
        {
            foreach (Expression e in arrayExpr.Expressions)
            {
                rest.Add(visitor.ResolveExpression(e));
            }
        }

        string argsSql = rest.Count == 0
            ? string.Empty
            : ", " + string.Join(", ", rest.Select(r => r.Sql));

        SQLiteExpression[] all = [format.SQLiteExpression!, .. rest.Select(r => r.SQLiteExpression!)];
        return new SQLiteExpression(
            typeof(string),
            visitor.Counters.IdentifierIndex++,
            $"printf({format.Sql}{argsSql})",
            ParameterHelpers.CombineParameters(all));
    }

    private static SQLiteExpression HandleFunctionsRegexp(SQLVisitor visitor, MethodCallExpression node)
    {
        ResolvedModel value = visitor.ResolveExpression(node.Arguments[0]);
        ResolvedModel pattern = visitor.ResolveExpression(node.Arguments[1]);
        return new SQLiteExpression(
            typeof(bool),
            visitor.Counters.IdentifierIndex++,
            $"({value.Sql} REGEXP {pattern.Sql})",
            ParameterHelpers.CombineParameters(value.SQLiteExpression!, pattern.SQLiteExpression!));
    }
}
