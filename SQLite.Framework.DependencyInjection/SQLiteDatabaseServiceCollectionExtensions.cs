using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace SQLite.Framework.DependencyInjection;

/// <summary>
/// Registers <see cref="SQLiteDatabase" /> (or a subclass) into an <see cref="IServiceCollection" />.
/// </summary>
public static class SQLiteDatabaseServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="SQLiteDatabase" /> as a service. The <paramref name="configure" /> callback
    /// builds the options when the database is first resolved. <see cref="SQLiteOptions" /> is
    /// registered with the same lifetime so consumers can inject either type.
    /// </summary>
    /// <param name="services">The target service collection.</param>
    /// <param name="configure">Builder callback. Must set <see cref="SQLiteOptionsBuilder.DatabasePath" />.</param>
    /// <param name="lifetime">Lifetime for both the options and the database. Defaults to <see cref="ServiceLifetime.Singleton" />.</param>
    /// <param name="migrations">Optional callback that declares migration versions on the runner.
    /// When set, every database instance this registration creates is migrated with
    /// <see cref="SQLiteMigrationRunner.Migrate" /> right after it is constructed. A database that
    /// is already up to date only reads the recorded version, so the check is cheap. Versions must
    /// not declare async callbacks, since the migration runs synchronously.</param>
    public static IServiceCollection AddSQLiteDatabase(this IServiceCollection services, Action<IServiceProvider, SQLiteOptionsBuilder> configure, ServiceLifetime lifetime = ServiceLifetime.Singleton, Action<SQLiteMigrationRunner>? migrations = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        return services.AddSQLiteDatabase<SQLiteDatabase>(configure, lifetime, migrations);
    }

    /// <summary>
    /// Registers <see cref="SQLiteDatabase" /> as a service using a builder callback that does not
    /// need the service provider.
    /// </summary>
    public static IServiceCollection AddSQLiteDatabase(this IServiceCollection services, Action<SQLiteOptionsBuilder> configure, ServiceLifetime lifetime = ServiceLifetime.Singleton, Action<SQLiteMigrationRunner>? migrations = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        return services.AddSQLiteDatabase<SQLiteDatabase>((_, builder) => configure(builder), lifetime, migrations);
    }

    /// <summary>
    /// Registers a subclass <typeparamref name="TDatabase" /> of <see cref="SQLiteDatabase" /> as a
    /// service. The subclass must expose a public constructor that accepts a single
    /// <see cref="SQLiteOptions" /> argument.
    /// </summary>
    public static IServiceCollection AddSQLiteDatabase<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TDatabase>(this IServiceCollection services, Action<SQLiteOptionsBuilder> configure, ServiceLifetime lifetime = ServiceLifetime.Singleton, Action<SQLiteMigrationRunner>? migrations = null)
        where TDatabase : SQLiteDatabase
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        return services.AddSQLiteDatabase<TDatabase>((_, builder) => configure(builder), lifetime, migrations);
    }

    /// <summary>
    /// Registers a subclass <typeparamref name="TDatabase" /> of <see cref="SQLiteDatabase" /> using
    /// a builder callback that receives the resolving <see cref="IServiceProvider" />.
    /// </summary>
    public static IServiceCollection AddSQLiteDatabase<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TDatabase>(this IServiceCollection services, Action<IServiceProvider, SQLiteOptionsBuilder> configure, ServiceLifetime lifetime = ServiceLifetime.Singleton, Action<SQLiteMigrationRunner>? migrations = null)
        where TDatabase : SQLiteDatabase
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.Add(new ServiceDescriptor(typeof(SQLiteOptions), typeof(TDatabase), (sp, _) => BuildOptions(sp, configure), lifetime));
        services.Add(new ServiceDescriptor(typeof(TDatabase), sp => CreateDatabase<TDatabase>(sp, migrations), lifetime));
        services.Add(new ServiceDescriptor(typeof(SQLiteOptions), sp => sp.GetRequiredKeyedService<SQLiteOptions>(typeof(TDatabase)), lifetime));
        if (typeof(TDatabase) != typeof(SQLiteDatabase))
        {
            services.Add(new ServiceDescriptor(typeof(SQLiteDatabase), sp => sp.GetRequiredService<TDatabase>(), lifetime));
        }

        return services;
    }

    /// <summary>
    /// Registers a factory <see cref="ISQLiteDatabaseFactory{TDatabase}" /> that creates a fresh
    /// <see cref="SQLiteDatabase" /> instance on demand. Use this when a single DI scope needs more
    /// than one database instance.
    /// </summary>
    public static IServiceCollection AddSQLiteDatabaseFactory<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TDatabase>(this IServiceCollection services, Action<SQLiteOptionsBuilder> configure, ServiceLifetime lifetime = ServiceLifetime.Singleton, Action<SQLiteMigrationRunner>? migrations = null)
        where TDatabase : SQLiteDatabase
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        return services.AddSQLiteDatabaseFactory<TDatabase>((_, builder) => configure(builder), lifetime, migrations);
    }

    /// <summary>
    /// Registers a factory <see cref="ISQLiteDatabaseFactory{TDatabase}" /> that creates a fresh
    /// <see cref="SQLiteDatabase" /> instance on demand, using a builder callback that receives the
    /// resolving <see cref="IServiceProvider" />.
    /// </summary>
    /// <param name="services">The target service collection.</param>
    /// <param name="configure">Builder callback. Must set <see cref="SQLiteOptionsBuilder.DatabasePath" />.</param>
    /// <param name="lifetime">Lifetime for the options and the factory. Defaults to <see cref="ServiceLifetime.Singleton" />.</param>
    /// <param name="migrations">Optional callback that declares migration versions on the runner.
    /// When set, every database instance the factory creates is migrated with
    /// <see cref="SQLiteMigrationRunner.Migrate" /> right after it is constructed.</param>
    public static IServiceCollection AddSQLiteDatabaseFactory<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TDatabase>(this IServiceCollection services, Action<IServiceProvider, SQLiteOptionsBuilder> configure, ServiceLifetime lifetime = ServiceLifetime.Singleton, Action<SQLiteMigrationRunner>? migrations = null)
        where TDatabase : SQLiteDatabase
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.Add(new ServiceDescriptor(typeof(SQLiteOptions), typeof(TDatabase), (sp, _) => BuildOptions(sp, configure), lifetime));
        services.Add(new ServiceDescriptor(
            typeof(ISQLiteDatabaseFactory<TDatabase>),
            sp => new SQLiteDatabaseFactory<TDatabase>(sp, sp.GetRequiredKeyedService<SQLiteOptions>(typeof(TDatabase)), migrations),
            lifetime));
        services.Add(new ServiceDescriptor(typeof(SQLiteOptions), sp => sp.GetRequiredKeyedService<SQLiteOptions>(typeof(TDatabase)), lifetime));

        return services;
    }

    private static TDatabase CreateDatabase<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TDatabase>(IServiceProvider sp, Action<SQLiteMigrationRunner>? migrations)
        where TDatabase : SQLiteDatabase
    {
        TDatabase database = ActivatorUtilities.CreateInstance<TDatabase>(sp, sp.GetRequiredKeyedService<SQLiteOptions>(typeof(TDatabase)));
        SQLiteDatabaseMigrations.Apply(database, migrations);
        return database;
    }

    private static SQLiteOptions BuildOptions(IServiceProvider sp, Action<IServiceProvider, SQLiteOptionsBuilder> configure)
    {
        SQLiteOptionsBuilder builder = new(string.Empty);
        configure(sp, builder);
        if (string.IsNullOrEmpty(builder.DatabasePath))
        {
            throw new InvalidOperationException(
                "SQLiteOptionsBuilder.DatabasePath was not set in the configure callback. " +
                "Assign the DatabasePath property on the builder before the callback returns.");
        }

        return builder.Build();
    }
}
