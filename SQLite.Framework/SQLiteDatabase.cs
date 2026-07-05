using System.Collections.Concurrent;

namespace SQLite.Framework;

/// <summary>
/// Represents a connection to the SQLite database.
/// </summary>
public class SQLiteDatabase : IQueryProvider, IDisposable
{
    private static readonly MethodInfo ExecuteGroupingQueryGeneric = typeof(SQLiteDatabase)
        .GetMethod(nameof(ExecuteGroupingQuery), BindingFlags.Instance | BindingFlags.NonPublic)!;

    // ReSharper disable once ChangeFieldTypeToSystemThreadingLock, it doesn't exist in .NET 8
    private readonly object connectionOpenLock = new();
    // ReSharper disable once ChangeFieldTypeToSystemThreadingLock, it doesn't exist in .NET 8
    private readonly object tableMappingsLock = new();
    private readonly object readGateLock = new();
    private readonly SemaphoreSlim connectionSemaphore = new(1, 1);
    private readonly AsyncLocal<bool> holdsConnectionLock = new();
    private readonly ConcurrentDictionary<Type, TableMapping> tableMappings = [];
    private readonly ConcurrentDictionary<SQLiteDatabase, string> attachedDatabases = new();
    private readonly PreparedStatementPool statementPool = new();
    private volatile bool modelFrozen;
    private bool modelCreated;
    private int activeTransactionCount;
    private TaskCompletionSource? readGateTcs;
    private long commandIds;

#if SQLITE_FRAMEWORK_TESTING
    private long entityMaterializerHits;
    private long selectMaterializerHits;
    private long selectCompilerFallbacks;
#endif

