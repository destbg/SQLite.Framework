using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Data.Sqlite;
using SQLite.Framework.Extensions;
using SQLite.Framework.Internals;
using SQLite.Framework.Internals.Helpers;
using SQLite.Framework.Internals.Models;
using SQLite.Framework.Models;

// ReSharper disable ChangeFieldTypeToSystemThreadingLock
// ReSharper disable InconsistentlySynchronizedField

namespace SQLite.Framework;

/// <summary>
/// Represents a connection to the SQLite database.
/// </summary>
public class SQLiteDatabase : SqliteConnection, IQueryProvider
{
    private readonly Dictionary<Type, TableMapping> tableMappings = [];
    private readonly object connectionOpenLock = new();
    private readonly object queryLock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="SQLiteDatabase"/> class.
    /// </summary>
    public SQLiteDatabase()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SQLiteDatabase"/> class.
    /// </summary>
    public SQLiteDatabase(string connectionString)
        : base(connectionString)
    {
    }

    /// <summary>
    /// Called when a command is created using the <see cref="CreateCommand(string, Dictionary{string, object?})"/> method.
    /// </summary>
    public event Action<SqliteCommand>? CommandCreated;

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
    public SQLiteTable<T> Table<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T>()
    {
        return new SQLiteTable<T>(this, TableMapping<T>());
    }

    /// <summary>
    /// Creates a new table for the specified type.
    /// </summary>
    public SQLiteTable Table([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type type)
    {
        return new SQLiteTable(this, TableMapping(type));
    }

    /// <summary>
    /// Creates a command with the specified SQL and parameters.
    /// </summary>
    public SqliteCommand CreateCommand(string sql, Dictionary<string, object?> parameters)
    {
        OpenConnection();

        SqliteCommand cmd = base.CreateCommand();
        cmd.CommandText = sql;

        foreach (KeyValuePair<string, object?> p in parameters)
        {
            cmd.Parameters.AddWithValue(p.Key, p.Value ?? DBNull.Value);
        }

        CommandCreated?.Invoke(cmd);

        return cmd;
    }

    /// <summary>
    /// Locks the all queries from entering the <see cref="Lock"/> method until <see cref="IDisposable.Dispose"/> is called.
    /// </summary>
    public IDisposable Lock()
    {
        return new LockObject(queryLock);
    }

    [UnconditionalSuppressMessage("Trimming", "IL2095", Justification = "The method has the right attributes to be preserved.")]
    IQueryable<TElement> IQueryProvider.CreateQuery<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] TElement>(Expression expression)
    {
        return new Queryable<TElement>(this, expression);
    }

    IQueryable IQueryProvider.CreateQuery(Expression expression)
    {
        throw new NotSupportedException("Only creating queries for IQueryable<T> is supported.");
    }

    [UnconditionalSuppressMessage("AOT", "IL2060", Justification = "Method is marked to not be trimmed.")]
    [UnconditionalSuppressMessage("AOT", "IL2072", Justification = "The type should be part of the client assemblies.")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "There is no problem as List<T> will not be trimmed.")]
    [UnconditionalSuppressMessage("Trimming", "IL2062", Justification = "Type does meet the requirements as it starts from SQLiteTable<T>.")]
    TResult IQueryProvider.Execute<TResult>(Expression expression)
    {
        // Build SQL + parameters
        SQLTranslator translator = new(this);
        SQLQuery query = translator.Translate(expression);

        Console.WriteLine(query.Sql);
        foreach (KeyValuePair<string, object?> p in query.Parameters)
        {
            Console.WriteLine($"  {p.Key} = {p.Value}");
        }

        if (expression.Type.IsGenericType)
        {
            Type genericType = expression.Type.GetGenericTypeDefinition();
            if (CommonHelpers.GetQueryableType(expression.Type) != null || genericType == typeof(IEnumerable<>))
            {
                Type genericElementType = expression.Type.GetGenericArguments()[0];
                MethodInfo executeQueryMethod = typeof(SQLiteCommandExtensions).GetMethod(
                    nameof(SQLiteCommandExtensions.ExecuteQuery),
                    new[] { typeof(SqliteCommand) }
                )!;
                MethodInfo genericExecuteQueryMethod = executeQueryMethod.MakeGenericMethod(genericElementType);

                using SqliteCommand command = CreateCommand(query.Sql, query.Parameters);

                return (TResult)genericExecuteQueryMethod.Invoke(null, new object[] { command })!;
            }
        }

        Type elementType = expression.Type;
        using SqliteCommand cmd = CreateCommand(query.Sql, query.Parameters);

        using SqliteDataReader reader = cmd.ExecuteReader();

        if (query.ThrowOnMoreThanOne)
        {
            if (reader.Read())
            {
                TResult result = (TResult)BuildQueryObject.CreateInstance(reader, elementType)!;

                if (reader.Read())
                {
                    throw new InvalidOperationException("Query returned more than one row");
                }

                return result;
            }
        }
        else if (reader.Read())
        {
            return (TResult)BuildQueryObject.CreateInstance(reader, elementType)!;
        }

        if (query.ThrowOnEmpty)
        {
            throw new InvalidOperationException("Query returned no rows");
        }

        return default!;
    }

    object IQueryProvider.Execute(Expression expression)
    {
        throw new NotSupportedException("Only generic queries are supported.");
    }

    private void OpenConnection()
    {
        if (State == ConnectionState.Connecting)
        {
            lock (connectionOpenLock)
            {
                // Wait for the connection to be opened
            }
        }
        else if (State != ConnectionState.Open)
        {
            lock (connectionOpenLock)
            {
                Open();
            }
        }
    }
}