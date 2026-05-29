namespace SQLite.Framework.Internals.Helpers;

/// <summary>
/// Guards table and column names that come from <c>[Table]</c> / <c>[Column]</c> attributes.
/// The framework writes these names straight into DDL (quoted for tables, bare for columns), so a
/// double-quote in a name could close the identifier and let the rest of the name run as SQL. The
/// names are normally compile-time constants, but rejecting the quote keeps the door shut if an
/// application ever derives a name from outside input.
/// </summary>
internal static class IdentifierGuard
{
    public static void EnsureNoQuote(string name, string kind)
    {
        if (name.Contains('"'))
        {
            throw new ArgumentException(
                $"{kind} name '{name}' contains a double-quote character, which is not allowed.");
        }
    }
}