    static SQLiteDatabase()
    {
        SQLiteProviderInitializer.Initialize();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SQLiteDatabase" /> class from a read-only
    /// <see cref="SQLiteOptions" /> instance. Use <see cref="SQLiteOptionsBuilder" /> to construct one.
    /// </summary>
    public SQLiteDatabase(SQLiteOptions options)
    {
        Options = options;
    }

    /// <summary>
    /// The connection handle to the SQLite database.
    /// </summary>
    /// <remarks>
    /// This is only set after the connection is opened.
    /// </remarks>
    public sqlite3? Handle { get; private set; }

    /// <summary>
    /// Indicates that the connection is open.
    /// </summary>
    [MemberNotNullWhen(true, nameof(Handle))]
    public bool IsConnected { get; private set; }

    /// <summary>
    /// Indicates that the connection is being established.
    /// </summary>
    public bool IsConnecting { get; private set; }

    /// <summary>
    /// Returns the cached table mappings in the current database instance.
    /// </summary>
    public IReadOnlyCollection<TableMapping> TableMappings => tableMappings.Values.ToArray();

    /// <summary>
    /// The configuration for the database. Pass an <see cref="SQLiteOptions" /> built
    /// via <see cref="SQLiteOptionsBuilder" /> to the constructor.
    /// </summary>
    public SQLiteOptions Options { get; }

#if SQLITE_FRAMEWORK_TESTING
    /// <summary>
    /// Number of times a generated entity materializer from <see cref="SQLiteOptions.EntityMaterializers" />
    /// has handled a query row for this database. Read-only counter that increments on every hit.
    /// Only present when the framework is built with the <c>SQLITE_FRAMEWORK_TESTING</c> symbol.
    /// </summary>
    public long EntityMaterializerHits => Interlocked.Read(ref entityMaterializerHits);

    /// <summary>
    /// Number of times a generated Select materializer from <see cref="SQLiteOptions.SelectMaterializers" />
    /// has handled a query for this database. Read-only counter that increments on every hit.
    /// Only present when the framework is built with the <c>SQLITE_FRAMEWORK_TESTING</c> symbol.
    /// </summary>
    public long SelectMaterializerHits => Interlocked.Read(ref selectMaterializerHits);

    /// <summary>
    /// Number of times a Select projection used runtime compilation because no generated
    /// materializer was found. Parity tests use this to check that every Select uses the
    /// source generator. A non-zero value means the generator did not cover this shape
    /// or produced a different signature than the runtime.
    /// </summary>
    public long SelectCompilerFallbacks => Interlocked.Read(ref selectCompilerFallbacks);
#endif

    /// <summary>
    /// Typed access to common SQLite pragmas like foreign keys, journal mode, cache size and user version.
    /// The instance is built the first time you read this property using <see cref="SQLiteOptions.PragmasFactory" />.
    /// </summary>
    public SQLitePragmas Pragmas { get => field ??= Options.PragmasFactory(this); private set; }

    /// <summary>
    /// DDL operations on the database, including create, drop, alter and inspection. The instance
    /// is built the first time you read this property using <see cref="SQLiteOptions.SchemaFactory" />.
    /// </summary>
    public SQLiteSchema Schema { get => field ??= Options.SchemaFactory(this); private set; }

    internal bool HoldsConnectionLock => holdsConnectionLock.Value;

    /// <summary>
    /// The command interceptors commands notify and the write fast paths gate on. Normally the
    /// list from <see cref="SQLiteOptions.CommandInterceptors" />. A migration script rehearsal
    /// swaps in a list with its capture appended for the duration of the rehearsal.
    /// </summary>
    internal IReadOnlyList<ISQLiteCommandInterceptor> EffectiveCommandInterceptors
    {
        get => field ?? Options.CommandInterceptors;
        set;
    }

    /// <summary>
    /// True once <see cref="OnModelCreating" /> has completed, after which table mappings no
    /// longer change. The single-item write fast path only caches SQL while this is set.
    /// </summary>
    internal bool ModelFrozen => modelFrozen;

    /// <inheritdoc />
    public virtual void Dispose()
    {
        if (IsConnected)
        {
            IsConnected = false;

            lock (connectionOpenLock)
            {
                statementPool.Clear();

                if (Handle != null)
                {
                    raw.sqlite3_close_v2(Handle);
                    Handle = null;
                }
            }
        }
    }

    /// <summary>
    /// Creates a new table for the specified type.
    /// </summary>
    public virtual TableMapping TableMapping([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type type)
    {
        EnsureModelCreated();
        if (tableMappings.TryGetValue(type, out TableMapping? table))
        {
            return table;
        }

        return tableMappings.GetOrAdd(type, new TableMapping(type, Options));
    }

    /// <summary>
    /// Creates a new table mapping for the specified type.
    /// </summary>
    public virtual TableMapping TableMapping<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>()
    {
        EnsureModelCreated();
        if (tableMappings.TryGetValue(typeof(T), out TableMapping? table))
        {
            return table;
        }

        return tableMappings.GetOrAdd(typeof(T), new TableMapping(typeof(T), Options));
    }

    /// <summary>
    /// Returns a queryable wrapper for the table mapped to <typeparamref name="T" />. Override on a
    /// <see cref="SQLiteDatabase" /> subclass to dispatch by entity type to a custom <see cref="SQLiteTable{T}" /> subclass.
    /// </summary>
    public virtual SQLiteTable<T> Table<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>()
    {
        return new SQLiteTable<T>(this, TableMapping<T>());
    }

    /// <summary>
    /// Returns a queryable wrapper for the table mapped to <paramref name="type" />. Override on
    /// a <see cref="SQLiteDatabase" /> subclass to dispatch by entity type.
    /// </summary>
    public virtual SQLiteTable Table([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] Type type)
    {
        return new SQLiteTable(this, TableMapping(type));
    }

    /// <summary>
    /// Returns a read-only queryable wrapper for the table mapped to <typeparamref name="T" /> in an
    /// attached database. Reads emit the schema-qualified name <c>"schema"."Table"</c>, so you can
    /// query and join against an attached file with the typed LINQ surface. The table must already be
    /// attached with <see cref="AttachDatabase(string, string, string)" />. The result is read-only
    /// because writes to attached tables are not supported through the typed API.
    /// </summary>
    /// <param name="schema">The name the database was attached with. Must be a plain identifier
    /// (letters, digits and underscores).</param>
    public virtual ReadOnlySQLiteTable<T> Table<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(string schema)
    {
        ValidateSchemaName(schema);
        return new ReadOnlySQLiteTable<T>(this, TableMapping<T>(), schema);
    }

    /// <summary>
    /// Returns a read-only queryable wrapper for the table mapped to <typeparamref name="T" />.
    /// The full LINQ surface (<c>Select</c>, <c>Where</c>, <c>Join</c>, etc.) works the same as
    /// <see cref="Table{T}()" />, but no mutation methods are exposed. Use this for SQLite system
    /// tables (such as <c>sqlite_master</c>) or any user table you want to expose as read-only.
    /// </summary>
    public virtual ReadOnlySQLiteTable<T> ReadOnlyTable<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>()
    {
        return new ReadOnlySQLiteTable<T>(this, TableMapping<T>());
    }

    /// <summary>
    /// Begins a transaction. Other writers from any async context wait on the framework's
    /// write semaphore until this transaction commits or rolls back. Uses <c>SAVEPOINT</c>
    /// under the hood so nested calls compose. Standalone reads from other async contexts
    /// are not blocked.
    /// </summary>
    public SQLiteTransaction BeginTransaction()
    {
        bool ownsLock = !holdsConnectionLock.Value;

        if (ownsLock)
        {
            connectionSemaphore.Wait();
            holdsConnectionLock.Value = true;
        }

        try
        {
            string savepointName = $"SQLITE_AUTOINDEX_{Guid.NewGuid():N}";
            CreateCommand($"SAVEPOINT {savepointName}", []).ExecuteNonQuery();
            if (ownsLock)
            {
                NotifyTransactionStarted();
            }
            return new SQLiteTransaction(this, savepointName, ownsLock);
        }
        catch
        {
            if (ownsLock)
            {
                ReleaseLock();
            }
            throw;
        }
    }

    /// <summary>
    /// Creates a command with the specified SQL and parameters.
    /// </summary>
    public virtual SQLiteCommand CreateCommand(string sql, List<SQLiteParameter> parameters)
    {
        OpenConnection();

        return new SQLiteCommand(this, sql, parameters);
    }

    /// <summary>
    /// Opens the connection to the SQLite database.
    /// </summary>
    public virtual void OpenConnection()
    {
        lock (connectionOpenLock)
        {
            if (IsConnected)
            {
                return;
            }

            IsConnecting = true;

            SQLiteResult result = (SQLiteResult)raw.sqlite3_open_v2(
                Options.DatabasePath,
                out sqlite3 handle,
                (int)Options.OpenFlags,
                null
            );

            if (result != SQLiteResult.OK)
            {
                IsConnecting = false;
                throw new SQLiteException(result, "Unable to open database", null);
            }

            Handle = handle;

            OnDatabaseConnecting();

#if SQLITECIPHER
            if (!string.IsNullOrEmpty(Options.EncryptionKey))
            {
                raw.sqlite3_prepare_v2(Handle, $"PRAGMA key = '{Options.EncryptionKey.Replace("'", "''")}'", out sqlite3_stmt keyStmt);
                raw.sqlite3_step(keyStmt);
                raw.sqlite3_finalize(keyStmt);
            }
#endif

#if SQLITE_FRAMEWORK_OS_BUNDLED_SQLITE
            if (Options.MinimumSqliteVersion != SQLiteMinimumVersion.Unspecified)
            {
                int loadedVersion = raw.sqlite3_libversion_number();
                if (loadedVersion < (int)Options.MinimumSqliteVersion)
                {
                    raw.sqlite3_close(Handle);
                    Handle = null;
                    IsConnecting = false;
                    throw new NotSupportedException(
                        $"The loaded SQLite version {CommonHelpers.Format(loadedVersion)} " +
                        $"is below the configured minimum {CommonHelpers.Format((int)Options.MinimumSqliteVersion)}. " +
                        $"Use the SQLite.Framework.Bundled package to ship a known SQLite version, " +
                        $"or lower the value passed to UseMinimumSqliteVersion (and retest your queries)."
                    );
                }
            }
#endif

            IsConnected = true;
            OnDatabaseConnected();

            if (Options.IsWalMode)
            {
                raw.sqlite3_prepare_v2(Handle, "PRAGMA journal_mode = WAL", out sqlite3_stmt walStmt);
                raw.sqlite3_step(walStmt);
                raw.sqlite3_finalize(walStmt);
            }

            string fkPragma = Options.IsForeignKeysEnabled
                ? "PRAGMA foreign_keys = ON"
                : "PRAGMA foreign_keys = OFF";
            raw.sqlite3_prepare_v2(Handle, fkPragma, out sqlite3_stmt fkStmt);
            raw.sqlite3_step(fkStmt);
            raw.sqlite3_finalize(fkStmt);

            IsConnecting = false;
        }
    }

    /// <summary>
    /// Copies this database into <paramref name="destination" /> using SQLite's backup API.
    /// The source database stays open for reads and writes during the copy. If a page changes
    /// while the copy is running, SQLite re-copies it for you. Use this for backups, for saving
    /// an in-memory database to a file or for loading a file into memory at startup.
    /// </summary>
    /// <param name="destination">The database to copy into. Existing data in it is replaced.</param>
    /// <param name="sourceName">The schema name of the source. The default is <c>main</c>. Pass another name to back up an attached database.</param>
    /// <param name="destName">The schema name of the destination. The default is <c>main</c>.</param>
    public virtual void BackupTo(SQLiteDatabase destination, string sourceName = "main", string destName = "main")
    {
        ArgumentNullException.ThrowIfNull(destination);

        OpenConnection();
        destination.OpenConnection();

        using IDisposable sourceLock = Lock();
        using IDisposable destLock = destination.Lock();

        if (raw.sqlite3_get_autocommit(Handle!) == 0 || raw.sqlite3_get_autocommit(destination.Handle!) == 0)
        {
            throw new InvalidOperationException(
                "BackupTo cannot run while a transaction is open on the source or the destination connection. Commit or roll back the transaction first.");
        }

        sqlite3_backup handle = raw.sqlite3_backup_init(destination.Handle!, destName, Handle!, sourceName);
        if (handle == null)
        {
            SQLiteResult code = (SQLiteResult)raw.sqlite3_errcode(destination.Handle!);
            string message = raw.sqlite3_errmsg(destination.Handle!).utf8_to_string();
            throw new SQLiteException(code, message, null);
        }

        try
        {
            while (true)
            {
                SQLiteResult result = (SQLiteResult)raw.sqlite3_backup_step(handle, -1);
                if (result == SQLiteResult.Done)
                {
                    return;
                }

                if (result == SQLiteResult.Busy || result == SQLiteResult.Locked)
                {
                    Thread.Sleep(50);
                    continue;
                }

                string message = raw.sqlite3_errmsg(destination.Handle!).utf8_to_string();
                throw new SQLiteException(result, message, null);
            }
        }
        finally
        {
            raw.sqlite3_backup_finish(handle);
        }
    }

    /// <summary>
    /// Opens a destination database at <paramref name="destinationPath" />, runs the backup and
    /// closes the destination for you. The destination file is overwritten if it already exists.
    /// On SQLCipher builds, the destination uses the same encryption key as this database so the
    /// backup file is encrypted the same way.
    /// </summary>
    public virtual void BackupTo(string destinationPath)
    {
        ArgumentException.ThrowIfNullOrEmpty(destinationPath);

        SQLiteOptionsBuilder destBuilder = new(destinationPath);
        if (!string.IsNullOrEmpty(Options.EncryptionKey))
        {
            destBuilder.UseEncryptionKey(Options.EncryptionKey);
        }
        using SQLiteDatabase destination = new(destBuilder.Build());
        BackupTo(destination);
    }

    /// <summary>
    /// Runs <c>VACUUM</c> to rebuild the database file. SQLite copies every page into a fresh
    /// file, defragments and reclaims free space. Pass an attached schema name to vacuum that
    /// schema instead of <c>main</c>. Cannot run inside a transaction.
    /// </summary>
    /// <param name="schema">Attached schema name. Defaults to <see langword="null" />, which
    /// means the main database.</param>
    public virtual void Vacuum(string? schema = null)
    {
        if (schema == null)
        {
            Execute("VACUUM");
        }
        else
        {
            ValidateSchemaName(schema);
            Execute($"VACUUM \"{schema}\"");
        }
    }

    /// <summary>
    /// Runs <c>VACUUM INTO '<paramref name="destinationPath" />'</c> to write a clean copy of
    /// the database to a separate file. The destination file must not already exist.
    /// Requires SQLite 3.27.0 or newer. Cannot run inside a transaction.
    /// </summary>
    /// <param name="destinationPath">Path where the new database file will be created.</param>
    /// <param name="schema">Attached schema name to copy. Defaults to <see langword="null" />,
    /// which means the main database.</param>
#if SQLITE_FRAMEWORK_OS_BUNDLED_SQLITE
    [UnsupportedOSPlatform("ios")]
    [SupportedOSPlatform("ios13.0")]
#endif
    public virtual void VacuumInto(string destinationPath, string? schema = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(destinationPath);
#if SQLITE_FRAMEWORK_VERSION_AWARE
        Options.EnsureMinimumVersion(SQLiteMinimumVersion.V3_27, "VACUUM INTO");
#endif
        string escapedPath = destinationPath.Replace("'", "''");
        if (schema == null)
        {
            Execute($"VACUUM INTO '{escapedPath}'");
        }
        else
        {
            ValidateSchemaName(schema);
            Execute($"VACUUM \"{schema}\" INTO '{escapedPath}'");
        }
    }

    /// <summary>
    /// Runs <c>REINDEX</c> to rebuild indexes. With no argument, rebuilds every index in every
    /// attached database. Pass a table name to rebuild every index on that table, an index name
    /// to rebuild that single index or a collation name to rebuild every index that uses the
    /// collation.
    /// </summary>
    /// <param name="nameOrCollation">Optional table name, index name or collation name.
    /// Must be a plain identifier (letters, digits and underscores).</param>
    public virtual void Reindex(string? nameOrCollation = null)
    {
        if (nameOrCollation == null)
        {
            Execute("REINDEX");
        }
        else
        {
            ValidateSchemaName(nameOrCollation);
            Execute($"REINDEX \"{nameOrCollation}\"");
        }
    }

    /// <summary>
    /// Attaches another SQLite file to this connection under the given schema name. After this
    /// call you can read tables in the attached file with raw SQL, like
    /// <c>SELECT * FROM aux.Books</c>.
    /// </summary>
    /// <param name="path">Path to the database file to attach.</param>
    /// <param name="schemaName">The name to give the attached database. Must be a plain identifier
    /// (letters, digits and underscores).</param>
    /// <param name="encryptionKey">SQLCipher encryption key for the attached file. Only used in the
    /// SQLCipher build. Pass <see langword="null" /> to skip the key step. Pass an empty string when
    /// the attached file is plain SQLite (not encrypted) and the main database is encrypted.</param>
    public virtual void AttachDatabase(string path, string schemaName, string? encryptionKey = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        ValidateSchemaName(schemaName);

        string sql = $"ATTACH DATABASE '{path.Replace("'", "''")}' AS \"{schemaName}\"";
#if SQLITECIPHER
        if (encryptionKey != null)
        {
            sql += $" KEY '{encryptionKey.Replace("'", "''")}'";
        }
#endif
        Execute(sql);
    }

    /// <summary>
    /// Attaches the file behind another <see cref="SQLiteDatabase" /> to this connection under the
    /// given schema name and remembers the link. After this call a typed query rooted on this
    /// database that joins or reads <paramref name="database" /><c>.Table&lt;T&gt;()</c> emits the
    /// schema-qualified name <c>"schemaName"."Table"</c> for those tables. On the SQLCipher build the
    /// attached file reuses the encryption key from <paramref name="database" />.
    /// </summary>
    /// <param name="database">The database whose file is attached. Its
    /// <see cref="SQLiteOptions.DatabasePath" /> is used as the attach path. Keep this instance alive
    /// while it is attached. Run the cross-database query on the current database, not on
    /// <paramref name="database" />, because the attach only exists on this connection.</param>
    /// <param name="schemaName">The name to give the attached database. Must be a plain identifier
    /// (letters, digits and underscores).</param>
    public virtual void AttachDatabase(SQLiteDatabase database, string schemaName)
    {
        ArgumentNullException.ThrowIfNull(database);
        if (string.IsNullOrEmpty(database.Options.DatabasePath) || database.Options.DatabasePath == ":memory:")
        {
            throw new NotSupportedException(
                "An in-memory database cannot be attached through its path, because every ATTACH of ':memory:' opens a new empty database. Use a file-backed database instead.");
        }

        string? encryptionKey = null;
#if SQLITECIPHER
        encryptionKey = database.Options.EncryptionKey;
#endif
        AttachDatabase(database.Options.DatabasePath, schemaName, encryptionKey);
        attachedDatabases[database] = schemaName;
    }

    /// <summary>
    /// Detaches a previously attached database from this connection.
    /// </summary>
    /// <param name="schemaName">The name the database was attached with.</param>
    public virtual void DetachDatabase(string schemaName)
    {
        ValidateSchemaName(schemaName);

        Execute($"DETACH DATABASE \"{schemaName}\"");

        foreach (KeyValuePair<SQLiteDatabase, string> entry in attachedDatabases)
        {
            if (entry.Value == schemaName)
            {
                attachedDatabases.TryRemove(entry.Key, out _);
            }
        }
    }

    /// <summary>
    /// Opens a <see cref="Stream" /> over a BLOB column on a specific row, using SQLite's
    /// incremental BLOB I/O. The blob must already exist at the size you want to read or write,
    /// pre-allocate by inserting a sized byte array or by running raw SQL with <c>zeroblob(n)</c>.
    /// The stream holds a connection-level lock until it is disposed, so wrap it in a
    /// <c>using</c> block.
    /// </summary>
    /// <param name="tableName">SQLite table name (not the entity type name).</param>
    /// <param name="columnName">SQLite column name (not the entity property name).</param>
    /// <param name="rowid">The rowid of the target row. For tables with an <c>INTEGER PRIMARY KEY</c>
    /// this matches the primary key value. For <c>WITHOUT ROWID</c> tables, incremental BLOB I/O
    /// is not supported by SQLite.</param>
    /// <param name="writable">When <see langword="true" />, the stream can be written to. The
    /// underlying blob must not change size, see <see cref="SQLiteBlobStream" /> for details.</param>
    /// <param name="schema">The database schema name. Defaults to <c>main</c>. Use the name passed
    /// to <see cref="AttachDatabase(string, string, string)" /> to target an attached database.</param>
    public virtual SQLiteBlobStream OpenBlobStream(string tableName, string columnName, long rowid, bool writable = false, string schema = "main")
    {
        return OpenBlobStreamWithLock(tableName, columnName, rowid, writable, schema, Lock());
    }

    /// <summary>
    /// Opens a <see cref="Stream" /> over a BLOB column on a specific row, resolving the table
    /// name and column name from the entity mapping. See
    /// <see cref="OpenBlobStream(string, string, long, bool, string)" /> for the constraints.
    /// </summary>
    /// <typeparam name="T">The entity type. Its <c>[Table]</c> attribute (or class name) sets
    /// the SQLite table name.</typeparam>
    /// <param name="rowid">The rowid of the target row.</param>
    /// <param name="columnSelector">A selector for the BLOB property, like <c>b =&gt; b.Cover</c>.
    /// The property must be mapped to a column.</param>
    /// <param name="writable">When <see langword="true" />, the stream can be written to.</param>
    /// <param name="schema">The database schema name. Defaults to <c>main</c>.</param>
    public virtual SQLiteBlobStream OpenBlobStream<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(long rowid, Expression<Func<T, byte[]?>> columnSelector, bool writable = false, string schema = "main")
    {
        (string tableName, string columnName) = ResolveBlobColumn(columnSelector);
        return OpenBlobStream(tableName, columnName, rowid, writable, schema);
    }

    /// <summary>
    /// Locks the all queries from entering the <see cref="Lock" /> method until <see cref="IDisposable.Dispose" /> is
    /// called.
    /// </summary>
    public virtual IDisposable Lock()
    {
        if (holdsConnectionLock.Value)
        {
            return NoOpLockObject.Instance;
        }

        return new LockObject(connectionSemaphore, holdsConnectionLock);
    }

    /// <summary>
    /// Returns a disposable that represents a read operation against the database.
    /// </summary>
    /// <remarks>
    /// Read operations do not take the exclusive connection lock. SQLite uses its own mutex
    /// to keep concurrent statements on the same connection safe and WAL mode gives each
    /// reader a consistent snapshot even when other connections are writing. Only write
    /// operations and transactions need the exclusive lock.
    /// When <see cref="SQLiteOptions.BlockReadsDuringTransaction" /> is set, this call waits
    /// until any active transaction running on another async context finishes.
    /// </remarks>
    public virtual IDisposable ReadLock()
    {
        WaitForActiveTransactionsAsync(default).GetAwaiter().GetResult();
        return NoOpLockObject.Instance;
    }

    /// <summary>
    /// Wraps the provided SQL query and parameters into a queryable object.
    /// </summary>
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "The type should be part of the client assemblies.")]
    [UnconditionalSuppressMessage("AOT", "IL2091", Justification = "The type should be part of the client assemblies.")]
    public IQueryable<T> FromSql<T>(string sql, params SQLiteParameter[] parameters)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            throw new ArgumentException("SQL query cannot be null or empty.", nameof(sql));
        }

