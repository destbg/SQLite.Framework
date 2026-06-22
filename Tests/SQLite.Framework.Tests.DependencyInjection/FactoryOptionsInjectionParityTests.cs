using Microsoft.Extensions.DependencyInjection;
using SQLite.Framework.DependencyInjection;

namespace SQLite.Framework.Tests.DependencyInjection;

public class FactoryOptionsInjectionParityTests
{
    [Fact]
    public void FactoryRegistration_ConfigureRunsOnce()
    {
        int calls = 0;
        ServiceCollection services = new();
        services.AddSQLiteDatabaseFactory<PrimaryDatabase>((_, b) =>
        {
            calls++;
            b.DatabasePath = "factory.db";
        });

        using ServiceProvider provider = services.BuildServiceProvider();
        _ = provider.GetRequiredService<ISQLiteDatabaseFactory<PrimaryDatabase>>();
        _ = provider.GetRequiredService<SQLiteOptions>();

        Assert.Equal(1, calls);
    }

    [Fact]
    public void FactoryRegistration_InjectedOptionsIsTheCreatedDatabaseOptions()
    {
        ServiceCollection services = new();
        services.AddSQLiteDatabaseFactory<PrimaryDatabase>((_, b) => b.DatabasePath = "factory.db");

        using ServiceProvider provider = services.BuildServiceProvider();
        ISQLiteDatabaseFactory<PrimaryDatabase> factory = provider.GetRequiredService<ISQLiteDatabaseFactory<PrimaryDatabase>>();
        SQLiteOptions injected = provider.GetRequiredService<SQLiteOptions>();
        using PrimaryDatabase db = factory.CreateDatabase();

        Assert.Same(injected, db.Options);
    }
}
