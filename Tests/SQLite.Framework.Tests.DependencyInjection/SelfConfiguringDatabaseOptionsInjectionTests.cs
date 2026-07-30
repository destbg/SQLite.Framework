using Microsoft.Extensions.DependencyInjection;
using SQLite.Framework.DependencyInjection;
using SQLite.Framework.Enums;

namespace SQLite.Framework.Tests.DependencyInjection;

public sealed class H25tSelfConfiguringDatabase : SQLiteDatabase
{
    public H25tSelfConfiguringDatabase(SQLiteOptions options)
        : base(options)
    {
    }

    protected override void OnConfiguring(SQLiteOptionsBuilder builder)
    {
        builder.UseEnumStorage(EnumStorageMode.Text);
    }
}

public class SelfConfiguringDatabaseOptionsInjectionTests
{
    [Fact]
    public void InjectedOptionsDescribeTheResolvedDatabase()
    {
        ServiceCollection services = new();
        services.AddSQLiteDatabase<H25tSelfConfiguringDatabase>(b => b.DatabasePath = ":memory:");

        using ServiceProvider provider = services.BuildServiceProvider();
        H25tSelfConfiguringDatabase db = provider.GetRequiredService<H25tSelfConfiguringDatabase>();
        SQLiteOptions injected = provider.GetRequiredService<SQLiteOptions>();

        Assert.Equal(EnumStorageMode.Text, db.Options.EnumStorage);
        Assert.Equal(db.Options.EnumStorage, injected.EnumStorage);
    }

    [Fact]
    public void InjectedOptionsIsTheSameInstanceTheResolvedDatabaseUses()
    {
        ServiceCollection services = new();
        services.AddSQLiteDatabase<H25tSelfConfiguringDatabase>(b => b.DatabasePath = ":memory:");

        using ServiceProvider provider = services.BuildServiceProvider();
        H25tSelfConfiguringDatabase db = provider.GetRequiredService<H25tSelfConfiguringDatabase>();
        SQLiteOptions injected = provider.GetRequiredService<SQLiteOptions>();

        Assert.Same(db.Options, injected);
    }
}
