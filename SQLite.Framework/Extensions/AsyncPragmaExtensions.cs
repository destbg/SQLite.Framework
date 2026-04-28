namespace SQLite.Framework.Extensions;

/// <summary>
/// Async wrappers for the typed pragma accessors on <see cref="SQLitePragmas" />. Each call runs
/// the matching read or write on a background thread.
/// </summary>
[ExcludeFromCodeCoverage]
public static class AsyncPragmaExtensions
{
    /// <summary>
    /// Reads <c>PRAGMA foreign_keys</c> on a background thread.
    /// </summary>
    public static Task<bool> GetForeignKeysAsync(this SQLitePragmas pragmas, CancellationToken ct = default)
    {
        return AsyncRunner.Run(() => pragmas.ForeignKeys, ct);
    }

    /// <summary>
    /// Writes <c>PRAGMA foreign_keys</c> on a background thread.
    /// </summary>
    public static Task SetForeignKeysAsync(this SQLitePragmas pragmas, bool value, CancellationToken ct = default)
    {
        return AsyncRunner.Run(() => pragmas.ForeignKeys = value, ct);
    }

    /// <summary>
    /// Reads <c>PRAGMA journal_mode</c> on a background thread.
    /// </summary>
    public static Task<string> GetJournalModeAsync(this SQLitePragmas pragmas, CancellationToken ct = default)
    {
        return AsyncRunner.Run(() => pragmas.JournalMode, ct);
    }

    /// <summary>
    /// Writes <c>PRAGMA journal_mode</c> on a background thread.
    /// </summary>
    public static Task SetJournalModeAsync(this SQLitePragmas pragmas, string value, CancellationToken ct = default)
    {
        return AsyncRunner.Run(() => pragmas.JournalMode = value, ct);
    }

    /// <summary>
    /// Reads <c>PRAGMA cache_size</c> on a background thread.
    /// </summary>
    public static Task<int> GetCacheSizeAsync(this SQLitePragmas pragmas, CancellationToken ct = default)
    {
        return AsyncRunner.Run(() => pragmas.CacheSize, ct);
    }

    /// <summary>
    /// Writes <c>PRAGMA cache_size</c> on a background thread.
    /// </summary>
    public static Task SetCacheSizeAsync(this SQLitePragmas pragmas, int value, CancellationToken ct = default)
    {
        return AsyncRunner.Run(() => pragmas.CacheSize = value, ct);
    }

    /// <summary>
    /// Reads <c>PRAGMA synchronous</c> on a background thread.
    /// </summary>
    public static Task<SQLiteSynchronousMode> GetSynchronousModeAsync(this SQLitePragmas pragmas, CancellationToken ct = default)
    {
        return AsyncRunner.Run(() => pragmas.SynchronousMode, ct);
    }

    /// <summary>
    /// Writes <c>PRAGMA synchronous</c> on a background thread.
    /// </summary>
    public static Task SetSynchronousModeAsync(this SQLitePragmas pragmas, SQLiteSynchronousMode value, CancellationToken ct = default)
    {
        return AsyncRunner.Run(() => pragmas.SynchronousMode = value, ct);
    }

    /// <summary>
    /// Reads <c>PRAGMA user_version</c> on a background thread.
    /// </summary>
    public static Task<int> GetUserVersionAsync(this SQLitePragmas pragmas, CancellationToken ct = default)
    {
        return AsyncRunner.Run(() => pragmas.UserVersion, ct);
    }

    /// <summary>
    /// Writes <c>PRAGMA user_version</c> on a background thread.
    /// </summary>
    public static Task SetUserVersionAsync(this SQLitePragmas pragmas, int value, CancellationToken ct = default)
    {
        return AsyncRunner.Run(() => pragmas.UserVersion = value, ct);
    }

    /// <summary>
    /// Reads <c>PRAGMA page_size</c> on a background thread.
    /// </summary>
    public static Task<long> GetPageSizeAsync(this SQLitePragmas pragmas, CancellationToken ct = default)
    {
        return AsyncRunner.Run(() => pragmas.PageSize, ct);
    }

    /// <summary>
    /// Reads <c>PRAGMA freelist_count</c> on a background thread.
    /// </summary>
    public static Task<long> GetFreelistCountAsync(this SQLitePragmas pragmas, CancellationToken ct = default)
    {
        return AsyncRunner.Run(() => pragmas.FreelistCount, ct);
    }

    /// <summary>
    /// Reads <c>PRAGMA recursive_triggers</c> on a background thread.
    /// </summary>
    public static Task<bool> GetRecursiveTriggersAsync(this SQLitePragmas pragmas, CancellationToken ct = default)
    {
        return AsyncRunner.Run(() => pragmas.RecursiveTriggers, ct);
    }

    /// <summary>
    /// Writes <c>PRAGMA recursive_triggers</c> on a background thread.
    /// </summary>
    public static Task SetRecursiveTriggersAsync(this SQLitePragmas pragmas, bool value, CancellationToken ct = default)
    {
        return AsyncRunner.Run(() => pragmas.RecursiveTriggers = value, ct);
    }

    /// <summary>
    /// Reads <c>PRAGMA temp_store</c> on a background thread.
    /// </summary>
    public static Task<int> GetTempStoreAsync(this SQLitePragmas pragmas, CancellationToken ct = default)
    {
        return AsyncRunner.Run(() => pragmas.TempStore, ct);
    }

    /// <summary>
    /// Writes <c>PRAGMA temp_store</c> on a background thread.
    /// </summary>
    public static Task SetTempStoreAsync(this SQLitePragmas pragmas, int value, CancellationToken ct = default)
    {
        return AsyncRunner.Run(() => pragmas.TempStore = value, ct);
    }

    /// <summary>
    /// Reads <c>PRAGMA secure_delete</c> on a background thread.
    /// </summary>
    public static Task<bool> GetSecureDeleteAsync(this SQLitePragmas pragmas, CancellationToken ct = default)
    {
        return AsyncRunner.Run(() => pragmas.SecureDelete, ct);
    }

    /// <summary>
    /// Writes <c>PRAGMA secure_delete</c> on a background thread.
    /// </summary>
    public static Task SetSecureDeleteAsync(this SQLitePragmas pragmas, bool value, CancellationToken ct = default)
    {
        return AsyncRunner.Run(() => pragmas.SecureDelete = value, ct);
    }
}
