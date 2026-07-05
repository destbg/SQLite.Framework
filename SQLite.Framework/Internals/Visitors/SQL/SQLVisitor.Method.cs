namespace SQLite.Framework.Internals.Visitors.SQL;

internal partial class SQLVisitor
{
    public Expression CoerceClientExpression(Expression expr, Type targetType)
    {
        if (expr.Type == targetType)
        {
            return expr;
        }

        if (expr is SQLiteExpression sql)
        {
            return SQLiteExpression.Alias(targetType, Counters.NextIdentifier(), sql, sql.Parameters);
        }

        return Expression.Convert(expr, targetType);
    }

    [UnconditionalSuppressMessage("AOT", "IL2072", Justification = "All types have public properties.")]
    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        bool instanceEquals = node.Method.Name == nameof(object.Equals) && node.Object != null && node.Arguments.Count == 1;
        bool staticEquals = node.Method.Name == nameof(object.Equals) && node.Object == null
            && node.Arguments.Count == 2 && node.Method.DeclaringType == typeof(object);
        if (instanceEquals || staticEquals)
        {
            Expression leftOperand = node.Object ?? node.Arguments[0];
            Expression rightOperand = instanceEquals ? node.Arguments[0] : node.Arguments[1];
            if (DayOfWeekHelpers.IsComputedDayOfWeek(leftOperand) || DayOfWeekHelpers.IsComputedDayOfWeek(rightOperand))
            {
                leftOperand = DayOfWeekHelpers.ConvertOperandToInt(Database.Options, leftOperand);
                rightOperand = DayOfWeekHelpers.ConvertOperandToInt(Database.Options, rightOperand);
            }

            ResolvedModel obj = ResolveExpression(leftOperand);
            ResolvedModel argument = ResolveExpression(rightOperand);

            if (obj.SQLiteExpression == null || argument.SQLiteExpression == null)
            {
                Expression clientLeft = ToClientExpression(node.Object ?? node.Arguments[0]);
                Expression clientRight = ToClientExpression(instanceEquals ? node.Arguments[0] : node.Arguments[1]);
                return instanceEquals
                    ? Expression.Call(clientLeft, node.Method, clientRight)
                    : Expression.Call(node.Method, BoxIfNeeded(clientLeft), BoxIfNeeded(clientRight));
            }

            SQLiteExpression left = BracketBinaryOperand(leftOperand, CoalesceLiftedOrderComparison(leftOperand, obj.SQLiteExpression!));
            SQLiteExpression right = BracketBinaryOperand(rightOperand, CoalesceLiftedOrderComparison(rightOperand, argument.SQLiteExpression!));
            left = CoerceJsonTemporalOperand(obj, left, argument.SQLiteExpression!);
            right = CoerceJsonTemporalOperand(argument, right, obj.SQLiteExpression!);
            SQLiteParameter[]? parameters = ParameterHelpers.CombineParameters(left, right);
            SQLiteExpression equalsResult = SQLiteExpression.Binary(typeof(bool), Counters.NextIdentifier(), "", left, " IS ", right, "", parameters);
            equalsResult.RequiresBrackets = true;
            return equalsResult;
        }

        Type? declaringType = node.Method.DeclaringType;
        SQLiteCallerContext ctx = new(this, node);

        if (declaringType == typeof(SQLiteColumn))
        {
            return ResolveColumnReference(node);
        }

        if (declaringType == typeof(SQLiteFunctions)
            || declaringType == typeof(SQLiteDateFunctions)
            || declaringType == typeof(SQLiteFTS5Functions)
            || declaringType == typeof(SQLiteJsonFunctions)
            || declaringType == typeof(SQLiteWindowFunctions)
            || declaringType == typeof(SQLiteFrameBoundary))
        {
            return Database.Options.MemberTranslators[declaringType](ctx);
        }

        if (declaringType is { IsGenericType: true } && declaringType.GetGenericTypeDefinition() == typeof(SQLiteWindow<>))
        {
            return Database.Options.MemberTranslators[typeof(SQLiteWindowFunctions)](ctx);
        }

        if (node.Arguments.Count > 0
            && node.Arguments[0] is MethodCallExpression firstCall
            && firstCall.Method.Name == nameof(Enumerable.Where)
            && firstCall.Arguments.Count == 2
            && firstCall.Arguments[0].Type.IsGenericType
            && firstCall.Arguments[0].Type.GetGenericTypeDefinition() == typeof(IGrouping<,>))
        {
            return QueryableMemberVisitor.HandleGroupingMethod(this, node);
        }

        if (JsonMethodTranslator.TryHandle(node, this) is { } jsonHandled)
        {
            return jsonHandled;
        }

