namespace SQLite.Framework;

/// <summary>
/// Represents a transaction in SQLite.
/// </summary>
public class SQLiteTransaction : IDisposable
{
    private readonly SQLiteDatabase database;
    private readonly string savepointName;
    private bool disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="SQLiteTransaction"/> class.
    /// </summary>
    public SQLiteTransaction(SQLiteDatabase database, string savepointName)
    {
        this.database = database;
        this.savepointName = savepointName;
    }

    /// <summary>
    /// Commits the transaction.
    /// </summary>
    public void Commit()
    {
        database.CreateCommand($"RELEASE {savepointName}", []).ExecuteNonQuery();
        disposed = true;
    }

    /// <summary>
    /// Rolls back the transaction.
    /// </summary>
    public void Rollback()
    {
        database.CreateCommand($"ROLLBACK TO {savepointName}", []).ExecuteNonQuery();
        disposed = true;
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