        if (parameters == null)
        {
            throw new ArgumentNullException(nameof(parameters), "Parameters cannot be null.");
        }


        return new Queryable<T>(this, Expression.Call(
            Expression.Constant(this),
            typeof(SQLiteDatabase).GetMethod(nameof(FromSql), BindingFlags.Instance | BindingFlags.Public)!
                .MakeGenericMethod(typeof(T)),
            Expression.Constant(sql),
            Expression.NewArrayInit(typeof(SQLiteParameter), parameters.Select(Expression.Constant))
        ));
    }

    /// <summary>
    /// Wraps a single row of values into a queryable object using a VALUES clause.
    /// </summary>
    public IQueryable<T> Values<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(T value)
    {
        return new Queryable<T>(this, Expression.Call(
            Expression.Constant(this),
            new Func<T, IQueryable<T>>(Values).Method,
            Expression.Constant(value, typeof(T))
        ));
    }

    /// <summary>
    /// Wraps an in-memory list of rows into a queryable backed by an inline values source, so you
    /// can join or filter against a small set without creating a temporary table. Each item becomes
    /// one row. An empty list yields a source with no rows. Handy for batch lookups, parameterized
    /// IN lists and tests. Named <c>ValuesRange</c> rather than overloading <c>Values</c> because a
    /// list argument would otherwise bind to the single-row <see cref="Values{T}(T)" />.
    /// </summary>
    public IQueryable<T> ValuesRange<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(IEnumerable<T> values)
    {
        ArgumentNullException.ThrowIfNull(values);

        return new Queryable<T>(this, Expression.Call(
            Expression.Constant(this),
            new Func<IEnumerable<T>, IQueryable<T>>(ValuesRange).Method,
            Expression.Constant(values, typeof(IEnumerable<T>))
        ));
    }

    /// <summary>
    /// Defines a non-recursive Common Table Expression (CTE) that can be used in a subsequent query.
    /// </summary>
    public SQLiteCte<T> With<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(Expression<Func<IQueryable<T>>> query)
    {
        return new SQLiteCte<T>(this, query);
    }

    /// <summary>
    /// Defines a non-recursive Common Table Expression (CTE) with an explicit materialization hint.
    /// </summary>
    /// <param name="query">The lambda that returns the CTE body query.</param>
    /// <param name="materialization">
    /// Materialization hint emitted between <c>AS</c> and the opening parenthesis.
    /// <see cref="SQLiteCteMaterialization.Default" /> emits no hint and lets SQLite choose.
    /// </param>
