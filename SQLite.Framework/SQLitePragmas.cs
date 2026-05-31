namespace SQLite.Framework;

/// <summary>
/// Typed access to common SQLite pragmas. Read or write them as properties instead of using raw
/// <c>PRAGMA</c> SQL. Get the instance from <see cref="SQLiteDatabase.Pragmas" />. To add more
/// pragmas, write a class that inherits from this one and pass it to <see cref="SQLiteOptionsBuilder.UsePragmas" />.
/// </summary>
public class SQLitePragmas
{
    /// <summary>
    /// Creates a new instance that reads from and writes to <paramref name="database" />.
    /// </summary>
    public SQLitePragmas(SQLiteDatabase database)
    {
        Database = database;
    }

    /// <summary>
    /// The database this accessor reads from and writes to.
    /// </summary>
    public SQLiteDatabase Database { get; }

    /// <summary>
    /// <c>PRAGMA foreign_keys</c>. Turns enforcement of foreign key constraints on or off for the
    /// connection. SQLite has it off by default.
    /// </summary>
    public virtual bool ForeignKeys
    {
        get => Database.ExecuteScalar<int>("PRAGMA foreign_keys") == 1;
        set => Database.Execute($"PRAGMA foreign_keys = {(value ? 1 : 0)}");
    }

