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
                    Type enumUnderlying = Enum.GetUnderlyingType(enumType);
                    SQLiteExpression objExpr = obj.SQLiteExpression!;
                    bool isUlongBacked = enumUnderlying == typeof(ulong);

                    string format = "G";
                    if (node.Arguments.Count > 0)
                    {
                        if (!arguments[0].IsConstant || arguments[0].Constant is not string formatArg)
                        {
                            throw new NotSupportedException(
                                "Enum.ToString with a non-constant format string is not translatable to SQL. " +
                                "Use a constant \"G\" (name), \"D\" (number) or \"X\" (hex) format.");
                        }

                        format = string.IsNullOrEmpty(formatArg) ? "G" : formatArg;
                    }

                    if (format.Length != 1)
                    {
                        throw new NotSupportedException(
                            $"Enum.ToString format \"{format}\" is not supported in a query. " +
                            "The supported formats are \"G\" (name), \"D\" (number) and \"X\" (hex).");
                    }

                    char formatChar = char.ToUpperInvariant(format[0]);

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
                        throw new NotSupportedException(
                            $"Enum.ToString format \"{format}\" is not supported in a query. " +
                            "The supported formats are \"G\" (name), \"D\" (number) and \"X\" (hex).");
                    }

                    if (enumType.IsDefined(typeof(FlagsAttribute), inherit: false))
                    {
                        throw new NotSupportedException(
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

                    return SQLiteExpression.Binary(node.Method.ReturnType, visitor.Counters.NextIdentifier(), "(CASE ", objExpr, elseOpen, objExpr, elseClose, parameters);
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

            string collate = ignoreCase ? " COLLATE NOCASE" : "";
            return SQLiteExpression.Binary(node.Method.ReturnType, visitor.Counters.NextIdentifier(), "(CASE ", stringArgExpr, collate + caseSb.ToString() + " ELSE CAST(", stringArgExpr, " AS INTEGER) END)", allParams);
        }

        throw new NotSupportedException($"Enum.{node.Method.Name} is not translatable to SQL.");
    }

    private static long ToSignedNumeric(object enumValue, Type enumUnderlying)
    {
        return enumUnderlying == typeof(ulong)
            ? unchecked((long)(ulong)enumValue)
            : Convert.ToInt64(enumValue);
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
