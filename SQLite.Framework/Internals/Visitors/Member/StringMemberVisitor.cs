namespace SQLite.Framework.Internals.Visitors.Member;

internal static class StringMemberVisitor
{
    public static Expression HandleStringMethod(SQLiteCallerContext ctx)
    {
        SQLVisitor visitor = ctx.Visitor;
        MethodCallExpression node = (MethodCallExpression)ctx.Node;
        List<ResolvedModel> arguments = node.Arguments
            .Select(visitor.ResolveExpression)
            .ToList();

        if (node.Object != null)
        {
            ResolvedModel obj = visitor.ResolveExpression(node.Object);

            if (obj.SQLiteExpression == null || arguments.Any(f => f.SQLiteExpression == null) || node.Method.Name == nameof(string.ToString))
            {
                return Expression.Call(obj.Expression, node.Method, arguments.Select(f => f.Expression));
            }

            switch (node.Method.Name)
            {
                case nameof(string.Contains):
                {
                    return ResolveLike(visitor, node.Method, obj.SQLiteExpression, arguments, value => $"%{value}%", valueSql => $"'%'||{EscapeLikeValueSql(valueSql)}||'%'");
                }
                case nameof(string.StartsWith):
                {
                    return ResolveLike(visitor, node.Method, obj.SQLiteExpression, arguments, value => $"{value}%", valueSql => $"{EscapeLikeValueSql(valueSql)}||'%'");
                }
                case nameof(string.EndsWith):
                {
                    return ResolveLike(visitor, node.Method, obj.SQLiteExpression, arguments, value => $"%{value}", valueSql => $"'%'||{EscapeLikeValueSql(valueSql)}");
                }
                case nameof(string.IndexOf):
                {
                    if (arguments.Count == 1)
                    {
                        SQLiteExpression needle = CharArgAsText(visitor, arguments[0]);
                        SQLiteParameter[]? parameters = ParameterHelpers.CombineParameters(obj.SQLiteExpression, needle);
                        return SQLiteExpression.Binary(node.Method.ReturnType, visitor.Counters.NextIdentifier(), "INSTR(", obj.SQLiteExpression!, ", ", needle, ") - 1", parameters);
                    }

                    if (arguments.Count == 2 && node.Arguments[1].Type == typeof(int))
                    {
                        SQLiteExpression objExpr = obj.SQLiteExpression!;
                        SQLiteExpression needle = CharArgAsText(visitor, arguments[0]);
                        SQLiteExpression start = ClampStartIndex(visitor, arguments[1]);
                        return CommonHelpers.EvaluateOnce(visitor.Counters, node.Method.ReturnType, [objExpr, needle, start], a =>
                            SQLiteExpression.Multi(node.Method.ReturnType, visitor.Counters.NextIdentifier(),
                                ["CASE WHEN INSTR(SUBSTR(", ", ", " + 1), ", ") = 0 THEN -1 ELSE INSTR(SUBSTR(", ", ", " + 1), ", ") - 1 + ", " END"],
                                [a[0], a[2], a[1], a[0], a[2], a[1], a[2]],
                                null));
                    }

                    return visitor.NotTranslatable(node,
                        "string.IndexOf is only translatable with a single value argument or a value plus a start index. " +
                        "StringComparison and count overloads are not supported.");
                }
                case nameof(string.LastIndexOf):
                {
#if SQLITE_FRAMEWORK_VERSION_AWARE
                    if (!visitor.Database.Options.OverMinimumVersion(SQLiteMinimumVersion.V3_8_3))
                    {
                        return visitor.NotTranslatableBelowVersion(node, SQLiteMinimumVersion.V3_8_3, "string.LastIndexOf (uses WITH RECURSIVE CTE)");
                    }
#endif
                    SQLiteExpression objExpr = obj.SQLiteExpression!;
                    SQLiteExpression arg0 = CharArgAsText(visitor, arguments[0]);

                    if (arguments.Count == 1)
                    {
                        return CommonHelpers.EvaluateOnce(visitor.Counters, node.Method.ReturnType, [objExpr, arg0], a =>
                            SQLiteExpression.Multi(node.Method.ReturnType, visitor.Counters.NextIdentifier(),
                                ["CASE WHEN LENGTH(", ") = 0 THEN LENGTH(", ") ELSE COALESCE((WITH RECURSIVE find_pos(pos, rem) AS (SELECT 0, ", " UNION ALL SELECT pos + INSTR(rem, ", "), SUBSTR(rem, INSTR(rem, ", ") + 1) FROM find_pos WHERE INSTR(rem, ", ") > 0) SELECT MAX(pos) - 1 FROM find_pos WHERE pos > 0), -1) END"],
                                [a[1], a[0], a[0], a[1], a[1], a[1]],
                                null));
                    }

                    if (arguments.Count == 2 && node.Arguments[1].Type == typeof(int))
                    {
                        SQLiteExpression start = arguments[1].SQLiteExpression!;
                        return CommonHelpers.EvaluateOnce(visitor.Counters, node.Method.ReturnType, [objExpr, arg0, start], a =>
                            SQLiteExpression.Multi(node.Method.ReturnType, visitor.Counters.NextIdentifier(),
                                ["CASE WHEN LENGTH(", ") = 0 THEN LENGTH(SUBSTR(", ", 1, ", " + 1)) ELSE COALESCE((WITH RECURSIVE find_pos(pos, rem) AS (SELECT 0, SUBSTR(", ", 1, ", " + 1) UNION ALL SELECT pos + INSTR(rem, ", "), SUBSTR(rem, INSTR(rem, ", ") + 1) FROM find_pos WHERE INSTR(rem, ", ") > 0) SELECT MAX(pos) - 1 FROM find_pos WHERE pos > 0), -1) END"],
                                [a[1], a[0], a[2], a[0], a[2], a[1], a[1], a[1]],
                                null));
                    }

                    return visitor.NotTranslatable(node,
                        "string.LastIndexOf is only translatable with a single value argument or a value plus a start index. " +
                        "StringComparison and count overloads are not supported.");
                }
                case nameof(string.Insert):
                {
                    SQLiteExpression objExpr = obj.SQLiteExpression!;
                    SQLiteExpression arg0 = arguments[0].SQLiteExpression!;
                    SQLiteExpression arg1 = arguments[1].SQLiteExpression!;
                    return CommonHelpers.EvaluateOnce(visitor.Counters, node.Method.ReturnType, [objExpr, arg0, arg1], a =>
                        SQLiteExpression.Multi(node.Method.ReturnType, visitor.Counters.NextIdentifier(),
                            ["SUBSTR(", ", 1, ", ") || ", " || SUBSTR(", ", ", " + 1)"],
                            [a[0], a[1], a[2], a[0], a[1]],
                            null));
                }
                case nameof(string.Remove):
                {
                    SQLiteExpression objExpr = obj.SQLiteExpression!;
                    SQLiteExpression arg0 = arguments[0].SQLiteExpression!;
                    if (node.Arguments.Count == 2)
                    {
                        SQLiteExpression arg1 = arguments[1].SQLiteExpression!;
                        return CommonHelpers.EvaluateOnce(visitor.Counters, node.Method.ReturnType, [objExpr, arg0, arg1], a =>
                            SQLiteExpression.Multi(node.Method.ReturnType, visitor.Counters.NextIdentifier(),
                                ["SUBSTR(", ", 1, ", ") || SUBSTR(", ", ", " + ", " + 1)"],
                                [a[0], a[1], a[0], a[1], a[2]],
                                null));
                    }
                    else
                    {
                        SQLiteParameter[]? parameters = ParameterHelpers.CombineParameters(objExpr, arg0);
                        return SQLiteExpression.Binary(node.Method.ReturnType, visitor.Counters.NextIdentifier(), "SUBSTR(", objExpr, ", 1, ", arg0, ")", parameters);
                    }
                }
                case nameof(string.Replace):
                {
                    SQLiteExpression oldArg = CharArgAsText(visitor, arguments[0]);
                    SQLiteExpression newArg = CharArgAsText(visitor, arguments[1]);
                    if (node.Arguments[1].Type == typeof(string))
                    {
                        newArg = visitor.CoalesceNullableStringOperand(node.Arguments[1], arguments[1], newArg);
                    }

                    SQLiteParameter[]? parameters = ParameterHelpers.CombineParameters(obj.SQLiteExpression, oldArg, newArg);
                    return SQLiteExpression.Trinary(node.Method.ReturnType, visitor.Counters.NextIdentifier(), "REPLACE(", obj.SQLiteExpression!, ", ", oldArg, ", ", newArg, ")", parameters);
                }
                case nameof(string.Trim):
                {
                    return ResolveTrim(visitor, node, obj.SQLiteExpression, arguments, "TRIM");
                }
                case nameof(string.TrimStart):
                {
                    return ResolveTrim(visitor, node, obj.SQLiteExpression, arguments, "LTRIM");
                }
                case nameof(string.TrimEnd):
                {
                    return ResolveTrim(visitor, node, obj.SQLiteExpression, arguments, "RTRIM");
                }
                case "get_Chars":
                {
                    SQLiteParameter[]? parameters = ParameterHelpers.CombineParameters(obj.SQLiteExpression, arguments[0].SQLiteExpression!);
                    SQLiteExpression charExpr = SQLiteExpression.Binary(node.Method.ReturnType, visitor.Counters.NextIdentifier(), "SUBSTR(", obj.SQLiteExpression!, ", ", arguments[0].SQLiteExpression!, " + 1, 1)", parameters);
                    if (visitor.Database.Options.CharStorage == CharStorageMode.Integer)
                    {
                        charExpr = SQLiteExpression.Wrap(node.Method.ReturnType, visitor.Counters.NextIdentifier(), "UNICODE(", charExpr, ")", parameters);
                    }

                    return charExpr;
                }
                case nameof(string.CompareTo):
                {
                    SQLiteExpression objExpr = obj.SQLiteExpression!;
                    SQLiteExpression arg0 = arguments[0].SQLiteExpression!;
                    return CommonHelpers.EvaluateOnce(visitor.Counters, node.Method.ReturnType, [objExpr, arg0], a =>
                        SQLiteExpression.Multi(node.Method.ReturnType, visitor.Counters.NextIdentifier(),
                            ["(CASE WHEN ", " IS NULL AND ", " IS NULL THEN 0 WHEN ", " IS NULL THEN -1 WHEN ", " IS NULL THEN 1 WHEN ", " = ", " THEN 0 WHEN ", " < ", " THEN -1 ELSE 1 END)"],
                            [a[0], a[1], a[0], a[1], a[0], a[1], a[0], a[1]],
                            null));
                }
                case nameof(string.Equals):
                {
                    StringComparison comparison = (StringComparison)arguments[1].Constant!;
                    string collation = comparison switch
                    {
                        StringComparison.OrdinalIgnoreCase or
                            StringComparison.CurrentCultureIgnoreCase or
                            StringComparison.InvariantCultureIgnoreCase => " COLLATE NOCASE",
                        _ => ""
                    };

                    SQLiteParameter[]? parameters = ParameterHelpers.CombineParameters(obj.SQLiteExpression, arguments[0].SQLiteExpression!);
                    return SQLiteExpression.Binary(node.Method.ReturnType, visitor.Counters.NextIdentifier(), "(", obj.SQLiteExpression!, " IS ", arguments[0].SQLiteExpression!, $"{collation})", parameters);
                }
                case nameof(string.Substring):
                {
                    SQLiteExpression substringStart = ClampStartIndex(visitor, arguments[0]);
                    if (node.Arguments.Count == 2)
                    {
                        SQLiteParameter[]? parameters = ParameterHelpers.CombineParameters(obj.SQLiteExpression, substringStart, arguments[1].SQLiteExpression!);
                        return SQLiteExpression.Trinary(node.Method.ReturnType, visitor.Counters.NextIdentifier(), "SUBSTR(", obj.SQLiteExpression!, ", ", substringStart, " + 1, ", arguments[1].SQLiteExpression!, ")", parameters);
                    }
                    else
                    {
                        SQLiteParameter[]? parameters = ParameterHelpers.CombineParameters(obj.SQLiteExpression, substringStart);
                        return SQLiteExpression.Binary(node.Method.ReturnType, visitor.Counters.NextIdentifier(), "SUBSTR(", obj.SQLiteExpression!, ", ", substringStart, " + 1)", parameters);
                    }
                }
                case nameof(string.ToUpper) when node.Arguments.Count == 0:
                case nameof(string.ToUpperInvariant):
                {
                    return SQLiteExpression.Wrap(node.Method.ReturnType, visitor.Counters.NextIdentifier(), "UPPER(", obj.SQLiteExpression!, ")", obj.Parameters);
                }
                case nameof(string.ToLower) when node.Arguments.Count == 0:
                case nameof(string.ToLowerInvariant):
                {
                    return SQLiteExpression.Wrap(node.Method.ReturnType, visitor.Counters.NextIdentifier(), "LOWER(", obj.SQLiteExpression!, ")", obj.Parameters);
                }
                case nameof(string.PadLeft):
                {
                    SQLiteExpression objExpr = obj.SQLiteExpression!;
                    SQLiteExpression arg0 = arguments[0].SQLiteExpression!;
                    if (node.Arguments.Count == 1)
                    {
                        SQLiteParameter spaceParam = new()
                        {
                            Name = visitor.Counters.NextParamName(),
                            Value = " "
                        };

                        return CommonHelpers.EvaluateOnce(visitor.Counters, node.Method.ReturnType, [objExpr, arg0], a =>
                            SQLiteExpression.Multi(node.Method.ReturnType, visitor.Counters.NextIdentifier(),
                                ["(CASE WHEN LENGTH(", ") >= ", " THEN ", " ELSE (SELECT SUBSTR(REPLACE(HEX(ZEROBLOB(", " - LENGTH(", "))), '00', " + spaceParam.Name + "), 1, ", " - LENGTH(", ")) || ", ") END)"],
                                [a[0], a[1], a[0], a[1], a[0], a[1], a[0], a[0]],
                                [spaceParam]));
                    }
                    else
                    {
                        SQLiteExpression arg1 = CharArgAsText(visitor, arguments[1]);

                        return CommonHelpers.EvaluateOnce(visitor.Counters, node.Method.ReturnType, [objExpr, arg0, arg1], a =>
                            SQLiteExpression.Multi(node.Method.ReturnType, visitor.Counters.NextIdentifier(),
                                ["(CASE WHEN LENGTH(", ") >= ", " THEN ", " ELSE (SELECT SUBSTR(REPLACE(HEX(ZEROBLOB(", " - LENGTH(", "))), '00', ", "), 1, ", " - LENGTH(", ")) || ", ") END)"],
                                [a[0], a[1], a[0], a[1], a[0], a[2], a[1], a[0], a[0]],
                                null));
                    }
                }
                case nameof(string.PadRight):
                {
                    SQLiteExpression objExpr = obj.SQLiteExpression!;
                    SQLiteExpression arg0 = arguments[0].SQLiteExpression!;
                    if (node.Arguments.Count == 1)
                    {
                        SQLiteParameter spaceParam = new()
                        {
                            Name = visitor.Counters.NextParamName(),
                            Value = " "
                        };

                        return CommonHelpers.EvaluateOnce(visitor.Counters, node.Method.ReturnType, [objExpr, arg0], a =>
                            SQLiteExpression.Multi(node.Method.ReturnType, visitor.Counters.NextIdentifier(),
                                ["(CASE WHEN LENGTH(", ") >= ", " THEN ", " ELSE (", " || (SELECT SUBSTR(REPLACE(HEX(ZEROBLOB(", " - LENGTH(", "))), '00', " + spaceParam.Name + "), 1, ", " - LENGTH(", ")))) END)"],
                                [a[0], a[1], a[0], a[0], a[1], a[0], a[1], a[0]],
                                [spaceParam]));
                    }
                    else
                    {
                        SQLiteExpression arg1 = CharArgAsText(visitor, arguments[1]);

                        return CommonHelpers.EvaluateOnce(visitor.Counters, node.Method.ReturnType, [objExpr, arg0, arg1], a =>
                            SQLiteExpression.Multi(node.Method.ReturnType, visitor.Counters.NextIdentifier(),
                                ["(CASE WHEN LENGTH(", ") >= ", " THEN ", " ELSE (", " || (SELECT SUBSTR(REPLACE(HEX(ZEROBLOB(", " - LENGTH(", "))), '00', ", "), 1, ", " - LENGTH(", ")))) END)"],
                                [a[0], a[1], a[0], a[0], a[1], a[0], a[2], a[1], a[0]],
                                null));
                    }
                }
            }
        }
        else if (QueryableMemberVisitor.CheckConstantMethod<string>(visitor, node, arguments, out Expression? expression))
        {
            return expression;
        }

        switch (node.Method.Name)
        {
            case nameof(string.IsNullOrEmpty):
            {
                SQLiteExpression a = arguments[0].SQLiteExpression!;
                return SQLiteExpression.Wrap(node.Method.ReturnType, visitor.Counters.NextIdentifier(), "(COALESCE(", a, ", '') = '')", arguments[0].Parameters);
            }
            case nameof(string.IsNullOrWhiteSpace):
            {
                SQLiteExpression a = arguments[0].SQLiteExpression!;
                return SQLiteExpression.Wrap(node.Method.ReturnType, visitor.Counters.NextIdentifier(), "(TRIM(COALESCE(", a, $", ''), {Constants.WhitespaceChars}) = '')", arguments[0].Parameters);
            }
            case nameof(string.Concat):
            {
                SQLiteExpression[] concatArgs;
                if (node.Arguments is [NewArrayExpression concatArray])
                {
                    concatArgs = new SQLiteExpression[concatArray.Expressions.Count];
                    for (int i = 0; i < concatArray.Expressions.Count; i++)
                    {
                        ResolvedModel resolvedElement = visitor.ResolveExpression(concatArray.Expressions[i]);
                        concatArgs[i] = visitor.CoalesceNullableStringOperand(concatArray.Expressions[i], resolvedElement, resolvedElement.SQLiteExpression!);
                    }
                }
                else
                {
                    concatArgs = new SQLiteExpression[arguments.Count];
                    for (int i = 0; i < arguments.Count; i++)
                    {
                        concatArgs[i] = visitor.CoalesceNullableStringOperand(node.Arguments[i], arguments[i], arguments[i].SQLiteExpression!);
                    }
                }

                return SQLiteExpression.Variadic(node.Method.ReturnType, visitor.Counters.NextIdentifier(), "", concatArgs, " || ", "", ParameterHelpers.CombineParameters(concatArgs));
            }
            case nameof(string.Join):
                if (node.Arguments[1] is NewArrayExpression arrayExpr)
                {
                    SQLiteExpression sep = visitor.CoalesceNullableStringOperand(node.Arguments[0], arguments[0], arguments[0].SQLiteExpression!);
                    SQLiteExpression[] joinArgs = arrayExpr.Expressions
                        .Select(e =>
                        {
                            ResolvedModel resolvedElement = visitor.ResolveExpression(e);
                            return visitor.CoalesceNullableStringOperand(e, resolvedElement, resolvedElement.SQLiteExpression!);
                        })
                        .ToArray();
                    if (joinArgs.Length == 0)
                    {
                        return SQLiteExpression.Leaf(node.Method.ReturnType, visitor.Counters.NextIdentifier(), "''");
                    }
                    int childCount = joinArgs.Length * 2 - 1;
                    SQLiteExpression[] interleaved = new SQLiteExpression[childCount];
                    string[] partsArr = new string[childCount + 1];
                    partsArr[0] = "";
                    partsArr[childCount] = "";
                    for (int i = 0; i < joinArgs.Length; i++)
                    {
                        int idx = i * 2;
                        interleaved[idx] = joinArgs[i];
                        if (i > 0)
                        {
                            interleaved[idx - 1] = sep;
                            partsArr[idx - 1] = " || ";
                            partsArr[idx] = " || ";
                        }
                    }
                    return SQLiteExpression.Multi(node.Method.ReturnType, visitor.Counters.NextIdentifier(), partsArr, interleaved, ParameterHelpers.CombineParameters([sep, .. joinArgs]));
                }

                if (typeof(IQueryable).IsAssignableFrom(node.Arguments[1].Type))
                {
                    return RewriteJoinAsGroupConcat(visitor, node);
                }

                return visitor.NotTranslatable(node, "string.Join with a non-array source is not translatable to SQL.");
            case nameof(string.Compare):
            {
                if (node.Arguments[1].Type == typeof(int))
                {
                    if (arguments.Count is not (5 or 6))
                    {
                        return visitor.NotTranslatable(node,
                            "string.Compare with a CultureInfo is not translatable to SQL. " +
                            "Only the substring overloads with an optional ignoreCase or StringComparison are supported.");
                    }

                    SQLiteExpression strA = arguments[0].SQLiteExpression!;
                    SQLiteExpression indexA = arguments[1].SQLiteExpression!;
                    SQLiteExpression strB = arguments[2].SQLiteExpression!;
                    SQLiteExpression indexB = arguments[3].SQLiteExpression!;
                    SQLiteExpression length = arguments[4].SQLiteExpression!;

                    SQLiteExpression subA = SQLiteExpression.Multi(typeof(string), visitor.Counters.NextIdentifier(),
                        ["SUBSTR(", ", ", " + 1, ", ")"], [strA, indexA, length], ParameterHelpers.CombineParameters(strA, indexA, length));
                    SQLiteExpression subB = SQLiteExpression.Multi(typeof(string), visitor.Counters.NextIdentifier(),
                        ["SUBSTR(", ", ", " + 1, ", ")"], [strB, indexB, length], ParameterHelpers.CombineParameters(strB, indexB));

                    bool ignoreCaseSub = arguments.Count == 6 && IsCompareIgnoreCase(arguments[5].Constant);
                    return BuildCompare(visitor, node.Method.ReturnType, subA, subB, ignoreCaseSub);
                }

                SQLiteExpression a0 = arguments[0].SQLiteExpression!;
                SQLiteExpression a1 = arguments[1].SQLiteExpression!;
                bool ignoreCase = arguments.Skip(2).Any(arg => IsCompareIgnoreCase(arg.Constant));
                return BuildCompare(visitor, node.Method.ReturnType, a0, a1, ignoreCase);
            }
            case nameof(string.Equals):
                if (arguments.Count == 3 && arguments[2].Constant is StringComparison comparison)
                {
                    string collation = comparison switch
                    {
                        StringComparison.OrdinalIgnoreCase or
                            StringComparison.CurrentCultureIgnoreCase or
                            StringComparison.InvariantCultureIgnoreCase => " COLLATE NOCASE",
                        _ => ""
                    };

                    return SQLiteExpression.Binary(node.Method.ReturnType, visitor.Counters.NextIdentifier(), "(", arguments[0].SQLiteExpression!, " IS ", arguments[1].SQLiteExpression!, $"{collation})", ParameterHelpers.CombineParameters(arguments[0].SQLiteExpression!, arguments[1].SQLiteExpression!));
                }

                return SQLiteExpression.Binary(node.Method.ReturnType, visitor.Counters.NextIdentifier(), "(", arguments[0].SQLiteExpression!, " IS ", arguments[1].SQLiteExpression!, ")", ParameterHelpers.CombineParameters(arguments[0].SQLiteExpression!, arguments[1].SQLiteExpression!));
            default:
                return visitor.NotTranslatable(node, $"string.{node.Method.Name} is not translatable to SQL.");
        }
    }

