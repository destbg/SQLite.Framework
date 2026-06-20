using System;
using Microsoft.Extensions.DependencyInjection;
using SQLite.Framework.DependencyInjection;

namespace SQLite.Framework.Tests.DependencyInjection;

public class ServiceCollectionRegistrationTests
{
    [Fact]
    public void GenericActionOverload_ResolvesDatabaseWithOptions()
    {
        ServiceCollection services = new();
        services.AddSQLiteDatabase<PrimaryDatabase>(b => b.DatabasePath = "primary.db");

        using ServiceProvider provider = services.BuildServiceProvider();
        PrimaryDatabase db = provider.GetRequiredService<PrimaryDatabase>();

        Assert.Equal("primary.db", db.Options.DatabasePath);
    }

    [Fact]
    public void GenericServiceProviderOverload_ResolvesDatabaseWithOptions()
    {
        ServiceCollection services = new();
        services.AddSQLiteDatabase<PrimaryDatabase>((_, b) => b.DatabasePath = "primary.db");

        using ServiceProvider provider = services.BuildServiceProvider();
        PrimaryDatabase db = provider.GetRequiredService<PrimaryDatabase>();

        Assert.Equal("primary.db", db.Options.DatabasePath);
    }

    [Fact]
    public void NonGenericActionOverload_ResolvesBaseDatabase()
    {
        ServiceCollection services = new();
        services.AddSQLiteDatabase(b => b.DatabasePath = "base.db");

        using ServiceProvider provider = services.BuildServiceProvider();
        SQLiteDatabase db = provider.GetRequiredService<SQLiteDatabase>();

        Assert.Equal("base.db", db.Options.DatabasePath);
    }

    [Fact]
    public void NonGenericServiceProviderOverload_ResolvesBaseDatabase()
    {
        ServiceCollection services = new();
        services.AddSQLiteDatabase((_, b) => b.DatabasePath = "base.db");

        using ServiceProvider provider = services.BuildServiceProvider();
        SQLiteDatabase db = provider.GetRequiredService<SQLiteDatabase>();

        Assert.Equal("base.db", db.Options.DatabasePath);
    }

    [Fact]
    public void ServiceProviderOverload_ReadsConfigurationFromAnotherService()
    {
        ServiceCollection services = new();
        services.AddSingleton(new PathSource { Path = "from-service.db" });
        services.AddSQLiteDatabase<PrimaryDatabase>((sp, b) =>
            b.DatabasePath = sp.GetRequiredService<PathSource>().Path);

        using ServiceProvider provider = services.BuildServiceProvider();
        PrimaryDatabase db = provider.GetRequiredService<PrimaryDatabase>();

        Assert.Equal("from-service.db", db.Options.DatabasePath);
    }

    [Fact]
    public void SubclassWithExtraDependency_ResolvesViaActivatorUtilities()
    {
        ServiceCollection services = new();
        services.AddSingleton<DatabaseDependency>();
        services.AddSQLiteDatabase<DependentDatabase>(b => b.DatabasePath = "dependent.db");

        using ServiceProvider provider = services.BuildServiceProvider();
        DependentDatabase db = provider.GetRequiredService<DependentDatabase>();

        Assert.Equal("dependent.db", db.Options.DatabasePath);
        Assert.Equal("marker", db.Dependency.Marker);
    }

    [Fact]
    public void FactoryActionOverload_CreatesFreshInstances()
    {
        ServiceCollection services = new();
        services.AddSQLiteDatabaseFactory<PrimaryDatabase>(b => b.DatabasePath = "factory.db");

        using ServiceProvider provider = services.BuildServiceProvider();
        ISQLiteDatabaseFactory<PrimaryDatabase> factory =
            provider.GetRequiredService<ISQLiteDatabaseFactory<PrimaryDatabase>>();

        using PrimaryDatabase first = factory.CreateDatabase();
        using PrimaryDatabase second = factory.CreateDatabase();

        Assert.Equal("factory.db", first.Options.DatabasePath);
        Assert.Equal("factory.db", second.Options.DatabasePath);
        Assert.NotSame(first, second);
    }

