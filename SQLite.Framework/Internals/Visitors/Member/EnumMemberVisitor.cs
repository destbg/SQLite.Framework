namespace SQLite.Framework.Internals.Visitors.Member;

internal static class EnumMemberVisitor
{
    public static Expression HandleEnumMethod(SQLiteCallerContext ctx)
    {

        SQLVisitor visitor = ctx.Visitor;
        MethodCallExpression node = (MethodCallExpression)ctx.Node;
        List<ResolvedModel> arguments = node.Arguments
            .Select(arg => visitor.ResolveExpression(StripEnumBoxing(arg)))
            .ToList();

        if (node.Object != null)
        {
            ResolvedModel obj = visitor.ResolveExpression(node.Object);

            if (obj.SQLiteExpression == null || arguments.Any(f => f.SQLiteExpression == null))
            {
                return Expression.Call(obj.Expression, node.Method, CoerceToParameterTypes(node.Method, arguments));
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
                    Type objectType = node.Object!.Type;
                    Type enumType = Nullable.GetUnderlyingType(objectType) ?? objectType;
                    bool isNullableEnum = enumType != objectType;
                    Type enumUnderlying = Enum.GetUnderlyingType(enumType);
                    SQLiteExpression objExpr = obj.SQLiteExpression!;
                    bool isUlongBacked = enumUnderlying == typeof(ulong);

                    string format = "G";
                    if (node.Arguments.Count > 0)
                    {
                        if (!arguments[0].IsConstant || arguments[0].Constant is not string formatArg)
                        {
                            return visitor.NotTranslatable(node,
                                "Enum.ToString with a non-constant format string is not translatable to SQL. " +
                                "Use a constant \"G\" (name), \"D\" (number) or \"X\" (hex) format.");
                        }

                        format = string.IsNullOrEmpty(formatArg) ? "G" : formatArg;
                    }

                    if (format.Length != 1)
                    {
                        return visitor.NotTranslatable(node,
                            $"Enum.ToString format \"{format}\" is not supported in a query. " +
                            "The supported formats are \"G\" (name), \"D\" (number) and \"X\" (hex).");
                    }

                    char formatChar = char.ToUpperInvariant(format[0]);

                    if (formatChar is 'D' or 'X' && visitor.Database.Options.EnumStorage == EnumStorageMode.Text)
                    {
                        return BuildTextStorageNumericFormat(visitor, node, enumType, enumUnderlying, objExpr, formatChar);
                    }

                    if (formatChar == 'D')
                    {
                        return isUlongBacked
                            ? SQLiteExpression.Wrap(node.Method.ReturnType, visitor.Counters.NextIdentifier(), "printf('%llu', ", objExpr, ")", obj.Parameters)
                            : SQLiteExpression.Wrap(node.Method.ReturnType, visitor.Counters.NextIdentifier(), "CAST(", objExpr, " AS TEXT)", obj.Parameters);
                    }

                    if (formatChar == 'X')
                    {
                        (int hexDigits, long hexMask, bool use64) = HexFormatInfo(enumUnderlying);
                        return use64
                            ? SQLiteExpression.Wrap(node.Method.ReturnType, visitor.Counters.NextIdentifier(), $"printf('%0{hexDigits}llX', ", objExpr, ")", obj.Parameters)
                            : SQLiteExpression.Wrap(node.Method.ReturnType, visitor.Counters.NextIdentifier(), $"printf('%0{hexDigits}X', (", objExpr, $" & {hexMask}))", obj.Parameters);
                    }

                    if (formatChar != 'G')
                    {
                        return visitor.NotTranslatable(node,
                            $"Enum.ToString format \"{format}\" is not supported in a query. " +
                            "The supported formats are \"G\" (name), \"D\" (number) and \"X\" (hex).");
                    }

                    if (enumType.IsDefined(typeof(FlagsAttribute), inherit: false))
                    {
                        return visitor.NotTranslatable(node,
                            $"ToString with the \"G\" (name) format on the [Flags] enum {enumType.Name} is not supported in a query " +
                            "because the name decomposition cannot be reproduced faithfully in SQL. " +
                            "Use the \"D\" (number) or \"X\" (hex) format.");
                    }

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

                    SQLiteParameter[] parameters = obj.Parameters == null
                        ? [.. nameParams]
                        : [.. obj.Parameters, .. nameParams];

                    string elseOpen = caseSb.ToString() + (isUlongBacked ? " ELSE printf('%llu', " : " ELSE CAST(");
                    string elseClose = isUlongBacked ? ") END)" : " AS TEXT) END)";

                    SQLiteExpression nameCase = CommonHelpers.EvaluateOnce(visitor.Counters, node.Method.ReturnType, [objExpr], v =>
                        SQLiteExpression.Binary(node.Method.ReturnType, visitor.Counters.NextIdentifier(), "(CASE ", v[0], elseOpen, v[0], elseClose, [.. nameParams]));

                    return isNullableEnum
                        ? SQLiteExpression.Wrap(node.Method.ReturnType, visitor.Counters.NextIdentifier(), "COALESCE(", nameCase, ", '')", parameters)
                        : nameCase;
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
            bool ignoreCase;

            if (node.Method.IsGenericMethod)
            {
                enumType = node.Method.GetGenericArguments()[0];
                stringArg = arguments[0];
                ignoreCase = arguments.Count >= 2 && Equals(arguments[1].Constant, true);
            }
            else
            {
                if (arguments[0].IsConstant && arguments[0].Constant is Type type)
                {
                    enumType = type;
                    stringArg = arguments[1];
                    ignoreCase = arguments.Count >= 3 && Equals(arguments[2].Constant, true);
                }
                else
                {
                    return node;
                }
            }

            Type enumUnderlying = Enum.GetUnderlyingType(enumType);
            Array enumValuesArray = Enum.GetValuesAsUnderlyingType(enumType);
            string[] enumNames = Enum.GetNames(enumType);

            SQLiteExpression stringArgExpr = stringArg.SQLiteExpression!;
            SQLiteExpression strippedExpr = stringArgExpr;
            foreach (int code in Constants.WhitespaceCodePoints)
            {
                if (code > 32)
                {
                    continue;
                }

                strippedExpr = SQLiteExpression.Wrap(typeof(string), visitor.Counters.NextIdentifier(), "REPLACE(", strippedExpr, $", CHAR({code}), '')", strippedExpr.Parameters);
            }

            strippedExpr = SQLiteExpression.Wrap(typeof(string), visitor.Counters.NextIdentifier(), "TRIM(", strippedExpr, $", {Constants.WhitespaceChars})", strippedExpr.Parameters);

            return CommonHelpers.EvaluateOnce(visitor.Counters, node.Method.ReturnType, [strippedExpr], v =>
            {
                string vsql = v[0].ToString();
                string norm = ignoreCase
                    ? $"LOWER(',' || {vsql} || ',')"
                    : $"(',' || {vsql} || ',')";

                List<SQLiteParameter> tokenParameters = new();
                List<string> parts = new();
                for (int i = 0; i < enumNames.Length; i++)
                {
                    long numericValue = ToSignedNumeric(enumValuesArray.GetValue(i)!, enumUnderlying);
                    string token = "," + (ignoreCase ? enumNames[i].ToLowerInvariant() : enumNames[i]) + ",";
                    SQLiteParameter tokenParam = new()
                    {
                        Name = visitor.Counters.NextParamName(),
                        Value = token
                    };
                    tokenParameters.Add(tokenParam);
                    parts.Add($"(CASE WHEN INSTR({norm}, {tokenParam.Name}) > 0 THEN {numericValue} ELSE 0 END)");
                }

                parts.Add($"CAST({vsql} AS INTEGER)");
                string body = $"({string.Join(" | ", parts)})";

                return SQLiteExpression.Leaf(node.Method.ReturnType, visitor.Counters.NextIdentifier(), body, tokenParameters.ToArray());
            });
        }

        return visitor.NotTranslatable(node, $"Enum.{node.Method.Name} is not translatable to SQL.");
    }

    private static List<Expression> CoerceToParameterTypes(MethodInfo method, List<ResolvedModel> arguments)
    {
        ParameterInfo[] parameters = method.GetParameters();
        List<Expression> result = new(arguments.Count);
        for (int i = 0; i < arguments.Count; i++)
        {
            result.Add(Expression.Convert(arguments[i].Expression, parameters[i].ParameterType));
        }

        return result;
    }

    private static Expression StripEnumBoxing(Expression argument)
    {
        return argument is UnaryExpression { NodeType: ExpressionType.Convert } convert
            ? convert.Operand
            : argument;
    }

    private static long ToSignedNumeric(object enumValue, Type enumUnderlying)
    {
        return enumUnderlying == typeof(ulong)
            ? unchecked((long)(ulong)enumValue)
            : Convert.ToInt64(enumValue);
    }

    [UnconditionalSuppressMessage("AOT", "IL2072", Justification = "Enum type comes from the entity surface. Users keep their enums reachable.")]
    private static SQLiteExpression BuildTextStorageNumericFormat(SQLVisitor visitor, MethodCallExpression node, Type enumType, Type enumUnderlying, SQLiteExpression objExpr, char formatChar)
    {
        Array enumValuesArray = Enum.GetValuesAsUnderlyingType(enumType);
        string[] enumNames = Enum.GetNames(enumType);
        (int hexDigits, _, _) = HexFormatInfo(enumUnderlying);

        StringBuilder caseSb = new();
        List<SQLiteParameter> caseParams = new();
        for (int i = 0; i < enumValuesArray.Length; i++)
        {
            object enumValue = enumValuesArray.GetValue(i)!;
            string formatted = formatChar == 'D'
                ? Convert.ToString(enumValue, CultureInfo.InvariantCulture)!
                : ((IFormattable)enumValue).ToString("X" + hexDigits, CultureInfo.InvariantCulture);

            SQLiteParameter nameParam = new() { Name = visitor.Counters.NextParamName(), Value = enumNames[i] };
            SQLiteParameter valueParam = new() { Name = visitor.Counters.NextParamName(), Value = formatted };
            caseParams.Add(nameParam);
            caseParams.Add(valueParam);
            caseSb.Append(" WHEN ").Append(nameParam.Name).Append(" THEN ").Append(valueParam.Name);
        }

        string elseOpen = caseSb.ToString() + " ELSE ";
        return CommonHelpers.EvaluateOnce(visitor.Counters, node.Method.ReturnType, [objExpr], v =>
            SQLiteExpression.Binary(node.Method.ReturnType, visitor.Counters.NextIdentifier(), "(CASE ", v[0], elseOpen, v[0], " END)", [.. caseParams]));
    }

    private static (int Digits, long Mask, bool Use64) HexFormatInfo(Type enumUnderlying)
    {
        if (enumUnderlying == typeof(byte) || enumUnderlying == typeof(sbyte))
        {
            return (2, 0xFF, false);
        }

        if (enumUnderlying == typeof(short) || enumUnderlying == typeof(ushort))
        {
            return (4, 0xFFFF, false);
        }

        if (enumUnderlying == typeof(int) || enumUnderlying == typeof(uint))
        {
            return (8, 0xFFFFFFFFL, false);
        }

        return (16, 0L, true);
    }
}
