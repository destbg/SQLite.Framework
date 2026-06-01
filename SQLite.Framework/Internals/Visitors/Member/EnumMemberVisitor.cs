namespace SQLite.Framework.Internals.Visitors.Member;

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
                    SQLiteExpression objExpr = obj.SQLiteExpression!;
                    SQLiteExpression arg0 = arguments[0].SQLiteExpression!;
                    SQLiteParameter[]? parameters = ParameterHelpers.CombineParameters(objExpr, arg0);
                    return SQLiteExpression.Trinary(node.Method.ReturnType, visitor.Counters.NextIdentifier(), "((", objExpr, " & ", arg0, ") = ", arg0, ")", parameters);
                }
                case nameof(Enum.ToString):
                {
                    Type enumType = node.Object!.Type;

                    if (enumType.IsDefined(typeof(FlagsAttribute), inherit: false))
                    {
                        SQLiteExpression? flagsResult = TryBuildFlagsToString(visitor, node, obj, enumType);
                        if (flagsResult != null)
                        {
                            return flagsResult;
                        }
                    }

                    Type enumUnderlying = Enum.GetUnderlyingType(enumType);
                    Array enumValuesArray = Enum.GetValuesAsUnderlyingType(enumType);
                    string[] enumNames = Enum.GetNames(enumType);

                    StringBuilder caseSb = new();
                    List<SQLiteParameter> nameParams = new();
                    for (int i = 0; i < enumValuesArray.Length; i++)
                    {
                        object enumValue = enumValuesArray.GetValue(i)!;
                        long numericValue = ToSignedNumeric(enumValue, enumUnderlying);
                        string enumName = enumNames[i];

                        SQLiteParameter nameParam = new()
                        {
                            Name = visitor.Counters.NextParamName(),
                            Value = enumName
                        };
                        nameParams.Add(nameParam);
                        caseSb.Append(" WHEN ");
                        caseSb.Append(numericValue);
                        caseSb.Append(" THEN ");
                        caseSb.Append(nameParam.Name);
                    }

                    SQLiteExpression objExpr = obj.SQLiteExpression!;
                    SQLiteParameter[] parameters = obj.Parameters == null
                        ? [.. nameParams]
                        : [.. obj.Parameters, .. nameParams];

                    return SQLiteExpression.Binary(node.Method.ReturnType, visitor.Counters.NextIdentifier(), "(CASE ", objExpr, caseSb.ToString() + " ELSE CAST(", objExpr, " AS TEXT) END)", parameters);
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

            Type enumUnderlying = Enum.GetUnderlyingType(enumType);
            Array enumValuesArray = Enum.GetValuesAsUnderlyingType(enumType);
            string[] enumNames = Enum.GetNames(enumType);

            StringBuilder caseSb = new();
            List<SQLiteParameter> parameters = new();

            for (int i = 0; i < enumValuesArray.Length; i++)
            {
                object enumValue = enumValuesArray.GetValue(i)!;
                long numericValue = ToSignedNumeric(enumValue, enumUnderlying);
                string enumName = enumNames[i];

                SQLiteParameter nameParam = new()
                {
                    Name = visitor.Counters.NextParamName(),
                    Value = enumName
                };
                parameters.Add(nameParam);
                caseSb.Append(" WHEN ");
                caseSb.Append(nameParam.Name);
                caseSb.Append(" THEN ");
                caseSb.Append(numericValue);
            }

            SQLiteExpression stringArgExpr = stringArg.SQLiteExpression!;
            SQLiteParameter[] allParams = stringArg.Parameters == null
                ? [.. parameters]
                : [.. stringArg.Parameters, .. parameters];

            return SQLiteExpression.Wrap(node.Method.ReturnType, visitor.Counters.NextIdentifier(), "(CASE ", stringArgExpr, caseSb.ToString() + " ELSE NULL END)", allParams);
        }

        throw new NotSupportedException($"Enum.{node.Method.Name} is not translatable to SQL.");
    }

    private static long ToSignedNumeric(object enumValue, Type enumUnderlying)
    {
        return enumUnderlying == typeof(ulong)
            ? unchecked((long)(ulong)enumValue)
            : Convert.ToInt64(enumValue);
    }

    private static SQLiteExpression? TryBuildFlagsToString(SQLVisitor visitor, MethodCallExpression node, ResolvedModel obj, Type enumType)
    {
        Type enumUnderlying = Enum.GetUnderlyingType(enumType);
        Array enumValuesArray = Enum.GetValuesAsUnderlyingType(enumType);
        string[] enumNames = Enum.GetNames(enumType);

        string? zeroName = null;
        List<(ulong Order, long Value, string Name)> namedValues = new();
        for (int i = 0; i < enumValuesArray.Length; i++)
        {
            long numericValue = ToSignedNumeric(enumValuesArray.GetValue(i)!, enumUnderlying);
            if (numericValue == 0)
            {
                zeroName = enumNames[i];
            }
            else
            {
                namedValues.Add((unchecked((ulong)numericValue), numericValue, enumNames[i]));
            }
        }

        if (namedValues.Count == 0)
        {
            return null;
        }

        namedValues.Sort((a, b) => b.Order.CompareTo(a.Order));

        SQLiteExpression objExpr = obj.SQLiteExpression!;
        string column = objExpr.ToString();
        List<SQLiteParameter> nameParams = new();

        StringBuilder valuesList = new();
        for (int i = 0; i < namedValues.Count; i++)
        {
            (ulong _, long value, string name) = namedValues[i];
            SQLiteParameter nameParam = new() { Name = visitor.Counters.NextParamName(), Value = name };
            nameParams.Add(nameParam);
            if (i > 0)
            {
                valuesList.Append(", ");
            }

            valuesList.Append('(').Append(i).Append(", ").Append(value).Append(", ").Append(nameParam.Name).Append(')');
        }

        string zeroText;
        if (zeroName != null)
        {
            SQLiteParameter zeroParam = new() { Name = visitor.Counters.NextParamName(), Value = zeroName };
            nameParams.Add(zeroParam);
            zeroText = zeroParam.Name;
        }
        else
        {
            zeroText = "'0'";
        }

        // Reproduce the .NET flags ToString algorithm in SQL. Walk the named values from the
        // largest to the smallest. Whenever the still-uncleared bits contain a value, take its
        // name and clear those bits. This makes a combined member like ReadWrite win over its
        // single bits, exactly like Enum.ToString does. The walk runs in a recursive CTE so the
        // SQL grows with the member count, not exponentially.
        StringBuilder sb = new();
        sb.Append("(CASE WHEN ").Append(column).Append(" = 0 THEN ").Append(zeroText);
        sb.Append(" ELSE COALESCE((SELECT CASE WHEN remaining = 0 THEN acc ELSE NULL END FROM (");
        sb.Append("WITH RECURSIVE vals(i, val, nm) AS (VALUES ").Append(valuesList).Append("), ");
        sb.Append("walk(i, remaining, acc) AS (SELECT 0, ").Append(column).Append(", '' UNION ALL ");
        sb.Append("SELECT vv.i + 1, ");
        sb.Append("CASE WHEN (w.remaining & vv.val) = vv.val THEN w.remaining & ~vv.val ELSE w.remaining END, ");
        sb.Append("CASE WHEN (w.remaining & vv.val) = vv.val THEN vv.nm || (CASE WHEN w.acc = '' THEN '' ELSE ', ' END) || w.acc ELSE w.acc END ");
        sb.Append("FROM walk w JOIN vals vv ON vv.i = w.i) ");
        sb.Append("SELECT remaining, acc FROM walk ORDER BY i DESC LIMIT 1)), ");
        sb.Append("CAST(").Append(column).Append(" AS TEXT)) END)");

        SQLiteParameter[] parameters = objExpr.Parameters == null
            ? [.. nameParams]
            : [.. objExpr.Parameters, .. nameParams];

        return SQLiteExpression.Leaf(node.Method.ReturnType, visitor.Counters.NextIdentifier(), sb.ToString(), parameters);
    }
}
