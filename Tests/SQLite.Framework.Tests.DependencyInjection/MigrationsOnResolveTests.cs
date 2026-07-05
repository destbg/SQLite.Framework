using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.Extensions.DependencyInjection;
using SQLite.Framework.DependencyInjection;

namespace SQLite.Framework.Tests.DependencyInjection;

[Table("DiMigrationRows")]
public class DiMigrationRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public sealed class DisposeTrackingDatabase : SQLiteDatabase
{
    public DisposeTrackingDatabase(SQLiteOptions options)
        : base(options)
    {
        LastCreated = this;
    }

    public static DisposeTrackingDatabase? LastCreated { get; private set; }

    public bool Disposed { get; private set; }

    public override void Dispose()
    {
        Disposed = true;
        base.Dispose();
    }
}

public class MigrationsOnResolveTests
{
    [Fact]
    public void ResolvedDatabaseIsMigrated()
    {
        ServiceCollection services = new();
        services.AddSQLiteDatabase(
            b => b.DatabasePath = ":memory:",
            migrations: r => r.Version(1, m => m.CreateTable<DiMigrationRow>()));

        using ServiceProvider provider = services.BuildServiceProvider();
        SQLiteDatabase db = provider.GetRequiredService<SQLiteDatabase>();

        Assert.True(db.Schema.TableExists<DiMigrationRow>());
        Assert.Equal(1, db.Pragmas.UserVersion);
    }

    [Fact]
    public void ProviderCallbackOverloadAppliesMigrations()
    {
        ServiceCollection services = new();
        services.AddSQLiteDatabase(
            (_, b) => b.DatabasePath = ":memory:",
            migrations: r => r.Version(1, m => m.CreateTable<DiMigrationRow>()));

        using ServiceProvider provider = services.BuildServiceProvider();
        SQLiteDatabase db = provider.GetRequiredService<SQLiteDatabase>();

        Assert.True(db.Schema.TableExists<DiMigrationRow>());
    }

    [Fact]
    public void SubclassOverloadAppliesMigrations()
    {
        ServiceCollection services = new();
        services.AddSQLiteDatabase<PrimaryDatabase>(
            b => b.DatabasePath = ":memory:",
            migrations: r => r.Version(1, m => m.CreateTable<DiMigrationRow>()));

        using ServiceProvider provider = services.BuildServiceProvider();
        PrimaryDatabase db = provider.GetRequiredService<PrimaryDatabase>();

        Assert.True(db.Schema.TableExists<DiMigrationRow>());
    }

    [Fact]
    public void FactoryCreatesMigratedInstances()
    {
        ServiceCollection services = new();
        services.AddSQLiteDatabaseFactory<PrimaryDatabase>(
            b => b.DatabasePath = ":memory:",
            migrations: r => r.Version(1, m => m.CreateTable<DiMigrationRow>().Insert(new DiMigrationRow { Id = 1, Name = "seed" })));

        using ServiceProvider provider = services.BuildServiceProvider();
        ISQLiteDatabaseFactory<PrimaryDatabase> factory = provider.GetRequiredService<ISQLiteDatabaseFactory<PrimaryDatabase>>();
        using PrimaryDatabase first = factory.CreateDatabase();
        using PrimaryDatabase second = factory.CreateDatabase();

        Assert.Equal("seed", first.Table<DiMigrationRow>().Single().Name);
        Assert.Equal("seed", second.Table<DiMigrationRow>().Single().Name);
    }

    [Fact]
    public void FactoryProviderCallbackOverloadAppliesMigrations()
    {
        ServiceCollection services = new();
        services.AddSQLiteDatabaseFactory<PrimaryDatabase>(
            (_, b) => b.DatabasePath = ":memory:",
            migrations: r => r.Version(1, m => m.CreateTable<DiMigrationRow>()));

        using ServiceProvider provider = services.BuildServiceProvider();
        using PrimaryDatabase db = provider.GetRequiredService<ISQLiteDatabaseFactory<PrimaryDatabase>>().CreateDatabase();

        Assert.True(db.Schema.TableExists<DiMigrationRow>());
    }

    [Fact]
    public void FailedMigrationDisposesDatabaseAndRethrows()
    {
        ServiceCollection services = new();
        services.AddSQLiteDatabase<DisposeTrackingDatabase>(
            b => b.DatabasePath = ":memory:",
            migrations: r => r.Version(1, m => m.Sql("NOT A STATEMENT")));

        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.ThrowsAny<Exception>(() => provider.GetRequiredService<DisposeTrackingDatabase>());
        Assert.NotNull(DisposeTrackingDatabase.LastCreated);
        Assert.True(DisposeTrackingDatabase.LastCreated!.Disposed);
    }

    [Fact]
    public void ResolvedDatabaseWithoutMigrationsIsUntouched()
    {
        ServiceCollection services = new();
        services.AddSQLiteDatabase(b => b.DatabasePath = ":memory:");

        using ServiceProvider provider = services.BuildServiceProvider();
        SQLiteDatabase db = provider.GetRequiredService<SQLiteDatabase>();

        Assert.Equal(0, db.Pragmas.UserVersion);
    }
}
