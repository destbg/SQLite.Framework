namespace SQLite.Framework.Internals.Visitors;

internal partial class SQLVisitor
{
    [UnconditionalSuppressMessage("AOT", "IL2072", Justification = "All types have public properties.")]
    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (node.Method.Name == nameof(object.Equals) && node.Object != null)
        {
            ResolvedModel obj = ResolveExpression(node.Object);
            ResolvedModel argument = ResolveExpression(node.Arguments[0]);

            if (obj.SQLiteExpression == null || argument.SQLiteExpression == null)
            {
                return Expression.Call(obj.Expression, node.Method, argument.Expression);
            }

            SQLiteParameter[]? parameters = ParameterHelpers.CombineParameters(obj.SQLiteExpression, argument.SQLiteExpression);
            return new SQLiteExpression(typeof(bool), Counters.IdentifierIndex++, $"{obj.Sql} = {argument.Sql}", parameters);
        }

        Type? declaringType = node.Method.DeclaringType;
        SQLiteCallerContext ctx = new(this, node);

        if (declaringType == typeof(SQLiteFunctions)
            || declaringType == typeof(SQLiteFTS5Functions)
            || declaringType == typeof(SQLiteJsonFunctions)
            || declaringType == typeof(SQLiteWindowFunctions)
            || declaringType == typeof(SQLiteFrameBoundary))
        {
            return Database.Options.MemberTranslators[declaringType](ctx);
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
            if (node.Object.Type.IsEnum)
            {
                return EnumMemberVisitor.HandleEnumMethod(ctx);
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

            if (arguments[0].IsConstant && arguments[0].Constant is IEnumerable enumerable)
            {
                return QueryableMemberVisitor.HandleEnumerableMethod(this, node, enumerable, arguments);
            }

            return Expression.Call(node.Method, arguments.Select(f => f.Expression));
        }

        return node;
    }
}
