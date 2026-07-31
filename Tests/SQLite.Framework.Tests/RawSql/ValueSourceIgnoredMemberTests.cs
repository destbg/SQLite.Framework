using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H26tIgnoredMemberRows")]
public class H26tIgnoredMemberRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public string Extra { get; set; } = "";
}

public class ValueSourceIgnoredMemberTests
{
    [Fact]
    public void ValuesRangeConcatWithItsTableReadsEveryRowWhenAMemberIsExcludedByTheModel()
    {
        using ModelTestDatabase db = new(model => model.Entity<H26tIgnoredMemberRow>().Ignore(r => r.Extra));
        db.Schema.CreateTable<H26tIgnoredMemberRow>();
        db.Table<H26tIgnoredMemberRow>().AddRange(StoredRows());

        List<string> expected = SeedRows()
            .Concat(StoredRows())
            .Select(r => r.Name)
            .OrderBy(n => n)
            .ToList();

        List<string> actual = db.ValuesRange(SeedRows())
            .Concat(db.Table<H26tIgnoredMemberRow>())
            .ToList()
            .Select(r => r.Name)
            .OrderBy(n => n)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TableConcatWithAValuesRangeReadsEveryRowWhenAMemberIsExcludedByTheModel()
    {
        using ModelTestDatabase db = new(model => model.Entity<H26tIgnoredMemberRow>().Ignore(r => r.Extra));
        db.Schema.CreateTable<H26tIgnoredMemberRow>();
        db.Table<H26tIgnoredMemberRow>().AddRange(StoredRows());

        List<string> expected = StoredRows()
            .Concat(SeedRows())
            .Select(r => r.Name)
            .OrderBy(n => n)
            .ToList();

        List<string> actual = db.Table<H26tIgnoredMemberRow>()
            .Concat(db.ValuesRange(SeedRows()))
            .ToList()
            .Select(r => r.Name)
            .OrderBy(n => n)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H26tIgnoredMemberRow> SeedRows()
    {
        return
        [
            new H26tIgnoredMemberRow { Id = 1, Name = "ann", Extra = "one" },
            new H26tIgnoredMemberRow { Id = 2, Name = "bob", Extra = "two" }
        ];
    }

    private static List<H26tIgnoredMemberRow> StoredRows()
    {
        return
        [
            new H26tIgnoredMemberRow { Id = 3, Name = "cid", Extra = "three" },
            new H26tIgnoredMemberRow { Id = 4, Name = "dee", Extra = "four" }
        ];
    }
}
