using System.Collections;
using System.Linq.Expressions;

namespace SQLite.Framework.Models;

/// <summary>
/// Represents a base class for SQLite tables.
/// </summary>
public abstract class BaseSQLiteTable : IQueryable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BaseSQLiteTable"/> class.
    /// </summary>
    protected BaseSQLiteTable(SQLiteDatabase database)
    {
        Database = database;
    }

    /// <summary>
    /// The SQLite database.
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
