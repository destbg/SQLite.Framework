namespace SQLite.Framework.Internals.Visitors.SQL;

internal partial class SQLVisitor
{
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
            ResolvedModel obj = ResolveExpression(leftOperand);
            ResolvedModel argument = ResolveExpression(rightOperand);

            if (obj.SQLiteExpression == null || argument.SQLiteExpression == null)
            {
                return instanceEquals
                    ? Expression.Call(obj.Expression, node.Method, argument.Expression)
                    : Expression.Call(node.Method, BoxIfNeeded(obj.Expression), BoxIfNeeded(argument.Expression));
            }

            SQLiteParameter[]? parameters = ParameterHelpers.CombineParameters(obj.SQLiteExpression, argument.SQLiteExpression);
            return SQLiteExpression.Binary(typeof(bool), Counters.NextIdentifier(), "", obj.SQLiteExpression!, " IS ", argument.SQLiteExpression!, "", parameters);
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

                    return SQLiteExpression.Wrap(typeof(string), Counters.NextIdentifier(),
                        "COALESCE(CAST(", nullableObj.SQLiteExpression, " AS TEXT), '')", nullableObj.SQLiteExpression.Parameters);
                }
            }

            ResolvedModel obj = ResolveExpression(node.Object);

            List<ResolvedModel> arguments = node.Arguments
                .Select(ResolveExpression)
                .ToList();

            if (obj is { IsConstant: true, Constant: IEnumerable enumerable })
            {
                return QueryableMemberVisitor.HandleEnumerableMethod(this, node, enumerable, arguments);
            }

            return Expression.Call(obj.Expression, node.Method, arguments.Select(f => f.Expression));
        }

        if (node.Arguments.Count > 0)
        {
            if (node.Arguments[0].Type.IsGenericType &&
                node.Arguments[0].Type.GetGenericTypeDefinition() == typeof(IGrouping<,>))
            {
                return QueryableMemberVisitor.HandleGroupingMethod(this, node);
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

            return Expression.Call(node.Method, arguments.Select(f => f.Expression));
        }

        return node;
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
            "row of a query (Where, Select, OrderBy, GroupBy, Join), CHECK, computed column, index filter, UPSERT, or " +
            "Migrate expression.");
    }

    [UnconditionalSuppressMessage("AOT", "IL2072", Justification = "The value type is the generic argument of SQLiteColumn.Of, which the caller declares.")]
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
