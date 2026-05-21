namespace SQLite.Framework.Extensions;

/// <summary>
/// Async wrappers for the typed pragma accessors on <see cref="SQLitePragmas" />.
/// Each method takes the connection lock and runs the sync version inside it.
/// </summary>
public static class AsyncPragmaExtensions
{
    /// <summary>
    /// Reads <c>PRAGMA foreign_keys</c>.
    /// </summary>
    public static Task<bool> GetForeignKeysAsync(this SQLitePragmas pragmas, CancellationToken ct = default)
    {
        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await pragmas.Database.LockAsync(ct);
            return pragmas.ForeignKeys;
        }, ct);
    }

    /// <summary>
    /// Writes <c>PRAGMA foreign_keys</c>.
    /// </summary>
    public static Task SetForeignKeysAsync(this SQLitePragmas pragmas, bool value, CancellationToken ct = default)
    {
        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await pragmas.Database.LockAsync(ct);
            pragmas.ForeignKeys = value;
        }, ct);
    }

    /// <summary>
    /// Reads <c>PRAGMA journal_mode</c>.
    /// </summary>
    public static Task<string> GetJournalModeAsync(this SQLitePragmas pragmas, CancellationToken ct = default)
    {
        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await pragmas.Database.LockAsync(ct);
            return pragmas.JournalMode;
        }, ct);
    }

    /// <summary>
    /// Writes <c>PRAGMA journal_mode</c>.
    /// </summary>
    public static Task SetJournalModeAsync(this SQLitePragmas pragmas, string value, CancellationToken ct = default)
    {
        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await pragmas.Database.LockAsync(ct);
            pragmas.JournalMode = value;
        }, ct);
    }

    /// <summary>
    /// Reads <c>PRAGMA cache_size</c>.
    /// </summary>
    public static Task<int> GetCacheSizeAsync(this SQLitePragmas pragmas, CancellationToken ct = default)
    {
        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await pragmas.Database.LockAsync(ct);
            return pragmas.CacheSize;
        }, ct);
    }

    /// <summary>
    /// Writes <c>PRAGMA cache_size</c>.
    /// </summary>
    public static Task SetCacheSizeAsync(this SQLitePragmas pragmas, int value, CancellationToken ct = default)
    {
        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await pragmas.Database.LockAsync(ct);
            pragmas.CacheSize = value;
        }, ct);
    }

    /// <summary>
    /// Reads <c>PRAGMA synchronous</c>.
    /// </summary>
    public static Task<SQLiteSynchronousMode> GetSynchronousModeAsync(this SQLitePragmas pragmas, CancellationToken ct = default)
    {
        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await pragmas.Database.LockAsync(ct);
            return pragmas.SynchronousMode;
        }, ct);
    }

    /// <summary>
    /// Writes <c>PRAGMA synchronous</c>.
    /// </summary>
    public static Task SetSynchronousModeAsync(this SQLitePragmas pragmas, SQLiteSynchronousMode value, CancellationToken ct = default)
    {
        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await pragmas.Database.LockAsync(ct);
            pragmas.SynchronousMode = value;
        }, ct);
    }

    /// <summary>
    /// Reads <c>PRAGMA user_version</c>.
    /// </summary>
    public static Task<int> GetUserVersionAsync(this SQLitePragmas pragmas, CancellationToken ct = default)
    {
        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await pragmas.Database.LockAsync(ct);
            return pragmas.UserVersion;
        }, ct);
    }

    /// <summary>
    /// Writes <c>PRAGMA user_version</c>.
    /// </summary>
    public static Task SetUserVersionAsync(this SQLitePragmas pragmas, int value, CancellationToken ct = default)
    {
        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await pragmas.Database.LockAsync(ct);
            pragmas.UserVersion = value;
        }, ct);
    }

    /// <summary>
    /// Reads <c>PRAGMA page_size</c>.
    /// </summary>
    public static Task<long> GetPageSizeAsync(this SQLitePragmas pragmas, CancellationToken ct = default)
    {
        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await pragmas.Database.LockAsync(ct);
            return pragmas.PageSize;
        }, ct);
    }

    /// <summary>
    /// Reads <c>PRAGMA freelist_count</c>.
    /// </summary>
    public static Task<long> GetFreelistCountAsync(this SQLitePragmas pragmas, CancellationToken ct = default)
    {
        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await pragmas.Database.LockAsync(ct);
            return pragmas.FreelistCount;
        }, ct);
    }

    /// <summary>
    /// Reads <c>PRAGMA recursive_triggers</c>.
    /// </summary>
    public static Task<bool> GetRecursiveTriggersAsync(this SQLitePragmas pragmas, CancellationToken ct = default)
    {
        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await pragmas.Database.LockAsync(ct);
            return pragmas.RecursiveTriggers;
        }, ct);
    }

    /// <summary>
    /// Writes <c>PRAGMA recursive_triggers</c>.
    /// </summary>
    public static Task SetRecursiveTriggersAsync(this SQLitePragmas pragmas, bool value, CancellationToken ct = default)
    {
        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await pragmas.Database.LockAsync(ct);
            pragmas.RecursiveTriggers = value;
        }, ct);
    }

    /// <summary>
    /// Reads <c>PRAGMA temp_store</c>.
    /// </summary>
    public static Task<int> GetTempStoreAsync(this SQLitePragmas pragmas, CancellationToken ct = default)
    {
        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await pragmas.Database.LockAsync(ct);
            return pragmas.TempStore;
        }, ct);
    }

    /// <summary>
    /// Writes <c>PRAGMA temp_store</c>.
    /// </summary>
    public static Task SetTempStoreAsync(this SQLitePragmas pragmas, int value, CancellationToken ct = default)
    {
        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await pragmas.Database.LockAsync(ct);
            pragmas.TempStore = value;
        }, ct);
    }

    /// <summary>
    /// Reads <c>PRAGMA secure_delete</c>.
    /// </summary>
    public static Task<bool> GetSecureDeleteAsync(this SQLitePragmas pragmas, CancellationToken ct = default)
    {
        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await pragmas.Database.LockAsync(ct);
            return pragmas.SecureDelete;
        }, ct);
    }

    /// <summary>
    /// Writes <c>PRAGMA secure_delete</c>.
    /// </summary>
    public static Task SetSecureDeleteAsync(this SQLitePragmas pragmas, bool value, CancellationToken ct = default)
    {
        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await pragmas.Database.LockAsync(ct);
            pragmas.SecureDelete = value;
        }, ct);
    }

    /// <summary>
    /// Reads <c>PRAGMA busy_timeout</c>.
    /// </summary>
    public static Task<int> GetBusyTimeoutAsync(this SQLitePragmas pragmas, CancellationToken ct = default)
    {
        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await pragmas.Database.LockAsync(ct);
            return pragmas.BusyTimeout;
        }, ct);
    }

    /// <summary>
    /// Writes <c>PRAGMA busy_timeout</c>.
    /// </summary>
    public static Task SetBusyTimeoutAsync(this SQLitePragmas pragmas, int value, CancellationToken ct = default)
    {
        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await pragmas.Database.LockAsync(ct);
            pragmas.BusyTimeout = value;
        }, ct);
    }

    /// <summary>
    /// Reads <c>PRAGMA mmap_size</c>.
    /// </summary>
    public static Task<long> GetMmapSizeAsync(this SQLitePragmas pragmas, CancellationToken ct = default)
    {
        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await pragmas.Database.LockAsync(ct);
            return pragmas.MmapSize;
        }, ct);
    }

    /// <summary>
    /// Writes <c>PRAGMA mmap_size</c>.
    /// </summary>
    public static Task SetMmapSizeAsync(this SQLitePragmas pragmas, long value, CancellationToken ct = default)
    {
        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await pragmas.Database.LockAsync(ct);
            pragmas.MmapSize = value;
        }, ct);
    }

    /// <summary>
    /// Reads <c>PRAGMA auto_vacuum</c>.
    /// </summary>
    public static Task<SQLiteAutoVacuumMode> GetAutoVacuumAsync(this SQLitePragmas pragmas, CancellationToken ct = default)
    {
        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await pragmas.Database.LockAsync(ct);
            return pragmas.AutoVacuum;
        }, ct);
    }

    /// <summary>
    /// Writes <c>PRAGMA auto_vacuum</c>.
    /// </summary>
    public static Task SetAutoVacuumAsync(this SQLitePragmas pragmas, SQLiteAutoVacuumMode value, CancellationToken ct = default)
    {
        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await pragmas.Database.LockAsync(ct);
            pragmas.AutoVacuum = value;
        }, ct);
    }

    /// <summary>
    /// Runs <c>PRAGMA incremental_vacuum</c>.
    /// </summary>
    public static Task IncrementalVacuumAsync(this SQLitePragmas pragmas, int? pages = null, CancellationToken ct = default)
    {
        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await pragmas.Database.LockAsync(ct);
            pragmas.IncrementalVacuum(pages);
        }, ct);
    }

    /// <summary>
    /// Reads <c>PRAGMA wal_autocheckpoint</c>.
    /// </summary>
    public static Task<int> GetWalAutoCheckpointAsync(this SQLitePragmas pragmas, CancellationToken ct = default)
    {
        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await pragmas.Database.LockAsync(ct);
            return pragmas.WalAutoCheckpoint;
        }, ct);
    }

    /// <summary>
    /// Writes <c>PRAGMA wal_autocheckpoint</c>.
    /// </summary>
    public static Task SetWalAutoCheckpointAsync(this SQLitePragmas pragmas, int value, CancellationToken ct = default)
    {
        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await pragmas.Database.LockAsync(ct);
            pragmas.WalAutoCheckpoint = value;
        }, ct);
    }

    /// <summary>
    /// Runs <c>PRAGMA wal_checkpoint(MODE)</c>.
    /// </summary>
    public static Task<bool> WalCheckpointAsync(this SQLitePragmas pragmas, SQLiteWalCheckpointMode mode = SQLiteWalCheckpointMode.Passive, CancellationToken ct = default)
    {
        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await pragmas.Database.LockAsync(ct);
            return pragmas.WalCheckpoint(mode);
        }, ct);
    }

    /// <summary>
    /// Runs <c>PRAGMA integrity_check</c>.
    /// </summary>
    public static Task<IReadOnlyList<string>> IntegrityCheckAsync(this SQLitePragmas pragmas, CancellationToken ct = default)
    {
        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await pragmas.Database.LockAsync(ct);
            return pragmas.IntegrityCheck();
        }, ct);
    }

    /// <summary>
    /// Runs <c>PRAGMA quick_check</c>.
    /// </summary>
    public static Task<IReadOnlyList<string>> QuickCheckAsync(this SQLitePragmas pragmas, CancellationToken ct = default)
    {
        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await pragmas.Database.LockAsync(ct);
            return pragmas.QuickCheck();
        }, ct);
    }

    /// <summary>
    /// Runs <c>PRAGMA optimize</c>.
    /// </summary>
    public static Task OptimizeAsync(this SQLitePragmas pragmas, CancellationToken ct = default)
    {
        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await pragmas.Database.LockAsync(ct);
            pragmas.Optimize();
        }, ct);
    }

    /// <summary>
    /// Reads <c>PRAGMA defer_foreign_keys</c>.
    /// </summary>
    public static Task<bool> GetDeferForeignKeysAsync(this SQLitePragmas pragmas, CancellationToken ct = default)
    {
        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await pragmas.Database.LockAsync(ct);
            return pragmas.DeferForeignKeys;
        }, ct);
    }

    /// <summary>
    /// Writes <c>PRAGMA defer_foreign_keys</c>.
    /// </summary>
    public static Task SetDeferForeignKeysAsync(this SQLitePragmas pragmas, bool value, CancellationToken ct = default)
    {
        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await pragmas.Database.LockAsync(ct);
            pragmas.DeferForeignKeys = value;
        }, ct);
    }

    /// <summary>
    /// Reads <c>PRAGMA encoding</c>.
    /// </summary>
    public static Task<SQLiteEncoding> GetEncodingAsync(this SQLitePragmas pragmas, CancellationToken ct = default)
    {
        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await pragmas.Database.LockAsync(ct);
            return pragmas.Encoding;
        }, ct);
    }

    /// <summary>
    /// Writes <c>PRAGMA encoding</c>.
    /// </summary>
    public static Task SetEncodingAsync(this SQLitePragmas pragmas, SQLiteEncoding value, CancellationToken ct = default)
    {
        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await pragmas.Database.LockAsync(ct);
            pragmas.Encoding = value;
        }, ct);
    }

    /// <summary>
    /// Reads <c>PRAGMA locking_mode</c>.
    /// </summary>
    public static Task<SQLiteLockingMode> GetLockingModeAsync(this SQLitePragmas pragmas, CancellationToken ct = default)
    {
        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await pragmas.Database.LockAsync(ct);
            return pragmas.LockingMode;
        }, ct);
    }

    /// <summary>
    /// Writes <c>PRAGMA locking_mode</c>.
    /// </summary>
    public static Task SetLockingModeAsync(this SQLitePragmas pragmas, SQLiteLockingMode value, CancellationToken ct = default)
    {
        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await pragmas.Database.LockAsync(ct);
            pragmas.LockingMode = value;
        }, ct);
    }

    /// <summary>
    /// Reads <c>PRAGMA application_id</c>.
    /// </summary>
    public static Task<int> GetApplicationIdAsync(this SQLitePragmas pragmas, CancellationToken ct = default)
    {
        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await pragmas.Database.LockAsync(ct);
            return pragmas.ApplicationId;
        }, ct);
    }

    /// <summary>
    /// Writes <c>PRAGMA application_id</c>.
    /// </summary>
    public static Task SetApplicationIdAsync(this SQLitePragmas pragmas, int value, CancellationToken ct = default)
    {
        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await pragmas.Database.LockAsync(ct);
            pragmas.ApplicationId = value;
        }, ct);
    }

    /// <summary>
    /// Reads <c>PRAGMA data_version</c>.
    /// </summary>
    public static Task<int> GetDataVersionAsync(this SQLitePragmas pragmas, CancellationToken ct = default)
    {
        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await pragmas.Database.LockAsync(ct);
            return pragmas.DataVersion;
        }, ct);
    }

    /// <summary>
    /// Reads <c>PRAGMA schema_version</c>.
    /// </summary>
    public static Task<int> GetSchemaVersionAsync(this SQLitePragmas pragmas, CancellationToken ct = default)
    {
        return AsyncRunner.Run(async () =>
        {
            using IDisposable _ = await pragmas.Database.LockAsync(ct);
            return pragmas.SchemaVersion;
        }, ct);
    }
}
