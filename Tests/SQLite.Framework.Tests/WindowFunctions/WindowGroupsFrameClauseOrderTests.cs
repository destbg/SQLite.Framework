using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("GroupsFrameRows")]
public class GroupsFrameRow
{
    [Key]
    public int Id { get; set; }

    public int Grp { get; set; }

    public int Amount { get; set; }
}

public class WindowGroupsFrameClauseOrderTests
{
    [Fact]
    public void PartitionByAfterAGroupsFrameThrows()
    {
        using TestDatabase db = Setup();

        Assert.ThrowsAny<Exception>(() => db.Table<GroupsFrameRow>()
            .Select(r => SQLiteWindowFunctions.Sum(r.Amount).Over()
                .OrderBy(r.Id)
                .Groups(SQLiteFrameBoundary.UnboundedPreceding(), SQLiteFrameBoundary.CurrentRow())
                .PartitionBy(r.Grp)
                .AsValue())
            .ToList());
    }

    [Fact]
    public void OrderByAfterAGroupsFrameThrows()
    {
        using TestDatabase db = Setup();

        Assert.ThrowsAny<Exception>(() => db.Table<GroupsFrameRow>()
            .Select(r => SQLiteWindowFunctions.Sum(r.Amount).Over()
                .OrderBy(r.Id)
                .Groups(SQLiteFrameBoundary.UnboundedPreceding(), SQLiteFrameBoundary.CurrentRow())
                .OrderBy(r.Grp)
                .AsValue())
            .ToList());
    }

    [Fact]
    public void PartitionByAfterARangeFrameThrows()
    {
        using TestDatabase db = Setup();

        Assert.ThrowsAny<Exception>(() => db.Table<GroupsFrameRow>()
            .Select(r => SQLiteWindowFunctions.Sum(r.Amount).Over()
                .OrderBy(r.Id)
                .Range(SQLiteFrameBoundary.UnboundedPreceding(), SQLiteFrameBoundary.CurrentRow())
                .PartitionBy(r.Grp)
                .AsValue())
            .ToList());
    }

    [Fact]
    public void PartitionByAfterARowsFrameThrows()
    {
        using TestDatabase db = Setup();

        Assert.ThrowsAny<Exception>(() => db.Table<GroupsFrameRow>()
            .Select(r => SQLiteWindowFunctions.Sum(r.Amount).Over()
                .OrderBy(r.Id)
                .Rows(SQLiteFrameBoundary.UnboundedPreceding(), SQLiteFrameBoundary.CurrentRow())
                .PartitionBy(r.Grp)
                .AsValue())
            .ToList());
    }

    private static TestDatabase Setup()
    {
        TestDatabase db = new();
        db.Table<GroupsFrameRow>().Schema.CreateTable();
        db.Table<GroupsFrameRow>().AddRange(
        [
            new GroupsFrameRow { Id = 1, Grp = 1, Amount = 10 },
            new GroupsFrameRow { Id = 2, Grp = 1, Amount = 20 }
        ]);
        return db;
    }
}
