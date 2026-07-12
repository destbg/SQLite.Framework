namespace SQLite.Framework.Internals.Visitors.Member;

internal static class SQLiteFunctionsMemberVisitor
{
    public static Expression HandleSQLiteFunctionsMethod(SQLiteCallerContext ctx)
    {

        SQLVisitor visitor = ctx.Visitor;
        MethodCallExpression node = (MethodCallExpression)ctx.Node;
        return node.Method.Name switch
        {
            nameof(SQLiteFunctions.Random) => SQLiteExpression.Leaf(typeof(double), visitor.Counters.NextIdentifier(), "RANDOM()", null),
            nameof(SQLiteFunctions.RandomBlob) => HandleFunctionsRandomBlob(visitor, node),
            nameof(SQLiteFunctions.Glob) => HandleFunctionsGlob(visitor, node),
            nameof(SQLiteFunctions.UnixEpoch) => HandleFunctionsUnixEpoch(visitor, node),
            nameof(SQLiteFunctions.Printf) => HandleFunctionsPrintf(visitor, node),
            nameof(SQLiteFunctions.Regexp) => HandleFunctionsRegexp(visitor, node),
            nameof(SQLiteFunctions.Between) => HandleFunctionsBetween(visitor, node),
            nameof(SQLiteFunctions.In) => HandleFunctionsIn(visitor, node),
            nameof(SQLiteFunctions.Coalesce) => HandleFunctionsVariadic(visitor, node, "coalesce", node.Method.ReturnType),
            nameof(SQLiteFunctions.Nullif) => HandleFunctionsNullif(visitor, node),
            nameof(SQLiteFunctions.DistinctFrom) => HandleFunctionsDistinctFrom(visitor, node),
            nameof(SQLiteFunctions.Iif) => HandleFunctionsIif(visitor, node),
            nameof(SQLiteFunctions.Typeof) => HandleFunctionsUnaryFn(visitor, node, "typeof", typeof(string)),
            nameof(SQLiteFunctions.Hex) => HandleFunctionsUnaryFn(visitor, node, "hex", typeof(string)),
            nameof(SQLiteFunctions.Unhex) => HandleFunctionsUnhex(visitor, node),
            nameof(SQLiteFunctions.Format) => HandleFunctionsFormat(visitor, node),
            nameof(SQLiteFunctions.Unicode) => HandleFunctionsUnaryFn(visitor, node, "unicode", typeof(int)),
            nameof(SQLiteFunctions.Char) => HandleFunctionsChar(visitor, node),
            nameof(SQLiteFunctions.Quote) => HandleFunctionsUnaryFn(visitor, node, "quote", typeof(string)),
            nameof(SQLiteFunctions.Zeroblob) => HandleFunctionsUnaryFn(visitor, node, "zeroblob", typeof(byte[])),
            nameof(SQLiteFunctions.Instr) => HandleFunctionsInstr(visitor, node),
            nameof(SQLiteFunctions.LastInsertRowId) => SQLiteExpression.Leaf(typeof(long), visitor.Counters.NextIdentifier(), "last_insert_rowid()", null),
            nameof(SQLiteFunctions.SqliteVersion) => SQLiteExpression.Leaf(typeof(string), visitor.Counters.NextIdentifier(), "sqlite_version()", null),
            nameof(SQLiteFunctions.Min) => HandleFunctionsVariadic(visitor, node, "min", node.Method.ReturnType),
            nameof(SQLiteFunctions.Max) => HandleFunctionsVariadic(visitor, node, "max", node.Method.ReturnType),
            nameof(SQLiteFunctions.Total) => HandleFunctionsTotal(visitor, node),
            nameof(SQLiteFunctions.Changes) => SQLiteExpression.Leaf(typeof(long), visitor.Counters.NextIdentifier(), "changes()", null),
            nameof(SQLiteFunctions.TotalChanges) => SQLiteExpression.Leaf(typeof(long), visitor.Counters.NextIdentifier(), "total_changes()", null),
            nameof(SQLiteFunctions.Collate) => HandleFunctionsCollate(visitor, node),
            _ => throw new NotSupportedException($"SQLiteFunctions.{node.Method.Name} is not translatable to SQL."),
        };
    }

