namespace SQLite.Framework;

/// <summary>
/// Wraps an <see cref="IQueryable{T}" /> source and a projection lambda so that the bulk
/// mutation methods emit a SQLite <c>RETURNING</c> clause and hand the projected rows back to
/// the caller. Obtain an instance through the <c>Returning</c> extension methods on <see cref="IQueryable{T}" />.
/// </summary>
/// <remarks>
/// <c>RETURNING</c> requires SQLite 3.35 or later.
/// </remarks>
public class SQLiteReturningQueryable<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] TResult>
{
    /// <summary>
    /// Initializes a new wrapper bound to the given source and projection.
    /// </summary>
    /// <param name="source">The underlying queryable that supplies <c>WHERE</c> and join shape.</param>
    /// <param name="projection">Projection emitted after the <c>RETURNING</c> keyword.</param>
    public SQLiteReturningQueryable(IQueryable<T> source, Expression<Func<T, TResult>> projection)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(projection);

        if (source is not BaseSQLiteQueryable table)
        {
            throw new InvalidOperationException($"Queryable must be of type {typeof(BaseSQLiteQueryable)}.");
        }

        Source = source;
        Projection = projection;
        Database = table.Database;
    }

    /// <summary>
    /// The original source queryable (carries <c>Where</c>, <c>Join</c>, and so on).
    /// </summary>
    public IQueryable<T> Source { get; }

    /// <summary>
    /// The projection emitted after the <c>RETURNING</c> keyword.
    /// </summary>
    public Expression<Func<T, TResult>> Projection { get; }

    /// <summary>
    /// The database that <see cref="Source" /> targets.
    /// </summary>
    public SQLiteDatabase Database { get; }
}
