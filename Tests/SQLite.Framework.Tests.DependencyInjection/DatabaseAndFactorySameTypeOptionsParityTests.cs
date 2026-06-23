using Microsoft.Extensions.DependencyInjection;
using SQLite.Framework.DependencyInjection;

namespace SQLite.Framework.Tests.DependencyInjection;

public class DatabaseAndFactorySameTypeOptionsParityTests
{
    [Fact]
    public void DatabaseAndFactoryForSameType_ShareOneOptionsPerType()
    {
        ServiceCollection services = new();
        services.AddSQLiteDatabase<PrimaryDatabase>(b => b.DatabasePath = "db_direct.db");
        services.AddSQLiteDatabaseFactory<PrimaryDatabase>(b => b.DatabasePath = "db_factory.db");

        using ServiceProvider provider = services.BuildServiceProvider();
        using PrimaryDatabase direct = provider.GetRequiredService<PrimaryDatabase>();
        ISQLiteDatabaseFactory<PrimaryDatabase> factory = provider.GetRequiredService<ISQLiteDatabaseFactory<PrimaryDatabase>>();
        using PrimaryDatabase fromFactory = factory.CreateDatabase();

        Assert.Same(direct.Options, fromFactory.Options);
        Assert.Equal("db_factory.db", direct.Options.DatabasePath);
    }
}
