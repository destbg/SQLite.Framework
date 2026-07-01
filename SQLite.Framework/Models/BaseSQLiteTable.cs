namespace SQLite.Framework.Models;

/// <summary>
/// Base class for any queryable that maps directly to a SQL table. Covers regular tables,
/// read-only tables and built-in system tables. CTEs and LINQ chain wrappers go through
/// <see cref="BaseSQLiteQueryable" /> instead.
/// </summary>
public abstract class BaseSQLiteTable : BaseSQLiteQueryable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BaseSQLiteTable"/> class.
    /// </summary>
    protected BaseSQLiteTable(SQLiteDatabase database, TableMapping table, string? schemaName = null)
        : base(database)
    {
        Table = table;
        SchemaName = schemaName;
    }

    /// <summary>
    /// The mapping between the entity type and the SQL table.
    /// </summary>
    public TableMapping Table { get; }

    /// <summary>
    /// The attached database schema this table is read from or <see langword="null" /> for the main database.
    /// Set when the table is created through <see cref="SQLiteDatabase.Table{T}(string)" />.
    /// When the table belongs to a different <see cref="SQLiteDatabase" /> than the one running the query,
    /// the schema is resolved from <see cref="SQLiteDatabase.AttachDatabase(SQLiteDatabase, string)" /> instead.
    /// </summary>
    public string? SchemaName { get; }
}
