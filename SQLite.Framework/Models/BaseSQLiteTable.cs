namespace SQLite.Framework.Models;

/// <summary>
/// Base class for any queryable that maps directly to a SQL table. This includes regular
/// tables, read-only tables, and built-in system tables. CTEs and LINQ chain wrappers do
/// not inherit from this class; they go through <see cref="BaseSQLiteQueryable" /> instead.
/// </summary>
public abstract class BaseSQLiteTable : BaseSQLiteQueryable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BaseSQLiteTable"/> class.
    /// </summary>
    protected BaseSQLiteTable(SQLiteDatabase database, TableMapping table)
        : base(database)
    {
        Table = table;
    }

    /// <summary>
    /// The mapping between the entity type and the SQL table.
    /// </summary>
    public TableMapping Table { get; }
}
