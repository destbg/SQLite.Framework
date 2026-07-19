namespace SQLite.Framework.Internals.Visitors;

/// <summary>
/// Expands table-row references that appear as method-call arguments into
/// <see cref="MemberInitExpression" />s that reconstruct the row from its individual members.
/// </summary>
internal sealed class RowParameterExpanderVisitor : ExpressionVisitor
{
    private readonly HashSet<ParameterExpression> rowParameters;

    public RowParameterExpanderVisitor(HashSet<ParameterExpression> rowParameters)
    {
        this.rowParameters = rowParameters;
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        if (node.Expression != null
            && node.Member is PropertyInfo prop
            && IsUnmappedProperty(prop)
            && IsConstructibleEntityType(node.Expression.Type)
            && LooksLikeRowReference(node.Expression))
        {
            if (node.Expression is not ParameterExpression)
            {
                throw new NotSupportedException(
                    $"Cannot read the computed member '{node.Member.Name}' off '{node.Expression}' in this projection. " +
                    $"The row is reached through another projection, such as a join result, so the framework cannot " +
                    $"reconstruct '{node.Expression.Type.Name}' to compute the member. Project the columns you need instead.");
            }

            return Expression.MakeMemberAccess(BuildMaterialization(node.Expression), node.Member);
        }

        return base.VisitMember(node);
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        bool substituteRowArguments = !IsFrameworkTranslatedMethod(node.Method);
        Expression? newObject;
        if (node.Object != null && substituteRowArguments && IsExpandableRowReceiver(node.Object))
        {
            newObject = BuildMaterialization(node.Object);
        }
        else
        {
            newObject = node.Object != null ? Visit(node.Object) : null;
        }

        Expression[] newArgs = new Expression[node.Arguments.Count];
        bool stringConcatMethod = node.Method.DeclaringType == typeof(string)
            && node.Method.Name is nameof(string.Join) or nameof(string.Concat);

        for (int i = 0; i < node.Arguments.Count; i++)
        {
            Expression arg = node.Arguments[i];
            if (substituteRowArguments && LooksLikeRowReference(arg) && !(stringConcatMethod && IsGroupingType(arg.Type)))
            {
                newArgs[i] = stringConcatMethod || !HasParameterlessConstructor(arg.Type)
                    ? BuildMaterialization(arg)
                    : arg;
            }
            else
            {
                newArgs[i] = Visit(arg);
            }
        }

        return node.Update(newObject, newArgs);
    }

    protected override Expression VisitInvocation(InvocationExpression node)
    {
        Expression target = Visit(node.Expression);
        Expression[] newArgs = new Expression[node.Arguments.Count];
        for (int i = 0; i < node.Arguments.Count; i++)
        {
            Expression arg = node.Arguments[i];
            newArgs[i] = LooksLikeRowReference(arg) ? BuildMaterialization(arg) : Visit(arg);
        }

        return node.Update(target, newArgs);
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

    private bool IsExpandableRowReceiver(Expression expression)
    {
        return expression is ParameterExpression pe
            && rowParameters.Contains(pe)
            && IsConstructibleEntityType(pe.Type);
    }

    private static bool IsUnmappedProperty(PropertyInfo property)
    {
        if (property.GetCustomAttribute<System.ComponentModel.DataAnnotations.Schema.NotMappedAttribute>() != null)
        {
            return true;
        }

        return property.SetMethod == null
            && property.GetMethod is { } getter
            && !getter.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false);
    }

    [UnconditionalSuppressMessage("AOT", "IL2070", Justification = "Row types are preserved by Queryable<T>.")]
    private static bool IsConstructibleEntityType(Type type)
    {
        if (type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal))
        {
            return false;
        }
        else if (type.IsArray || typeof(IEnumerable).IsAssignableFrom(type))
        {
            return false;
        }

        return type.GetConstructor(Type.EmptyTypes) != null;
    }

    private static bool IsGroupingType(Type type)
    {
        return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IGrouping<,>);
    }

    [UnconditionalSuppressMessage("AOT", "IL2070", Justification = "Row types are preserved by Queryable<T>.")]
    private static bool HasParameterlessConstructor(Type type)
    {
        return type.GetConstructor(Type.EmptyTypes) != null;
    }

    private static bool IsFrameworkTranslatedMethod(MethodInfo method)
    {
        Type? declaringType = method.DeclaringType;
        if (declaringType == null)
        {
            return false;
        }

        if (declaringType.IsGenericType && declaringType.GetGenericTypeDefinition() == typeof(SQLiteWindow<>))
        {
            return true;
        }

        return declaringType == typeof(System.Linq.Queryable)
            || declaringType == typeof(Enumerable)
            || declaringType == typeof(SQLiteColumn)
            || declaringType == typeof(SQLiteFunctions)
            || declaringType == typeof(SQLiteDateFunctions)
            || declaringType == typeof(SQLiteJsonFunctions)
            || declaringType == typeof(SQLiteFTS5Functions)
            || declaringType == typeof(SQLiteWindowFunctions)
            || declaringType == typeof(SQLiteFrameBoundary)
            || declaringType.Namespace == "SQLite.Framework.Extensions";
    }

    [UnconditionalSuppressMessage("AOT", "IL2075", Justification = "Row types are preserved by Queryable<T>.")]
    [UnconditionalSuppressMessage("AOT", "IL2072", Justification = "Row types are preserved by Queryable<T>.")]
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
            if (prop.CanWrite
                && prop.GetCustomAttribute<System.ComponentModel.DataAnnotations.Schema.NotMappedAttribute>() == null)
            {
                bindings.Add(Expression.Bind(prop, Expression.Property(rowReference, prop)));
            }
        }

        return Expression.MemberInit(Expression.New(ctor), bindings);
    }
}
