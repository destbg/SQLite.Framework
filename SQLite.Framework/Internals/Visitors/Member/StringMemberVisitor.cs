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
                    return ResolveLike(visitor, node.Method, obj.SQLiteExpression, arguments, value => $"%{value}%", valueSql => $"'%'||{valueSql}||'%'");
                }
                case nameof(string.StartsWith):
                {
                    return ResolveLike(visitor, node.Method, obj.SQLiteExpression, arguments, value => $"{value}%", valueSql => $"{valueSql}||'%'");
                }
                case nameof(string.EndsWith):
                {
                    return ResolveLike(visitor, node.Method, obj.SQLiteExpression, arguments, value => $"%{value}", valueSql => $"'%'||{valueSql}");
                }
                case nameof(string.IndexOf):
                {
                    SQLiteParameter[]? parameters = ParameterHelpers.CombineParameters(obj.SQLiteExpression, arguments[0].SQLiteExpression!);
                    return SQLiteExpression.Binary(node.Method.ReturnType, visitor.Counters.NextIdentifier(), "INSTR(", obj.SQLiteExpression!, ", ", arguments[0].SQLiteExpression!, ") - 1", parameters);
                }
                case nameof(string.LastIndexOf):
                {
#if SQLITE_FRAMEWORK_VERSION_AWARE
                    visitor.Database.Options.EnsureMinimumVersion(SQLiteMinimumVersion.V3_8_3, "string.LastIndexOf (uses WITH RECURSIVE CTE)");
#endif
                    SQLiteExpression objExpr = obj.SQLiteExpression!;
                    SQLiteExpression arg0 = arguments[0].SQLiteExpression!;
                    SQLiteParameter[]? parameters = ParameterHelpers.CombineParameters(objExpr, arg0);
                    return SQLiteExpression.Multi(node.Method.ReturnType, visitor.Counters.NextIdentifier(),
                        ["CASE WHEN LENGTH(", ") = 0 THEN LENGTH(", ") ELSE COALESCE((WITH RECURSIVE find_pos(pos, rem) AS (SELECT 0, ", " UNION ALL SELECT pos + INSTR(rem, ", "), SUBSTR(rem, INSTR(rem, ", ") + 1) FROM find_pos WHERE INSTR(rem, ", ") > 0) SELECT MAX(pos) - 1 FROM find_pos WHERE pos > 0), -1) END"],
                        [arg0, objExpr, objExpr, arg0, arg0, arg0],
                        parameters);
                }
                case nameof(string.Insert):
                {
                    SQLiteExpression objExpr = obj.SQLiteExpression!;
                    SQLiteExpression arg0 = arguments[0].SQLiteExpression!;
                    SQLiteExpression arg1 = arguments[1].SQLiteExpression!;
                    SQLiteParameter[]? parameters = ParameterHelpers.CombineParameters(objExpr, arg0, arg1);
                    return SQLiteExpression.Multi(node.Method.ReturnType, visitor.Counters.NextIdentifier(),
                        ["SUBSTR(", ", 1, ", ") || ", " || SUBSTR(", ", ", " + 1)"],
                        [objExpr, arg0, arg1, objExpr, arg0],
                        parameters);
                }
                case nameof(string.Remove):
                {
                    SQLiteExpression objExpr = obj.SQLiteExpression!;
                    SQLiteExpression arg0 = arguments[0].SQLiteExpression!;
                    if (node.Arguments.Count == 2)
                    {
                        SQLiteExpression arg1 = arguments[1].SQLiteExpression!;
                        SQLiteParameter[]? parameters = ParameterHelpers.CombineParameters(objExpr, arg0, arg1);
                        return SQLiteExpression.Multi(node.Method.ReturnType, visitor.Counters.NextIdentifier(),
                            ["SUBSTR(", ", 1, ", ") || SUBSTR(", ", ", " + ", " + 1)"],
                            [objExpr, arg0, objExpr, arg0, arg1],
                            parameters);
                    }
                    else
                    {
                        SQLiteParameter[]? parameters = ParameterHelpers.CombineParameters(objExpr, arg0);
                        return SQLiteExpression.Binary(node.Method.ReturnType, visitor.Counters.NextIdentifier(), "SUBSTR(", objExpr, ", 1, ", arg0, ")", parameters);
                    }
                }
                case nameof(string.Replace):
                {
                    SQLiteParameter[]? parameters = ParameterHelpers.CombineParameters(obj.SQLiteExpression, arguments[0].SQLiteExpression!, arguments[1].SQLiteExpression!);
                    return SQLiteExpression.Trinary(node.Method.ReturnType, visitor.Counters.NextIdentifier(), "REPLACE(", obj.SQLiteExpression!, ", ", arguments[0].SQLiteExpression!, ", ", arguments[1].SQLiteExpression!, ")", parameters);
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
                    return SQLiteExpression.Binary(node.Method.ReturnType, visitor.Counters.NextIdentifier(), "SUBSTR(", obj.SQLiteExpression!, ", ", arguments[0].SQLiteExpression!, " + 1, 1)", parameters);
                }
                case nameof(string.CompareTo):
                {
                    SQLiteExpression objExpr = obj.SQLiteExpression!;
                    SQLiteExpression arg0 = arguments[0].SQLiteExpression!;
                    SQLiteParameter[]? parameters = ParameterHelpers.CombineParameters(objExpr, arg0);
                    return SQLiteExpression.Multi(node.Method.ReturnType, visitor.Counters.NextIdentifier(),
                        ["(CASE WHEN ", " = ", " THEN 0 WHEN ", " < ", " THEN -1 ELSE 1 END)"],
                        [objExpr, arg0, objExpr, arg0],
                        parameters);
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
                    return SQLiteExpression.Binary(node.Method.ReturnType, visitor.Counters.NextIdentifier(), "(", obj.SQLiteExpression!, " = ", arguments[0].SQLiteExpression!, $"{collation})", parameters);
                }
                case nameof(string.Substring):
                {
                    if (node.Arguments.Count == 2)
                    {
                        SQLiteParameter[]? parameters = ParameterHelpers.CombineParameters(obj.SQLiteExpression, arguments[0].SQLiteExpression!, arguments[1].SQLiteExpression!);
                        return SQLiteExpression.Trinary(node.Method.ReturnType, visitor.Counters.NextIdentifier(), "SUBSTR(", obj.SQLiteExpression!, ", ", arguments[0].SQLiteExpression!, " + 1, ", arguments[1].SQLiteExpression!, ")", parameters);
                    }
                    else
                    {
                        SQLiteParameter[]? parameters = ParameterHelpers.CombineParameters(obj.SQLiteExpression, arguments[0].SQLiteExpression!);
                        return SQLiteExpression.Binary(node.Method.ReturnType, visitor.Counters.NextIdentifier(), "SUBSTR(", obj.SQLiteExpression!, ", ", arguments[0].SQLiteExpression!, " + 1)", parameters);
                    }
                }
                case nameof(string.ToUpper):
                case nameof(string.ToUpperInvariant):
                {
                    return SQLiteExpression.Wrap(node.Method.ReturnType, visitor.Counters.NextIdentifier(), "UPPER(", obj.SQLiteExpression!, ")", obj.Parameters);
                }
                case nameof(string.ToLower):
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
                            Value = ' '
                        };
                        SQLiteParameter[]? parameters = ParameterHelpers.CombineParameters(objExpr, arg0);
                        parameters = [.. parameters ?? [], spaceParam];

                        return SQLiteExpression.Multi(node.Method.ReturnType, visitor.Counters.NextIdentifier(),
                            ["(CASE WHEN LENGTH(", ") >= ", " THEN ", " ELSE (SELECT SUBSTR(REPLACE(HEX(ZEROBLOB(", " - LENGTH(", "))), '00', " + spaceParam.Name + "), 1, ", " - LENGTH(", ")) || ", ") END)"],
                            [objExpr, arg0, objExpr, arg0, objExpr, arg0, objExpr, objExpr],
                            parameters);
                    }
                    else
                    {
                        SQLiteExpression arg1 = arguments[1].SQLiteExpression!;
                        SQLiteParameter[]? parameters = ParameterHelpers.CombineParameters(objExpr, arg0, arg1);

                        return SQLiteExpression.Multi(node.Method.ReturnType, visitor.Counters.NextIdentifier(),
                            ["(CASE WHEN LENGTH(", ") >= ", " THEN ", " ELSE (SELECT SUBSTR(REPLACE(HEX(ZEROBLOB(", " - LENGTH(", "))), '00', ", "), 1, ", " - LENGTH(", ")) || ", ") END)"],
                            [objExpr, arg0, objExpr, arg0, objExpr, arg1, arg0, objExpr, objExpr],
                            parameters);
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
                            Value = ' '
                        };
                        SQLiteParameter[]? parameters = ParameterHelpers.CombineParameters(objExpr, arg0);
                        parameters = [.. parameters ?? [], spaceParam];

                        return SQLiteExpression.Multi(node.Method.ReturnType, visitor.Counters.NextIdentifier(),
                            ["(CASE WHEN LENGTH(", ") >= ", " THEN ", " ELSE (", " || (SELECT SUBSTR(REPLACE(HEX(ZEROBLOB(", " - LENGTH(", "))), '00', " + spaceParam.Name + "), 1, ", " - LENGTH(", ")))) END)"],
                            [objExpr, arg0, objExpr, objExpr, arg0, objExpr, arg0, objExpr],
                            parameters);
                    }
                    else
                    {
                        SQLiteExpression arg1 = arguments[1].SQLiteExpression!;
                        SQLiteParameter[]? parameters = ParameterHelpers.CombineParameters(objExpr, arg0, arg1);

                        return SQLiteExpression.Multi(node.Method.ReturnType, visitor.Counters.NextIdentifier(),
                            ["(CASE WHEN LENGTH(", ") >= ", " THEN ", " ELSE (", " || (SELECT SUBSTR(REPLACE(HEX(ZEROBLOB(", " - LENGTH(", "))), '00', ", "), 1, ", " - LENGTH(", ")))) END)"],
                            [objExpr, arg0, objExpr, objExpr, arg0, objExpr, arg1, arg0, objExpr],
                            parameters);
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
                return SQLiteExpression.Binary(node.Method.ReturnType, visitor.Counters.NextIdentifier(), "(", a, " IS NULL OR ", a, " = '')", arguments[0].Parameters);
            }
            case nameof(string.IsNullOrWhiteSpace):
            {
                SQLiteExpression a = arguments[0].SQLiteExpression!;
                return SQLiteExpression.Binary(node.Method.ReturnType, visitor.Counters.NextIdentifier(), "(", a, " IS NULL OR TRIM(", a, ", ' ') = '')", arguments[0].Parameters);
            }
            case nameof(string.Concat):
            {
                SQLiteExpression[] concatArgs = arguments.Select(a => a.SQLiteExpression!).ToArray();
                return SQLiteExpression.Variadic(node.Method.ReturnType, visitor.Counters.NextIdentifier(), "", concatArgs, " || ", "", ParameterHelpers.CombineParametersFromModels(arguments));
            }
            case nameof(string.Join):
                if (node.Arguments[1] is NewArrayExpression arrayExpr)
                {
                    SQLiteExpression sep = arguments[0].SQLiteExpression!;
                    SQLiteExpression[] joinArgs = arrayExpr.Expressions.Select(e => visitor.ResolveExpression(e).SQLiteExpression!).ToArray();
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

                throw new NotSupportedException("string.Join with a non-array source is not translatable to SQL.");
            case nameof(string.Compare):
            {
                SQLiteExpression a0 = arguments[0].SQLiteExpression!;
                SQLiteExpression a1 = arguments[1].SQLiteExpression!;
                return SQLiteExpression.Multi(node.Method.ReturnType, visitor.Counters.NextIdentifier(),
                    ["(CASE WHEN ", " = ", " THEN 0 WHEN ", " < ", " THEN -1 ELSE 1 END)"],
                    [a0, a1, a0, a1],
                    ParameterHelpers.CombineParameters(a0, a1));
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

                    return SQLiteExpression.Binary(node.Method.ReturnType, visitor.Counters.NextIdentifier(), "(", arguments[0].SQLiteExpression!, " = ", arguments[1].SQLiteExpression!, $"{collation})", ParameterHelpers.CombineParameters(arguments[0].SQLiteExpression!, arguments[1].SQLiteExpression!));
                }

                return SQLiteExpression.Binary(node.Method.ReturnType, visitor.Counters.NextIdentifier(), "(", arguments[0].SQLiteExpression!, " = ", arguments[1].SQLiteExpression!, ")", ParameterHelpers.CombineParameters(arguments[0].SQLiteExpression!, arguments[1].SQLiteExpression!));
            default:
                throw new NotSupportedException($"string.{node.Method.Name} is not translatable to SQL.");
        }
    }

    private static SQLiteExpression ResolveLike(SQLVisitor visitor, MethodInfo method, SQLiteExpression obj, List<ResolvedModel> arguments, Func<object?, string> selectParameter, Func<SQLiteExpression, string> selectValue)
    {
        string rest = "ESCAPE '\\'";
        if (arguments.Count == 2)
        {
            StringComparison comparison = (StringComparison)arguments[1].Constant!;
            if (comparison is StringComparison.OrdinalIgnoreCase or StringComparison.CurrentCultureIgnoreCase or StringComparison.InvariantCultureIgnoreCase)
            {
                rest += " COLLATE NOCASE";
            }
        }

        if (arguments[0].IsConstant)
        {
            string pName = visitor.Counters.NextParamName();
            SQLiteParameter parameter = new()
            {
                Name = pName,
                Value = arguments[0].Constant is string likeText
                    ? selectParameter(likeText
                        .Replace("\\", "\\\\")
                        .Replace("%", "\\%")
                        .Replace("_", "\\_"))
                    : arguments[0].Constant
            };

            SQLiteParameter[] parameters = obj.Parameters == null
                ? [parameter]
                : [.. obj.Parameters, parameter];

            return SQLiteExpression.Wrap(method.ReturnType, visitor.Counters.NextIdentifier(), "", obj, $" LIKE {pName} {rest}", parameters);
        }
        else
        {
            SQLiteParameter[]? parameters = ParameterHelpers.CombineParameters(obj, arguments[0].SQLiteExpression!);
            string valueSql = selectValue(arguments[0].SQLiteExpression!);

            return SQLiteExpression.Wrap(method.ReturnType, visitor.Counters.NextIdentifier(), "", obj, $" LIKE {valueSql} {rest}", parameters);
        }
    }

    private static Expression ResolveTrim(SQLVisitor visitor, MethodCallExpression node, SQLiteExpression obj, List<ResolvedModel> arguments, string trimType)
    {
        if (arguments.Count == 0)
        {
            return SQLiteExpression.Wrap(node.Method.ReturnType, visitor.Counters.NextIdentifier(), $"{trimType}(", obj, ")", obj.Parameters);
        }

        if (node.Arguments[0] is NewArrayExpression expression)
        {
            ResolvedModel[] args = expression.Expressions
                .Select(visitor.ResolveExpression)
                .ToArray();

            if (args.Any(f => f.SQLiteExpression == null))
            {
                return Expression.Call(obj, node.Method, arguments.Select(f => f.Expression));
            }

            SQLiteExpression[] argExprs = args.Select(f => f.SQLiteExpression!).ToArray();

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
        else
        {
            SQLiteParameter[]? parameters = ParameterHelpers.CombineParameters(obj, arguments[0].SQLiteExpression!);
            return SQLiteExpression.Binary(node.Method.ReturnType, visitor.Counters.NextIdentifier(), $"{trimType}(", obj, ", ", arguments[0].SQLiteExpression!, ")", parameters);
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
}
