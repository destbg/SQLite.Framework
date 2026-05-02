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
                    return new SQLiteExpression(
                        node.Method.ReturnType,
                        visitor.Counters.IdentifierIndex++,
                        $"INSTR({obj.Sql}, {arguments[0].Sql}) - 1",
                        parameters
                    );
                }
                case nameof(string.LastIndexOf):
                {
                    SQLiteParameter[]? parameters = ParameterHelpers.CombineParameters(obj.SQLiteExpression, arguments[0].SQLiteExpression!);
                    return new SQLiteExpression(
                        node.Method.ReturnType,
                        visitor.Counters.IdentifierIndex++,
                        $"CASE WHEN LENGTH({arguments[0].Sql}) = 0 THEN LENGTH({obj.Sql}) ELSE COALESCE((WITH RECURSIVE find_pos(pos, rem) AS (SELECT 0, {obj.Sql} UNION ALL SELECT pos + INSTR(rem, {arguments[0].Sql}), SUBSTR(rem, INSTR(rem, {arguments[0].Sql}) + 1) FROM find_pos WHERE INSTR(rem, {arguments[0].Sql}) > 0) SELECT MAX(pos) - 1 FROM find_pos WHERE pos > 0), -1) END",
                        parameters
                    );
                }
                case nameof(string.Insert):
                {
                    SQLiteParameter[]? parameters = ParameterHelpers.CombineParameters(obj.SQLiteExpression, arguments[0].SQLiteExpression!, arguments[1].SQLiteExpression!);
                    return new SQLiteExpression(
                        node.Method.ReturnType,
                        visitor.Counters.IdentifierIndex++,
                        $"SUBSTR({obj.Sql}, 1, {arguments[0].Sql}) || {arguments[1].Sql} || SUBSTR({obj.Sql}, {arguments[0].Sql} + 1)",
                        parameters
                    );
                }
                case nameof(string.Remove):
                {
                    if (node.Arguments.Count == 2)
                    {
                        SQLiteParameter[]? parameters = ParameterHelpers.CombineParameters(obj.SQLiteExpression, arguments[0].SQLiteExpression!, arguments[1].SQLiteExpression!);
                        return new SQLiteExpression(
                            node.Method.ReturnType,
                            visitor.Counters.IdentifierIndex++,
                            $"SUBSTR({obj.Sql}, 1, {arguments[0].Sql}) || SUBSTR({obj.Sql}, {arguments[0].Sql} + {arguments[1].Sql} + 1)",
                            parameters
                        );
                    }
                    else
                    {
                        SQLiteParameter[]? parameters = ParameterHelpers.CombineParameters(obj.SQLiteExpression, arguments[0].SQLiteExpression!);
                        return new SQLiteExpression(
                            node.Method.ReturnType,
                            visitor.Counters.IdentifierIndex++,
                            $"SUBSTR({obj.Sql}, 1, {arguments[0].Sql})",
                            parameters
                        );
                    }
                }
                case nameof(string.Replace):
                {
                    SQLiteParameter[]? parameters = ParameterHelpers.CombineParameters(obj.SQLiteExpression, arguments[0].SQLiteExpression!, arguments[1].SQLiteExpression!);
                    return new SQLiteExpression(
                        node.Method.ReturnType,
                        visitor.Counters.IdentifierIndex++,
                        $"REPLACE({obj.Sql}, {arguments[0].Sql}, {arguments[1].Sql})",
                        parameters
                    );
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
                    return new SQLiteExpression(
                        node.Method.ReturnType,
                        visitor.Counters.IdentifierIndex++,
                        $"SUBSTR({obj.Sql}, {arguments[0].Sql} + 1, 1)",
                        parameters
                    );
                }
                case nameof(string.CompareTo):
                {
                    SQLiteParameter[]? parameters = ParameterHelpers.CombineParameters(obj.SQLiteExpression, arguments[0].SQLiteExpression!);
                    return new SQLiteExpression(
                        node.Method.ReturnType,
                        visitor.Counters.IdentifierIndex++,
                        $"(CASE WHEN {obj.Sql} = {arguments[0].Sql} THEN 0 WHEN {obj.Sql} < {arguments[0].Sql} THEN -1 ELSE 1 END)",
                        parameters
                    );
                }
                case nameof(string.Substring):
                {
                    if (node.Arguments.Count == 2)
                    {
                        SQLiteParameter[]? parameters = ParameterHelpers.CombineParameters(obj.SQLiteExpression, arguments[0].SQLiteExpression!, arguments[1].SQLiteExpression!);
                        return new SQLiteExpression(
                            node.Method.ReturnType,
                            visitor.Counters.IdentifierIndex++,
                            $"SUBSTR({obj.Sql}, {arguments[0].Sql} + 1, {arguments[1].Sql})",
                            parameters
                        );
                    }
                    else
                    {
                        SQLiteParameter[]? parameters = ParameterHelpers.CombineParameters(obj.SQLiteExpression, arguments[0].SQLiteExpression!);
                        return new SQLiteExpression(
                            node.Method.ReturnType,
                            visitor.Counters.IdentifierIndex++,
                            $"SUBSTR({obj.Sql}, {arguments[0].Sql} + 1)",
                            parameters
                        );
                    }
                }
                case nameof(string.ToUpper):
                case nameof(string.ToUpperInvariant):
                {
                    return new SQLiteExpression(
                        node.Method.ReturnType,
                        visitor.Counters.IdentifierIndex++,
                        $"UPPER({obj.Sql})",
                        obj.Parameters
                    );
                }
                case nameof(string.ToLower):
                case nameof(string.ToLowerInvariant):
                {
                    return new SQLiteExpression(
                        node.Method.ReturnType,
                        visitor.Counters.IdentifierIndex++,
                        $"LOWER({obj.Sql})",
                        obj.Parameters
                    );
                }
                case nameof(string.PadLeft):
                {
                    if (node.Arguments.Count == 1)
                    {
                        SQLiteParameter spaceParam = new()
                        {
                            Name = $"@p{visitor.Counters.ParamIndex++}",
                            Value = ' '
                        };
                        SQLiteParameter[]? parameters = ParameterHelpers.CombineParameters(obj.SQLiteExpression, arguments[0].SQLiteExpression!);
                        parameters = [.. parameters ?? [], spaceParam];

                        return new SQLiteExpression(
                            node.Method.ReturnType,
                            visitor.Counters.IdentifierIndex++,
                            $"(CASE WHEN LENGTH({obj.Sql}) >= {arguments[0].Sql} THEN {obj.Sql} ELSE (SELECT SUBSTR(REPLACE(HEX(ZEROBLOB({arguments[0].Sql} - LENGTH({obj.Sql}))), '00', {spaceParam.Name}), 1, {arguments[0].Sql} - LENGTH({obj.Sql})) || {obj.Sql}) END)",
                            parameters
                        );
                    }
                    else
                    {
                        SQLiteParameter[]? parameters = ParameterHelpers.CombineParameters(obj.SQLiteExpression, arguments[0].SQLiteExpression!, arguments[1].SQLiteExpression!);

                        return new SQLiteExpression(
                            node.Method.ReturnType,
                            visitor.Counters.IdentifierIndex++,
                            $"(CASE WHEN LENGTH({obj.Sql}) >= {arguments[0].Sql} THEN {obj.Sql} ELSE (SELECT SUBSTR(REPLACE(HEX(ZEROBLOB({arguments[0].Sql} - LENGTH({obj.Sql}))), '00', {arguments[1].Sql}), 1, {arguments[0].Sql} - LENGTH({obj.Sql})) || {obj.Sql}) END)",
                            parameters
                        );
                    }
                }
                case nameof(string.PadRight):
                {
                    if (node.Arguments.Count == 1)
                    {
                        SQLiteParameter spaceParam = new()
                        {
                            Name = $"@p{visitor.Counters.ParamIndex++}",
                            Value = ' '
                        };
                        SQLiteParameter[]? parameters = ParameterHelpers.CombineParameters(obj.SQLiteExpression, arguments[0].SQLiteExpression!);
                        parameters = [.. parameters ?? [], spaceParam];

                        return new SQLiteExpression(
                            node.Method.ReturnType,
                            visitor.Counters.IdentifierIndex++,
                            $"(CASE WHEN LENGTH({obj.Sql}) >= {arguments[0].Sql} THEN {obj.Sql} ELSE ({obj.Sql} || (SELECT SUBSTR(REPLACE(HEX(ZEROBLOB({arguments[0].Sql} - LENGTH({obj.Sql}))), '00', {spaceParam.Name}), 1, {arguments[0].Sql} - LENGTH({obj.Sql})))) END)",
                            parameters
                        );
                    }
                    else
                    {
                        SQLiteParameter[]? parameters = ParameterHelpers.CombineParameters(obj.SQLiteExpression, arguments[0].SQLiteExpression!, arguments[1].SQLiteExpression!);

                        return new SQLiteExpression(
                            node.Method.ReturnType,
                            visitor.Counters.IdentifierIndex++,
                            $"(CASE WHEN LENGTH({obj.Sql}) >= {arguments[0].Sql} THEN {obj.Sql} ELSE ({obj.Sql} || (SELECT SUBSTR(REPLACE(HEX(ZEROBLOB({arguments[0].Sql} - LENGTH({obj.Sql}))), '00', {arguments[1].Sql}), 1, {arguments[0].Sql} - LENGTH({obj.Sql})))) END)",
                            parameters
                        );
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
                return new SQLiteExpression(
                    node.Method.ReturnType,
                    visitor.Counters.IdentifierIndex++,
                    $"({arguments[0].Sql} IS NULL OR {arguments[0].Sql} = '')",
                    arguments[0].Parameters
                );
            case nameof(string.IsNullOrWhiteSpace):
                return new SQLiteExpression(
                    node.Method.ReturnType,
                    visitor.Counters.IdentifierIndex++,
                    $"({arguments[0].Sql} IS NULL OR TRIM({arguments[0].Sql}, ' ') = '')",
                    arguments[0].Parameters
                );
            case nameof(string.Concat):
                return new SQLiteExpression(
                    node.Method.ReturnType,
                    visitor.Counters.IdentifierIndex++,
                    string.Join(" || ", arguments.Select(f => f.Sql)),
                    ParameterHelpers.CombineParametersFromModels(arguments)
                );
            case nameof(string.Join):
                if (node.Arguments[1] is NewArrayExpression arrayExpr)
                {
                    return new SQLiteExpression(
                        node.Method.ReturnType,
                        visitor.Counters.IdentifierIndex++,
                        string.Join($" || {arguments[0].Sql} || ", arrayExpr.Expressions.Select(e => visitor.ResolveExpression(e).Sql)),
                        ParameterHelpers.CombineParameters([arguments[0].SQLiteExpression!, .. arrayExpr.Expressions.Select(e => visitor.ResolveExpression(e).SQLiteExpression!)])
                    );
                }

                throw new NotSupportedException("string.Join with a non-array source is not translatable to SQL.");
            case nameof(string.Compare):
                return new SQLiteExpression(
                    node.Method.ReturnType,
                    visitor.Counters.IdentifierIndex++,
                    $"(CASE WHEN {arguments[0].Sql} = {arguments[1].Sql} THEN 0 WHEN {arguments[0].Sql} < {arguments[1].Sql} THEN -1 ELSE 1 END)",
                    ParameterHelpers.CombineParameters(arguments[0].SQLiteExpression!, arguments[1].SQLiteExpression!)
                );
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

                    return new SQLiteExpression(
                        node.Method.ReturnType,
                        visitor.Counters.IdentifierIndex++,
                        $"({arguments[0].Sql} = {arguments[1].Sql}{collation})",
                        ParameterHelpers.CombineParameters(arguments[0].SQLiteExpression!, arguments[1].SQLiteExpression!)
                    );
                }

                return new SQLiteExpression(
                    node.Method.ReturnType,
                    visitor.Counters.IdentifierIndex++,
                    $"({arguments[0].Sql} = {arguments[1].Sql})",
                    ParameterHelpers.CombineParameters(arguments[0].SQLiteExpression!, arguments[1].SQLiteExpression!)
                );
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
            string pName = $"@p{visitor.Counters.ParamIndex++}";
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

            return new SQLiteExpression(method.ReturnType, visitor.Counters.IdentifierIndex++, $"{obj.Sql} LIKE {pName} {rest}", parameters);
        }
        else
        {
            SQLiteParameter[]? parameters = ParameterHelpers.CombineParameters(obj, arguments[0].SQLiteExpression!);

            return new SQLiteExpression(
                method.ReturnType,
                visitor.Counters.IdentifierIndex++,
                $"{obj.Sql} LIKE {selectValue(arguments[0].SQLiteExpression!)} {rest}",
                parameters
            );
        }
    }

    private static Expression ResolveTrim(SQLVisitor visitor, MethodCallExpression node, SQLiteExpression obj, List<ResolvedModel> arguments, string trimType)
    {
        if (arguments.Count == 0)
        {
            return new SQLiteExpression(node.Method.ReturnType, visitor.Counters.IdentifierIndex++, $"{trimType}({obj.Sql})", obj.Parameters);
        }

        if (node.Arguments[0] is NewArrayExpression expression)
        {
            ResolvedModel[] args = expression.Expressions
                .Select(visitor.ResolveExpression)
                .ToArray();

            if (args.Any(f => f.Sql == null))
            {
                return Expression.Call(obj, node.Method, arguments.Select(f => f.Expression));
            }

            string concatenatedChars = string.Join(" || ", args.Select(f => f.Sql));

            SQLiteParameter[]? parameters = ParameterHelpers.CombineParameters([obj, .. args.Select(f => f.SQLiteExpression!)]);
            return new SQLiteExpression(
                node.Method.ReturnType,
                visitor.Counters.IdentifierIndex++,
                $"{trimType}({obj.Sql}, {concatenatedChars})",
                parameters
            );
        }
        else
        {
            SQLiteParameter[]? parameters = ParameterHelpers.CombineParameters(obj, arguments[0].SQLiteExpression!);

            return new SQLiteExpression(
                node.Method.ReturnType,
                visitor.Counters.IdentifierIndex++,
                $"{trimType}({obj.Sql}, {arguments[0].Sql})",
                parameters
            );
        }
    }

    public static Expression HandleStringProperty(SQLVisitor visitor, string propertyName, Type type, SQLiteExpression node)
    {
        return propertyName switch
        {
            nameof(string.Length) => new SQLiteExpression(
                type,
                visitor.Counters.IdentifierIndex++,
                $"LENGTH({node.Sql})",
                node.Parameters
            ),
            _ => node
        };
    }
}