        if (node.Method.Name == nameof(Enumerable.SequenceEqual) && node.Arguments.Count == 2)
        {
            Expression seqLeft = StripSpanConversion(node.Arguments[0]);
            Expression seqRight = StripSpanConversion(node.Arguments[1]);
            if (seqLeft.Type == typeof(byte[]) && seqRight.Type == typeof(byte[]))
            {
                return Visit(Expression.Equal(seqLeft, seqRight));
            }
        }

        if (Database.Options.TryGetMethodTranslator(node.Method, out SQLiteMemberTranslator? memberTranslator))
        {
            return memberTranslator(ctx);
        }

        if (declaringType != null && Database.Options.MemberTranslators.TryGetValue(declaringType, out SQLiteMemberTranslator? typeTranslator))
        {
            return typeTranslator(ctx);
        }

        if (node.Object != null)
        {
            if (node.Object.Type.IsEnum
                || ((Nullable.GetUnderlyingType(node.Object.Type)?.IsEnum ?? false)
                    && node.Method.Name == nameof(object.ToString)))
            {
                return EnumMemberVisitor.HandleEnumMethod(ctx);
            }

            if (Nullable.GetUnderlyingType(node.Object.Type) is { } underlying
                && node.Method.Name == nameof(Nullable<>.GetValueOrDefault))
            {
                return NullableMemberVisitor.HandleGetValueOrDefault(this, node, underlying);
            }

            if (Nullable.GetUnderlyingType(node.Object.Type) is { } nullableUnderlying
                && node.Method.Name == nameof(object.ToString)
                && node.Arguments.Count == 0
                && TranslationPatterns.IsNumericCastType(nullableUnderlying))
            {
                ResolvedModel nullableObj = ResolveExpression(node.Object);
                if (nullableObj.SQLiteExpression != null)
                {
                    if (nullableUnderlying == typeof(ulong))
                    {
                        SQLiteExpression u = nullableObj.SQLiteExpression;
                        return SQLiteExpression.Multi(typeof(string), Counters.NextIdentifier(),
                            ["(CASE WHEN ", " IS NULL THEN '' ELSE printf('%llu', ", ") END)"],
                            [u, u],
                            u.Parameters);
                    }

                    if (nullableUnderlying == typeof(double) || nullableUnderlying == typeof(float)
                        || (nullableUnderlying == typeof(decimal) && Database.Options.DecimalStorage == DecimalStorageMode.Real))
                    {
                        SQLiteExpression r = nullableObj.SQLiteExpression;
                        SQLiteExpression formatted = NumericMemberVisitor.BuildRealToString(this, typeof(string), r);
                        return SQLiteExpression.Multi(typeof(string), Counters.NextIdentifier(),
                            ["(CASE WHEN ", " IS NULL THEN '' ELSE ", " END)"],
                            [r, formatted],
                            r.Parameters);
                    }

                    return SQLiteExpression.Wrap(typeof(string), Counters.NextIdentifier(),
                        "COALESCE(CAST(", nullableObj.SQLiteExpression, " AS TEXT), '')", nullableObj.SQLiteExpression.Parameters);
                }
            }

            ResolvedModel obj = ResolveExpression(node.Object);

            if (obj is { IsConstant: true, Constant: IEnumerable }
                && node.Arguments.Any(a => ExpressionHelpers.StripQuotes(a) is LambdaExpression))
            {
                return NotTranslatable(node, $"{node.Method.Name} over a captured collection with a selector runs in memory in a Select and is not translatable in a Where.");
            }

            List<ResolvedModel> arguments = node.Arguments
                .Select(ResolveExpression)
                .ToList();

            if (obj is { IsConstant: true, Constant: IEnumerable enumerable })
            {
                return QueryableMemberVisitor.HandleEnumerableMethod(this, node, enumerable, arguments);
            }

            if (RequiresClientEvalFallback(node, arguments, obj))
            {
                return NotTranslatable(node, $"{node.Method.Name} runs in memory because one of its arguments is more than a single column or value.");
            }

            return Expression.Call(obj.Expression, node.Method, CoerceArguments(node.Method, arguments));
        }

        if (node.Arguments.Count > 0)
        {
            if (node.Arguments[0].Type.IsGenericType &&
                node.Arguments[0].Type.GetGenericTypeDefinition() == typeof(IGrouping<,>))
            {
                return QueryableMemberVisitor.HandleGroupingMethod(this, node);
            }

            if (QueryableMemberVisitor.TryHandleConstantAnyPredicate(this, node) is { } anyHandled)
            {
                return anyHandled;
            }

            List<ResolvedModel> arguments = node.Arguments
                .Select(ResolveExpression)
                .ToList();

            IEnumerable? sourceEnumerable = null;
            if (arguments[0].IsConstant && arguments[0].Constant is IEnumerable directEnumerable)
            {
                sourceEnumerable = directEnumerable;
            }
            else if (TryGetImplicitlyConvertedConstantCollection(node.Arguments[0]) is { } unwrapped)
            {
                sourceEnumerable = unwrapped;
            }

            if (sourceEnumerable != null)
            {
                return QueryableMemberVisitor.HandleEnumerableMethod(this, node, sourceEnumerable, arguments);
            }

            if (RequiresClientEvalFallback(node, arguments, null))
            {
                return NotTranslatable(node, $"{node.Method.Name} runs in memory because one of its arguments is more than a single column or value.");
            }

            return Expression.Call(node.Method, CoerceArguments(node.Method, arguments));
        }