    private static SQLiteExpression HandleFunctionsRandomBlob(SQLVisitor visitor, MethodCallExpression node)
    {
        ResolvedModel arg = visitor.ResolveExpression(node.Arguments[0]);
        return SQLiteExpression.Wrap(typeof(byte[]), visitor.Counters.NextIdentifier(), "RANDOMBLOB(", arg.SQLiteExpression!, ")", arg.Parameters);
    }

    private static SQLiteExpression HandleFunctionsGlob(SQLVisitor visitor, MethodCallExpression node)
    {
        ResolvedModel pattern = visitor.ResolveExpression(node.Arguments[0]);
        ResolvedModel value = visitor.ResolveExpression(node.Arguments[1]);
        return SQLiteExpression.Binary(typeof(bool), visitor.Counters.NextIdentifier(), "(", value.SQLiteExpression!, " GLOB ", pattern.SQLiteExpression!, ")", ParameterHelpers.CombineParameters(value.SQLiteExpression!, pattern.SQLiteExpression!));
    }

    private static SQLiteExpression HandleFunctionsBetween(SQLVisitor visitor, MethodCallExpression node)
    {
        ResolvedModel value = visitor.ResolveExpression(node.Arguments[0]);
        ResolvedModel low = visitor.ResolveExpression(node.Arguments[1]);
        ResolvedModel high = visitor.ResolveExpression(node.Arguments[2]);
        SQLiteExpression[] operands = [value.SQLiteExpression!, low.SQLiteExpression!, high.SQLiteExpression!];
        CoerceDayOfWeekOperands(visitor, [node.Arguments[0], node.Arguments[1], node.Arguments[2]], operands);
        SQLiteExpression valueExpr = MathMemberVisitor.CastTextDecimal(visitor, operands[0]);
        SQLiteExpression lowExpr = MathMemberVisitor.CastTextDecimal(visitor, operands[1]);
        SQLiteExpression highExpr = MathMemberVisitor.CastTextDecimal(visitor, operands[2]);

        if (TypeHelpers.UnsignedIntegerKey(valueExpr.Type) == typeof(ulong))
        {
            SQLiteExpression ge = visitor.BuildUnsignedComparison(ExpressionType.GreaterThanOrEqual, valueExpr, lowExpr, mayBeNull: false, ParameterHelpers.CombineParameters(valueExpr, lowExpr));
            SQLiteExpression le = visitor.BuildUnsignedComparison(ExpressionType.LessThanOrEqual, valueExpr, highExpr, mayBeNull: false, ParameterHelpers.CombineParameters(valueExpr, highExpr));
            return SQLiteExpression.Binary(typeof(bool), visitor.Counters.NextIdentifier(), "(", ge, " AND ", le, ")", ParameterHelpers.CombineParameters(ge, le));
        }

        return SQLiteExpression.Trinary(typeof(bool), visitor.Counters.NextIdentifier(), "(", valueExpr, " BETWEEN ", lowExpr, " AND ", highExpr, ")", ParameterHelpers.CombineParameters(valueExpr, lowExpr, highExpr));
    }

