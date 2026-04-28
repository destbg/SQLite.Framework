using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using SQLite.Framework.Enums;
using SQLite.Framework.Exceptions;
using SQLite.Framework.Extensions;
using SQLite.Framework.Internals;
using SQLite.Framework.Internals.Helpers;
using SQLite.Framework.Internals.Models;
using SQLite.Framework.Internals.Visitors;
using SQLite.Framework.Models;
using SQLitePCL;

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
    private readonly SemaphoreSlim connectionSemaphore = new(1, 1);
    private readonly AsyncLocal<bool> holdsConnectionLock = new();
    private readonly AsyncLocal<sqlite3?> transactionHandle = new();
    private readonly SemaphoreSlim walWriterGate = new(1, 1);
    private readonly Dictionary<Type, TableMapping> tableMappings = [];
    private int walWriterCount;

#if SQLITE_FRAMEWORK_TESTING
    private long entityMaterializerHits;
    private long selectMaterializerHits;
    private long selectCompilerFallbacks;
#endif

    static SQLiteDatabase()
    {
#if !NO_SQLITEPCL_RAW_BATTERIES
        Batteries_V2.Init();
#endif
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
    public IReadOnlyCollection<TableMapping> TableMappings
    {
        get
        {
            lock (tableMappingsLock)
            {
                return tableMappings.Values.ToArray();
            }
        }
    }

    /// <summary>
    /// The configuration for the database. Pass an <see cref="SQLiteOptions" /> built
    /// via <see cref="SQLiteOptionsBuilder" /> to the constructor.
    /// </summary>
    public SQLiteOptions Options { get; }

#if SQLITE_FRAMEWORK_TESTING
    /// <summary>
    /// Number of times a generated entity materializer from <see cref="SQLiteOptions.EntityMaterializers" />
    /// has handled a query row for this database. Read-only counter; increments on every hit.
    /// Only present when the framework is built with the <c>SQLITE_FRAMEWORK_TESTING</c> symbol.
    /// </summary>
    public long EntityMaterializerHits => Interlocked.Read(ref entityMaterializerHits);

    /// <summary>
    /// Number of times a generated Select materializer from <see cref="SQLiteOptions.SelectMaterializers" />
    /// has handled a query for this database. Read-only counter; increments on every hit.
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
    /// Typed access to common SQLite pragmas like foreign keys, journal mode, cache size, and user version.
    /// The instance is built the first time you read this property using <see cref="SQLiteOptions.PragmasFactory" />.
    /// </summary>
    public SQLitePragmas Pragmas { get => field ??= Options.PragmasFactory(this); private set; }

    /// <summary>
    /// DDL operations on the database, including create, drop, alter, and inspection. The instance
    /// is built the first time you read this property using <see cref="SQLiteOptions.SchemaFactory" />.
    /// </summary>
    public SQLiteSchema Schema { get => field ??= Options.SchemaFactory(this); private set; }

    internal bool HoldsConnectionLock => holdsConnectionLock.Value;

    /// <inheritdoc />
    public virtual void Dispose()
    {
        if (IsConnected)
        {
            IsConnected = false;

            lock (connectionOpenLock)
            {
                if (Handle != null)
                {
                    raw.sqlite3_close(Handle);
                    Handle = null;
                }
            }
        }
    }

    [UnconditionalSuppressMessage("AOT", "IL2095", Justification = "The method has the right attributes to be preserved.")]
    IQueryable<TElement> IQueryProvider.CreateQuery<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] TElement>(Expression expression)
    {
        return new Queryable<TElement>(this, expression);
    }

    [ExcludeFromCodeCoverage]
    IQueryable IQueryProvider.CreateQuery(Expression expression)
    {
        throw new NotSupportedException("Only generic queries are supported.");
    }

    [UnconditionalSuppressMessage("AOT", "IL2072", Justification = "The type should be part of the client assemblies.")]
    [UnconditionalSuppressMessage("AOT", "IL2076", Justification = "The type should be part of the client assemblies.")]
    [UnconditionalSuppressMessage("AOT", "IL2062", Justification = "Type does meet the requirements as it starts from SQLiteTable<T>.")]
    TResult IQueryProvider.Execute<TResult>(Expression expression)
    {
        // Build SQL + parameters
        SQLTranslator translator = new(this);
        SQLQuery query = translator.Translate(expression);

        if (typeof(TResult) == typeof(IEnumerable) && CommonHelpers.IsConstant(expression))
        {
            BaseSQLiteTable table = (BaseSQLiteTable)CommonHelpers.GetConstantValue(expression)!;
            SQLiteCommand command = CreateCommand(query.Sql, query.Parameters);
            return (TResult)command.ExecuteQueryUntypedInternal(query, table.ElementType);
        }

        Type elementType = expression.Type;
        SQLiteCommand cmd = CreateCommand(query.Sql, query.Parameters);

        using SQLiteDataReader reader = cmd.ExecuteReader();

        Dictionary<string, int> columns;

        if (query.ThrowOnMoreThanOne)
        {
            if (reader.Read())
            {
                columns = CommandHelpers.GetColumnNames(reader.Statement);
                TResult result = (TResult)BuildQueryObject.CreateInstance(reader, elementType, columns, query)!;

                if (reader.Read())
                {
                    throw new InvalidOperationException("Query returned more than one row");
                }

                return result;
            }
        }
        else if (reader.Read())
        {
            columns = CommandHelpers.GetColumnNames(reader.Statement);
            return (TResult)BuildQueryObject.CreateInstance(reader, elementType, columns, query)!;
        }

        if (query.ThrowOnEmpty)
        {
            throw new InvalidOperationException("Query returned no rows");
        }

        return default!;
    }

    [ExcludeFromCodeCoverage]
    object IQueryProvider.Execute(Expression expression)
    {
        throw new NotSupportedException("Only generic queries are supported.");
    }

    /// <summary>
    /// Called when a command is created using the <see cref="CreateCommand" /> method.
    /// </summary>
    public event Action<SQLiteCommand>? CommandCreated;

    /// <summary>
    /// Creates a new table for the specified type.
    /// </summary>
    public TableMapping TableMapping([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type type)
    {
        lock (tableMappingsLock)
        {
            if (!tableMappings.TryGetValue(type, out TableMapping? table))
            {
                table = new TableMapping(type, Options);
                tableMappings.Add(type, table);
            }

            return table;
        }
    }

    /// <summary>
    /// Creates a new table mapping for the specified type.
    /// </summary>
    public TableMapping TableMapping<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>()
    {
        lock (tableMappingsLock)
        {
            if (!tableMappings.TryGetValue(typeof(T), out TableMapping? table))
            {
                table = new TableMapping(typeof(T), Options);
                tableMappings.Add(typeof(T), table);
            }

            return table;
        }
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
    /// Begins a transaction on the database.
    /// </summary>
    /// <param name="separateConnection">
    /// When <see langword="true" />, the transaction runs on its own dedicated connection to the database file.
    /// All operations in the current async context are routed to that connection automatically,
    /// so standalone reads and writes on the shared connection are not blocked for the duration of the transaction.
    /// When <see langword="false" /> (the default), the transaction uses the shared connection
    /// and holds the exclusive write lock until it is committed or rolled back.
    /// </param>
    public SQLiteTransaction BeginTransaction(bool separateConnection = false)
    {
        bool ownsLock = !holdsConnectionLock.Value;

        if (ownsLock && separateConnection)
        {
            sqlite3 handle = OpenTransactionConnection();
            SetTransactionConnection(handle);
            return new SQLiteTransaction(this, handle);
        }

        if (ownsLock)
        {
            connectionSemaphore.Wait();
            holdsConnectionLock.Value = true;
        }

        string savepointName = $"SQLITE_AUTOINDEX_{Guid.NewGuid():N}";
        CreateCommand($"SAVEPOINT {savepointName}", []).ExecuteNonQuery();
        return new SQLiteTransaction(this, savepointName, ownsLock);
    }

    /// <summary>
    /// Creates a command with the specified SQL and parameters.
    /// </summary>
    public SQLiteCommand CreateCommand(string sql, List<SQLiteParameter> parameters)
    {
        OpenConnection();

        SQLiteCommand cmd = new(this, sql, parameters);

        CommandCreated?.Invoke(cmd);

        return cmd;
    }

    /// <summary>
    /// Opens the connection to the SQLite database.
    /// </summary>
    public void OpenConnection()
    {
        if (IsConnecting)
        {
            lock (connectionOpenLock)
            {
                // Wait for the connection to be opened
            }
        }
        else if (!IsConnected)
        {
            lock (connectionOpenLock)
            {
                IsConnecting = true;

                SQLiteResult result = (SQLiteResult)raw.sqlite3_open_v2(
                    Options.DatabasePath,
                    out sqlite3 handle,
                    (int)Options.OpenFlags,
                    null
                );

                if (result != SQLiteResult.OK)
                {
                    throw new SQLiteException(result, "Unable to open database", null);
                }

                Handle = handle;

#if SQLITECIPHER
                if (!string.IsNullOrEmpty(Options.EncryptionKey))
                {
                    raw.sqlite3_prepare_v2(Handle, $"PRAGMA key = '{Options.EncryptionKey.Replace("'", "''")}'", out sqlite3_stmt keyStmt);
                    raw.sqlite3_step(keyStmt);
                    raw.sqlite3_finalize(keyStmt);
                }
#endif

                IsConnected = true;

                if (Options.IsWalMode)
                {
                    raw.sqlite3_prepare_v2(Handle, "PRAGMA journal_mode = WAL", out sqlite3_stmt walStmt);
                    raw.sqlite3_step(walStmt);
                    raw.sqlite3_finalize(walStmt);
                }

                IsConnecting = false;
            }
        }
    }

    /// <summary>
    /// Copies this database into <paramref name="destination" /> using SQLite's backup API.
    /// The source database stays open for reads and writes during the copy. If a page changes
    /// while the copy is running, SQLite re-copies it for you. Use this for backups, for saving
    /// an in-memory database to a file, or for loading a file into memory at startup.
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
    /// Opens a destination database at <paramref name="destinationPath" />, runs the backup, and
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
    /// Attaches another SQLite file to this connection under the given schema name. After this
    /// call you can read tables in the attached file with raw SQL, like
    /// <c>SELECT * FROM aux.Books</c>.
    /// </summary>
    /// <param name="path">Path to the database file to attach.</param>
    /// <param name="schemaName">The name to give the attached database. Must be a plain identifier
    /// (letters, digits, and underscores).</param>
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
    /// Detaches a previously attached database from this connection.
    /// </summary>
    /// <param name="schemaName">The name the database was attached with.</param>
    public virtual void DetachDatabase(string schemaName)
    {
        ValidateSchemaName(schemaName);

        Execute($"DETACH DATABASE \"{schemaName}\"");
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

        if (Options.IsWalMode)
        {
            AcquireWalWrite();
            return new WalWriteLockObject(this);
        }

        return new LockObject(connectionSemaphore, holdsConnectionLock);
    }

    /// <summary>
    /// Returns a disposable that represents a read operation against the database.
    /// </summary>
    /// <remarks>
    /// Read operations do not take the exclusive connection lock. SQLite uses its own mutex
    /// to keep concurrent statements on the same connection safe, and WAL mode gives each
    /// reader a consistent snapshot even when other connections are writing. Only write
    /// operations and transactions need the exclusive lock.
    /// </remarks>
    public virtual IDisposable ReadLock()
    {
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
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "The type should be part of the client assemblies.")]
    [UnconditionalSuppressMessage("AOT", "IL2060", Justification = "The type should be part of the client assemblies.")]
    public IQueryable<T> Values<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(T value)
    {
        return new Queryable<T>(this, Expression.Call(
            Expression.Constant(this),
            typeof(SQLiteDatabase).GetMethod(nameof(Values), BindingFlags.Instance | BindingFlags.Public)!
                .MakeGenericMethod(typeof(T)),
            Expression.Constant(value, typeof(T))
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
    /// Defines a recursive Common Table Expression (CTE). The lambda parameter is the self-reference used in the recursive term.
    /// </summary>
    public SQLiteCte<T> WithRecursive<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(Expression<Func<IQueryable<T>, IQueryable<T>>> query)
    {
        return new SQLiteCte<T>(this, query);
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
    /// Executes the SQL query and returns the first result, or throws if the sequence is empty.
    /// </summary>
    public T QueryFirst<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(string sql, params SQLiteParameter[] parameters)
    {
        return QueryFirstOrDefault<T>(sql, parameters) ?? throw new InvalidOperationException("Query returned no rows.");
    }

    /// <summary>
    /// Executes the SQL query and returns the first result, or throws if the sequence is empty.
    /// </summary>
    public T QueryFirst<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(string sql, object parameters)
    {
        return QueryFirstOrDefault<T>(sql, parameters) ?? throw new InvalidOperationException("Query returned no rows.");
    }

    /// <summary>
    /// Executes the SQL query and returns the first result, or <see langword="null" /> if the sequence is empty.
    /// </summary>
    public T? QueryFirstOrDefault<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(string sql, params SQLiteParameter[] parameters)
    {
        return CreateCommand(sql, [.. parameters]).ExecuteQuery<T>().FirstOrDefault();
    }

    /// <summary>
    /// Executes the SQL query and returns the first result, or <see langword="null" /> if the sequence is empty.
    /// </summary>
    public T? QueryFirstOrDefault<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(string sql, object parameters)
    {
        return CreateCommand(sql, ToParameterList(parameters)).ExecuteQuery<T>().FirstOrDefault();
    }

    /// <summary>
    /// Executes the SQL query and returns a single result, or throws if the sequence is empty or contains more than one row.
    /// </summary>
    public T QuerySingle<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(string sql, params SQLiteParameter[] parameters)
    {
        return CreateCommand(sql, [.. parameters]).ExecuteQuery<T>().Single();
    }

    /// <summary>
    /// Executes the SQL query and returns a single result, or throws if the sequence is empty or contains more than one row.
    /// </summary>
    public T QuerySingle<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(string sql, object parameters)
    {
        return CreateCommand(sql, ToParameterList(parameters)).ExecuteQuery<T>().Single();
    }

    /// <summary>
    /// Executes the SQL query and returns a single result, or <see langword="null" /> if the sequence is empty. Throws if more
    /// than one row is returned.
    /// </summary>
    public T? QuerySingleOrDefault<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(string sql, params SQLiteParameter[] parameters)
    {
        return CreateCommand(sql, [.. parameters]).ExecuteQuery<T>().SingleOrDefault();
    }

    /// <summary>
    /// Executes the SQL query and returns a single result, or <see langword="null" /> if the sequence is empty. Throws if more
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

        return (T?)reader.GetValue(0, reader.GetColumnType(0), typeof(T));
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

        return (T?)reader.GetValue(0, reader.GetColumnType(0), typeof(T));
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
    /// Returns the active connection handle for the current async context.
    /// When a transaction was started with <c>separateConnection: true</c>,
    /// this returns that transaction's dedicated connection.
    /// Otherwise, it returns the shared connection handle.
    /// </summary>
    internal sqlite3 GetActiveHandle()
    {
        return transactionHandle.Value ?? Handle!;
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

    [UnconditionalSuppressMessage("AOT", "IL2060", Justification = "IGrouping<TKey, TElement> is referenced by user code; TKey and TElement are already rooted by their own code.")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "IGrouping<TKey, TElement> is referenced by user code; TKey and TElement are already rooted by their own code.")]
    internal IEnumerable<T> ExecuteSequenceQuery<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(Expression expression)
    {
        if (typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(IGrouping<,>))
        {
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

    internal void AcquireWalWrite()
    {
        walWriterGate.Wait();
        walWriterCount++;
        if (walWriterCount == 1)
        {
            connectionSemaphore.Wait();
        }

        walWriterGate.Release();
    }

    internal void ReleaseWalWrite()
    {
        walWriterGate.Wait();
        walWriterCount--;
        if (walWriterCount == 0)
        {
            walWriterGate.Release();
            connectionSemaphore.Release();
            return;
        }

        walWriterGate.Release();
    }

    internal void SetConnectionLock()
    {
        holdsConnectionLock.Value = true;
    }

    internal void SetTransactionConnection(sqlite3 handle)
    {
        transactionHandle.Value = handle;
        holdsConnectionLock.Value = true;
    }

    internal void ReleaseTransactionLock()
    {
        holdsConnectionLock.Value = false;
        transactionHandle.Value = null;
    }

    internal sqlite3 OpenTransactionConnection()
    {
        SQLiteResult result = (SQLiteResult)raw.sqlite3_open_v2(
            Options.DatabasePath,
            out sqlite3 handle,
            (int)Options.OpenFlags,
            null
        );

        if (result != SQLiteResult.OK)
        {
            throw new SQLiteException(result, "Unable to open database", null);
        }

#if SQLITECIPHER
        if (!string.IsNullOrEmpty(Options.EncryptionKey))
        {
            raw.sqlite3_prepare_v2(handle, $"PRAGMA key = '{Options.EncryptionKey.Replace("'", "''")}'", out sqlite3_stmt keyStmt);
            raw.sqlite3_step(keyStmt);
            raw.sqlite3_finalize(keyStmt);
        }
#endif

        raw.sqlite3_prepare_v2(handle, "BEGIN", out sqlite3_stmt? stmt);
        SQLiteResult beginResult = (SQLiteResult)raw.sqlite3_step(stmt);
        raw.sqlite3_finalize(stmt);

        if (beginResult != SQLiteResult.Done)
        {
            raw.sqlite3_close(handle);
            throw new SQLiteException(beginResult, "Failed to begin transaction", null);
        }

        return handle;
    }

    internal void CommitOwnedConnection(sqlite3 handle)
    {
        raw.sqlite3_prepare_v2(handle, "COMMIT", out sqlite3_stmt? stmt);
        raw.sqlite3_step(stmt);
        raw.sqlite3_finalize(stmt);
        raw.sqlite3_close(handle);
        ReleaseTransactionLock();
    }

    internal void RollbackOwnedConnection(sqlite3 handle)
    {
        raw.sqlite3_prepare_v2(handle, "ROLLBACK", out sqlite3_stmt? stmt);
        raw.sqlite3_step(stmt);
        raw.sqlite3_finalize(stmt);
        raw.sqlite3_close(handle);
        ReleaseTransactionLock();
    }

    internal async Task<string> AcquireConnectionAndCreateSavepoint(CancellationToken cancellationToken)
    {
        await connectionSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        holdsConnectionLock.Value = true;
        return CreateSavepoint();
    }

    internal string CreateSavepoint()
    {
        string name = $"SQLITE_AUTOINDEX_{Guid.NewGuid():N}";
        CreateCommand($"SAVEPOINT {name}", []).ExecuteNonQuery();
        return name;
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
                "Move the extra operators after the ToList/ToDictionary call, or materialize with ToListAsync() first and group client-side.");
        }

        Expression source = mce.Arguments[0];
        LambdaExpression keyLambda = (LambdaExpression)CommonHelpers.StripQuotes(mce.Arguments[1]);

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
                "change the key selector to a shape the generator supports (member access, anonymous type, or simple operator), " +
                "or remove the DisableReflectionFallback call.");
        }

        CompiledExpression? compiledKey = null;
        if (keyExtractor == null)
        {
            QueryCompilerVisitor compiler = new(keyLambda.Parameters);
            compiledKey = (CompiledExpression)compiler.Visit(keyLambda.Body);
        }

        SQLTranslator translator = new(this);
        SQLQuery query = translator.Translate(source);
        SQLiteCommand command = CreateCommand(query.Sql, query.Parameters);

        Dictionary<TKey, List<TElement>> groups = new();
        List<TKey> order = new();
        foreach (TElement row in command.ExecuteQueryInternal<TElement>(query))
        {
            SQLiteQueryContext keyContext = new() { Input = row };
            TKey key = keyExtractor != null
                ? (TKey)keyExtractor(keyContext)!
                : (TKey)compiledKey!.Call(keyContext)!;
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
            yield return new Grouping<TKey, TElement>(key, groups[key]);
        }
    }

    private static void ValidateSchemaName(string schemaName)
    {
        ArgumentException.ThrowIfNullOrEmpty(schemaName);

        foreach (char c in schemaName)
        {
            if (!char.IsLetterOrDigit(c) && c != '_')
            {
                throw new ArgumentException(
                    "Schema name must contain only letters, digits, and underscores.",
                    nameof(schemaName));
            }
        }
    }

    [UnconditionalSuppressMessage("AOT", "IL2073", Justification = "BaseSQLiteTable.ElementType comes from Queryable<T> / SQLiteTable<T>, which already require PublicProperties and PublicConstructors via DynamicallyAccessedMembers on T.")]
    [UnconditionalSuppressMessage("AOT", "IL2063", Justification = "The fallback path is unreachable in practice, every framework queryable chain bottoms out at a BaseSQLiteTable constant. The fallback exists only for defensiveness.")]
    [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)]
    private static Type FindRootElementType(Expression expression)
    {
        Expression current = expression;
        while (current is MethodCallExpression mce && mce.Arguments.Count > 0)
        {
            current = mce.Arguments[0];
        }
        if (CommonHelpers.IsConstant(current) && CommonHelpers.GetConstantValue(current) is BaseSQLiteTable table)
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

    [UnconditionalSuppressMessage("AOT", "IL2075", Justification = "Parameter objects are user-provided; callers using an anonymous object must preserve its properties (anonymous types declared in user code are preserved automatically).")]
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