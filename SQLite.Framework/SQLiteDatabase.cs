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

// ReSharper disable ChangeFieldTypeToSystemThreadingLock
// ReSharper disable InconsistentlySynchronizedField

namespace SQLite.Framework;

/// <summary>
/// Represents a connection to the SQLite database.
/// </summary>
public class SQLiteDatabase : IQueryProvider, IDisposable
{
    private readonly Dictionary<Type, TableMapping> tableMappings = [];
    private readonly object connectionOpenLock = new();
    private readonly object queryLock = new();

    static SQLiteDatabase()
    {
        Batteries_V2.Init();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SQLiteDatabase"/> class.
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
    /// The connection string to the SQLite database.
    /// </summary>
    public string DatabasePath { get; }

    /// <summary>
    /// Returns the cached table mappings in the current database instance.
    /// </summary>
    public IReadOnlyCollection<TableMapping> TableMappings => tableMappings.Values;

    /// <summary>
    /// Called when a command is created using the <see cref="CreateCommand"/> method.
    /// </summary>
    public event Action<SQLiteCommand>? CommandCreated;

    /// <summary>
    /// Creates a new table for the specified type.
    /// </summary>
    public TableMapping TableMapping([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type type)
    {
        if (!tableMappings.TryGetValue(type, out TableMapping? table))
        {
            table = new(type);
            tableMappings.Add(type, table);
        }

        return table;
    }

    /// <summary>
    /// Creates a new table mapping for the specified type.
    /// </summary>
    public TableMapping TableMapping<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>()
    {
        if (!tableMappings.TryGetValue(typeof(T), out TableMapping? table))
        {
            table = new(typeof(T));
            tableMappings.Add(typeof(T), table);
        }

        return table;
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
    public SQLiteTransaction BeginTransaction()
    {
        string savepointName = $"SQLITE_AUTOINDEX_{Guid.NewGuid():N}";

        CreateCommand($"SAVEPOINT {savepointName}", []).ExecuteNonQuery();
        return new SQLiteTransaction(this, savepointName);
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
                    throw new SQLiteException(result, "Unable to open database");
                }

                Handle = handle;
                IsConnected = true;
                IsConnecting = false;
            }
        }
    }

    /// <summary>
    /// Locks the all queries from entering the <see cref="Lock"/> method until <see cref="IDisposable.Dispose"/> is called.
    /// </summary>
    public IDisposable Lock()
    {
        return new LockObject(queryLock);
    }

    /// <summary>
    /// Wraps the provided SQL query and parameters into a queryable object.
    /// </summary>
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "The type should be part of the client assemblies.")]
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

    [UnconditionalSuppressMessage("AOT", "IL2060", Justification = "Method is marked to not be trimmed.")]
    [UnconditionalSuppressMessage("AOT", "IL2072", Justification = "The type should be part of the client assemblies.")]
    [UnconditionalSuppressMessage("AOT", "IL2076", Justification = "The type should be part of the client assemblies.")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "There is no problem as List<T> will not be trimmed.")]
    [UnconditionalSuppressMessage("AOT", "IL2062", Justification = "Type does meet the requirements as it starts from SQLiteTable<T>.")]
    TResult IQueryProvider.Execute<TResult>(Expression expression)
    {
        // Build SQL + parameters
        SQLTranslator translator = new(this);
        SQLQuery query = translator.Translate(expression);

        if (expression.Type.IsGenericType)
        {
            Type genericType = expression.Type.GetGenericTypeDefinition();
            if (CommonHelpers.GetQueryableType(expression.Type) != null || genericType == typeof(IEnumerable<>))
            {
                Type genericElementType = expression.Type.GetGenericArguments()[0];
                MethodInfo executeQueryMethod = typeof(SQLiteCommandExtensions).GetMethod(
                    nameof(SQLiteCommandExtensions.ExecuteQueryInternal),
                    BindingFlags.Static | BindingFlags.NonPublic
                )!;
                MethodInfo genericExecuteQueryMethod = executeQueryMethod.MakeGenericMethod(genericElementType);

                SQLiteCommand command = CreateCommand(query.Sql, query.Parameters);
                return (TResult)genericExecuteQueryMethod.Invoke(null, [command, query.CreateObject])!;
            }
        }
        else if (typeof(TResult) == typeof(IEnumerable) && CommonHelpers.IsConstant(expression))
        {
#pragma warning disable IL2075 // The element has public constructor
            BaseSQLiteTable table = (BaseSQLiteTable)CommonHelpers.GetConstantValue(expression)!;

            Type genericElementType = table.ElementType;
            MethodInfo executeQueryMethod = typeof(SQLiteCommandExtensions).GetMethod(
                nameof(SQLiteCommandExtensions.ExecuteQueryInternal),
                BindingFlags.Static | BindingFlags.NonPublic
            )!;
            MethodInfo genericExecuteQueryMethod = executeQueryMethod.MakeGenericMethod(genericElementType);

            SQLiteCommand command = CreateCommand(query.Sql, query.Parameters);
            return (TResult)genericExecuteQueryMethod.Invoke(null, [command, query.CreateObject])!;
#pragma warning restore IL2075
        }

        Type elementType = expression.Type;
        SQLiteCommand cmd = CreateCommand(query.Sql, query.Parameters);

        using SQLiteDataReader reader = cmd.ExecuteReader();

        Dictionary<string, (int Index, SQLiteColumnType ColumnType)> columns;

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

    /// <inheritdoc/>
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
}