    private static SQLiteExpression HandleFunctionsIn(SQLVisitor visitor, MethodCallExpression node)
    {
        ResolvedModel value = visitor.ResolveExpression(node.Arguments[0]);
        List<ResolvedModel> items = ResolveVariadic(visitor, node.Arguments[1], out List<Expression> itemNodes);

        SQLiteExpression[] parts = [value.SQLiteExpression!, .. items.Select(r => r.SQLiteExpression!)];
        CoerceDayOfWeekOperands(visitor, [node.Arguments[0], .. itemNodes], parts);
        SQLiteExpression valueExpr = parts[0];
        SQLiteExpression[] itemExprs = parts[1..];
        if (itemExprs.Length == 0)
        {
            return SQLiteExpression.Wrap(typeof(bool), visitor.Counters.NextIdentifier(), "(", valueExpr, " IN ())", ParameterHelpers.CombineParameters(parts));
        }
        SQLiteExpression[] children = new SQLiteExpression[1 + itemExprs.Length];
        children[0] = valueExpr;
        string[] partsArr = new string[children.Length + 1];
        partsArr[0] = "(";
        partsArr[1] = " IN (";
        for (int i = 0; i < itemExprs.Length; i++)
        {
            children[i + 1] = itemExprs[i];
            partsArr[i + 2] = i == itemExprs.Length - 1 ? "))" : ", ";
        }
        return SQLiteExpression.Multi(typeof(bool), visitor.Counters.NextIdentifier(), partsArr, children, ParameterHelpers.CombineParameters(parts));
    }

    private static SQLiteExpression HandleFunctionsVariadic(SQLVisitor visitor, MethodCallExpression node, string sqlFunction, Type returnType)
    {
        List<ResolvedModel> items = ResolveVariadic(visitor, node.Arguments[0], out List<Expression> itemNodes);
        bool ordered = sqlFunction is "min" or "max";
        SQLiteExpression[] itemExprs = items.Select(r => r.SQLiteExpression!).ToArray();
        bool dayOfWeek = CoerceDayOfWeekOperands(visitor, [.. itemNodes], itemExprs);
        if (ordered)
        {
            for (int i = 0; i < itemExprs.Length; i++)
            {
                itemExprs[i] = MathMemberVisitor.CastTextDecimal(visitor, itemExprs[i]);
            }
        }

        if (ordered && TypeHelpers.UnsignedIntegerKey(returnType) == typeof(ulong))
        {
            SQLiteExpression result = itemExprs[0];
            for (int i = 1; i < itemExprs.Length; i++)
            {
                result = MathMemberVisitor.BuildUnsignedMinMax(visitor, sqlFunction == "max", returnType, result, itemExprs[i], ParameterHelpers.CombineParameters(result, itemExprs[i]));
            }

            return result;
        }

        SQLiteExpression variadic = SQLiteExpression.Variadic(returnType, visitor.Counters.NextIdentifier(), $"{sqlFunction}(", itemExprs, ", ", ")", ParameterHelpers.CombineParameters(itemExprs));
        return dayOfWeek ? variadic.WithDayOfWeekInteger() : variadic;
    }

    private static SQLiteExpression HandleFunctionsTotal(SQLVisitor visitor, MethodCallExpression node)
    {
        const string BadShape = "SQLiteFunctions.Total expects a Select projection over a grouping, for example `g.Select(x => x.Price)`.";

        if (node.Arguments[0] is not MethodCallExpression selectCall)
        {
            throw new NotSupportedException(BadShape);
        }

        if (selectCall.Method.Name != nameof(Enumerable.Select))
        {
            throw new NotSupportedException(BadShape);
        }

        Expression receiver = selectCall.Arguments[0];
        LambdaExpression? filterLambda = QueryableMemberVisitor.TryPeelWhereFilter(ref receiver);

        if (receiver is not ParameterExpression)
        {
            throw new NotSupportedException(BadShape);
        }

        Dictionary<string, Expression> newTableColumns = QueryableMemberVisitor.BuildGroupingColumnMap(visitor, receiver);

        SQLiteExpression? filterExpression = null;
        if (filterLambda != null)
        {
            visitor.MethodArguments[filterLambda.Parameters[0]] = newTableColumns;
            Expression resolvedFilter = visitor.Visit(filterLambda.Body);
            if (resolvedFilter is not SQLiteExpression sqlFilter)
            {
                throw new NotSupportedException("Aggregate FILTER predicate could not be resolved.");
            }
            filterExpression = sqlFilter;
#if SQLITE_FRAMEWORK_VERSION_AWARE
            visitor.Database.Options.EnsureMinimumVersion(SQLiteMinimumVersion.V3_30, "FILTER (WHERE ...) on aggregates");
#endif
        }

        LambdaExpression lambda = (LambdaExpression)ExpressionHelpers.StripQuotes(selectCall.Arguments[1]);
        if (lambda.Parameters.Count != 1)
        {
            throw new NotSupportedException(BadShape);
        }
        visitor.MethodArguments[lambda.Parameters[0]] = newTableColumns;

        Expression resolved = visitor.Visit(lambda.Body);
        if (resolved is not SQLiteExpression sql)
        {
            throw new NotSupportedException("SQLiteFunctions.Total could not resolve the projected expression.");
        }

        if (filterExpression == null)
        {
            return SQLiteExpression.Wrap(typeof(double), visitor.Counters.NextIdentifier(), "total(", sql, ")", sql.Parameters);
        }

        return SQLiteExpression.Binary(
            typeof(double),
            visitor.Counters.NextIdentifier(),
            "total(",
            sql,
            ") FILTER (WHERE ",
            filterExpression,
            ")",
            ParameterHelpers.CombineParameters(sql, filterExpression));
    }

