namespace SQLite.Framework.Internals.Visitors.Member;

internal static class SQLiteDateFunctionsMemberVisitor
{
    public static Expression HandleSQLiteDateFunctionsMethod(SQLiteCallerContext ctx)
    {
        SQLVisitor visitor = ctx.Visitor;
        MethodCallExpression node = (MethodCallExpression)ctx.Node;
        return node.Method.Name switch
        {
            nameof(SQLiteDateFunctions.Date) => HandleTimeFunction(visitor, node, "date", typeof(string)),
            nameof(SQLiteDateFunctions.Time) => HandleTimeFunction(visitor, node, "time", typeof(string)),
            nameof(SQLiteDateFunctions.Datetime) => HandleTimeFunction(visitor, node, "datetime", typeof(string)),
            nameof(SQLiteDateFunctions.JulianDay) => HandleTimeFunction(visitor, node, "julianday", typeof(double)),
            nameof(SQLiteDateFunctions.Strftime) => HandleStrftime(visitor, node),
            nameof(SQLiteDateFunctions.Timediff) => HandleTimediff(visitor, node),
            _ => throw new NotSupportedException($"SQLiteDateFunctions.{node.Method.Name} is not translatable to SQL."),
        };
    }

    private static SQLiteExpression HandleTimeFunction(SQLVisitor visitor, MethodCallExpression node, string sqlFunction, Type returnType)
    {
        if (node.Arguments.Count == 0)
        {
            return SQLiteExpression.Leaf(returnType, visitor.Counters.NextIdentifier(), $"{sqlFunction}()", null);
        }

        ResolvedModel when = visitor.ResolveExpression(node.Arguments[0]);
        List<ResolvedModel> modifiers = ResolveModifiers(visitor, node.Arguments[1]);

        SQLiteExpression[] all = [when.SQLiteExpression!, .. modifiers.Select(r => r.SQLiteExpression!)];
        return SQLiteExpression.Variadic(returnType, visitor.Counters.NextIdentifier(), $"{sqlFunction}(", all, ", ", ")", ParameterHelpers.CombineParameters(all));
    }

    private static SQLiteExpression HandleStrftime(SQLVisitor visitor, MethodCallExpression node)
    {
        ResolvedModel format = visitor.ResolveExpression(node.Arguments[0]);
        ResolvedModel when = visitor.ResolveExpression(node.Arguments[1]);
        List<ResolvedModel> modifiers = ResolveModifiers(visitor, node.Arguments[2]);

        SQLiteExpression[] all = [format.SQLiteExpression!, when.SQLiteExpression!, .. modifiers.Select(r => r.SQLiteExpression!)];
        return SQLiteExpression.Variadic(typeof(string), visitor.Counters.NextIdentifier(), "strftime(", all, ", ", ")", ParameterHelpers.CombineParameters(all));
    }

    private static SQLiteExpression HandleTimediff(SQLVisitor visitor, MethodCallExpression node)
    {
#if SQLITE_FRAMEWORK_VERSION_AWARE
        visitor.Database.Options.EnsureMinimumVersion(SQLiteMinimumVersion.V3_43, "SQLiteDateFunctions.Timediff");
#endif
        ResolvedModel when1 = visitor.ResolveExpression(node.Arguments[0]);
        ResolvedModel when2 = visitor.ResolveExpression(node.Arguments[1]);
        return SQLiteExpression.Binary(typeof(string), visitor.Counters.NextIdentifier(), "timediff(", when1.SQLiteExpression!, ", ", when2.SQLiteExpression!, ")", ParameterHelpers.CombineParameters(when1.SQLiteExpression!, when2.SQLiteExpression!));
    }

    private static List<ResolvedModel> ResolveModifiers(SQLVisitor visitor, Expression argument)
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
}
