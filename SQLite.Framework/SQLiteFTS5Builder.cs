namespace SQLite.Framework;

/// <summary>
/// FTS5 query builder. Used as the lambda parameter of
/// <see cref="SQLiteFTS5Functions.Match{T}(T, Func{SQLiteFTS5Builder, bool})" /> so you can write
/// <c>f =&gt; f.Term("a") || f.Term("b")</c> without repeating the <c>SQLiteFTS5Functions.</c> prefix.
/// All methods on this type throw at runtime. They are only valid inside the LINQ expression tree.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class SQLiteFTS5Builder
{
    private const string OutsideQuery = "SQLiteFTS5Builder methods can only be used inside an SQLiteFTS5Functions.Match lambda.";

    internal SQLiteFTS5Builder()
    {
    }

    /// <summary>
    /// A bare term in an FTS5 query expression.
    /// </summary>
    public bool Term(string term)
    {
        throw new InvalidOperationException(OutsideQuery);
    }

    /// <summary>
    /// A quoted phrase in an FTS5 query expression. Translates to <c>"phrase"</c>.
    /// </summary>
    public bool Phrase(string phrase)
    {
        throw new InvalidOperationException(OutsideQuery);
    }

    /// <summary>
    /// A prefix term in an FTS5 query expression. Translates to <c>prefix*</c>.
    /// </summary>
    public bool Prefix(string prefix)
    {
        throw new InvalidOperationException(OutsideQuery);
    }

    /// <summary>
    /// Matches when the given terms appear within <paramref name="distance" /> tokens of each other.
    /// Translates to <c>NEAR(term1 term2 ..., distance)</c>.
    /// </summary>
    public bool Near(int distance, params string[] terms)
    {
        throw new InvalidOperationException(OutsideQuery);
    }

    /// <summary>
    /// Scopes the inner FTS5 query expression to a specific column.
    /// Translates to <c>{Column} : (&lt;subQuery&gt;)</c>.
    /// </summary>
    public bool Column(string column, bool subQuery)
    {
        throw new InvalidOperationException(OutsideQuery);
    }
}