    private static SQLiteExpression HandleFunctionsNullif(SQLVisitor visitor, MethodCallExpression node)
    {
        ResolvedModel a = visitor.ResolveExpression(node.Arguments[0]);
        ResolvedModel b = visitor.ResolveExpression(node.Arguments[1]);
        SQLiteExpression[] operands = [a.SQLiteExpression!, b.SQLiteExpression!];
        bool dayOfWeek = CoerceDayOfWeekOperands(visitor, [node.Arguments[0], node.Arguments[1]], operands);
        SQLiteExpression aExpr = MathMemberVisitor.CastTextDecimal(visitor, operands[0]);
        SQLiteExpression bExpr = MathMemberVisitor.CastTextDecimal(visitor, operands[1]);
        SQLiteExpression result = SQLiteExpression.Binary(node.Method.ReturnType, visitor.Counters.NextIdentifier(), "nullif(", aExpr, ", ", bExpr, ")", ParameterHelpers.CombineParameters(aExpr, bExpr));
        return dayOfWeek ? result.WithDayOfWeekInteger() : result;
    }

    private static SQLiteExpression HandleFunctionsDistinctFrom(SQLVisitor visitor, MethodCallExpression node)
    {
#if SQLITE_FRAMEWORK_VERSION_AWARE
        visitor.Database.Options.EnsureMinimumVersion(SQLiteMinimumVersion.V3_39, "SQLiteFunctions.DistinctFrom");
#endif
        ResolvedModel a = visitor.ResolveExpression(node.Arguments[0]);
        ResolvedModel b = visitor.ResolveExpression(node.Arguments[1]);
        SQLiteExpression[] operands = [a.SQLiteExpression!, b.SQLiteExpression!];
        CoerceDayOfWeekOperands(visitor, [node.Arguments[0], node.Arguments[1]], operands);
        return SQLiteExpression.Binary(typeof(bool), visitor.Counters.NextIdentifier(), "(", operands[0], " IS DISTINCT FROM ", operands[1], ")", ParameterHelpers.CombineParameters(operands[0], operands[1]));
    }

    private static SQLiteExpression HandleFunctionsIif(SQLVisitor visitor, MethodCallExpression node)
    {
#if SQLITE_FRAMEWORK_VERSION_AWARE
        visitor.Database.Options.EnsureMinimumVersion(SQLiteMinimumVersion.V3_32, "SQLiteFunctions.Iif");
#endif
        ResolvedModel condition = visitor.ResolveExpression(node.Arguments[0]);
        ResolvedModel whenTrue = visitor.ResolveExpression(node.Arguments[1]);
        ResolvedModel whenFalse = visitor.ResolveExpression(node.Arguments[2]);
        SQLiteExpression[] branches = [whenTrue.SQLiteExpression!, whenFalse.SQLiteExpression!];
        bool dayOfWeek = CoerceDayOfWeekOperands(visitor, [node.Arguments[1], node.Arguments[2]], branches);
        SQLiteExpression result = SQLiteExpression.Trinary(node.Method.ReturnType, visitor.Counters.NextIdentifier(), "iif(", condition.SQLiteExpression!, ", ", branches[0], ", ", branches[1], ")", ParameterHelpers.CombineParameters(condition.SQLiteExpression!, branches[0], branches[1]));
        return dayOfWeek ? result.WithDayOfWeekInteger() : result;
    }

