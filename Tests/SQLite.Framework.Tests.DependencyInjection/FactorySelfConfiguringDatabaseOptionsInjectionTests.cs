using Microsoft.Extensions.DependencyInjection;
using SQLite.Framework.DependencyInjection;
using SQLite.Framework.Enums;

namespace SQLite.Framework.Tests.DependencyInjection;

public sealed class H26tSelfConfiguringFactoryDatabase : SQLiteDatabase
{
    public H26tSelfConfiguringFactoryDatabase(SQLiteOptions options)
        : base(options)
    {
    }

    protected override void OnConfiguring(SQLiteOptionsBuilder builder)
    {
        builder.UseEnumStorage(EnumStorageMode.Text);
        builder.UseCaseSensitiveStringComparison();
    }
}

public class FactorySelfConfiguringDatabaseOptionsInjectionTests
{
    [Fact]
    public void InjectedOptionsDescribeTheDatabaseTheFactoryCreates()
    {
        ServiceCollection services = new();
        services.AddSQLiteDatabaseFactory<H26tSelfConfiguringFactoryDatabase>(b => b.DatabasePath = ":memory:");

        using ServiceProvider provider = services.BuildServiceProvider();
        ISQLiteDatabaseFactory<H26tSelfConfiguringFactoryDatabase> factory =
            provider.GetRequiredService<ISQLiteDatabaseFactory<H26tSelfConfiguringFactoryDatabase>>();
        SQLiteOptions injected = provider.GetRequiredService<SQLiteOptions>();
        using H26tSelfConfiguringFactoryDatabase db = factory.CreateDatabase();

        Assert.Equal(EnumStorageMode.Text, db.Options.EnumStorage);
        Assert.Equal(EnumStorageMode.Text, injected.EnumStorage);
    }

    [Fact]
    public void InjectedOptionsCarryEveryChangeTheDatabaseMakesWhileConfiguring()
    {
        ServiceCollection services = new();
        services.AddSQLiteDatabaseFactory<H26tSelfConfiguringFactoryDatabase>(b => b.DatabasePath = ":memory:");

        using ServiceProvider provider = services.BuildServiceProvider();
        ISQLiteDatabaseFactory<H26tSelfConfiguringFactoryDatabase> factory =
            provider.GetRequiredService<ISQLiteDatabaseFactory<H26tSelfConfiguringFactoryDatabase>>();
        SQLiteOptions injected = provider.GetRequiredService<SQLiteOptions>();
        using H26tSelfConfiguringFactoryDatabase db = factory.CreateDatabase();

        Assert.True(db.Options.CaseSensitiveStringComparison);
        Assert.True(injected.CaseSensitiveStringComparison);
    }
}
