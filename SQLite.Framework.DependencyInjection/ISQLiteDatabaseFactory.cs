namespace SQLite.Framework.DependencyInjection;

/// <summary>
/// Produces fresh <typeparamref name="TDatabase" /> instances on demand. Mirrors EF's
/// <c>IDbContextFactory&lt;TContext&gt;</c>. Use this when a single DI scope needs more than
/// one <see cref="SQLiteDatabase" /> instance (e.g. a background worker that iterates items and
/// wants a short-lived database per item).
/// </summary>
public interface ISQLiteDatabaseFactory<out TDatabase>
    where TDatabase : SQLiteDatabase
{
    /// <summary>
    /// Creates a new <typeparamref name="TDatabase" /> instance using the options registered
    /// for this factory. The caller owns the returned instance and is responsible for disposing it.
    /// </summary>
    TDatabase CreateDatabase();
}