    /// <summary>
    /// <c>PRAGMA journal_mode</c>. Controls how SQLite keeps the rollback journal.
    /// </summary>
    public virtual SQLiteJournalMode JournalMode
    {
        get => ParseJournalMode(Database.ExecuteScalar<string>("PRAGMA journal_mode"));
        set => Database.ExecuteScalar<string>($"PRAGMA journal_mode = {value switch
        {
            SQLiteJournalMode.Delete => "DELETE",
            SQLiteJournalMode.Truncate => "TRUNCATE",
            SQLiteJournalMode.Persist => "PERSIST",
            SQLiteJournalMode.Memory => "MEMORY",
            SQLiteJournalMode.Wal => "WAL",
            SQLiteJournalMode.Off => "OFF",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null),
        }}");
    }

    /// <summary>
    /// <c>PRAGMA cache_size</c>. Tells SQLite how many pages to keep in memory for the connection.
    /// A negative number is read as kibibytes instead of pages.
    /// </summary>
    public virtual int CacheSize
    {
        get => Database.ExecuteScalar<int>("PRAGMA cache_size");
        set => Database.Execute($"PRAGMA cache_size = {value}");
    }

    /// <summary>
    /// <c>PRAGMA synchronous</c>. Tells SQLite how often to call sync on the disk.
    /// </summary>
    public virtual SQLiteSynchronousMode SynchronousMode
    {
        get => (SQLiteSynchronousMode)Database.ExecuteScalar<int>("PRAGMA synchronous");
        set => Database.Execute($"PRAGMA synchronous = {(int)value}");
    }

    /// <summary>
    /// <c>PRAGMA user_version</c>. An integer your app picks. SQLite stores it in the file header.
    /// Apps often use it to track schema versions.
    /// </summary>
    public virtual int UserVersion
    {
        get => Database.ExecuteScalar<int?>("PRAGMA user_version") ?? 0;
        set => Database.Execute($"PRAGMA user_version = {value}");
    }

    /// <summary>
    /// <c>PRAGMA page_size</c>. Read only once the database file has been written.
    /// </summary>
    public virtual long PageSize => Database.ExecuteScalar<long>("PRAGMA page_size");

    /// <summary>
    /// <c>PRAGMA freelist_count</c>. The number of unused pages in the database file.
    /// </summary>
    public virtual long FreelistCount => Database.ExecuteScalar<long>("PRAGMA freelist_count");

    /// <summary>
    /// <c>PRAGMA recursive_triggers</c>. Turns recursive trigger calls on or off.
    /// </summary>
    public virtual bool RecursiveTriggers
    {
        get => Database.ExecuteScalar<int>("PRAGMA recursive_triggers") == 1;
        set => Database.Execute($"PRAGMA recursive_triggers = {(value ? 1 : 0)}");
    }

    /// <summary>
    /// <c>PRAGMA temp_store</c>. Tells SQLite where to keep temporary tables and indexes.
    /// <c>0</c> is the default, <c>1</c> is file, <c>2</c> is memory.
    /// </summary>
    public virtual int TempStore
    {
        get => Database.ExecuteScalar<int>("PRAGMA temp_store");
        set => Database.Execute($"PRAGMA temp_store = {value}");
    }

    /// <summary>
    /// <c>PRAGMA secure_delete</c>. When this is on, SQLite writes zeros over deleted content
    /// before it gives the pages back.
    /// </summary>
    public virtual bool SecureDelete
    {
        get => Database.ExecuteScalar<int>("PRAGMA secure_delete") != 0;
        set => Database.ExecuteScalar<int>($"PRAGMA secure_delete = {(value ? 1 : 0)}");
    }

    /// <summary>
    /// <c>PRAGMA busy_timeout</c>. Number of milliseconds the busy handler waits before
    /// returning <c>SQLITE_BUSY</c>. Setting to <c>0</c> disables the busy handler.
    /// </summary>
    public virtual int BusyTimeout
    {
        get => Database.ExecuteScalar<int>("PRAGMA busy_timeout");
        set => Database.ExecuteScalar<int>($"PRAGMA busy_timeout = {value}");
    }

    /// <summary>
    /// <c>PRAGMA mmap_size</c>. Maximum number of bytes SQLite will memory-map. Setting to
    /// <c>0</c> disables memory mapping. The build of SQLite must have
    /// <c>SQLITE_MAX_MMAP_SIZE</c> greater than zero or the pragma is a no-op.
    /// </summary>
    public virtual long MmapSize
    {
        get => Database.ExecuteScalar<long>("PRAGMA mmap_size");
        set => Database.ExecuteScalar<long>($"PRAGMA mmap_size = {value}");
    }

    /// <summary>
    /// <c>PRAGMA auto_vacuum</c>. Controls how SQLite reclaims free pages. Only takes effect
    /// before the database is first written.
    /// </summary>
    public virtual SQLiteAutoVacuumMode AutoVacuum
    {
        get => (SQLiteAutoVacuumMode)Database.ExecuteScalar<int>("PRAGMA auto_vacuum");
        set => Database.Execute($"PRAGMA auto_vacuum = {(int)value}");
    }

    /// <summary>
    /// <c>PRAGMA wal_autocheckpoint</c>. Number of pages in the WAL that triggers an automatic
    /// checkpoint. Setting to <c>0</c> or a negative value disables auto-checkpointing.
    /// </summary>
    public virtual int WalAutoCheckpoint
    {
        get => Database.ExecuteScalar<int>("PRAGMA wal_autocheckpoint");
        set => Database.ExecuteScalar<int>($"PRAGMA wal_autocheckpoint = {value}");
    }

    /// <summary>
    /// <c>PRAGMA defer_foreign_keys</c>. When set to <see langword="true" /> inside a
    /// transaction, defers foreign key checks until <c>COMMIT</c>. Useful for bulk operations
    /// that temporarily violate referential integrity. Resets to <see langword="false" /> at
    /// each commit or rollback.
    /// </summary>
    public virtual bool DeferForeignKeys
    {
        get => Database.ExecuteScalar<int>("PRAGMA defer_foreign_keys") == 1;
        set => Database.Execute($"PRAGMA defer_foreign_keys = {(value ? 1 : 0)}");
    }

    /// <summary>
    /// <c>PRAGMA encoding</c>. Character encoding used to store TEXT values. Only takes effect
    /// before any table is created in a new database.
    /// </summary>
    public virtual SQLiteEncoding Encoding
    {
        get => ParseEncoding(Database.ExecuteScalar<string>("PRAGMA encoding"));
        set => Database.Execute($"PRAGMA encoding = '{value switch
        {
            SQLiteEncoding.Utf8 => "UTF-8",
            SQLiteEncoding.Utf16 => "UTF-16",
            SQLiteEncoding.Utf16le => "UTF-16le",
            SQLiteEncoding.Utf16be => "UTF-16be",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null),
        }}'");
    }

    /// <summary>
    /// <c>PRAGMA locking_mode</c>. Controls whether SQLite releases the file lock between
    /// transactions. <see cref="SQLiteLockingMode.Exclusive" /> keeps the lock for the
    /// connection's lifetime, which blocks other processes but is faster for single-process
    /// access.
    /// </summary>
    public virtual SQLiteLockingMode LockingMode
    {
        get => ParseLockingMode(Database.ExecuteScalar<string>("PRAGMA locking_mode"));
        set => Database.ExecuteScalar<string>($"PRAGMA locking_mode = {value switch
        {
            SQLiteLockingMode.Normal => "NORMAL",
            SQLiteLockingMode.Exclusive => "EXCLUSIVE",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null),
        }}");
    }

    /// <summary>
    /// <c>PRAGMA application_id</c>. 32-bit integer stored in the database file header. Apps
    /// can use it to identify which application owns the file (analogous to the magic number
    /// on other file formats).
    /// </summary>
    public virtual int ApplicationId
    {
        get => Database.ExecuteScalar<int>("PRAGMA application_id");
        set => Database.Execute($"PRAGMA application_id = {value}");
    }

    /// <summary>
    /// <c>PRAGMA data_version</c>. Read-only integer that increases whenever the database is
    /// modified by another connection. Useful for cache invalidation.
    /// </summary>
    public virtual int DataVersion => Database.ExecuteScalar<int>("PRAGMA data_version");

    /// <summary>
    /// <c>PRAGMA schema_version</c>. Read-only integer that increases whenever the database
    /// schema changes. Writing this value is technically allowed but unsafe.
    /// </summary>
    public virtual int SchemaVersion => Database.ExecuteScalar<int>("PRAGMA schema_version");

