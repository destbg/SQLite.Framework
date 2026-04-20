using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace SQLite.Framework.DependencyInjection;

internal sealed class SQLiteDatabaseFactory<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TDatabase> : ISQLiteDatabaseFactory<TDatabase>
    where TDatabase : SQLiteDatabase
{
    private readonly IServiceProvider services;
    private readonly SQLiteOptions options;

    public SQLiteDatabaseFactory(IServiceProvider services, SQLiteOptions options)
    {
        this.services = services;
        this.options = options;
    }

    public TDatabase CreateDatabase()
    {
        return ActivatorUtilities.CreateInstance<TDatabase>(services, options);
    }
}
