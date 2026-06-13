namespace SQLite.Framework.Internals.Helpers;

/// <summary>
/// Rebinds a filter lambda from an interface or base type onto a concrete entity type.
/// </summary>
internal static class QueryFilterRebinder
{
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Entity types are preserved by the user Table<T>().")]
    public static LambdaExpression Rebind(LambdaExpression source, Type entityType)
    {
        ParameterExpression oldP = source.Parameters[0];
        if (oldP.Type == entityType)
        {
            return source;
        }

        ParameterExpression newP = Expression.Parameter(entityType, oldP.Name);
        QueryFilterRebinderVisitor visitor = new(oldP, newP);
        Expression body = visitor.Visit(source.Body)!;
        Type funcType = typeof(Func<,>).MakeGenericType(entityType, typeof(bool));
        return Expression.Lambda(funcType, body, newP);
    }
}
