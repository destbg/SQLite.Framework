namespace SQLite.Framework.Internals.Visitors.Member;

internal static class NullableMemberVisitor
{
    public static Expression HandleNullableProperty(SQLVisitor visitor, string propertyName, Type type, SQLiteExpression node)
    {
        return propertyName switch
        {
            nameof(Nullable<>.HasValue) => SQLiteExpression.Wrap(type, visitor.Counters.NextIdentifier(), "(", node, " IS NOT NULL)", node.Parameters),
            _ => node
        };
    }

    [UnconditionalSuppressMessage("AOT", "IL2067", Justification = "Nullable<T> underlying value type has a parameterless constructor.")]
    public static Expression HandleGetValueOrDefault(SQLVisitor visitor, MethodCallExpression node, Type underlying)
    {
        ResolvedModel obj = visitor.ResolveExpression(node.Object!);

        Expression defaultArg = node.Arguments.Count == 0
            ? Expression.Constant(Activator.CreateInstance(underlying), underlying)
            : node.Arguments[0];

        ResolvedModel arg = visitor.ResolveExpression(defaultArg);

        if (obj.SQLiteExpression == null || arg.SQLiteExpression == null)
        {
            return node.Arguments.Count == 0
                ? Expression.Call(visitor.ToClientOperand(node.Object!, obj), node.Method)
                : Expression.Call(visitor.ToClientOperand(node.Object!, obj), node.Method, visitor.ToClientOperand(defaultArg, arg));
        }

        SQLiteParameter[]? parameters = ParameterHelpers.CombineParameters(obj.SQLiteExpression, arg.SQLiteExpression);
        return SQLiteExpression.Binary(underlying, visitor.Counters.NextIdentifier(), "COALESCE(", obj.SQLiteExpression, ", ", arg.SQLiteExpression, ")", parameters);
    }
}