    [Fact]
    public void FactoryServiceProviderOverload_CreatesFreshInstances()
    {
        ServiceCollection services = new();
        services.AddSQLiteDatabaseFactory<PrimaryDatabase>((_, b) => b.DatabasePath = "factory.db");

        using ServiceProvider provider = services.BuildServiceProvider();
        ISQLiteDatabaseFactory<PrimaryDatabase> factory =
            provider.GetRequiredService<ISQLiteDatabaseFactory<PrimaryDatabase>>();

        using PrimaryDatabase db = factory.CreateDatabase();

        Assert.Equal("factory.db", db.Options.DatabasePath);
    }

    [Fact]
    public void ScopedLifetime_CreatesOnePerScope()
    {
        ServiceCollection services = new();
        services.AddSQLiteDatabase<PrimaryDatabase>(b => b.DatabasePath = "scoped.db", ServiceLifetime.Scoped);

        using ServiceProvider provider = services.BuildServiceProvider();

        PrimaryDatabase first;
        PrimaryDatabase firstAgain;
        using (IServiceScope scope = provider.CreateScope())
        {
            first = scope.ServiceProvider.GetRequiredService<PrimaryDatabase>();
            firstAgain = scope.ServiceProvider.GetRequiredService<PrimaryDatabase>();
        }

        PrimaryDatabase second;
        using (IServiceScope scope = provider.CreateScope())
        {
            second = scope.ServiceProvider.GetRequiredService<PrimaryDatabase>();
        }

        Assert.Same(first, firstAgain);
        Assert.NotSame(first, second);
    }

    [Fact]
    public void MissingDatabasePath_ThrowsInvalidOperation()
    {
        ServiceCollection services = new();
        services.AddSQLiteDatabase<PrimaryDatabase>(_ => { });

        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<PrimaryDatabase>());
    }

    [Fact]
    public void NullServices_Throws()
    {
        IServiceCollection services = null!;

        Assert.Throws<ArgumentNullException>(() => services.AddSQLiteDatabase(_ => { }));
        Assert.Throws<ArgumentNullException>(() => services.AddSQLiteDatabase((_, _) => { }));
        Assert.Throws<ArgumentNullException>(() => services.AddSQLiteDatabase<PrimaryDatabase>(_ => { }));
        Assert.Throws<ArgumentNullException>(() => services.AddSQLiteDatabase<PrimaryDatabase>((_, _) => { }));
        Assert.Throws<ArgumentNullException>(() => services.AddSQLiteDatabaseFactory<PrimaryDatabase>(_ => { }));
        Assert.Throws<ArgumentNullException>(() => services.AddSQLiteDatabaseFactory<PrimaryDatabase>((_, _) => { }));
    }

    [Fact]
    public void NullConfigure_Throws()
    {
        ServiceCollection services = new();

        Assert.Throws<ArgumentNullException>(() => services.AddSQLiteDatabase((Action<SQLiteOptionsBuilder>)null!));
        Assert.Throws<ArgumentNullException>(() => services.AddSQLiteDatabase((Action<IServiceProvider, SQLiteOptionsBuilder>)null!));
        Assert.Throws<ArgumentNullException>(() => services.AddSQLiteDatabase<PrimaryDatabase>((Action<SQLiteOptionsBuilder>)null!));
        Assert.Throws<ArgumentNullException>(() => services.AddSQLiteDatabase<PrimaryDatabase>((Action<IServiceProvider, SQLiteOptionsBuilder>)null!));
        Assert.Throws<ArgumentNullException>(() => services.AddSQLiteDatabaseFactory<PrimaryDatabase>((Action<SQLiteOptionsBuilder>)null!));
        Assert.Throws<ArgumentNullException>(() => services.AddSQLiteDatabaseFactory<PrimaryDatabase>((Action<IServiceProvider, SQLiteOptionsBuilder>)null!));
    }
}

file sealed class PathSource
{
    public required string Path { get; init; }
}