#if SQLITECIPHER
    /// <summary>
    /// <c>PRAGMA cipher_version</c>. SQLCipher only. Read-only string identifying the SQLCipher
    /// build, for example <c>"4.6.0 community"</c>.
    /// </summary>
    public virtual string CipherVersion => Database.ExecuteScalar<string>("PRAGMA cipher_version")!;

    /// <summary>
    /// <c>PRAGMA cipher_provider</c>. SQLCipher only. Read-only string identifying the
    /// underlying crypto provider, for example <c>"openssl"</c> or <c>"commoncrypto"</c>.
    /// </summary>
    public virtual string CipherProvider => Database.ExecuteScalar<string>("PRAGMA cipher_provider")!;

    /// <summary>
    /// <c>PRAGMA cipher_provider_version</c>. SQLCipher only. Read-only string identifying the
    /// version of the underlying crypto provider.
    /// </summary>
    public virtual string CipherProviderVersion => Database.ExecuteScalar<string>("PRAGMA cipher_provider_version")!;

    /// <summary>
    /// <c>PRAGMA cipher_compatibility</c>. SQLCipher only. Compatibility version <c>1</c>
    /// through <c>4</c>. Use to read databases created by older SQLCipher releases.
    /// </summary>
    public virtual int CipherCompatibility
    {
        get => Database.ExecuteScalar<int>("PRAGMA cipher_compatibility");
        set => Database.Execute($"PRAGMA cipher_compatibility = {value}");
    }

    /// <summary>
    /// <c>PRAGMA cipher_page_size</c>. SQLCipher only. Page size used for the encrypted file.
    /// Must be set before the database is read or written.
    /// </summary>
    public virtual int CipherPageSize
    {
        get => Database.ExecuteScalar<int>("PRAGMA cipher_page_size");
        set => Database.Execute($"PRAGMA cipher_page_size = {value}");
    }

    /// <summary>
    /// <c>PRAGMA cipher_use_hmac</c>. SQLCipher only. Enables or disables HMAC integrity
    /// protection on each page.
    /// </summary>
    public virtual bool CipherUseHmac
    {
        get => Database.ExecuteScalar<int>("PRAGMA cipher_use_hmac") == 1;
        set => Database.Execute($"PRAGMA cipher_use_hmac = {(value ? "ON" : "OFF")}");
    }

    /// <summary>
    /// <c>PRAGMA cipher_kdf_iter</c>. SQLCipher only. Number of PBKDF2 iterations applied to
    /// the passphrase when deriving the encryption key.
    /// </summary>
    public virtual int CipherKdfIter
    {
        get => Database.ExecuteScalar<int>("PRAGMA cipher_kdf_iter");
        set => Database.Execute($"PRAGMA cipher_kdf_iter = {value}");
    }

    /// <summary>
    /// <c>PRAGMA cipher_memory_security</c>. SQLCipher only. When enabled, SQLCipher zeroes
    /// internal memory buffers after use.
    /// </summary>
    public virtual bool CipherMemorySecurity
    {
        get => Database.ExecuteScalar<int>("PRAGMA cipher_memory_security") == 1;
        set => Database.Execute($"PRAGMA cipher_memory_security = {(value ? "ON" : "OFF")}");
    }
