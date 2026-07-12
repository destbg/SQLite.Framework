namespace SQLite.Framework.Internals.Helpers;

/// <summary>
/// Turns <c>PRAGMA recursive_triggers</c> on around an <c>INSERT OR REPLACE</c> against a table
/// that feeds a trigger-synced FTS5 index, so the delete side of the REPLACE fires the sync
/// trigger and stale terms leave the index. Restores the previous pragma value on dispose.
/// Does nothing when the write is not a REPLACE, when the table feeds no trigger-synced FTS5
/// index or when the pragma is already on.
/// </summary>
internal readonly struct RecursiveTriggerScope : IDisposable
{
    private readonly SQLiteDatabase? database;

    public RecursiveTriggerScope(SQLiteDatabase database, TableMapping mapping, bool replaceWrite)
    {
        if (!replaceWrite || !mapping.HasFtsSyncTriggers || database.ExecuteScalar<int>("PRAGMA recursive_triggers") == 1)
        {
            this.database = null;
            return;
        }

        database.Execute("PRAGMA recursive_triggers = 1");
        this.database = database;
    }

    public void Dispose()
    {
        database?.Execute("PRAGMA recursive_triggers = 0");
    }
}
