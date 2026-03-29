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
    public static SQLiteBeginTransactionAwaitable BeginTransactionAsync(this SQLiteDatabase database)
    {
        return new SQLiteBeginTransactionAwaitable(database);
    }

    /// <summary>
    /// Executes the SQL query and returns the results as a list.
    /// </summary>
    public static Task<List<T>> QueryAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this SQLiteDatabase database, string sql, params SQLiteParameter[] parameters)
    {
        return ExecuteAsync(database.Query<T>, sql, parameters);
    }

    /// <summary>
    /// Executes the SQL query and returns the results as a list.
    /// </summary>
    public static Task<List<T>> QueryAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this SQLiteDatabase database, string sql, object parameters)
    {
        return ExecuteAsync(database.Query<T>, sql, parameters);
    }

    /// <summary>
    /// Executes the SQL query and returns the first result, or throws if the sequence is empty.
    /// </summary>
    public static Task<T> QueryFirstAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this SQLiteDatabase database, string sql, params SQLiteParameter[] parameters)
    {
        return ExecuteAsync(database.QueryFirst<T>, sql, parameters);
    }

    /// <summary>
    /// Executes the SQL query and returns the first result, or throws if the sequence is empty.
    /// </summary>
    public static Task<T> QueryFirstAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this SQLiteDatabase database, string sql, object parameters)
    {
        return ExecuteAsync(database.QueryFirst<T>, sql, parameters);
    }

    /// <summary>
    /// Executes the SQL query and returns the first result, or <see langword="null" /> if the sequence is empty.
    /// </summary>
    public static Task<T?> QueryFirstOrDefaultAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this SQLiteDatabase database, string sql, params SQLiteParameter[] parameters)
    {
        return ExecuteAsync(database.QueryFirstOrDefault<T>, sql, parameters);
    }

    /// <summary>
    /// Executes the SQL query and returns the first result, or <see langword="null" /> if the sequence is empty.
    /// </summary>
    public static Task<T?> QueryFirstOrDefaultAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this SQLiteDatabase database, string sql, object parameters)
    {
        return ExecuteAsync(database.QueryFirstOrDefault<T>, sql, parameters);
    }

    /// <summary>
    /// Executes the SQL query and returns a single result, or throws if the sequence is empty or contains more than one row.
    /// </summary>
    public static Task<T> QuerySingleAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this SQLiteDatabase database, string sql, params SQLiteParameter[] parameters)
    {
        return ExecuteAsync(database.QuerySingle<T>, sql, parameters);
    }

    /// <summary>
    /// Executes the SQL query and returns a single result, or throws if the sequence is empty or contains more than one row.
    /// </summary>
    public static Task<T> QuerySingleAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this SQLiteDatabase database, string sql, object parameters)
    {
        return ExecuteAsync(database.QuerySingle<T>, sql, parameters);
    }

    /// <summary>
    /// Executes the SQL query and returns a single result, or <see langword="null" /> if the sequence is empty. Throws if more
    /// than one row is returned.
    /// </summary>
    public static Task<T?> QuerySingleOrDefaultAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this SQLiteDatabase database, string sql, params SQLiteParameter[] parameters)
    {
        return ExecuteAsync(database.QuerySingleOrDefault<T>, sql, parameters);
    }

    /// <summary>
    /// Executes the SQL query and returns a single result, or <see langword="null" /> if the sequence is empty. Throws if more
    /// than one row is returned.
    /// </summary>
    public static Task<T?> QuerySingleOrDefaultAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this SQLiteDatabase database, string sql, object parameters)
    {
        return ExecuteAsync(database.QuerySingleOrDefault<T>, sql, parameters);
    }

    /// <summary>
    /// Executes the SQL query and returns the value of the first column of the first row.
    /// </summary>
    public static Task<T?> ExecuteScalarAsync<T>(this SQLiteDatabase database, string sql, params SQLiteParameter[] parameters)
    {
        return ExecuteAsync(database.ExecuteScalar<T>, sql, parameters);
    }

    /// <summary>
    /// Executes the SQL query and returns the value of the first column of the first row.
    /// </summary>
    public static Task<T?> ExecuteScalarAsync<T>(this SQLiteDatabase database, string sql, object parameters)
    {
        return ExecuteAsync(database.ExecuteScalar<T>, sql, parameters);
    }

    /// <summary>
    /// Executes the SQL statement and returns the number of rows affected.
    /// </summary>
    public static Task<int> ExecuteAsync(this SQLiteDatabase database, string sql, params SQLiteParameter[] parameters)
    {
        return ExecuteAsync(database.Execute, sql, parameters);
    }

    /// <summary>
    /// Executes the SQL statement and returns the number of rows affected.
    /// </summary>
    public static Task<int> ExecuteAsync(this SQLiteDatabase database, string sql, object parameters)
    {
        return ExecuteAsync(database.Execute, sql, parameters);
    }

    private static Task<T> ExecuteAsync<T, TP1, TP2>(Func<TP1, TP2, T> execute, TP1 parameter1, TP2 parameter2)
    {
        return Task.Factory.StartNew(() => execute(parameter1, parameter2), CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
    }
}