#endif

    /// <summary>
    /// Queryable view over SQLite's built-in <c>sqlite_master</c> table, which lists every
    /// table, index, view, and trigger in the database. Supports the full LINQ surface
    /// (<c>Select</c>, <c>Where</c>, <c>Join</c>, etc.) like any other framework table.
    /// </summary>
    public virtual ReadOnlySQLiteTable<SQLiteMaster> Master => field ??= Database.ReadOnlyTable<SQLiteMaster>();

    /// <summary>
    /// Queryable view over SQLite's built-in <c>sqlite_sequence</c> table, which tracks the
    /// highest <c>AUTOINCREMENT</c> value assigned per table. The table only exists once at
    /// least one <c>AUTOINCREMENT</c> table has been created.
    /// </summary>
    public virtual ReadOnlySQLiteTable<SQLiteSequence> Sequence => field ??= Database.ReadOnlyTable<SQLiteSequence>();

    /// <summary>
    /// <c>PRAGMA incremental_vacuum(<paramref name="pages" />)</c>. Reclaims up to
    /// <paramref name="pages" /> free pages and returns them to the file system. Pass
    /// <see langword="null" /> to reclaim every free page. Only works when
    /// <see cref="AutoVacuum" /> is <see cref="SQLiteAutoVacuumMode.Incremental" />.
    /// </summary>
    public virtual void IncrementalVacuum(int? pages = null)
    {
        string sql = pages == null
            ? "PRAGMA incremental_vacuum"
            : $"PRAGMA incremental_vacuum({pages.Value})";
        Database.Execute(sql);
    }

    /// <summary>
    /// <c>PRAGMA wal_checkpoint(<paramref name="mode" />)</c>. Runs a checkpoint with the given
    /// mode. Returns <see langword="true" /> when the WAL was fully checkpointed,
    /// <see langword="false" /> when a reader blocked the operation.
    /// </summary>
    public virtual bool WalCheckpoint(SQLiteWalCheckpointMode mode = SQLiteWalCheckpointMode.Passive)
    {
        string modeName = mode switch
        {
            SQLiteWalCheckpointMode.Passive => "PASSIVE",
            SQLiteWalCheckpointMode.Full => "FULL",
            SQLiteWalCheckpointMode.Restart => "RESTART",
            SQLiteWalCheckpointMode.Truncate => "TRUNCATE",
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null),
        };
        int busy = Database.ExecuteScalar<int>($"PRAGMA wal_checkpoint({modeName})");
        return busy == 0;
    }

    /// <summary>
    /// <c>PRAGMA integrity_check</c>. Walks the database file and verifies it is well-formed.
    /// Returns a list whose only element is <c>"ok"</c> when the database is healthy, otherwise
    /// one row per problem. Expensive on large databases.
    /// </summary>
    public virtual IReadOnlyList<string> IntegrityCheck()
    {
        return Database.Query<string>("PRAGMA integrity_check").ToList();
    }

    /// <summary>
    /// <c>PRAGMA quick_check</c>. Faster but less thorough sibling of
    /// <see cref="IntegrityCheck" />. Skips checks that verify index content matches the table,
    /// which is the most expensive part.
    /// </summary>
    public virtual IReadOnlyList<string> QuickCheck()
    {
        return Database.Query<string>("PRAGMA quick_check").ToList();
    }

    /// <summary>
    /// <c>PRAGMA optimize</c>. Asks SQLite to perform any maintenance the query planner thinks
    /// is worthwhile, mainly running <c>ANALYZE</c> on tables whose statistics are stale.
    /// Safe to run at app shutdown.
    /// </summary>
    public virtual void Optimize()
    {
#if SQLITE_FRAMEWORK_VERSION_AWARE
        Database.Options.EnsureMinimumVersion(SQLiteMinimumVersion.V3_18, "PRAGMA optimize");
#endif
        Database.Execute("PRAGMA optimize");
    }

#if SQLITECIPHER
    /// <summary>
    /// <c>PRAGMA rekey = '...'</c>. SQLCipher only. Re-encrypts the database with a new
    /// passphrase. The connection must already be authenticated with the current key. The new
    /// key is bound as a parameter to keep it out of the SQL string.
    /// </summary>
    public virtual void Rekey(string newKey)
    {
        ArgumentNullException.ThrowIfNull(newKey);
        string escaped = newKey.Replace("'", "''");
        Database.ExecuteScalar<string>($"PRAGMA rekey = '{escaped}'");
    }
