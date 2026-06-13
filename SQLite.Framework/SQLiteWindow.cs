namespace SQLite.Framework;

/// <summary>
/// Marker type returned by every method on <see cref="SQLiteWindowFunctions" />.
/// Holds the value type <typeparamref name="T" /> and lets you chain window clauses
/// (PartitionBy, OrderBy, frame methods) without clashing with regular LINQ operators.
/// Convert back to <typeparamref name="T" /> using <see cref="AsValue" /> or the implicit operator.
/// </summary>
/// <remarks>
/// All members on this type throw at runtime. They are recognized by the SQLite translator
/// and turned into SQL inside a LINQ query.
/// </remarks>
public readonly struct SQLiteWindow<T>
{
    /// <summary>
    /// Unwraps the chain back to <typeparamref name="T" />.
    /// Use this in anonymous projections where the field type is inferred.
    /// </summary>
    public T AsValue()
    {
        throw new InvalidOperationException("SQLiteWindow<T>.AsValue can only be used inside a LINQ query.");
    }

    /// <summary>
    /// No-op kept for older code that called <c>.Over()</c> explicitly.
    /// The OVER clause is now opened automatically, so this just returns the receiver.
    /// </summary>
    public SQLiteWindow<T> Over()
    {
        throw new InvalidOperationException("SQLiteWindow<T> chain methods can only be used inside a LINQ query.");
    }

    /// <summary>
    /// Adds a <c>FILTER (WHERE ...)</c> clause so only rows matching <paramref name="predicate" />
    /// feed the aggregate. The clause is emitted between the function and <c>OVER</c>, so the
    /// chain order relative to the other window clauses does not matter. SQLite only allows
    /// <c>FILTER</c> on aggregate window functions (SUM, AVG, MIN, MAX, COUNT). It rejects it on
    /// ranking functions such as ROW_NUMBER.
    /// </summary>
#if SQLITE_FRAMEWORK_OS_BUNDLED_SQLITE
    [UnsupportedOSPlatform("android")]
    [SupportedOSPlatform("android31.0")]
    [UnsupportedOSPlatform("ios")]
    [SupportedOSPlatform("ios14.0")]
#endif
    public SQLiteWindow<T> Filter(bool predicate)
    {
        throw new InvalidOperationException("SQLiteWindow<T> chain methods can only be used inside a LINQ query.");
    }

    /// <summary>
    /// Adds a <c>PARTITION BY</c> clause to the window.
    /// </summary>
    public SQLiteWindow<T> PartitionBy<TKey>(TKey key)
    {
        throw new InvalidOperationException("SQLiteWindow<T> chain methods can only be used inside a LINQ query.");
    }

    /// <summary>
    /// Adds an additional partition column after <see cref="PartitionBy{TKey}" />.
    /// </summary>
    public SQLiteWindow<T> ThenPartitionBy<TKey>(TKey key)
    {
        throw new InvalidOperationException("SQLiteWindow<T> chain methods can only be used inside a LINQ query.");
    }

    /// <summary>
    /// Adds an <c>ORDER BY</c> clause to the window in ascending order.
    /// </summary>
    public SQLiteWindow<T> OrderBy<TKey>(TKey key)
    {
        throw new InvalidOperationException("SQLiteWindow<T> chain methods can only be used inside a LINQ query.");
    }

    /// <summary>
    /// Adds an <c>ORDER BY</c> clause to the window in descending order.
    /// </summary>
    public SQLiteWindow<T> OrderByDescending<TKey>(TKey key)
    {
        throw new InvalidOperationException("SQLiteWindow<T> chain methods can only be used inside a LINQ query.");
    }

    /// <summary>
    /// Adds an additional ascending sort key after <see cref="OrderBy{TKey}" /> or
    /// <see cref="OrderByDescending{TKey}" />.
    /// </summary>
    public SQLiteWindow<T> ThenOrderBy<TKey>(TKey key)
    {
        throw new InvalidOperationException("SQLiteWindow<T> chain methods can only be used inside a LINQ query.");
    }

    /// <summary>
    /// Adds an additional descending sort key after <see cref="OrderBy{TKey}" /> or
    /// <see cref="OrderByDescending{TKey}" />.
    /// </summary>
    public SQLiteWindow<T> ThenOrderByDescending<TKey>(TKey key)
    {
        throw new InvalidOperationException("SQLiteWindow<T> chain methods can only be used inside a LINQ query.");
    }

    /// <summary>
    /// Adds a <c>ROWS BETWEEN start AND end</c> frame clause to the window. Pass
    /// <paramref name="exclude" /> to leave rows near the current row out of the frame
    /// (needs SQLite 3.28.0 for any value other than <see cref="SQLiteFrameExclude.NoOthers" />).
    /// </summary>
    public SQLiteWindow<T> Rows(SQLiteFrameBoundary start, SQLiteFrameBoundary end, SQLiteFrameExclude exclude = SQLiteFrameExclude.NoOthers)
    {
        throw new InvalidOperationException("SQLiteWindow<T> chain methods can only be used inside a LINQ query.");
    }

    /// <summary>
    /// Adds a <c>RANGE BETWEEN start AND end</c> frame clause to the window. Pass
    /// <paramref name="exclude" /> to leave rows near the current row out of the frame
    /// (needs SQLite 3.28.0 for any value other than <see cref="SQLiteFrameExclude.NoOthers" />).
    /// </summary>
    public SQLiteWindow<T> Range(SQLiteFrameBoundary start, SQLiteFrameBoundary end, SQLiteFrameExclude exclude = SQLiteFrameExclude.NoOthers)
    {
        throw new InvalidOperationException("SQLiteWindow<T> chain methods can only be used inside a LINQ query.");
    }

    /// <summary>
    /// Adds a <c>GROUPS BETWEEN start AND end</c> frame clause to the window. Pass
    /// <paramref name="exclude" /> to leave rows near the current row out of the frame
    /// (needs SQLite 3.28.0 for any value other than <see cref="SQLiteFrameExclude.NoOthers" />).
    /// </summary>
    public SQLiteWindow<T> Groups(SQLiteFrameBoundary start, SQLiteFrameBoundary end, SQLiteFrameExclude exclude = SQLiteFrameExclude.NoOthers)
    {
        throw new InvalidOperationException("SQLiteWindow<T> chain methods can only be used inside a LINQ query.");
    }

    /// <summary>
    /// Implicit conversion to <typeparamref name="T" /> when the destination type is known
    /// (such as a typed projection field). For anonymous projections, use <see cref="AsValue" />.
    /// </summary>
    public static implicit operator T(SQLiteWindow<T> _)
    {
        throw new InvalidOperationException("SQLiteWindow<T> conversion can only be used inside a LINQ query.");
    }
}
