using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H22kSpacedFrameRows")]
public class H22kSpacedFrameRow
{
    [Key]
    public int Id { get; set; }

    public int Grp { get; set; }

    public int Amount { get; set; }

    [Column("a ROWS b")]
    public int Weight { get; set; }

    [Column("g GROUPS h")]
    public int Bucket { get; set; }
}

public class WindowClauseAfterSpacedColumnNameTests
{
    [Fact]
    public void PartitionByAfterAValueColumnNamedRowsSumsPerPartition()
    {
        using TestDatabase db = Setup();
        List<H22kSpacedFrameRow> local = Rows();

        List<int> expected = local
            .OrderBy(r => r.Id)
            .Select(r => local.Where(o => o.Grp == r.Grp).Sum(o => o.Weight))
            .ToList();

        List<int> actual = db.Table<H22kSpacedFrameRow>()
            .OrderBy(r => r.Id)
            .Select(r => SQLiteWindowFunctions.Sum(r.Weight)
                .Over()
                .PartitionBy(r.Grp)
                .AsValue())
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void OrderByAfterAPartitionColumnNamedGroupsBuildsRunningTotals()
    {
        using TestDatabase db = Setup();
        List<H22kSpacedFrameRow> local = Rows();

        List<int> expected = local
            .OrderBy(r => r.Id)
            .Select(r => local.Where(o => o.Bucket == r.Bucket && o.Id <= r.Id).Sum(o => o.Amount))
            .ToList();

        List<int> actual = db.Table<H22kSpacedFrameRow>()
            .OrderBy(r => r.Id)
            .Select(r => SQLiteWindowFunctions.Sum(r.Amount)
                .Over()
                .PartitionBy(r.Bucket)
                .OrderBy(r.Id)
                .AsValue())
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H22kSpacedFrameRow> Rows()
    {
        return
        [
            new H22kSpacedFrameRow { Id = 1, Grp = 1, Amount = 10, Weight = 3, Bucket = 7 },
            new H22kSpacedFrameRow { Id = 2, Grp = 1, Amount = 20, Weight = 4, Bucket = 7 },
            new H22kSpacedFrameRow { Id = 3, Grp = 2, Amount = 5, Weight = 6, Bucket = 8 }
        ];
    }

    private static TestDatabase Setup()
    {
        TestDatabase db = new();
        db.Table<H22kSpacedFrameRow>().Schema.CreateTable();
        db.Table<H22kSpacedFrameRow>().AddRange(Rows());
        return db;
    }
}