#if SQLITE_FRAMEWORK_OS_BUNDLED_SQLITE
    [UnsupportedOSPlatform("ios")]
    [SupportedOSPlatform("ios15.0")]
#endif
    public SQLiteCte<T> With<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(Expression<Func<IQueryable<T>>> query, SQLiteCteMaterialization materialization)
    {
        return new SQLiteCte<T>(this, query) { Materialization = materialization };
    }

    /// <summary>
    /// Defines a recursive Common Table Expression (CTE). The lambda parameter is the self-reference used in the recursive term.
    /// </summary>
    public SQLiteCte<T> WithRecursive<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(Expression<Func<IQueryable<T>, IQueryable<T>>> query)
    {
        return new SQLiteCte<T>(this, query);
    }

    /// <summary>
    /// Defines a recursive Common Table Expression (CTE) with an explicit materialization hint.
    /// </summary>
    /// <param name="query">The lambda that defines the recursive CTE body. The parameter is the self-reference.</param>
    /// <param name="materialization">
    /// Materialization hint emitted between <c>AS</c> and the opening parenthesis.
    /// <see cref="SQLiteCteMaterialization.Default" /> emits no hint and lets SQLite choose.
    /// </param>
#if SQLITE_FRAMEWORK_OS_BUNDLED_SQLITE
    [UnsupportedOSPlatform("ios")]
    [SupportedOSPlatform("ios15.0")]
