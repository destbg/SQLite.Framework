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
using SQLite.Framework.Models;
using SQLitePCL;

namespace SQLite.Framework;

/// <summary>
/// Represents a connection to the SQLite database.
/// </summary>
public class SQLiteDatabase : IQueryProvider, IDisposable
{
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

    static SQLiteDatabase()
    {
#if !NO_SQLITEPCL_RAW_BATTERIES
        Batteries_V2.Init();
#endif
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SQLiteDatabase" /> class.
    /// </summary>
    public SQLiteDatabase(string databasePath)
    {
        DatabasePath = databasePath;
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
    /// The connection string to the SQLite database.
    /// </summary>
    public SQLiteOpenFlags OpenFlags { get; set; } = SQLiteOpenFlags.Create | SQLiteOpenFlags.ReadWrite;

    /// <summary>
    /// The path to the SQLite database file.
    /// </summary>
    public string DatabasePath { get; }

#if SQLITECIPHER
    /// <summary>
    /// The encryption key for the SQLCipher database. Set this before the first operation.
    /// </summary>
    public string? Key { get; set; }
#endif

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
    /// Controls how specific .NET types are stored in and read from the database.
    /// </summary>
    public SQLiteStorageOptions StorageOptions { get; set; } = new();

    /// <summary>
    /// When <see langword="true" />, the database operates in WAL (Write-Ahead Logging) mode.
    /// Concurrent writes from independent async contexts proceed without serialization.
    /// A <c>PRAGMA journal_mode = WAL</c> statement is issued automatically when the connection
    /// is first opened. Set this before the first database operation.
    /// </summary>
    public bool IsWalMode { get; set; }

    /// <summary>
    /// Gets or sets the user-defined version number stored in the database file header.
    /// This can be used to track schema versions.
    /// </summary>
    public int UserVersion
    {
        get => ExecuteScalar<int?>("PRAGMA user_version") ?? 0;
        set => Execute($"PRAGMA user_version = {value}");
    }

    internal bool HoldsConnectionLock => holdsConnectionLock.Value;

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
                TResult result = (TResult)BuildQueryObject.CreateInstance(reader, elementType, columns, query.CreateObject)!;

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
            return (TResult)BuildQueryObject.CreateInstance(reader, elementType, columns, query.CreateObject)!;
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
                table = new TableMapping(type, StorageOptions);
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
                table = new TableMapping(typeof(T), StorageOptions);
                tableMappings.Add(typeof(T), table);
            }

            return table;
        }
    }

    /// <summary>
    /// Creates a new table for the specified type.
    /// </summary>
    public SQLiteTable<T> Table<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>()
    {
        return new SQLiteTable<T>(this, TableMapping<T>());
    }

    /// <summary>
    /// Creates a new table for the specified type.
    /// </summary>
    public SQLiteTable Table([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] Type type)
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
                    DatabasePath,
                    out sqlite3 handle,
                    (int)OpenFlags,
                    null
                );

                if (result != SQLiteResult.OK)
                {
                    throw new SQLiteException(result, "Unable to open database", null);
                }

                Handle = handle;

#if SQLITECIPHER
                if (!string.IsNullOrEmpty(Key))
                {
                    raw.sqlite3_prepare_v2(Handle, $"PRAGMA key = '{Key.Replace("'", "''")}'", out sqlite3_stmt keyStmt);
                    raw.sqlite3_step(keyStmt);
                    raw.sqlite3_finalize(keyStmt);
                }
#endif

                IsConnected = true;

                if (IsWalMode)
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
    /// Locks the all queries from entering the <see cref="Lock" /> method until <see cref="IDisposable.Dispose" /> is
    /// called.
    /// </summary>
    public virtual IDisposable Lock()
    {
        if (holdsConnectionLock.Value)
        {
            return NoOpLockObject.Instance;
        }

        if (IsWalMode)
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
    /// Read operations do not acquire the exclusive connection lock. SQLite's own serialized-mode mutex
    /// ensures that concurrent statements on the same connection are safe, and WAL mode gives each reader
    /// a consistent snapshot regardless of concurrent writers. Only write operations and transactions need
    /// the exclusive lock.
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

    internal IEnumerable<T> ExecuteSequenceQuery<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(Expression expression)
    {
        if (typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(IGrouping<,>))
        {
            Type[] args = typeof(T).GetGenericArguments();
            throw new NotSupportedException(
                $"Materializing GroupBy results as IGrouping<{args[0].Name}, {args[1].Name}> is not supported. " +
                $"Either project the grouping in SQL (e.g. `.GroupBy(x => x.Key).Select(g => new {{ g.Key, Count = g.Count() }})`) " +
                $"so the result is reduced server-side, or materialize rows first and group client-side " +
                $"(`.ToListAsync()` then LINQ `.GroupBy(...)`).");
        }

        SQLTranslator translator = new(this);
        SQLQuery query = translator.Translate(expression);
        SQLiteCommand command = CreateCommand(query.Sql, query.Parameters);
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
            DatabasePath,
            out sqlite3 handle,
            (int)OpenFlags,
            null
        );

        if (result != SQLiteResult.OK)
        {
            throw new SQLiteException(result, "Unable to open database", null);
        }

#if SQLITECIPHER
        if (!string.IsNullOrEmpty(Key))
        {
            raw.sqlite3_prepare_v2(handle, $"PRAGMA key = '{Key.Replace("'", "''")}'", out sqlite3_stmt keyStmt);
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

    internal async Task<string> AcquireConnectionAndCreateSavepoint()
    {
        await connectionSemaphore.WaitAsync().ConfigureAwait(false);
        holdsConnectionLock.Value = true;
        return CreateSavepoint();
    }

    internal string CreateSavepoint()
    {
        string name = $"SQLITE_AUTOINDEX_{Guid.NewGuid():N}";
        CreateCommand($"SAVEPOINT {name}", []).ExecuteNonQuery();
        return name;
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