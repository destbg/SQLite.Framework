namespace SQLite.Framework.Internals.Helpers;

/// <summary>
/// Guards table and column names that come from <c>[Table]</c> / <c>[Column]</c> attributes.
/// The framework wraps these names in double quotes when it writes them into SQL, so a double-quote
/// in a name could close the identifier and let the rest of the name run as SQL. The names are
/// normally compile-time constants, but rejecting the quote keeps the door shut if an application
/// ever derives a name from outside input.
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

    /// <summary>
    /// Wraps a column or table name in double quotes so SQLite reads it as a single identifier even
    /// when the name is a keyword or holds unusual characters. Any double-quote inside the name is
    /// doubled, the standard SQLite escape.
    /// </summary>
    public static string Quote(string name)
    {
        return $"\"{name.Replace("\"", "\"\"")}\"";
    }
}