#endif
    public SQLiteCte<T> WithRecursive<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(Expression<Func<IQueryable<T>, IQueryable<T>>> query, SQLiteCteMaterialization materialization)
    {
        return new SQLiteCte<T>(this, query) { Materialization = materialization };
    }

    /// <summary>
    /// Executes the SQL query and returns the results as a list.
    /// </summary>
    public List<T> Query<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(string sql, params SQLiteParameter[] parameters)
    {
        return CreateCommand(sql, [.. parameters]).ExecuteQuery<T>().ToList();
    }

    /// <summary>
    /// Executes the SQL query and returns the results as a list.
    /// </summary>
    public List<T> Query<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(string sql, object parameters)
    {
        return CreateCommand(sql, ToParameterList(parameters)).ExecuteQuery<T>().ToList();
    }

    /// <summary>
    /// Executes the SQL query and returns the first result or throws if the sequence is empty.
    /// </summary>
    public T QueryFirst<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(string sql, params SQLiteParameter[] parameters)
    {
        return CreateCommand(sql, [.. parameters]).ExecuteQuery<T>().First();
    }

    /// <summary>
    /// Executes the SQL query and returns the first result or throws if the sequence is empty.
    /// </summary>
    public T QueryFirst<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(string sql, object parameters)
    {
        return CreateCommand(sql, ToParameterList(parameters)).ExecuteQuery<T>().First();
    }

    /// <summary>
    /// Executes the SQL query and returns the first result or <see langword="null" /> if the sequence is empty.
    /// </summary>
    public T? QueryFirstOrDefault<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(string sql, params SQLiteParameter[] parameters)
    {
        return CreateCommand(sql, [.. parameters]).ExecuteQuery<T>().FirstOrDefault();
    }

    /// <summary>
    /// Executes the SQL query and returns the first result or <see langword="null" /> if the sequence is empty.
    /// </summary>
    public T? QueryFirstOrDefault<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(string sql, object parameters)
    {
        return CreateCommand(sql, ToParameterList(parameters)).ExecuteQuery<T>().FirstOrDefault();
    }

    /// <summary>
    /// Executes the SQL query and returns a single result or throws if the sequence is empty or contains more than one row.
    /// </summary>
    public T QuerySingle<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(string sql, params SQLiteParameter[] parameters)
    {
        return CreateCommand(sql, [.. parameters]).ExecuteQuery<T>().Single();
    }

    /// <summary>
    /// Executes the SQL query and returns a single result or throws if the sequence is empty or contains more than one row.
    /// </summary>
    public T QuerySingle<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(string sql, object parameters)
    {
        return CreateCommand(sql, ToParameterList(parameters)).ExecuteQuery<T>().Single();
    }

    /// <summary>
    /// Executes the SQL query and returns a single result or <see langword="null" /> if the sequence is empty. Throws if more
    /// than one row is returned.
    /// </summary>
    public T? QuerySingleOrDefault<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(string sql, params SQLiteParameter[] parameters)
    {
        return CreateCommand(sql, [.. parameters]).ExecuteQuery<T>().SingleOrDefault();
    }

    /// <summary>
    /// Executes the SQL query and returns a single result or <see langword="null" /> if the sequence is empty. Throws if more
    /// than one row is returned.
    /// </summary>
    public T? QuerySingleOrDefault<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(string sql, object parameters)
    {
        return CreateCommand(sql, ToParameterList(parameters)).ExecuteQuery<T>().SingleOrDefault();
    }

    /// <summary>
    /// Executes the SQL query and returns the value of the first column of the first row.
    /// </summary>
    public T? ExecuteScalar<T>(string sql, params SQLiteParameter[] parameters)
    {
        using SQLiteDataReader reader = CreateCommand(sql, [.. parameters]).ExecuteReader();

        if (!reader.Read())
        {
            return default;
        }

        object? scalar = reader.GetValue(0, reader.GetColumnType(0), typeof(T));
        return scalar is null ? default : (T?)scalar;
    }

    /// <summary>
    /// Executes the SQL query and returns the value of the first column of the first row.
    /// </summary>
    public T? ExecuteScalar<T>(string sql, object parameters)
    {
        using SQLiteDataReader reader = CreateCommand(sql, ToParameterList(parameters)).ExecuteReader();

        if (!reader.Read())
        {
            return default;
        }

        object? scalar = reader.GetValue(0, reader.GetColumnType(0), typeof(T));
        return scalar is null ? default : (T?)scalar;
    }

    /// <summary>
    /// Executes the SQL statement and returns the number of rows affected.
    /// </summary>
    public int Execute(string sql, params SQLiteParameter[] parameters)
    {
        return CreateCommand(sql, [.. parameters]).ExecuteNonQuery();
    }

    /// <summary>
    /// Executes the SQL statement and returns the number of rows affected.
    /// </summary>
    public int Execute(string sql, object parameters)
    {
        return CreateCommand(sql, ToParameterList(parameters)).ExecuteNonQuery();
    }

    /// <summary>
    /// Runs a <c>GroupBy(keySelector)</c> query and groups the rows into <c>IGrouping&lt;,&gt;</c>
    /// values with the type arguments fixed. Generated code calls this so materializing a grouping
    /// works under Native AOT without <c>MakeGenericMethod</c>. Prefer the LINQ surface. This is a
    /// hook for the source generator.
    /// </summary>
    public IEnumerable<IGrouping<TKey, TElement>> ExecuteGeneratedGroupingQuery<TKey, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] TElement>(Expression expression)
        where TKey : notnull
    {
        return ExecuteGroupingQuery<TKey, TElement>(expression);
    }

    internal SQLiteBlobStream OpenBlobStreamWithLock(string tableName, string columnName, long rowid, bool writable, string schema, IDisposable connectionLock)
    {
        try
        {
            ArgumentException.ThrowIfNullOrEmpty(tableName);
            ArgumentException.ThrowIfNullOrEmpty(columnName);
            ValidateSchemaName(schema);
            OpenConnection();
            SQLiteResult result = (SQLiteResult)raw.sqlite3_blob_open(
                Handle!,
                schema,
                tableName,
                columnName,
                rowid,
                writable ? 1 : 0,
                out sqlite3_blob blobHandle);

            if (result != SQLiteResult.OK)
            {
                string message = raw.sqlite3_errmsg(Handle!).utf8_to_string();
                throw new SQLiteException(result, message, null);
            }

            return new SQLiteBlobStream(this, blobHandle, writable, connectionLock);
        }
        catch
        {
            connectionLock.Dispose();
            throw;
        }
    }

    internal (string TableName, string ColumnName) ResolveBlobColumn<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(Expression<Func<T, byte[]?>> columnSelector)
    {
        ArgumentNullException.ThrowIfNull(columnSelector);

        TableMapping mapping = TableMapping<T>();
        if (columnSelector.Body is not MemberExpression member)
        {
            throw new ArgumentException("Expected a property access expression like b => b.Cover.", nameof(columnSelector));
        }

        TableColumn column = mapping.Columns.FirstOrDefault(c => c.PropertyInfo.Name == member.Member.Name)
            ?? throw new ArgumentException($"Property '{member.Member.Name}' is not mapped on {typeof(T).Name}.", nameof(columnSelector));

        return (mapping.TableName, column.Name);
    }

    internal Task WaitConnectionSemaphoreAsync(CancellationToken cancellationToken)
    {
        return connectionSemaphore.WaitAsync(cancellationToken);
    }

    /// <summary>
    /// Returns the shared connection handle for the current async context.
    /// </summary>
    internal sqlite3 GetActiveHandle()
    {
        return Handle!;
    }

    internal bool TryGetCachedTableMapping(Type type, [NotNullWhen(true)] out TableMapping? mapping)
    {
        return tableMappings.TryGetValue(type, out mapping);
    }

    internal bool TryGetAttachedSchema(SQLiteDatabase other, [NotNullWhen(true)] out string? schema)
    {
        return attachedDatabases.TryGetValue(other, out schema);
    }

    internal sqlite3_stmt RentStatement(string sql)
    {
        if (statementPool.TryRent(sql, out sqlite3_stmt pooled))
        {
            return pooled;
        }

        sqlite3 handle = GetActiveHandle();
        SQLiteResult result = (SQLiteResult)raw.sqlite3_prepare_v2(handle, sql, out sqlite3_stmt? stmt);
        if (result != SQLiteResult.OK)
        {
            throw new SQLiteException(result, raw.sqlite3_errmsg(handle).utf8_to_string(), sql);
        }

        return stmt;
    }

    internal void ReturnStatement(string sql, sqlite3_stmt statement)
    {
        statementPool.Return(sql, statement);
    }

    internal long NextCommandId()
    {
        return Interlocked.Increment(ref commandIds);
    }

