namespace SQLite.Framework.Internals.Visitors;

internal static class EnumMemberVisitor
{
    public static Expression HandleEnumMethod(SQLiteCallerContext ctx)
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

            switch (node.Method.Name)
            {
                case nameof(Enum.HasFlag):
                {
                    SQLiteParameter[]? parameters = ParameterHelpers.CombineParameters(obj.SQLiteExpression, arguments[0].SQLiteExpression!);
                    return new SQLiteExpression(
                        node.Method.ReturnType,
                        visitor.Counters.IdentifierIndex++,
                        $"(({obj.Sql} & {arguments[0].Sql}) = {arguments[0].Sql})",
                        parameters
                    );
                }
                case nameof(Enum.ToString):
                {
                    Type enumType = node.Object!.Type;
                    Array enumValuesArray = Enum.GetValuesAsUnderlyingType(enumType);
                    string[] enumNames = Enum.GetNames(enumType);

                    List<string> caseClauses = new();
                    List<SQLiteParameter> nameParams = new();

                    for (int i = 0; i < enumValuesArray.Length; i++)
                    {
                        object enumValue = enumValuesArray.GetValue(i)!;
                        long numericValue = Convert.ToInt64(enumValue);
                        string enumName = enumNames[i];

                        SQLiteParameter nameParam = new()
                        {
                            Name = $"@p{visitor.Counters.ParamIndex++}",
                            Value = enumName
                        };
                        nameParams.Add(nameParam);
                        caseClauses.Add($"WHEN {numericValue} THEN {nameParam.Name}");
                    }

                    string caseExpression = $"(CASE {obj.Sql} {string.Join(" ", caseClauses)} ELSE CAST({obj.Sql} AS TEXT) END)";

                    SQLiteParameter[] parameters = obj.Parameters == null
                        ? [.. nameParams]
                        : [.. obj.Parameters, .. nameParams];

                    return new SQLiteExpression(
                        node.Method.ReturnType,
                        visitor.Counters.IdentifierIndex++,
                        caseExpression,
                        parameters
                    );
                }
            }
        }

        if (QueryableMemberVisitor.CheckConstantMethod<Enum>(visitor, node, arguments, out Expression? expression))
        {
            return expression;
        }

        if (node.Method.Name == nameof(Enum.Parse))
        {
            Type enumType;
            ResolvedModel stringArg;

            if (node.Method.IsGenericMethod)
            {
                enumType = node.Method.GetGenericArguments()[0];
                stringArg = arguments[0];
            }
            else
            {
                if (arguments[0].IsConstant && arguments[0].Constant is Type type)
                {
                    enumType = type;
                    stringArg = arguments[1];
                }
                else
                {
                    return node;
                }
            }

            Array enumValuesArray = Enum.GetValuesAsUnderlyingType(enumType);
            string[] enumNames = Enum.GetNames(enumType);

            List<string> caseClauses = new();
            List<SQLiteParameter> parameters = new();

            for (int i = 0; i < enumValuesArray.Length; i++)
            {
                object enumValue = enumValuesArray.GetValue(i)!;
                long numericValue = Convert.ToInt64(enumValue);
                string enumName = enumNames[i];

                SQLiteParameter nameParam = new()
                {
                    Name = $"@p{visitor.Counters.ParamIndex++}",
                    Value = enumName
                };
                parameters.Add(nameParam);
                caseClauses.Add($"WHEN {nameParam.Name} THEN {numericValue}");
            }

            string caseExpression = $"(CASE {stringArg.Sql} {string.Join(" ", caseClauses)} ELSE NULL END)";

            SQLiteParameter[] allParams = stringArg.Parameters == null
                ? [.. parameters]
                : [.. stringArg.Parameters, .. parameters];

            return new SQLiteExpression(
                node.Method.ReturnType,
                visitor.Counters.IdentifierIndex++,
                caseExpression,
                allParams
            );
        }

        throw new NotSupportedException($"Enum.{node.Method.Name} is not translatable to SQL.");
    }
}
