using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public enum H24qStoreKind
{
    Newspaper = 0,
    Magazine = 1
}

[Table("H24qStoreRows")]
public class H24qStoreRow
{
    [Key]
    public int Id { get; set; }

    public H24qStoreKind Kind { get; set; }
}

public class MigrationStorageModeChangeReencodeTests
{
    [Theory]
    [InlineData(MigrateMode.InPlace)]
    [InlineData(MigrateMode.Rebuild)]
    public void EnumColumnMovedToIntegerStorageStillMatchesAFilterAfterMigrating(MigrateMode mode)
    {
        using ModelTestDatabase db = new(model => model.Entity<H24qStoreRow>());
        db.Execute("CREATE TABLE \"H24qStoreRows\" (\"Id\" INTEGER PRIMARY KEY, \"Kind\" TEXT NOT NULL)");
        db.Execute("INSERT INTO \"H24qStoreRows\" (\"Id\", \"Kind\") VALUES (1, 'Magazine'), (2, 'Newspaper')");

        db.Table<H24qStoreRow>().Schema.Migrate(mode);

        List<H24qStoreRow> materialized = db.Table<H24qStoreRow>().OrderBy(r => r.Id).ToList();
        List<int> expected = materialized
            .Where(r => r.Kind == H24qStoreKind.Magazine)
            .Select(r => r.Id)
            .ToList();

        List<int> actual = db.Table<H24qStoreRow>()
            .Where(r => r.Kind == H24qStoreKind.Magazine)
            .OrderBy(r => r.Id)
            .Select(r => r.Id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void EnumColumnMissingInTheLiveTableIsAddedByTheRebuild()
    {
        using ModelTestDatabase db = new(
            model => model.Entity<H24qStoreRow>(),
            b => b.UseEnumStorage(EnumStorageMode.Text));
        db.Execute("CREATE TABLE \"H24qStoreRows\" (\"Id\" INTEGER PRIMARY KEY)");

        db.Table<H24qStoreRow>().Schema.Migrate(MigrateMode.Rebuild);
        db.Table<H24qStoreRow>().Add(new H24qStoreRow { Id = 1, Kind = H24qStoreKind.Magazine });

        H24qStoreKind kind = db.Table<H24qStoreRow>().Select(r => r.Kind).Single();

        Assert.Equal(H24qStoreKind.Magazine, kind);
    }

    [Theory]
    [InlineData("VARCHAR(20)")]
    [InlineData("CLOB")]
    public void EnumColumnAlreadyStoredAsTextKeepsItsValuesAfterMigrating(string liveType)
    {
        using ModelTestDatabase db = new(
            model => model.Entity<H24qStoreRow>(),
            b => b.UseEnumStorage(EnumStorageMode.Text));
        db.Execute($"CREATE TABLE \"H24qStoreRows\" (\"Id\" INTEGER PRIMARY KEY, \"Kind\" {liveType} NOT NULL)");
        db.Execute("INSERT INTO \"H24qStoreRows\" (\"Id\", \"Kind\") VALUES (1, 'Magazine'), (2, 'Newspaper')");

        db.Table<H24qStoreRow>().Schema.Migrate(MigrateMode.Rebuild);

        List<H24qStoreKind> kinds = db.Table<H24qStoreRow>().OrderBy(r => r.Id).Select(r => r.Kind).ToList();

        Assert.Equal(new List<H24qStoreKind> { H24qStoreKind.Magazine, H24qStoreKind.Newspaper }, kinds);
    }

    [Theory]
    [InlineData(MigrateMode.InPlace)]
    [InlineData(MigrateMode.Rebuild)]
    public void EnumColumnMovedToTextStorageStillMatchesAFilterAfterMigrating(MigrateMode mode)
    {
        using ModelTestDatabase db = new(
            model => model.Entity<H24qStoreRow>(),
            b => b.UseEnumStorage(EnumStorageMode.Text));
        db.Execute("CREATE TABLE \"H24qStoreRows\" (\"Id\" INTEGER PRIMARY KEY, \"Kind\" INTEGER NOT NULL)");
        db.Execute("INSERT INTO \"H24qStoreRows\" (\"Id\", \"Kind\") VALUES (1, 1), (2, 0)");

        db.Table<H24qStoreRow>().Schema.Migrate(mode);

        List<H24qStoreRow> materialized = db.Table<H24qStoreRow>().OrderBy(r => r.Id).ToList();
        List<int> expected = materialized
            .Where(r => r.Kind == H24qStoreKind.Magazine)
            .Select(r => r.Id)
            .ToList();

        List<int> actual = db.Table<H24qStoreRow>()
            .Where(r => r.Kind == H24qStoreKind.Magazine)
            .OrderBy(r => r.Id)
            .Select(r => r.Id)
            .ToList();

        Assert.Equal(expected, actual);
    }
}
