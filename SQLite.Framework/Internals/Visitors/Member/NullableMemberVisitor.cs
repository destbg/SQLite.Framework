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

    public static SQLiteExpression HandleGetValueOrDefault(SQLVisitor visitor, MethodCallExpression node, Type underlying)
    {
        ResolvedModel obj = visitor.ResolveExpression(node.Object!);

        if (node.Arguments.Count == 0)
        {
            return new SQLiteExpression(
                underlying,
                visitor.Counters.IdentifierIndex++,
                $"COALESCE({obj.Sql}, 0)",
                obj.SQLiteExpression!.Parameters
            );
        }

        ResolvedModel arg = visitor.ResolveExpression(node.Arguments[0]);
        SQLiteParameter[]? parameters = ParameterHelpers.CombineParameters(obj.SQLiteExpression!, arg.SQLiteExpression!);
        return new SQLiteExpression(
            underlying,
            visitor.Counters.IdentifierIndex++,
            $"COALESCE({obj.Sql}, {arg.Sql})",
            parameters
        );
    }
}
