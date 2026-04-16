using System.Diagnostics.CodeAnalysis;

namespace SQLite.Framework.Extensions;

/// <summary>
/// <see cref="Queryable" /> extensions for <see cref="IQueryable{T}" />.
/// </summary>
[ExcludeFromCodeCoverage]
public static class AsyncDatabaseExtensions
{
    /// <summary>
    /// Begins a transaction on the database asynchronously.
    /// </summary>
    /// <param name="database">The database to begin the transaction on.</param>
    /// <param name="separateConnection">
    /// When <see langword="true" />, the transaction runs on its own dedicated connection to the database file.
    /// All operations in the current async context are routed to that connection automatically,
    /// so standalone reads and writes on the shared connection are not blocked for the duration of the transaction.
    /// When <see langword="false" /> (the default), the transaction uses the shared connection
    /// and holds the exclusive write lock until it is committed or rolled back.
    /// </param>
    public static SQLiteBeginTransactionAwaitable BeginTransactionAsync(this SQLiteDatabase database, bool separateConnection = false)
    {
        return new SQLiteBeginTransactionAwaitable(database, separateConnection);
    }

    /// <summary>
    /// Executes the SQL query and returns the results as a list.
    /// </summary>
    public static Task<List<T>> QueryAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this SQLiteDatabase database, string sql, SQLiteParameter[] parameters, CancellationToken ct = default)
    {
        return ExecuteAsync(database.Query<T>, sql, parameters, ct);
    }

    /// <summary>
    /// Executes the SQL query and returns the results as a list.
    /// </summary>
    public static Task<List<T>> QueryAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this SQLiteDatabase database, string sql, object parameters, CancellationToken ct = default)
    {
        return ExecuteAsync(database.Query<T>, sql, parameters, ct);
    }

    /// <summary>
    /// Executes the SQL query and returns the first result, or throws if the sequence is empty.
    /// </summary>
    public static Task<T> QueryFirstAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this SQLiteDatabase database, string sql, SQLiteParameter[] parameters, CancellationToken ct = default)
    {
        return ExecuteAsync(database.QueryFirst<T>, sql, parameters, ct);
    }

    /// <summary>
    /// Executes the SQL query and returns the first result, or throws if the sequence is empty.
    /// </summary>
    public static Task<T> QueryFirstAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this SQLiteDatabase database, string sql, object parameters, CancellationToken ct = default)
    {
        return ExecuteAsync(database.QueryFirst<T>, sql, parameters, ct);
    }

    /// <summary>
    /// Executes the SQL query and returns the first result, or <see langword="null" /> if the sequence is empty.
    /// </summary>
    public static Task<T?> QueryFirstOrDefaultAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this SQLiteDatabase database, string sql, SQLiteParameter[] parameters, CancellationToken ct = default)
    {
        return ExecuteAsync(database.QueryFirstOrDefault<T>, sql, parameters, ct);
    }

    /// <summary>
    /// Executes the SQL query and returns the first result, or <see langword="null" /> if the sequence is empty.
    /// </summary>
    public static Task<T?> QueryFirstOrDefaultAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this SQLiteDatabase database, string sql, object parameters, CancellationToken ct = default)
    {
        return ExecuteAsync(database.QueryFirstOrDefault<T>, sql, parameters, ct);
    }

    /// <summary>
    /// Executes the SQL query and returns a single result, or throws if the sequence is empty or contains more than one row.
    /// </summary>
    public static Task<T> QuerySingleAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this SQLiteDatabase database, string sql, SQLiteParameter[] parameters, CancellationToken ct = default)
    {
        return ExecuteAsync(database.QuerySingle<T>, sql, parameters, ct);
    }

    /// <summary>
    /// Executes the SQL query and returns a single result, or throws if the sequence is empty or contains more than one row.
    /// </summary>
    public static Task<T> QuerySingleAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this SQLiteDatabase database, string sql, object parameters, CancellationToken ct = default)
    {
        return ExecuteAsync(database.QuerySingle<T>, sql, parameters, ct);
    }

    /// <summary>
    /// Executes the SQL query and returns a single result, or <see langword="null" /> if the sequence is empty. Throws if more
    /// than one row is returned.
    /// </summary>
    public static Task<T?> QuerySingleOrDefaultAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this SQLiteDatabase database, string sql, SQLiteParameter[] parameters, CancellationToken ct = default)
    {
        return ExecuteAsync(database.QuerySingleOrDefault<T>, sql, parameters, ct);
    }

    /// <summary>
    /// Executes the SQL query and returns a single result, or <see langword="null" /> if the sequence is empty. Throws if more
    /// than one row is returned.
    /// </summary>
    public static Task<T?> QuerySingleOrDefaultAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this SQLiteDatabase database, string sql, object parameters, CancellationToken ct = default)
    {
        return ExecuteAsync(database.QuerySingleOrDefault<T>, sql, parameters, ct);
    }

    /// <summary>
    /// Executes the SQL query and returns the value of the first column of the first row.
    /// </summary>
    public static Task<T?> ExecuteScalarAsync<T>(this SQLiteDatabase database, string sql, SQLiteParameter[] parameters, CancellationToken ct = default)
    {
        return ExecuteAsync(database.ExecuteScalar<T>, sql, parameters, ct);
    }

    /// <summary>
    /// Executes the SQL query and returns the value of the first column of the first row.
    /// </summary>
    public static Task<T?> ExecuteScalarAsync<T>(this SQLiteDatabase database, string sql, object parameters, CancellationToken ct = default)
    {
        return ExecuteAsync(database.ExecuteScalar<T>, sql, parameters, ct);
    }

    /// <summary>
    /// Executes the SQL statement and returns the number of rows affected.
    /// </summary>
    public static Task<int> ExecuteAsync(this SQLiteDatabase database, string sql, SQLiteParameter[] parameters, CancellationToken ct = default)
    {
        return ExecuteAsync(database.Execute, sql, parameters, ct);
    }

    /// <summary>
    /// Executes the SQL statement and returns the number of rows affected.
    /// </summary>
    public static Task<int> ExecuteAsync(this SQLiteDatabase database, string sql, object parameters, CancellationToken ct = default)
    {
        return ExecuteAsync(database.Execute, sql, parameters, ct);
    }

    /// <summary>
    /// Returns the user-defined version number stored in the database file header.
    /// </summary>
    public static Task<int> GetUserVersionAsync(this SQLiteDatabase database, CancellationToken ct = default)
    {
        return Task.Factory.StartNew(() => database.UserVersion, ct, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
    }

    /// <summary>
    /// Sets the user-defined version number stored in the database file header.
    /// </summary>
    public static Task SetUserVersionAsync(this SQLiteDatabase database, int version, CancellationToken ct = default)
    {
        return Task.Factory.StartNew(() => database.UserVersion = version, ct, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
    }

    private static Task<T> ExecuteAsync<T, TP1, TP2>(Func<TP1, TP2, T> execute, TP1 parameter1, TP2 parameter2, CancellationToken ct)
    {
        return Task.Factory.StartNew(() => execute(parameter1, parameter2), ct, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
    }
}
