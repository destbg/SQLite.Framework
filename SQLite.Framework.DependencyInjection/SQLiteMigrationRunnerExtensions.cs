namespace SQLite.Framework.DependencyInjection;

/// <summary>
/// Dependency injection helpers for <see cref="SQLiteMigrationRunner" />.
/// </summary>
public static class SQLiteMigrationRunnerExtensions
{
    /// <summary>
    /// Wires the runner so migration classes registered with <see cref="SQLiteMigrationRunner.Add{T}" />
    /// have their constructor arguments resolved from <paramref name="services" />. A migration's
    /// dependencies must be registered, but the migration class itself does not. A runner reached
    /// through <see cref="SQLiteSchema.Migrations" /> on a database created by
    /// <see cref="SQLiteDatabaseServiceCollectionExtensions" /> is already wired. Call this yourself
    /// only when you drive a runner from a database that was not created through dependency injection.
    /// </summary>
    /// <param name="runner">The runner to wire.</param>
    /// <param name="services">The service provider used to build migration classes.</param>
    public static SQLiteMigrationRunner UseServices(this SQLiteMigrationRunner runner, IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(runner);
        ArgumentNullException.ThrowIfNull(services);
        runner.UseMigrationActivator(MigrationServiceActivator.For(services));
        return runner;
    }
}