        return node;
    }

    private bool RequiresClientEvalFallback(MethodCallExpression node, List<ResolvedModel> resolvedArguments, ResolvedModel? resolvedInstance)
    {
        if (!ClientEvalAllowed)
        {
            return false;
        }

        if (node.Object != null && resolvedInstance is { } instanceModel && IsCompositeSqlOperand(node.Object, instanceModel))
        {
            return true;
        }

        for (int i = 0; i < resolvedArguments.Count; i++)
        {
            if (IsCompositeSqlOperand(node.Arguments[i], resolvedArguments[i]))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsCompositeSqlOperand(Expression original, ResolvedModel resolved)
    {
        if (resolved.SQLiteExpression == null
            || resolved.SQLiteExpression.IsJsonSource
            || ExpressionHelpers.IsConstant(original))
        {
            return false;
        }

        return TryResolveColumnLeaf(original) == null;
    }

    private IEnumerable<Expression> CoerceArguments(MethodInfo method, List<ResolvedModel> arguments)
    {
        ParameterInfo[] parameters = method.GetParameters();
        List<Expression> result = new(arguments.Count);
        for (int i = 0; i < arguments.Count; i++)
        {
            result.Add(CoerceClientExpression(arguments[i].Expression, parameters[i].ParameterType));
        }

        return result;
    }

    private SQLiteExpression ResolveColumnReference(MethodCallExpression node)
    {
        string name = (string)ExpressionHelpers.GetConstantValue(node.Arguments[1])!;
        Expression receiver = node.Arguments[0];

        if (receiver is ParameterExpression bareParameter && RowColumnPrefixes.TryGetValue(bareParameter, out string? barePrefix))
        {
            return SQLiteExpression.Leaf(node.Type, Counters.NextIdentifier(), barePrefix + IdentifierGuard.Quote(name));
        }

        (string path, ParameterExpression? parameter) = ExpressionHelpers.ResolveNullableParameterPath(receiver);
        if (parameter != null
            && TableColumnPrefixes.TryGetValue(MethodArguments[parameter], out Dictionary<string, string?>? prefixes)
            && prefixes.TryGetValue(path, out string? aliasPrefix))
        {
            return BuildShadowColumnLeaf(node.Type, aliasPrefix, name);
        }

        throw new NotSupportedException(
            $"SQLiteColumn.Of<{node.Type.Name}>(row, \"{name}\") could not be bound to a column. It must be called on a " +
            "row of a query (Where, Select, OrderBy, GroupBy, Join), CHECK, computed column, index filter, UPSERT or " +
            "Migrate expression.");
    }

    [UnconditionalSuppressMessage("AOT", "IL2072", Justification = "Value type is the caller SQLiteColumn.Of argument.")]
    private SQLiteExpression BuildShadowColumnLeaf(Type type, string? aliasPrefix, string name)
    {
        string colSql = aliasPrefix != null
            ? $"{aliasPrefix}.{IdentifierGuard.Quote(name)}"
            : IdentifierGuard.Quote(name);

        if (Database.Options.TypeConverters.TryGetValue(type, out ISQLiteTypeConverter? converter)
            && converter.ColumnSqlExpression is { } columnExpr)
        {
            colSql = string.Format(columnExpr, colSql);
        }

        return SQLiteExpression.Leaf(type, Counters.NextIdentifier(), colSql);
    }

    private static UnaryExpression BoxIfNeeded(Expression expr)
    {
        return Expression.Convert(expr, typeof(object));
    }

    private static Expression StripSpanConversion(Expression expr)
    {
        while (expr is MethodCallExpression { Method.Name: "op_Implicit", Arguments.Count: 1 } implicitCall)
        {
            expr = implicitCall.Arguments[0];
        }

        return expr;
    }

    private static IEnumerable? TryGetImplicitlyConvertedConstantCollection(Expression expr)
    {
        if (expr is MethodCallExpression mce
            && mce.Method.IsSpecialName
            && mce.Method.Name == "op_Implicit"
            && mce.Object == null
            && mce.Arguments.Count == 1
            && ExpressionHelpers.IsConstant(mce.Arguments[0])
            && ExpressionHelpers.GetConstantValue(mce.Arguments[0]) is IEnumerable enumerable)
        {
            return enumerable;
        }

        return null;
    }
}
