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
    /// <param name="ct">Token that cancels the wait for the shared connection lock. Has no effect once the transaction has started.</param>
    public static SQLiteBeginTransactionAwaitable BeginTransactionAsync(this SQLiteDatabase database, bool separateConnection = false, CancellationToken ct = default)
    {
        return new SQLiteBeginTransactionAwaitable(database, separateConnection, ct);
    }

    /// <summary>
    /// Executes the SQL query and returns the results as a list.
    /// </summary>
    public static Task<List<T>> QueryAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this SQLiteDatabase database, string sql, SQLiteParameter[] parameters, CancellationToken ct = default)
    {
        return AsyncRunner.Run(database.Query<T>, sql, parameters, ct);
    }

    /// <summary>
    /// Executes the SQL query and returns the results as a list.
    /// </summary>
    public static Task<List<T>> QueryAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this SQLiteDatabase database, string sql, object parameters, CancellationToken ct = default)
    {
        return AsyncRunner.Run(database.Query<T>, sql, parameters, ct);
    }

    /// <summary>
    /// Executes the SQL query and returns the first result, or throws if the sequence is empty.
    /// </summary>
    public static Task<T> QueryFirstAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this SQLiteDatabase database, string sql, SQLiteParameter[] parameters, CancellationToken ct = default)
    {
        return AsyncRunner.Run(database.QueryFirst<T>, sql, parameters, ct);
    }

    /// <summary>
    /// Executes the SQL query and returns the first result, or throws if the sequence is empty.
    /// </summary>
    public static Task<T> QueryFirstAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this SQLiteDatabase database, string sql, object parameters, CancellationToken ct = default)
    {
        return AsyncRunner.Run(database.QueryFirst<T>, sql, parameters, ct);
    }

    /// <summary>
    /// Executes the SQL query and returns the first result, or <see langword="null" /> if the sequence is empty.
    /// </summary>
    public static Task<T?> QueryFirstOrDefaultAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this SQLiteDatabase database, string sql, SQLiteParameter[] parameters, CancellationToken ct = default)
    {
        return AsyncRunner.Run(database.QueryFirstOrDefault<T>, sql, parameters, ct);
    }

    /// <summary>
    /// Executes the SQL query and returns the first result, or <see langword="null" /> if the sequence is empty.
    /// </summary>
    public static Task<T?> QueryFirstOrDefaultAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this SQLiteDatabase database, string sql, object parameters, CancellationToken ct = default)
    {
        return AsyncRunner.Run(database.QueryFirstOrDefault<T>, sql, parameters, ct);
    }

    /// <summary>
    /// Executes the SQL query and returns a single result, or throws if the sequence is empty or contains more than one row.
    /// </summary>
    public static Task<T> QuerySingleAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this SQLiteDatabase database, string sql, SQLiteParameter[] parameters, CancellationToken ct = default)
    {
        return AsyncRunner.Run(database.QuerySingle<T>, sql, parameters, ct);
    }

    /// <summary>
    /// Executes the SQL query and returns a single result, or throws if the sequence is empty or contains more than one row.
    /// </summary>
    public static Task<T> QuerySingleAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this SQLiteDatabase database, string sql, object parameters, CancellationToken ct = default)
    {
        return AsyncRunner.Run(database.QuerySingle<T>, sql, parameters, ct);
    }

    /// <summary>
    /// Executes the SQL query and returns a single result, or <see langword="null" /> if the sequence is empty. Throws if more
    /// than one row is returned.
    /// </summary>
    public static Task<T?> QuerySingleOrDefaultAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this SQLiteDatabase database, string sql, SQLiteParameter[] parameters, CancellationToken ct = default)
    {
        return AsyncRunner.Run(database.QuerySingleOrDefault<T>, sql, parameters, ct);
    }

    /// <summary>
    /// Executes the SQL query and returns a single result, or <see langword="null" /> if the sequence is empty. Throws if more
    /// than one row is returned.
    /// </summary>
    public static Task<T?> QuerySingleOrDefaultAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this SQLiteDatabase database, string sql, object parameters, CancellationToken ct = default)
    {
        return AsyncRunner.Run(database.QuerySingleOrDefault<T>, sql, parameters, ct);
    }

    /// <summary>
    /// Executes the SQL query and returns the value of the first column of the first row.
    /// </summary>
    public static Task<T?> ExecuteScalarAsync<T>(this SQLiteDatabase database, string sql, SQLiteParameter[] parameters, CancellationToken ct = default)
    {
        return AsyncRunner.Run(database.ExecuteScalar<T>, sql, parameters, ct);
    }

    /// <summary>
    /// Executes the SQL query and returns the value of the first column of the first row.
    /// </summary>
    public static Task<T?> ExecuteScalarAsync<T>(this SQLiteDatabase database, string sql, object parameters, CancellationToken ct = default)
    {
        return AsyncRunner.Run(database.ExecuteScalar<T>, sql, parameters, ct);
    }

    /// <summary>
    /// Executes the SQL statement and returns the number of rows affected.
    /// </summary>
    public static Task<int> ExecuteAsync(this SQLiteDatabase database, string sql, SQLiteParameter[] parameters, CancellationToken ct = default)
    {
        return AsyncRunner.Run(database.Execute, sql, parameters, ct);
    }

    /// <summary>
    /// Executes the SQL statement and returns the number of rows affected.
    /// </summary>
    public static Task<int> ExecuteAsync(this SQLiteDatabase database, string sql, object parameters, CancellationToken ct = default)
    {
        return AsyncRunner.Run(database.Execute, sql, parameters, ct);
    }

    /// <summary>
    /// Copies the database into <paramref name="destination" /> using SQLite's backup API. Runs on a
    /// background thread.
    /// </summary>
    public static Task BackupToAsync(this SQLiteDatabase database, SQLiteDatabase destination, string sourceName = "main", string destName = "main", CancellationToken ct = default)
    {
        return AsyncRunner.Run(() => database.BackupTo(destination, sourceName, destName), ct);
    }

    /// <summary>
    /// Copies the database into a new file at <paramref name="destinationPath" />. Runs on a
    /// background thread.
    /// </summary>
    public static Task BackupToAsync(this SQLiteDatabase database, string destinationPath, CancellationToken ct = default)
    {
        return AsyncRunner.Run(() => database.BackupTo(destinationPath), ct);
    }

    /// <summary>
    /// Attaches another SQLite file to this connection. Runs on a background thread.
    /// </summary>
    public static Task AttachDatabaseAsync(this SQLiteDatabase database, string path, string schemaName, string? encryptionKey = null, CancellationToken ct = default)
    {
        return AsyncRunner.Run(() => database.AttachDatabase(path, schemaName, encryptionKey), ct);
    }

    /// <summary>
    /// Detaches a previously attached database from this connection. Runs on a background thread.
    /// </summary>
    public static Task DetachDatabaseAsync(this SQLiteDatabase database, string schemaName, CancellationToken ct = default)
    {
        return AsyncRunner.Run(() => database.DetachDatabase(schemaName), ct);
    }
}
