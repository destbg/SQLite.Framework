using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Enums;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H22dMemoryBackupRows")]
public class H22dMemoryBackupRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public class BackupBetweenPrivateMemoryDatabasesTests
{
    private const string SharedName = "H22dPrivateMemoryDatabase.db3";

    [Fact]
    public void BackupToCopiesRowsBetweenMemoryDatabasesThatShareAName()
    {
        using TestDatabase source = new(UsePrivateMemory);
        using TestDatabase destination = new(UsePrivateMemory);
        Seed(source, 1, "a");
        Assert.False(File.Exists(SharedName));

        source.BackupTo(destination);

        List<H22dMemoryBackupRow> rows = destination.Table<H22dMemoryBackupRow>().ToList();
        Assert.Single(rows);
        Assert.Equal("a", rows[0].Name);
    }

    [Fact]
    public async Task BackupToAsyncCopiesRowsBetweenMemoryDatabasesThatShareAName()
    {
        using TestDatabase source = new(UsePrivateMemory);
        using TestDatabase destination = new(UsePrivateMemory);
        Seed(source, 2, "b");
        Assert.False(File.Exists(SharedName));

        await source.BackupToAsync(destination, ct: TestContext.Current.CancellationToken);

        List<H22dMemoryBackupRow> rows = destination.Table<H22dMemoryBackupRow>().ToList();
        Assert.Single(rows);
        Assert.Equal("b", rows[0].Name);
    }

    private static void Seed(TestDatabase database, int id, string name)
    {
        database.Table<H22dMemoryBackupRow>().Schema.CreateTable();
        database.Table<H22dMemoryBackupRow>().Add(new H22dMemoryBackupRow { Id = id, Name = name });
    }

    private static void UsePrivateMemory(SQLiteOptionsBuilder builder)
    {
        builder.DatabasePath = SharedName;
        builder.UseOpenFlags(SQLiteOpenFlags.Memory | SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create);
    }
}
