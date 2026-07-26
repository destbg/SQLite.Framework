using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("IgnoredConflictRows")]
public class IgnoredConflictRow
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public required string Code { get; set; }
}

public class IgnoredConflictChangeCountTests
{
    [Fact]
    public void IgnoredSingleWriteReportsNoChanges()
    {
        using TestDatabase db = Seeded();

        IgnoredConflictRow duplicate = new() { Id = 1, Code = "first" };
        int changes = db.Table<IgnoredConflictRow>().AddOrUpdate(duplicate, SQLiteConflict.Ignore);

        Assert.Equal(0, changes);
    }

    [Fact]
    public void IgnoredSingleWriteLeavesTheKeyAlone()
    {
        using TestDatabase db = Seeded();

        IgnoredConflictRow duplicate = new() { Id = 1, Code = "first" };
        db.Table<IgnoredConflictRow>().AddOrUpdate(duplicate, SQLiteConflict.Ignore);

        Assert.Equal(1, duplicate.Id);
    }

    [Fact]
    public void IgnoredRangeWriteCountsOnlyTheStoredRows()
    {
        using TestDatabase db = Seeded();

        List<IgnoredConflictRow> rows =
        [
            new IgnoredConflictRow { Id = 1, Code = "first" },
            new IgnoredConflictRow { Id = 2, Code = "second" },
        ];
        int changes = db.Table<IgnoredConflictRow>().AddOrUpdateRange(rows, conflict: SQLiteConflict.Ignore);

        Assert.Equal(1, changes);
    }

    [Fact]
    public void IgnoredRangeWriteLeavesTheDuplicateKeyAlone()
    {
        using TestDatabase db = Seeded();

        IgnoredConflictRow duplicate = new() { Id = 1, Code = "first" };
        List<IgnoredConflictRow> rows =
        [
            duplicate,
            new IgnoredConflictRow { Id = 2, Code = "second" },
        ];
        db.Table<IgnoredConflictRow>().AddOrUpdateRange(rows, conflict: SQLiteConflict.Ignore);

        Assert.Equal(1, duplicate.Id);
    }

    private static TestDatabase Seeded()
    {
        TestDatabase db = new();
        db.Table<IgnoredConflictRow>().Schema.CreateTable();
        db.Table<IgnoredConflictRow>().Add(new IgnoredConflictRow { Id = 0, Code = "seed" });
        db.Execute("UPDATE \"IgnoredConflictRows\" SET \"Id\" = 1 WHERE \"Code\" = 'seed'");
        return db;
    }
}
