namespace SQLite.Framework.Internals.Visitors;

/// <summary>
/// Walks a visited projection tree and reports whether it still holds a method call or a delegate
/// invocation. A translated piece has become a <see cref="SQLiteExpression" /> leaf by the time
/// this runs, so a remaining call is one that can only run in memory.
/// </summary>
internal sealed class ClientCallFinder : ExpressionVisitor
{
    public bool Found { get; private set; }

    public override Expression? Visit(Expression? node)
    {
        if (Found || node is SQLiteExpression)
        {
            return node;
        }

        if (node is MethodCallExpression or InvocationExpression)
        {
            Found = true;
            return node;
        }

        return base.Visit(node);
    }
}
