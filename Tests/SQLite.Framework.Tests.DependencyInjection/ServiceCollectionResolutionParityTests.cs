using Microsoft.Extensions.DependencyInjection;
using SQLite.Framework.DependencyInjection;

namespace SQLite.Framework.Tests.DependencyInjection;

public class ServiceCollectionResolutionParityTests
{
    [Fact]
    public void MultipleRegistrations_EachDatabaseUsesItsOwnOptions()
    {
        ServiceCollection services = new();
        services.AddSQLiteDatabase<PrimaryDatabase>(b => b.DatabasePath = "primary.db");
        services.AddSQLiteDatabase<SecondaryDatabase>(b => b.DatabasePath = "secondary.db");

        using ServiceProvider provider = services.BuildServiceProvider();
        PrimaryDatabase primary = provider.GetRequiredService<PrimaryDatabase>();
        SecondaryDatabase secondary = provider.GetRequiredService<SecondaryDatabase>();

        Assert.Equal("primary.db", primary.Options.DatabasePath);
        Assert.Equal("secondary.db", secondary.Options.DatabasePath);
    }

    [Fact]
    public void GenericRegistration_ResolvesBaseDatabaseType()
    {
        ServiceCollection services = new();
        services.AddSQLiteDatabase<PrimaryDatabase>(b => b.DatabasePath = "primary.db");

        using ServiceProvider provider = services.BuildServiceProvider();
        SQLiteDatabase db = provider.GetRequiredService<SQLiteDatabase>();

        Assert.Equal("primary.db", db.Options.DatabasePath);
    }
}