#if SQLITE_FRAMEWORK_TESTING
    internal void IncrementEntityMaterializerHits()
    {
        Interlocked.Increment(ref entityMaterializerHits);
    }

    internal void IncrementSelectMaterializerHits()
    {
        Interlocked.Increment(ref selectMaterializerHits);
    }

    internal void IncrementSelectCompilerFallbacks()
    {
        Interlocked.Increment(ref selectCompilerFallbacks);
    }
#endif

    [UnconditionalSuppressMessage("AOT", "IL2060", Justification = "IGrouping<TKey, TElement> is rooted by user code.")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "IGrouping<TKey, TElement> is rooted by user code.")]
    internal IEnumerable<T> ExecuteSequenceQuery<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(Expression expression)
    {
        if (typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(IGrouping<,>))
        {
            if (Options.GroupingQueryMaterializers.TryGetValue(typeof(T), out Func<SQLiteDatabase, Expression, object>? groupingMaterializer))
            {
                return (IEnumerable<T>)groupingMaterializer(this, expression);
            }

            if (!RuntimeFeature.IsDynamicCodeSupported && Options.GroupByKeyMaterializers.Count == 0)
            {
                throw new NotSupportedException(
                    $"Materializing 'IGrouping<,>' for '{typeof(T).FullName}' uses MakeGenericMethod, " +
                    "which requires runtime code generation. This path is unavailable when the assembly is built with PublishAot=true. " +
                    "Use the SQLite.Framework source generator with UseGeneratedMaterializers or remove PublishAot.");
            }
            return (IEnumerable<T>)ExecuteGroupingQueryGeneric
                .MakeGenericMethod(typeof(T).GetGenericArguments())
                .Invoke(this, [expression])!;
        }

        SQLTranslator translator = new(this);
        SQLQuery query = translator.Translate(expression);
        SQLiteCommand command = CreateCommand(query.Sql, query.Parameters);

        if (typeof(T).IsInterface || typeof(T).IsAbstract)
        {
            Type concrete = FindRootElementType(expression);
            return CastSequence<T>(command.ExecuteQueryUntypedInternal(query, concrete));
        }

        return command.ExecuteQueryInternal<T>(query);
    }

    internal void ReleaseLock()
    {
        holdsConnectionLock.Value = false;
        connectionSemaphore.Release();
    }

    internal void NotifyTransactionStarted()
    {
        if (!Options.BlockReadsDuringTransaction)
        {
            return;
        }

        lock (readGateLock)
        {
            if (++activeTransactionCount == 1)
            {
                readGateTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            }
        }
    }

    internal void NotifyTransactionEnded()
    {
        if (!Options.BlockReadsDuringTransaction)
        {
            return;
        }

        TaskCompletionSource? toSignal = null;
        lock (readGateLock)
        {
            if (--activeTransactionCount == 0)
            {
                toSignal = readGateTcs;
                readGateTcs = null;
            }
        }

        if (toSignal != null)
        {
            toSignal.TrySetResult();
        }
    }

    internal Task WaitForActiveTransactionsAsync(CancellationToken cancellationToken = default)
    {
        Task? gate = TryGetReadGate();
        return gate == null ? Task.CompletedTask : gate.WaitAsync(cancellationToken);
    }

    internal void SetConnectionLock()
    {
        holdsConnectionLock.Value = true;
    }

    internal async Task<string> AcquireConnectionAndCreateSavepoint(CancellationToken cancellationToken)
    {
        await connectionSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        holdsConnectionLock.Value = true;
        try
        {
            return CreateSavepoint();
        }
        catch
        {
            ReleaseLock();
            throw;
        }
    }

    internal string CreateSavepoint()
    {
        string name = $"SQLITE_AUTOINDEX_{Guid.NewGuid():N}";
        CreateCommand($"SAVEPOINT {name}", []).ExecuteNonQuery();
        return name;
    }

    /// <summary>
    /// Override to declare the database model in one place. The framework calls this once, before
    /// any table mapping is used. Use <paramref name="builder" /> to declare each entity's columns,
    /// keys, computed columns, checks, indexes, foreign keys, defaults, STRICT, WITHOUT ROWID and
    /// triggers, so create, migrate and validate all read the same definition. The base method does nothing.
    /// </summary>
    /// <param name="builder">Builds the model.</param>
    protected virtual void OnModelCreating(SQLiteModelBuilder builder)
    {
    }

    /// <summary>
    /// Override to run code when the database is connecting, before any table mapping is used. The base method does nothing.
    /// </summary>
    protected virtual void OnDatabaseConnecting()
    {
    }

    /// <summary>
    /// Override to run code when the database is connected, after any table mapping is used. The base method does nothing.
    /// </summary>
    protected virtual void OnDatabaseConnected()
    {
    }

    [UnconditionalSuppressMessage("AOT", "IL2095", Justification = "The method has the right attributes to be preserved.")]
    IQueryable<TElement> IQueryProvider.CreateQuery<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] TElement>(Expression expression)
    {
        return new Queryable<TElement>(this, expression);
    }

    IQueryable IQueryProvider.CreateQuery(Expression expression)
    {
        throw new NotSupportedException("Only generic queries are supported.");
    }

    [UnconditionalSuppressMessage("AOT", "IL2072", Justification = "The type should be part of the client assemblies.")]
    [UnconditionalSuppressMessage("AOT", "IL2076", Justification = "The type should be part of the client assemblies.")]
    [UnconditionalSuppressMessage("AOT", "IL2062", Justification = "Type does meet the requirements as it starts from SQLiteTable<T>.")]
    TResult IQueryProvider.Execute<TResult>(Expression expression)
    {
        if (typeof(TResult).IsGenericType
            && typeof(TResult).GetGenericTypeDefinition() == typeof(IGrouping<,>)
            && expression is MethodCallExpression groupingScalar
            && groupingScalar.Method.DeclaringType == typeof(Queryable)
            && groupingScalar.Method.Name is nameof(Queryable.First) or nameof(Queryable.FirstOrDefault)
                or nameof(Queryable.Single) or nameof(Queryable.SingleOrDefault)
                or nameof(Queryable.Last) or nameof(Queryable.LastOrDefault)
                or nameof(Queryable.ElementAt) or nameof(Queryable.ElementAtOrDefault)
            && groupingScalar.Arguments[0] is MethodCallExpression { Method.Name: nameof(Queryable.GroupBy) })
        {
            return ExecuteGroupingScalar<TResult>(groupingScalar);
        }

        if (typeof(TResult).IsGenericType
            && typeof(TResult).GetGenericTypeDefinition() == typeof(IGrouping<,>))
        {
            throw new NotSupportedException(
                "Returning an 'IGrouping<,>' is supported only for First, FirstOrDefault, Single, SingleOrDefault, " +
                "Last, LastOrDefault, ElementAt or ElementAtOrDefault applied directly to a GroupBy(keySelector) call. " +
                "Wrapping GroupBy in other operators before the terminal call is not supported.");
        }

        // Build SQL + parameters
        SQLTranslator translator = new(this);
        SQLQuery query = translator.Translate(expression);

        if (typeof(TResult) == typeof(IEnumerable) && ExpressionHelpers.IsConstant(expression))
        {
            BaseSQLiteQueryable table = (BaseSQLiteQueryable)ExpressionHelpers.GetConstantValue(expression)!;
            SQLiteCommand command = CreateCommand(query.Sql, query.Parameters);
            return (TResult)command.ExecuteQueryUntypedInternal(query, table.ElementType);
        }

        Type elementType = expression.Type;
        SQLiteCommand cmd = CreateCommand(query.Sql, query.Parameters);

        using SQLiteDataReader reader = cmd.ExecuteReader();

        if (query.ThrowOnMoreThanOne)
        {
            if (reader.Read())
            {
                Dictionary<string, int> columns = CommandHelpers.GetColumnNames(reader.Statement);
                SQLiteQueryContext context = BuildQueryObject.BuildContext(reader, columns, query);
                object? raw = BuildQueryObject.CreateInstance(context, elementType, query);

                if (reader.Read())
                {
                    throw new InvalidOperationException("Query returned more than one row");
                }

                return CoerceScalar<TResult>(raw);
            }
        }
        else if (reader.Read())
        {
            Dictionary<string, int> columns = CommandHelpers.GetColumnNames(reader.Statement);
            SQLiteQueryContext context = BuildQueryObject.BuildContext(reader, columns, query);
            object? raw = BuildQueryObject.CreateInstance(context, elementType, query);

            if (raw == null
                && typeof(TResult).IsValueType
                && Nullable.GetUnderlyingType(typeof(TResult)) == null
                && !query.IsRowSelector)
            {
                throw new InvalidOperationException("Query sequence contains no elements");
            }

            return CoerceScalar<TResult>(raw);
        }

        if (query.ThrowOnEmpty)
        {
            if (query.ElementAtSemantic)
            {
                throw new ArgumentOutOfRangeException("index",
                    "ElementAt index is out of range. The sequence does not contain that many elements.");
            }

            throw new InvalidOperationException("Query returned no rows");
        }

        if (query.HasDefaultValue)
        {
            return (TResult)query.DefaultValue!;
        }

        return default!;
    }

    object IQueryProvider.Execute(Expression expression)
    {
        throw new NotSupportedException("Only generic queries are supported.");
    }

    private Task? TryGetReadGate()
    {
        if (!Options.BlockReadsDuringTransaction)
        {
            return null;
        }

        if (holdsConnectionLock.Value)
        {
            return null;
        }

        lock (readGateLock)
        {
            return readGateTcs?.Task;
        }
    }

    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Compiles a grouping predicate. The IGrouping path already needs dynamic code.")]
    [UnconditionalSuppressMessage("AOT", "IL2091", Justification = "TResult is an IGrouping<,> rooted by user code.")]
    private TResult ExecuteGroupingScalar<TResult>(MethodCallExpression scalarCall)
    {
        IEnumerable<TResult> groupings = ExecuteSequenceQuery<TResult>(scalarCall.Arguments[0]);

        string methodName = scalarCall.Method.Name;

        if (methodName is nameof(Queryable.ElementAt) or nameof(Queryable.ElementAtOrDefault))
        {
            int index = (int)ExpressionHelpers.GetConstantValue(scalarCall.Arguments[1])!;
            return methodName == nameof(Queryable.ElementAt)
                ? groupings.ElementAt(index)
                : groupings.ElementAtOrDefault(index)!;
        }

        Func<TResult, bool>? predicate = null;
        if (scalarCall.Arguments.Count == 2)
        {
            LambdaExpression lambda = (LambdaExpression)ExpressionHelpers.StripQuotes(scalarCall.Arguments[1]);
            predicate = (Func<TResult, bool>)lambda.Compile();
        }

        return methodName switch
        {
            nameof(Queryable.First) => predicate != null ? groupings.First(predicate) : groupings.First(),
            nameof(Queryable.FirstOrDefault) => predicate != null ? groupings.FirstOrDefault(predicate)! : groupings.FirstOrDefault()!,
            nameof(Queryable.Single) => predicate != null ? groupings.Single(predicate) : groupings.Single(),
            nameof(Queryable.SingleOrDefault) => predicate != null ? groupings.SingleOrDefault(predicate)! : groupings.SingleOrDefault()!,
            nameof(Queryable.Last) => predicate != null ? groupings.Last(predicate) : groupings.Last(),
            _ => predicate != null ? groupings.LastOrDefault(predicate)! : groupings.LastOrDefault()!,
        };
    }

    private IEnumerable<IGrouping<TKey, TElement>> ExecuteGroupingQuery<TKey, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] TElement>(Expression expression)
        where TKey : notnull
    {
        if (expression is not MethodCallExpression mce
            || mce.Method.DeclaringType != typeof(Queryable)
            || mce.Method.Name != nameof(Queryable.GroupBy)
            || mce.Arguments.Count != 2)
        {
            throw new NotSupportedException(
                "Materializing IGrouping<,> is only supported for a direct GroupBy(keySelector) call. " +
                "Wrapping GroupBy in other LINQ operators before materialization is not supported. " +
                "Move the extra operators after the ToList/ToDictionary call or materialize with ToListAsync() first and group client-side.");
        }

        Expression source = mce.Arguments[0];
        LambdaExpression keyLambda = (LambdaExpression)ExpressionHelpers.StripQuotes(mce.Arguments[1]);

        string keySignature = SelectSignature.Compute(keyLambda.Body);
        Func<SQLiteQueryContext, object?>? keyExtractor = null;
        if (Options.GroupByKeyMaterializers.TryGetValue(keySignature, out Func<SQLiteQueryContext, object?>? generated))
        {
            keyExtractor = generated;
        }
        else if (Options.ReflectionFallbackDisabled)
        {
            throw new InvalidOperationException(
                $"GroupBy key selector fell back to runtime reflection but ReflectionFallbackDisabled is set. " +
                $"The source generator did not cover this key shape. " +
                $"Key signature: {keySignature}. " +
                "Install SQLite.Framework.SourceGenerator and call UseGeneratedMaterializers, " +
                "change the key selector to a shape the generator supports (member access, anonymous type or simple operator), " +
                "or remove the DisableReflectionFallback call.");
        }

        CompiledExpression? compiledKey = null;
        IReadOnlyList<object?>? keyCapturedValues = null;
        if (keyExtractor == null)
        {
            QueryCompilerVisitor compiler = new(Options, keyLambda.Parameters);
            compiledKey = (CompiledExpression)compiler.Visit(keyLambda.Body);
        }
        else
        {
            ReflectedBindingsCollector keyCollector = new();
            keyCollector.Visit(keyLambda.Body);
            keyCapturedValues = keyCollector.CapturedValues;
        }

        SQLTranslator translator = new(this);
        SQLQuery query = translator.Translate(source);
        SQLiteCommand command = CreateCommand(query.Sql, query.Parameters);

        Dictionary<TKey, List<TElement>> groups = new();
        List<TElement>? nullKeyGroup = null;
        List<TKey> order = new();
        foreach (TElement row in command.ExecuteQueryInternal<TElement>(query))
        {
            SQLiteQueryContext keyContext = new() { Input = row, CapturedValues = keyCapturedValues };
            TKey key = keyExtractor != null
                ? (TKey)keyExtractor(keyContext)!
                : (TKey)compiledKey!.Call(keyContext)!;

            if (key is null)
            {
                if (nullKeyGroup == null)
                {
                    nullKeyGroup = new List<TElement>();
                    order.Add(key!);
                }

                nullKeyGroup.Add(row);
                continue;
            }

            if (!groups.TryGetValue(key, out List<TElement>? list))
            {
                list = new List<TElement>();
                groups.Add(key, list);
                order.Add(key);
            }

            list.Add(row);
        }

        foreach (TKey key in order)
        {
            yield return new Grouping<TKey, TElement>(key, key is null ? nullKeyGroup! : groups[key]);
        }
    }

    private void EnsureModelCreated()
    {
        if (modelFrozen)
        {
            return;
        }

        lock (tableMappingsLock)
        {
            if (modelCreated)
            {
                return;
            }

            modelCreated = true;
            try
            {
                OnModelCreating(new SQLiteModelBuilder(this));
            }
            catch
            {
                modelCreated = false;
                throw;
            }

            modelFrozen = true;
        }
    }

    private static TResult CoerceScalar<TResult>(object? raw)
    {
        if (raw == null
            && typeof(TResult).IsValueType
            && Nullable.GetUnderlyingType(typeof(TResult)) == null)
        {
            return default!;
        }

        return (TResult)raw!;
    }

    private static void ValidateSchemaName(string schemaName)
    {
        ArgumentException.ThrowIfNullOrEmpty(schemaName);

        foreach (char c in schemaName)
        {
            if (!char.IsLetterOrDigit(c) && c != '_')
            {
                throw new ArgumentException(
                    "Schema name must contain only letters, digits and underscores.",
                    nameof(schemaName));
            }
        }
    }

    [UnconditionalSuppressMessage("AOT", "IL2073", Justification = "ElementType is preserved by Queryable<T>/SQLiteTable<T>.")]
    [UnconditionalSuppressMessage("AOT", "IL2063", Justification = "Defensive fallback. Chains bottom out at a BaseSQLiteQueryable constant.")]
    [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)]
    private static Type FindRootElementType(Expression expression)
    {
        Expression current = expression;
        while (current is MethodCallExpression mce && mce.Arguments.Count > 0)
        {
            current = mce.Arguments[0];
        }
        if (ExpressionHelpers.IsConstant(current) && ExpressionHelpers.GetConstantValue(current) is BaseSQLiteQueryable table)
        {
            return table.ElementType;
        }
        return current.Type.IsGenericType ? current.Type.GetGenericArguments()[0] : current.Type;
    }

    private static IEnumerable<T> CastSequence<T>(IEnumerable source)
    {
        foreach (object item in source)
        {
            yield return (T)item;
        }
    }

    [UnconditionalSuppressMessage("AOT", "IL2075", Justification = "Parameter objects are user-provided and rooted by user code.")]
    private static List<SQLiteParameter> ToParameterList(object parameters)
    {
        return parameters switch
        {
            SQLiteParameter single => [single],
            IEnumerable<SQLiteParameter> list => [.. list],
            _ => parameters.GetType()
                .GetProperties()
                .Select(p => new SQLiteParameter
                {
                    Name = $"@{p.Name}",
                    Value = p.GetValue(parameters)
                })
                .ToList()
        };
    }
}