#endif

    /// <summary>
    /// Returns the rows of <c>pragma_table_info(<paramref name="tableName" />)</c>, one per
    /// column on the table. Can be used inside a LINQ expression with the argument bound to
    /// a column from the outer query, for example
    /// <c>from m in db.Pragmas.Master from p in db.Pragmas.TableInfo(m.Name)</c>.
    /// </summary>
    [SQLitePragmaFunction("pragma_table_info")]
#if SQLITE_FRAMEWORK_OS_BUNDLED_SQLITE
    [UnsupportedOSPlatform("android")]
    [SupportedOSPlatform("android26.0")]
    [UnsupportedOSPlatform("ios")]
    [SupportedOSPlatform("ios11.0")]
#endif
    public virtual IQueryable<PragmaTableInfo> TableInfo(string tableName)
    {
#if SQLITE_FRAMEWORK_VERSION_AWARE
        Database.Options.EnsureMinimumVersion(SQLiteMinimumVersion.V3_16, "pragma_table_info() as a table-valued function");
#endif
        return new SQLitePragmaTable<PragmaTableInfo>(Database, "pragma_table_info", tableName);
    }

    /// <summary>
    /// Returns the rows of <c>pragma_index_list(<paramref name="tableName" />)</c>, one per
    /// index attached to the table.
    /// </summary>
    [SQLitePragmaFunction("pragma_index_list")]
#if SQLITE_FRAMEWORK_OS_BUNDLED_SQLITE
    [UnsupportedOSPlatform("android")]
    [SupportedOSPlatform("android26.0")]
    [UnsupportedOSPlatform("ios")]
    [SupportedOSPlatform("ios11.0")]
#endif
    public virtual IQueryable<PragmaIndexList> IndexList(string tableName)
    {
#if SQLITE_FRAMEWORK_VERSION_AWARE
        Database.Options.EnsureMinimumVersion(SQLiteMinimumVersion.V3_16, "pragma_index_list() as a table-valued function");
#endif
        return new SQLitePragmaTable<PragmaIndexList>(Database, "pragma_index_list", tableName);
    }

    /// <summary>
    /// Returns the rows of <c>pragma_foreign_key_list(<paramref name="tableName" />)</c>,
    /// one per foreign key column on the table.
    /// </summary>
    [SQLitePragmaFunction("pragma_foreign_key_list")]
#if SQLITE_FRAMEWORK_OS_BUNDLED_SQLITE
    [UnsupportedOSPlatform("android")]
    [SupportedOSPlatform("android26.0")]
    [UnsupportedOSPlatform("ios")]
    [SupportedOSPlatform("ios11.0")]
#endif
    public virtual IQueryable<PragmaForeignKey> ForeignKeyList(string tableName)
    {
#if SQLITE_FRAMEWORK_VERSION_AWARE
        Database.Options.EnsureMinimumVersion(SQLiteMinimumVersion.V3_16, "pragma_foreign_key_list() as a table-valued function");
#endif
        return new SQLitePragmaTable<PragmaForeignKey>(Database, "pragma_foreign_key_list", tableName);
    }

    internal static SQLiteEncoding ParseEncoding(string? value)
    {
        return value switch
        {
            "UTF-8" => SQLiteEncoding.Utf8,
            "UTF-16le" => SQLiteEncoding.Utf16le,
            "UTF-16be" => SQLiteEncoding.Utf16be,
            _ => throw new InvalidOperationException($"Unrecognized PRAGMA encoding value '{value ?? "<null>"}'."),
        };
    }

    internal static SQLiteLockingMode ParseLockingMode(string? value)
    {
        return value switch
        {
            "normal" => SQLiteLockingMode.Normal,
            "exclusive" => SQLiteLockingMode.Exclusive,
            _ => throw new InvalidOperationException($"Unrecognized PRAGMA locking_mode value '{value ?? "<null>"}'."),
        };
    }

    internal static SQLiteJournalMode ParseJournalMode(string? value)
    {
        return value switch
        {
            "delete" => SQLiteJournalMode.Delete,
            "truncate" => SQLiteJournalMode.Truncate,
            "persist" => SQLiteJournalMode.Persist,
            "memory" => SQLiteJournalMode.Memory,
            "wal" => SQLiteJournalMode.Wal,
            "off" => SQLiteJournalMode.Off,
            _ => throw new InvalidOperationException($"Unrecognized PRAGMA journal_mode value '{value ?? "<null>"}'."),
        };
    }
}