    public static Expression HandleStringProperty(SQLVisitor visitor, string propertyName, Type type, SQLiteExpression node)
    {
        return propertyName switch
        {
            nameof(string.Length) => SQLiteExpression.Wrap(type, visitor.Counters.NextIdentifier(), "LENGTH(", node, ")", node.Parameters),
            _ => node
        };
    }

    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "The marker method is only used to build an Expression tree, it is never invoked.")]
    private static Expression RewriteJoinAsGroupConcat(SQLVisitor visitor, MethodCallExpression node)
    {
        Type elementType = node.Arguments[1].Type.GenericTypeArguments[0];

        MethodInfo openMarker = typeof(QueryableExtensions).GetMethod(
            nameof(QueryableExtensions.GroupConcatMarker),
            BindingFlags.Static | BindingFlags.NonPublic)!;
        MethodInfo closedMarker = openMarker.MakeGenericMethod(elementType);
        MethodCallExpression rewritten = Expression.Call(closedMarker, node.Arguments[1], node.Arguments[0]);

        SQLiteCallerContext ctx = new(visitor, rewritten);
        return QueryableMemberVisitor.HandleQueryableMethod(ctx);
    }

    private static SQLiteExpression ResolveLike(SQLVisitor visitor, MethodInfo method, SQLiteExpression obj, List<ResolvedModel> arguments, Func<object?, string> selectParameter, Func<SQLiteExpression, string> selectValue)
    {
        bool ignoreCase = false;
        bool explicitComparison = arguments.Count == 2;
        if (explicitComparison)
        {
            StringComparison comparison = (StringComparison)arguments[1].Constant!;
            ignoreCase = comparison is StringComparison.OrdinalIgnoreCase or StringComparison.CurrentCultureIgnoreCase or StringComparison.InvariantCultureIgnoreCase;
        }

        if (!ignoreCase && (visitor.Database.Options.CaseSensitiveStringComparison || explicitComparison))
        {
            return ResolveCaseSensitiveSearch(visitor, method, obj, arguments);
        }

        string rest = ignoreCase ? "ESCAPE '\\' COLLATE NOCASE" : "ESCAPE '\\'";

        if (arguments[0].IsConstant)
        {
            string pName = visitor.Counters.NextParamName();
            SQLiteParameter parameter = new()
            {
                Name = pName,
                Value = arguments[0].Constant is string likeText
                    ? selectParameter(EscapeLikePattern(likeText))
                    : selectParameter(EscapeLikePattern(((char)arguments[0].Constant!).ToString()))
            };

            SQLiteParameter[] parameters = obj.Parameters == null
                ? [parameter]
                : [.. obj.Parameters, parameter];

            return SQLiteExpression.Wrap(method.ReturnType, visitor.Counters.NextIdentifier(), "", obj, $" LIKE {pName} {rest}", parameters);
        }
        else
        {
            SQLiteExpression argExpr = CharArgAsText(visitor, arguments[0]);
            SQLiteParameter[]? parameters = ParameterHelpers.CombineParameters(obj, argExpr);
            string valueSql = selectValue(argExpr);

            return SQLiteExpression.Wrap(method.ReturnType, visitor.Counters.NextIdentifier(), "", obj, $" LIKE {valueSql} {rest}", parameters);
        }
    }

    private static SQLiteExpression ClampStartIndex(SQLVisitor visitor, ResolvedModel start)
    {
        SQLiteExpression startExpr = start.SQLiteExpression!;
        if (start.IsConstant && start.Constant is int value && value >= 0)
        {
            return startExpr;
        }

        return SQLiteExpression.Wrap(startExpr.Type, visitor.Counters.NextIdentifier(), "MAX(", startExpr, ", 0)", startExpr.Parameters);
    }

    private static SQLiteExpression CharArgAsText(SQLVisitor visitor, ResolvedModel arg)
    {
        if (arg is { IsConstant: true, Constant: char c, SQLiteExpression: { Parameters: [{ Name: { } name }] } expr })
        {
            return SQLiteExpression.Leaf(typeof(string), expr.Identifier, name, c.ToString());
        }

        SQLiteExpression sqlExpr = arg.SQLiteExpression!;
        if (sqlExpr.Type == typeof(char) && visitor.Database.Options.CharStorage == CharStorageMode.Integer)
        {
            return SQLiteExpression.Wrap(typeof(string), visitor.Counters.NextIdentifier(), "CHAR(", sqlExpr, ")", sqlExpr.Parameters);
        }

        return sqlExpr;
    }

    private static string EscapeLikePattern(string value)
    {
        return value
            .Replace("\\", "\\\\")
            .Replace("%", "\\%")
            .Replace("_", "\\_");
    }

    private static string EscapeLikeValueSql(SQLiteExpression value)
    {
        return $"REPLACE(REPLACE(REPLACE({value}, '\\', '\\\\'), '%', '\\%'), '_', '\\_')";
    }

    private static SQLiteExpression ResolveCaseSensitiveSearch(SQLVisitor visitor, MethodInfo method, SQLiteExpression obj, List<ResolvedModel> arguments)
    {
        SQLiteExpression valueExpr;
        if (arguments[0].IsConstant)
        {
            string pName = visitor.Counters.NextParamName();
            object? raw = arguments[0].Constant is char c ? c.ToString() : arguments[0].Constant;
            valueExpr = SQLiteExpression.Leaf(typeof(string), visitor.Counters.NextIdentifier(), pName, raw);
        }
        else
        {
            valueExpr = CharArgAsText(visitor, arguments[0]);
        }

        int id = visitor.Counters.NextIdentifier();

        return method.Name switch
        {
            nameof(string.Contains) => SQLiteExpression.Binary(method.ReturnType, id,
                "INSTR(", obj, ", ", valueExpr, ") > 0", ParameterHelpers.CombineParameters(obj, valueExpr)),
            nameof(string.StartsWith) => CommonHelpers.EvaluateOnce(visitor.Counters, method.ReturnType, [valueExpr], v =>
                SQLiteExpression.Multi(method.ReturnType, visitor.Counters.NextIdentifier(),
                    ["SUBSTR(", ", 1, LENGTH(", ")) = ", ""], [obj, v[0], v[0]], obj.Parameters)),
            _ => CommonHelpers.EvaluateOnce(visitor.Counters, method.ReturnType, [valueExpr], v =>
                SQLiteExpression.Multi(method.ReturnType, visitor.Counters.NextIdentifier(),
                    ["(LENGTH(", ") = 0 OR SUBSTR(", ", -LENGTH(", ")) = ", ")"], [v[0], obj, v[0], v[0]], obj.Parameters))
        };
    }

    private static Expression ResolveTrim(SQLVisitor visitor, MethodCallExpression node, SQLiteExpression obj, List<ResolvedModel> arguments, string trimType)
    {
        if (arguments.Count == 0)
        {
            return SQLiteExpression.Wrap(node.Method.ReturnType, visitor.Counters.NextIdentifier(), $"{trimType}(", obj, $", {Constants.WhitespaceChars})", obj.Parameters);
        }

        if (node.Arguments[0] is NewArrayExpression expression)
        {
            bool isEmptyTrimSet = expression.Expressions.Count == 0
                || (expression.NodeType == ExpressionType.NewArrayBounds
                    && expression.Expressions.Count == 1
                    && ExpressionHelpers.IsConstant(expression.Expressions[0])
                    && Equals(ExpressionHelpers.GetConstantValue(expression.Expressions[0]), 0));

            if (isEmptyTrimSet)
            {
                return SQLiteExpression.Wrap(node.Method.ReturnType, visitor.Counters.NextIdentifier(), $"{trimType}(", obj, $", {Constants.WhitespaceChars})", obj.Parameters);
            }

            if (expression.NodeType == ExpressionType.NewArrayBounds)
            {
                return SQLiteExpression.Wrap(node.Method.ReturnType, visitor.Counters.NextIdentifier(), $"{trimType}(", obj, ", CHAR(0))", obj.Parameters);
            }

            ResolvedModel[] args = expression.Expressions
                .Select(visitor.ResolveExpression)
                .ToArray();

            if (args.Any(f => f.SQLiteExpression == null))
            {
                return Expression.Call(obj, node.Method, arguments.Select(f => f.Expression));
            }

            SQLiteExpression[] argExprs = args.Select(f => CharArgAsText(visitor, f)).ToArray();

            SQLiteParameter[]? parameters = ParameterHelpers.CombineParameters([obj, .. argExprs]);

            int childCount = 1 + argExprs.Length;
            SQLiteExpression[] children = new SQLiteExpression[childCount];
            string[] parts = new string[childCount + 1];
            children[0] = obj;
            parts[0] = $"{trimType}(";
            parts[1] = ", ";
            for (int i = 0; i < argExprs.Length; i++)
            {
                children[i + 1] = argExprs[i];
                parts[i + 2] = i == argExprs.Length - 1 ? ")" : " || ";
            }

            return SQLiteExpression.Multi(node.Method.ReturnType, visitor.Counters.NextIdentifier(), parts, children, parameters);
        }
        else if (node.Arguments[0].Type == typeof(char[]))
        {
            char[]? chars = (char[]?)arguments[0].Constant;
            if (chars == null || chars.Length == 0)
            {
                return SQLiteExpression.Wrap(node.Method.ReturnType, visitor.Counters.NextIdentifier(), $"{trimType}(", obj, $", {Constants.WhitespaceChars})", obj.Parameters);
            }

            SQLiteParameter parameter = new()
            {
                Name = visitor.Counters.NextParamName(),
                Value = new string(chars)
            };

            SQLiteParameter[] parameters = obj.Parameters == null
                ? [parameter]
                : [.. obj.Parameters, parameter];

            return SQLiteExpression.Wrap(node.Method.ReturnType, visitor.Counters.NextIdentifier(), $"{trimType}(", obj, $", {parameter.Name})", parameters);
        }
        else
        {
            SQLiteExpression argExpr = CharArgAsText(visitor, arguments[0]);
            SQLiteParameter[]? parameters = ParameterHelpers.CombineParameters(obj, argExpr);
            return SQLiteExpression.Binary(node.Method.ReturnType, visitor.Counters.NextIdentifier(), $"{trimType}(", obj, ", ", argExpr, ")", parameters);
        }
    }

    private static SQLiteExpression BuildCompare(SQLVisitor visitor, Type returnType, SQLiteExpression a, SQLiteExpression b, bool ignoreCase)
    {
        return CommonHelpers.EvaluateOnce(visitor.Counters, returnType, [a, b], v =>
        {
            SQLiteExpression va = v[0];
            SQLiteExpression vb = v[1];
            SQLiteExpression cmpA = ignoreCase ? SQLiteExpression.Wrap(va.Type, visitor.Counters.NextIdentifier(), "UPPER(", va, ")", null) : va;
            SQLiteExpression cmpB = ignoreCase ? SQLiteExpression.Wrap(vb.Type, visitor.Counters.NextIdentifier(), "UPPER(", vb, ")", null) : vb;
            return SQLiteExpression.Multi(returnType, visitor.Counters.NextIdentifier(),
                ["(CASE WHEN ", " IS NULL AND ", " IS NULL THEN 0 WHEN ", " IS NULL THEN -1 WHEN ", " IS NULL THEN 1 WHEN ", " = ", " THEN 0 WHEN ", " < ", " THEN -1 ELSE 1 END)"],
                [va, vb, va, vb, cmpA, cmpB, cmpA, cmpB],
                null);
        });
    }

    private static bool IsCompareIgnoreCase(object? constant)
    {
        return (constant is StringComparison c
                && c is StringComparison.OrdinalIgnoreCase
                    or StringComparison.CurrentCultureIgnoreCase
                    or StringComparison.InvariantCultureIgnoreCase)
            || (constant is bool b && b)
            || (constant is CompareOptions options && options.HasFlag(CompareOptions.IgnoreCase));
    }
}
