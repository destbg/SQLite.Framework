namespace SQLite.Framework;

/// <summary>
/// Represents a transaction in SQLite.
/// </summary>
public class SQLiteTransaction : IDisposable
{
    private bool disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="SQLiteTransaction"/> class.
    /// </summary>
    public SQLiteTransaction(SQLiteDatabase database, string savepointName)
    {
        Database = database;
        SavepointName = savepointName;
    }

    /// <summary>
    /// The SQLite database.
    /// </summary>
    public SQLiteDatabase Database { get; }

    /// <summary>
    /// The name of the savepoint.
    /// </summary>
    public string SavepointName { get; }

    /// <summary>
    /// Commits the transaction.
    /// </summary>
    public void Commit()
    {
        disposed = true;
        Database.CreateCommand($"RELEASE {SavepointName}", []).ExecuteNonQuery();
    }

    /// <summary>
    /// Rolls back the transaction.
    /// </summary>
    public void Rollback()
    {
        disposed = true;
        Database.CreateCommand($"ROLLBACK TO {SavepointName}", []).ExecuteNonQuery();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        Rollback();
    }
}