namespace SQLite.Framework.Internals.Visitors.SQL;

internal partial class SQLVisitor
{
    private static readonly HashSet<Type> ClientEvalSqlMarkerTypes =
    [
        typeof(SQLiteColumn),
        typeof(SQLiteFunctions),
        typeof(SQLiteDateFunctions),
        typeof(SQLiteFTS5Functions),
        typeof(SQLiteJsonFunctions),
        typeof(SQLiteWindowFunctions),
        typeof(SQLiteFrameBoundary),
    ];

    [return: NotNullIfNotNull(nameof(node))]
    public override Expression? Visit(Expression? node)
    {
        if (node != null
            && ClientEvalAllowed
            && IsClientEvalCandidate(node))
        {
            try
            {
                return base.Visit(node);
            }
            catch (NotSupportedException)
            {
                return BuildClientEvalFallback(node);
            }
        }

        return base.Visit(node);
    }

    public SQLiteExpression? TryResolveColumnLeaf(Expression node)
    {
        (string path, ParameterExpression? pe) = ExpressionHelpers.ResolveNullableParameterPath(node);
        if (pe != null
            && MethodArguments.TryGetValue(pe, out Dictionary<string, Expression>? columns)
            && columns.TryGetValue(path, out Expression? column)
            && column is SQLiteExpression)
        {
            return (SQLiteExpression)Visit(node);
        }

        return null;
    }

    private bool IsClientEvalCandidate(Expression node)
    {
        return node switch
        {
            MethodCallExpression methodCall => IsScalarMethodCall(methodCall),
            MemberExpression { Expression: { } target } => IsClientEvalScalarType(target.Type),
            _ => false
        };
    }

    private bool IsScalarMethodCall(MethodCallExpression node)
    {
        Type? declaringType = node.Method.DeclaringType;
        if (declaringType == null
            || declaringType == typeof(Enumerable)
            || declaringType == typeof(System.Linq.Queryable)
            || ClientEvalSqlMarkerTypes.Contains(declaringType)
            || (declaringType.IsGenericType && declaringType.GetGenericTypeDefinition() == typeof(SQLiteWindow<>)))
        {
            return false;
        }

        if (!node.Arguments.All(argument => IsClientEvalScalarType(argument.Type)))
        {
            return false;
        }

        return node.Object != null
            ? IsClientEvalScalarType(node.Object.Type)
            : IsClientEvalScalarType(node.Type);
    }

    private bool IsClientEvalScalarType(Type type)
    {
        Type underlying = Nullable.GetUnderlyingType(type) ?? type;
        if (underlying != typeof(string) && TypeHelpers.GetEnumerableElementType(underlying) != null)
        {
            return false;
        }

        return underlying == typeof(string) || TypeHelpers.IsSimple(underlying, Database.Options);
    }

    private Expression BuildClientEvalFallback(Expression node)
    {
        if (node is MethodCallExpression methodCall)
        {
            Expression[] arguments = methodCall.Arguments.Select(ToClientExpression).ToArray();
            return methodCall.Object == null
                ? Expression.Call(methodCall.Method, arguments)
                : Expression.Call(ToClientExpression(methodCall.Object), methodCall.Method, arguments);
        }

        MemberExpression memberExpression = (MemberExpression)node;
        return Expression.MakeMemberAccess(ToClientExpression(memberExpression.Expression!), memberExpression.Member);
    }

    private Expression ToClientExpression(Expression node)
    {
        return new ClientLeafRewriter(this).Visit(node)!;
    }
}
