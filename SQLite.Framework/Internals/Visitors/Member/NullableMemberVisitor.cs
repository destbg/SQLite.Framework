namespace SQLite.Framework.Internals.Visitors.Member;

internal static class NullableMemberVisitor
{
    public static Expression HandleNullableProperty(SQLVisitor visitor, string propertyName, Type type, SQLiteExpression node)
    {
        return propertyName switch
        {
            nameof(Nullable<>.HasValue) => new SQLiteExpression(
                type,
                visitor.Counters.IdentifierIndex++,
                $"({node.Sql} IS NOT NULL)",
                node.Parameters
            ),
            _ => node
        };
    }

    [UnconditionalSuppressMessage("AOT", "IL2067", Justification = "underlying is the unwrapped value type from a Nullable<T>, which always has a public parameterless constructor.")]
    public static SQLiteExpression HandleGetValueOrDefault(SQLVisitor visitor, MethodCallExpression node, Type underlying)
    {
        ResolvedModel obj = visitor.ResolveExpression(node.Object!);

        Expression defaultArg = node.Arguments.Count == 0
            ? Expression.Constant(Activator.CreateInstance(underlying), underlying)
            : node.Arguments[0];

        ResolvedModel arg = visitor.ResolveExpression(defaultArg);
        SQLiteParameter[]? parameters = ParameterHelpers.CombineParameters(obj.SQLiteExpression!, arg.SQLiteExpression!);
        return new SQLiteExpression(
            underlying,
            visitor.Counters.IdentifierIndex++,
            $"COALESCE({obj.Sql}, {arg.Sql})",
            parameters
        );
    }
}
