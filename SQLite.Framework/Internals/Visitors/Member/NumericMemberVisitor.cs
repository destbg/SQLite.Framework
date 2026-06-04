namespace SQLite.Framework.Internals.Visitors.Member;

internal static class NumericMemberVisitor
{
    public static Expression HandleIntegerMethod(SQLiteCallerContext ctx)
    {

        SQLVisitor visitor = ctx.Visitor;
        MethodCallExpression node = (MethodCallExpression)ctx.Node;
        List<ResolvedModel> arguments = node.Arguments
            .Select(visitor.ResolveExpression)
            .ToList();

        if (node.Object != null)
        {
            ResolvedModel obj = visitor.ResolveExpression(node.Object);

            if (obj.SQLiteExpression == null || arguments.Any(f => f.SQLiteExpression == null))
            {
                return Expression.Call(obj.Expression, node.Method, arguments.Select(f => f.Expression));
            }

            if (node.Method.Name == nameof(int.ToString))
            {
                if (node.Arguments.Count > 0)
                {
                    return visitor.NotTranslatable(node, $"{node.Method.DeclaringType!.Name}.ToString with a format string is not translatable to SQL.");
                }

                Type objType = node.Object!.Type;
                if (objType == typeof(ulong))
                {
                    return SQLiteExpression.Wrap(node.Method.ReturnType, visitor.Counters.NextIdentifier(), "printf('%llu', ", obj.SQLiteExpression!, ")", obj.Parameters);
                }

                return SQLiteExpression.Wrap(node.Method.ReturnType, visitor.Counters.NextIdentifier(), "CAST(", obj.SQLiteExpression!, " AS TEXT)", obj.Parameters);
            }
        }

        if (QueryableMemberVisitor.CheckConstantMethod<long>(visitor, node, arguments, out Expression? expression))
        {
            return expression;
        }

        if (node.Method.Name == "Parse")
        {
            return SQLiteExpression.Wrap(node.Method.ReturnType, visitor.Counters.NextIdentifier(), "CAST(", arguments[0].SQLiteExpression!, " AS INTEGER)", arguments[0].Parameters);
        }

        return visitor.NotTranslatable(node, $"{node.Method.DeclaringType!.Name}.{node.Method.Name} is not translatable to SQL.");
    }

    public static Expression HandleFloatingPointMethod(SQLiteCallerContext ctx)
    {

        SQLVisitor visitor = ctx.Visitor;
        MethodCallExpression node = (MethodCallExpression)ctx.Node;
        List<ResolvedModel> arguments = node.Arguments
            .Select(visitor.ResolveExpression)
            .ToList();

        if (node.Object != null)
        {
            ResolvedModel obj = visitor.ResolveExpression(node.Object);

            if (obj.SQLiteExpression == null || arguments.Any(f => f.SQLiteExpression == null))
            {
                return Expression.Call(obj.Expression, node.Method, arguments.Select(f => f.Expression));
            }

            if (node.Method.Name == nameof(double.ToString))
            {
                if (node.Arguments.Count > 0)
                {
                    return visitor.NotTranslatable(node, $"{node.Method.DeclaringType!.Name}.ToString with a format string is not translatable to SQL.");
                }

                return SQLiteExpression.Wrap(node.Method.ReturnType, visitor.Counters.NextIdentifier(), "CAST(", obj.SQLiteExpression!, " AS TEXT)", obj.Parameters);
            }
        }

        if (QueryableMemberVisitor.CheckConstantMethod<double>(visitor, node, arguments, out Expression? expression))
        {
            return expression;
        }

        if (node.Method.Name == "Parse")
        {
            return SQLiteExpression.Wrap(node.Method.ReturnType, visitor.Counters.NextIdentifier(), "CAST(", arguments[0].SQLiteExpression!, " AS REAL)", arguments[0].Parameters);
        }

        if (node.Method.Name == nameof(double.DegreesToRadians))
        {
#if SQLITE_FRAMEWORK_VERSION_AWARE
            if (!visitor.Database.Options.OverMinimumVersion(SQLiteMinimumVersion.V3_35))
            {
                return visitor.NotTranslatableBelowVersion(node, SQLiteMinimumVersion.V3_35, $"{node.Method.DeclaringType!.Name}.DegreesToRadians");
            }
#endif
            return SQLiteExpression.Wrap(node.Method.ReturnType, visitor.Counters.NextIdentifier(), "RADIANS(", arguments[0].SQLiteExpression!, ")", arguments[0].Parameters);
        }

        if (node.Method.Name == nameof(double.RadiansToDegrees))
        {
#if SQLITE_FRAMEWORK_VERSION_AWARE
            if (!visitor.Database.Options.OverMinimumVersion(SQLiteMinimumVersion.V3_35))
            {
                return visitor.NotTranslatableBelowVersion(node, SQLiteMinimumVersion.V3_35, $"{node.Method.DeclaringType!.Name}.RadiansToDegrees");
            }
#endif
            return SQLiteExpression.Wrap(node.Method.ReturnType, visitor.Counters.NextIdentifier(), "DEGREES(", arguments[0].SQLiteExpression!, ")", arguments[0].Parameters);
        }

        return visitor.NotTranslatable(node, $"{node.Method.DeclaringType!.Name}.{node.Method.Name} is not translatable to SQL.");
    }
}
