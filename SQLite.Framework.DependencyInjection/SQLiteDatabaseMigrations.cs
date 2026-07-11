namespace SQLite.Framework.DependencyInjection;

/// <summary>
/// Applies the migrations declared on a registration when a database instance is created.
/// </summary>
internal static class SQLiteDatabaseMigrations
{
    /// <summary>
    /// Declares the versions with <paramref name="migrations" /> and migrates the database. The
    /// runner comes from <see cref="SQLiteSchema.Migrations" />, which is already wired to resolve
    /// migration classes from the service provider, so classes added with
    /// <see cref="SQLiteMigrationRunner.Add{T}" /> get their constructor arguments injected. On a
    /// throw the database is disposed before the exception leaves, so a failed resolve does not
    /// leak a connection.
    /// </summary>
    public static void Apply(SQLiteDatabase database, Action<SQLiteMigrationRunner>? migrations)
    {
        if (migrations == null)
        {
            return;
        }

        try
        {
            SQLiteMigrationRunner runner = database.Schema.Migrations();
            migrations(runner);
            runner.Migrate();
        }
        catch
        {
            database.Dispose();
            throw;
        }
    }
}
