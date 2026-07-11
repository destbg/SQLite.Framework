using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace SQLite.Framework.DependencyInjection;

internal sealed class SQLiteDatabaseFactory<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TDatabase> : ISQLiteDatabaseFactory<TDatabase>
    where TDatabase : SQLiteDatabase
{
    private readonly IServiceProvider services;
    private readonly SQLiteOptions options;
    private readonly Action<SQLiteMigrationRunner>? migrations;

    public SQLiteDatabaseFactory(IServiceProvider services, SQLiteOptions options, Action<SQLiteMigrationRunner>? migrations)
    {
        this.services = services;
        this.options = options;
        this.migrations = migrations;
    }

    public TDatabase CreateDatabase()
    {
        TDatabase database = ActivatorUtilities.CreateInstance<TDatabase>(services, options);
        database.MigrationActivator = MigrationServiceActivator.For(services);
        SQLiteDatabaseMigrations.Apply(database, migrations);
        return database;
    }
}