    private static SQLiteExpression HandleFunctionsUnaryFn(SQLVisitor visitor, MethodCallExpression node, string sqlFunction, Type returnType)
    {
        ResolvedModel arg = visitor.ResolveExpression(node.Arguments[0]);
        return SQLiteExpression.Wrap(returnType, visitor.Counters.NextIdentifier(), $"{sqlFunction}(", arg.SQLiteExpression!, ")", arg.Parameters);
    }

    private static SQLiteExpression HandleFunctionsInstr(SQLVisitor visitor, MethodCallExpression node)
    {
        ResolvedModel haystack = visitor.ResolveExpression(node.Arguments[0]);
        ResolvedModel needle = visitor.ResolveExpression(node.Arguments[1]);
        return SQLiteExpression.Binary(typeof(int), visitor.Counters.NextIdentifier(), "instr(", haystack.SQLiteExpression!, ", ", needle.SQLiteExpression!, ")", ParameterHelpers.CombineParameters(haystack.SQLiteExpression!, needle.SQLiteExpression!));
    }

    private static List<ResolvedModel> ResolveVariadic(SQLVisitor visitor, Expression argument, out List<Expression> nodes)
    {
        nodes = [];
        if (argument is NewArrayExpression arrayExpr)
        {
            nodes.AddRange(arrayExpr.Expressions);
        }
        else
        {
            Array array = (Array)ExpressionHelpers.GetConstantValue(argument)!;
            Type elementType = argument.Type.GetElementType()!;
            foreach (object? item in array)
            {
                nodes.Add(Expression.Constant(item, elementType));
            }
        }

        List<ResolvedModel> resolved = [];
        foreach (Expression e in nodes)
        {
            resolved.Add(visitor.ResolveExpression(e));
        }
        return resolved;
    }

    private static bool CoerceDayOfWeekOperands(SQLVisitor visitor, Expression[] nodes, SQLiteExpression[] operands)
    {
        SQLiteExpression? flagged = null;
        foreach (SQLiteExpression operand in operands)
        {
            if (operand.IsDayOfWeekInteger)
            {
                flagged = operand;
                break;
            }
        }

        if (flagged == null)
        {
            return false;
        }

        for (int i = 0; i < operands.Length; i++)
        {
            operands[i] = visitor.CoerceDayOfWeekOperand(nodes[i], operands[i], flagged);
        }

        return true;
    }

    private static SQLiteExpression HandleFunctionsUnixEpoch(SQLVisitor visitor, MethodCallExpression node)
    {
#if SQLITE_FRAMEWORK_VERSION_AWARE
        visitor.Database.Options.EnsureMinimumVersion(SQLiteMinimumVersion.V3_38, "SQLiteFunctions.UnixEpoch");
#endif
        if (node.Arguments.Count == 0)
        {
            return SQLiteExpression.Leaf(typeof(long), visitor.Counters.NextIdentifier(), "unixepoch()", null);
        }

        ResolvedModel arg = visitor.ResolveExpression(node.Arguments[0]);
        return SQLiteExpression.Wrap(typeof(long), visitor.Counters.NextIdentifier(), "unixepoch(", arg.SQLiteExpression!, ")", arg.Parameters);
    }

    private static SQLiteExpression HandleFunctionsPrintf(SQLVisitor visitor, MethodCallExpression node)
    {
#if SQLITE_FRAMEWORK_VERSION_AWARE
        visitor.Database.Options.EnsureMinimumVersion(SQLiteMinimumVersion.V3_8_3, "SQLiteFunctions.Printf");
#endif
        return HandleFunctionsFormatLike(visitor, node, "printf");
    }

