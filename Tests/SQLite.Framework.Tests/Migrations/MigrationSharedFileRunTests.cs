using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Attributes;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;
#if SQLITE_FRAMEWORK_SOURCE_GENERATOR
using SQLite.Framework.Generated;
#endif

namespace SQLite.Framework.Tests;

[Table("MigISharedSeeds")]
public class MigISharedSeedRow
{
    [Key]
    [AutoIncrement]
    public int Id { get; set; }

    public string? Name { get; set; }
}

public class MigrationSharedFileRunTests
{
    [Fact]
    public void SecondRunnerOnTheSameFileSeesTheCommittedVersion()
    {
        using TestDatabase dbA = new(b => { b.IsWalMode = true; }, useFile: true);
        dbA.Execute("CREATE TABLE \"MigISharedSeeds\" (\"Id\" INTEGER PRIMARY KEY AUTOINCREMENT, \"Name\" TEXT)");
        string path = dbA.Options.DatabasePath;
        using SQLiteDatabase dbB = OpenSecond(path);

        ManualResetEventSlim midRun = new();
        ManualResetEventSlim letFinish = new();
        Task first = Task.Run(() =>
        {
            dbA.Schema.Migrations()
                .Progress(_ =>
                {
                    midRun.Set();
                    letFinish.Wait(TimeSpan.FromSeconds(5));
                })
                .Version(1, m => m.Insert(new MigISharedSeedRow { Name = "seed" }))
                .Migrate();
        });

        Assert.True(midRun.Wait(TimeSpan.FromSeconds(30)));

        dbB.Schema.Migrations()
            .Version(1, m => m.Insert(new MigISharedSeedRow { Name = "seed" }))
            .Migrate();
        letFinish.Set();
        first.GetAwaiter().GetResult();

        Assert.Equal(1, dbA.ExecuteScalar<int>("SELECT COUNT(*) FROM \"MigISharedSeeds\""));
        Assert.Equal(1, dbA.Pragmas.UserVersion);
    }

    [Fact]
    public async Task SecondRunnerOnTheSameFileSeesTheCommittedVersionDuringMigrateAsync()
    {
        using TestDatabase dbA = new(b => { b.IsWalMode = true; }, useFile: true);
        dbA.Execute("CREATE TABLE \"MigISharedSeeds\" (\"Id\" INTEGER PRIMARY KEY AUTOINCREMENT, \"Name\" TEXT)");
        string path = dbA.Options.DatabasePath;
        using SQLiteDatabase dbB = OpenSecond(path);

        ManualResetEventSlim midRun = new();
        ManualResetEventSlim letFinish = new();
        Task first = Task.Run(async () =>
        {
            await dbA.Schema.Migrations()
                .Progress(_ =>
                {
                    midRun.Set();
                    letFinish.Wait(TimeSpan.FromSeconds(5));
                })
                .Version(1, m => m.Insert(new MigISharedSeedRow { Name = "seed" }))
                .MigrateAsync();
        });

        Assert.True(midRun.Wait(TimeSpan.FromSeconds(30)));

        dbB.Schema.Migrations()
            .Version(1, m => m.Insert(new MigISharedSeedRow { Name = "seed" }))
            .Migrate();
        letFinish.Set();
        await first;

        Assert.Equal(1, dbA.ExecuteScalar<int>("SELECT COUNT(*) FROM \"MigISharedSeeds\""));
        Assert.Equal(1, dbA.Pragmas.UserVersion);
    }

    [Fact]
    public void ScriptOnTheSameFileRetriesAndFindsNothingPending()
    {
        using TestDatabase dbA = new(b => { b.IsWalMode = true; }, useFile: true);
        dbA.Execute("CREATE TABLE \"MigISharedSeeds\" (\"Id\" INTEGER PRIMARY KEY AUTOINCREMENT, \"Name\" TEXT)");
        string path = dbA.Options.DatabasePath;
        using SQLiteDatabase dbB = OpenSecond(path);

        ManualResetEventSlim midRun = new();
        ManualResetEventSlim letFinish = new();
        IReadOnlyList<string>? statements = null;
        Task first = Task.Run(() =>
        {
            statements = dbA.Schema.Migrations()
                .Progress(_ =>
                {
                    midRun.Set();
                    letFinish.Wait(TimeSpan.FromSeconds(5));
                })
                .Version(1, m => m.Insert(new MigISharedSeedRow { Name = "seed" }))
                .Script();
        });

        Assert.True(midRun.Wait(TimeSpan.FromSeconds(30)));

        dbB.Schema.Migrations()
            .Version(1, m => m.Insert(new MigISharedSeedRow { Name = "seed" }))
            .Migrate();
        letFinish.Set();
        first.GetAwaiter().GetResult();

        Assert.Empty(statements!);
        Assert.Equal(1, dbA.ExecuteScalar<int>("SELECT COUNT(*) FROM \"MigISharedSeeds\""));
        Assert.Equal(1, dbA.Pragmas.UserVersion);
    }

    private static SQLiteDatabase OpenSecond(string path)
    {
        SQLiteOptionsBuilder builder = new(path) { IsWalMode = true };
#if SQLITECIPHER
        builder.UseEncryptionKey("test-key");
#endif
#if SQLITE_FRAMEWORK_SOURCE_GENERATOR
        builder.UseGeneratedMaterializers();
        builder.DisableReflectionFallback();
#endif
        return new SQLiteDatabase(builder.Build());
    }
}
