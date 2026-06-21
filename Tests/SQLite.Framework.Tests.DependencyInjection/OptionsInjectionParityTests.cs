using Microsoft.Extensions.DependencyInjection;
using SQLite.Framework.DependencyInjection;

namespace SQLite.Framework.Tests.DependencyInjection;

public class OptionsInjectionParityTests
{
    [Fact]
    public void SingleRegistration_ConfigureRunsOnce()
    {
        int calls = 0;
        ServiceCollection services = new();
        services.AddSQLiteDatabase<PrimaryDatabase>(b =>
        {
            calls++;
            b.DatabasePath = "primary.db";
        });

        using ServiceProvider provider = services.BuildServiceProvider();
        _ = provider.GetRequiredService<PrimaryDatabase>();
        _ = provider.GetRequiredService<SQLiteOptions>();

        Assert.Equal(1, calls);
    }

    [Fact]
    public void SingleRegistration_InjectedOptionsIsTheDatabaseOptions()
    {
        ServiceCollection services = new();
        services.AddSQLiteDatabase<PrimaryDatabase>(b => b.DatabasePath = "primary.db");

        using ServiceProvider provider = services.BuildServiceProvider();
        PrimaryDatabase db = provider.GetRequiredService<PrimaryDatabase>();
        SQLiteOptions injected = provider.GetRequiredService<SQLiteOptions>();

        Assert.Same(db.Options, injected);
    }
}
