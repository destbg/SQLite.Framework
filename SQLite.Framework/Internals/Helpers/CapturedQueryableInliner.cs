namespace SQLite.Framework.Internals.Helpers;

/// <summary>
/// Inlines captured <see cref="Queryable{T}" /> wrappers into the LINQ expression tree before translation.
/// </summary>
internal static class CapturedQueryableInliner
{
    public static Expression Inline(Expression node)
    {
        return new CapturedQueryableInlinerVisitor().Visit(node);
    }
}
