namespace SQLite.Framework.Models;

/// <summary>
/// Base class for any queryable owned by the framework. Holds the
/// <see cref="SQLiteDatabase" /> the queryable belongs to. Subclasses include mapped tables
/// (see <see cref="BaseSQLiteTable" />), CTEs (see <see cref="SQLiteCte" />), and the
/// internal LINQ chain wrapper.
/// </summary>
public abstract class BaseSQLiteQueryable : IQueryable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BaseSQLiteQueryable"/> class.
    /// </summary>
    protected BaseSQLiteQueryable(SQLiteDatabase database)
    {
        Database = database;
    }

    /// <summary>
    /// The SQLite database this queryable belongs to.
    /// </summary>
    public SQLiteDatabase Database { get; }

    /// <inheritdoc />
    public abstract Type ElementType { get; }

    /// <inheritdoc />
    public abstract Expression Expression { get; }

    /// <inheritdoc />
    public abstract IQueryProvider Provider { get; }

    /// <inheritdoc />
    public abstract IEnumerator GetEnumerator();
}
