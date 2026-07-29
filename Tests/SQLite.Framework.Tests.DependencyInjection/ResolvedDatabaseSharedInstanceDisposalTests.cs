using Microsoft.Extensions.DependencyInjection;
using SQLite.Framework.DependencyInjection;

namespace SQLite.Framework.Tests.DependencyInjection;

public sealed class H24pDisposeCountingDatabase : SQLiteDatabase
{
    public H24pDisposeCountingDatabase(SQLiteOptions options)
        : base(options)
    {
    }

    public int DisposeCount { get; private set; }

    public override void Dispose()
    {
        DisposeCount++;
        base.Dispose();
    }
}

public class ResolvedDatabaseSharedInstanceDisposalTests
{
    [Fact]
    public void SingletonResolvedThroughBothServiceTypesSharesOneInstanceAndSurvivesBothDisposals()
    {
        ServiceCollection services = new();
        services.AddSQLiteDatabase<H24pDisposeCountingDatabase>(b => b.DatabasePath = "h24p_dispose_singleton.db");

        H24pDisposeCountingDatabase concrete;
        using (ServiceProvider provider = services.BuildServiceProvider())
        {
            concrete = provider.GetRequiredService<H24pDisposeCountingDatabase>();
            SQLiteDatabase viaBase = provider.GetRequiredService<SQLiteDatabase>();
            Assert.Same(concrete, viaBase);
        }

        Assert.Equal(2, concrete.DisposeCount);
    }

    [Fact]
    public void ScopedResolvedThroughBothServiceTypesSharesOneInstanceAndSurvivesBothDisposals()
    {
        ServiceCollection services = new();
        services.AddSQLiteDatabase<H24pDisposeCountingDatabase>(
            b => b.DatabasePath = "h24p_dispose_scoped.db",
            ServiceLifetime.Scoped);

        using ServiceProvider provider = services.BuildServiceProvider();

        H24pDisposeCountingDatabase concrete;
        using (IServiceScope scope = provider.CreateScope())
        {
            concrete = scope.ServiceProvider.GetRequiredService<H24pDisposeCountingDatabase>();
            SQLiteDatabase viaBase = scope.ServiceProvider.GetRequiredService<SQLiteDatabase>();
            Assert.Same(concrete, viaBase);
        }

        Assert.Equal(2, concrete.DisposeCount);
    }
}
