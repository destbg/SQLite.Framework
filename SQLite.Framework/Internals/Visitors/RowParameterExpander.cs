namespace SQLite.Framework.Internals.Visitors;

/// <summary>
/// Expands table-row references that appear as method-call arguments into
/// <see cref="MemberInitExpression" />s that reconstruct the row from its individual members.
/// </summary>
internal sealed class RowParameterExpander : ExpressionVisitor
{
    private readonly HashSet<ParameterExpression> rowParameters;

    private RowParameterExpander(HashSet<ParameterExpression> rowParameters)
    {
        this.rowParameters = rowParameters;
    }

    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "The returned LambdaExpression is consumed as an expression tree by the translator, never compiled into a delegate.")]
    public static LambdaExpression ExpandRowsInMethodCalls(LambdaExpression lambda, IEnumerable<ParameterExpression> rowParameters)
    {
        HashSet<ParameterExpression> set = [.. rowParameters];

        if (set.Count == 0)
        {
            return lambda;
        }

        RowParameterExpander expander = new(set);
        Expression body = expander.Visit(lambda.Body) ?? lambda.Body;
        return body == lambda.Body ? lambda : Expression.Lambda(body, lambda.Parameters);
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        Expression? newObject = node.Object != null ? Visit(node.Object) : null;
        Expression[] newArgs = new Expression[node.Arguments.Count];
        bool substituteRowArguments = !IsFrameworkTranslatedMethod(node.Method);

        for (int i = 0; i < node.Arguments.Count; i++)
        {
            Expression arg = node.Arguments[i];
            if (substituteRowArguments && LooksLikeRowReference(arg))
            {
                newArgs[i] = BuildMaterialization(arg);
            }
            else
            {
                newArgs[i] = Visit(arg) ?? arg;
            }
        }

        return node.Update(newObject, newArgs);
    }

    private bool LooksLikeRowReference(Expression expression)
    {
        if (expression is ParameterExpression pe && rowParameters.Contains(pe))
        {
            return true;
        }
        else if (expression is MemberExpression { Expression: ParameterExpression innerPe } me
            && rowParameters.Contains(innerPe)
            && IsConstructibleEntityType(me.Type))
        {
            return true;
        }

        return false;
    }

    [UnconditionalSuppressMessage("AOT", "IL2070", Justification = "Row types are preserved via DynamicallyAccessedMembers on the Queryable<T> type parameter.")]
    private static bool IsConstructibleEntityType(Type type)
    {
        if (type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal))
        {
            return false;
        }
        else if (type.IsArray || typeof(System.Collections.IEnumerable).IsAssignableFrom(type))
        {
            return false;
        }

        return type.GetConstructor(Type.EmptyTypes) != null;
    }

    private static bool IsFrameworkTranslatedMethod(MethodInfo method)
    {
        Type? declaringType = method.DeclaringType;
        if (declaringType == null)
        {
            return false;
        }

        return declaringType == typeof(Queryable)
            || declaringType == typeof(Enumerable)
            || declaringType.Namespace == "SQLite.Framework.Extensions";
    }

    [UnconditionalSuppressMessage("AOT", "IL2075", Justification = "Row types are preserved via DynamicallyAccessedMembers on the Queryable<T> type parameter.")]
    [UnconditionalSuppressMessage("AOT", "IL2072", Justification = "Row types are preserved via DynamicallyAccessedMembers on the Queryable<T> type parameter.")]
    private static MemberInitExpression BuildMaterialization(Expression rowReference)
    {
        Type type = rowReference.Type;
        ConstructorInfo? ctor = type.GetConstructor(Type.EmptyTypes);

        if (ctor == null)
        {
            throw new NotSupportedException(
                $"Cannot pass `{rowReference}` of type {type.Name} directly to a client-side method inside a Select projection. " +
                $"{type.Name} has no parameterless constructor, so the framework cannot reconstruct it from the query result. " +
                $"Project the members you need explicitly (e.g. `.Select(x => new {{ x.Id, x.Name }})`) and call the method with those values, " +
                $"or materialize first with `.ToListAsync()` and call the method client-side.");
        }

        List<MemberBinding> bindings = [];
        foreach (PropertyInfo prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (prop.CanWrite && prop.GetIndexParameters().Length == 0)
            {
                bindings.Add(Expression.Bind(prop, Expression.Property(rowReference, prop)));
            }
        }

        return Expression.MemberInit(Expression.New(ctor), bindings);
    }
}