    private static SQLiteExpression HandleFunctionsFormat(SQLVisitor visitor, MethodCallExpression node)
    {
#if SQLITE_FRAMEWORK_VERSION_AWARE
        visitor.Database.Options.EnsureMinimumVersion(SQLiteMinimumVersion.V3_38, "SQLiteFunctions.Format");
#endif
        return HandleFunctionsFormatLike(visitor, node, "format");
    }

    private static SQLiteExpression HandleFunctionsFormatLike(SQLVisitor visitor, MethodCallExpression node, string sqlFunction)
    {
        ResolvedModel format = visitor.ResolveExpression(node.Arguments[0]);

        List<ResolvedModel> rest = ResolveVariadic(visitor, node.Arguments[1], out _);

        SQLiteExpression formatExpr = format.SQLiteExpression!;
        SQLiteExpression[] restExprs = rest.Select(r => r.SQLiteExpression!).ToArray();
        SQLiteExpression[] all = [formatExpr, .. restExprs];
        return SQLiteExpression.Variadic(typeof(string), visitor.Counters.NextIdentifier(), $"{sqlFunction}(", all, ", ", ")", ParameterHelpers.CombineParameters(all));
    }

    private static SQLiteExpression HandleFunctionsUnhex(SQLVisitor visitor, MethodCallExpression node)
    {
#if SQLITE_FRAMEWORK_VERSION_AWARE
        visitor.Database.Options.EnsureMinimumVersion(SQLiteMinimumVersion.V3_41, "SQLiteFunctions.Unhex");
#endif
        ResolvedModel value = visitor.ResolveExpression(node.Arguments[0]);
        if (node.Arguments.Count == 1)
        {
            return SQLiteExpression.Wrap(typeof(byte[]), visitor.Counters.NextIdentifier(), "unhex(", value.SQLiteExpression!, ")", value.Parameters);
        }
        ResolvedModel ignoreChars = visitor.ResolveExpression(node.Arguments[1]);
        return SQLiteExpression.Binary(typeof(byte[]), visitor.Counters.NextIdentifier(), "unhex(", value.SQLiteExpression!, ", ", ignoreChars.SQLiteExpression!, ")", ParameterHelpers.CombineParameters(value.SQLiteExpression!, ignoreChars.SQLiteExpression!));
    }

    private static SQLiteExpression HandleFunctionsChar(SQLVisitor visitor, MethodCallExpression node)
    {
        List<ResolvedModel> items = ResolveVariadic(visitor, node.Arguments[0], out _);
        SQLiteExpression[] itemExprs = items.Select(r => r.SQLiteExpression!).ToArray();
        return SQLiteExpression.Variadic(typeof(string), visitor.Counters.NextIdentifier(), "char(", itemExprs, ", ", ")", ParameterHelpers.CombineParameters(itemExprs));
    }

    private static SQLiteExpression HandleFunctionsRegexp(SQLVisitor visitor, MethodCallExpression node)
    {
        ResolvedModel value = visitor.ResolveExpression(node.Arguments[0]);
        ResolvedModel pattern = visitor.ResolveExpression(node.Arguments[1]);
        return SQLiteExpression.Binary(typeof(bool), visitor.Counters.NextIdentifier(), "(", value.SQLiteExpression!, " REGEXP ", pattern.SQLiteExpression!, ")", ParameterHelpers.CombineParameters(value.SQLiteExpression!, pattern.SQLiteExpression!));
    }

    private static SQLiteExpression HandleFunctionsCollate(SQLVisitor visitor, MethodCallExpression node)
    {
        ResolvedModel value = visitor.ResolveExpression(node.Arguments[0]);
        SQLiteCollation collation = (SQLiteCollation)visitor.ResolveExpression(node.Arguments[1]).Constant!;
        string clause = CommonHelpers.Clause(collation);
        if (clause.Length == 0)
        {
            return value.SQLiteExpression!;
        }
        return SQLiteExpression.Wrap(typeof(string), visitor.Counters.NextIdentifier(), "(", value.SQLiteExpression!, clause + ")", value.Parameters);
    }
}
