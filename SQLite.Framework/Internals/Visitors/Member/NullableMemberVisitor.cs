namespace SQLite.Framework.Internals.Visitors;

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
}
