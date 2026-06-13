using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class SmLeftRow
{
    [Key]
    public int Id { get; set; }
}

internal sealed class SmRightRow
{
    [Key]
    public int Id { get; set; }

    public string Label { get; set; } = "";
}

internal sealed class SmPairResult
{
    public int LeftId { get; set; }

    public string RightLabel { get; set; } = "";
}

public class SelectManyScalarResultTests
{
    [Fact]
    public void SelectManyWithScalarResultSelectorProjectsRightColumn()
    {
        using TestDatabase db = new();
        db.Table<SmLeftRow>().Schema.CreateTable();
        db.Table<SmRightRow>().Schema.CreateTable();
        db.Table<SmLeftRow>().Add(new SmLeftRow { Id = 1 });
        db.Table<SmRightRow>().Add(new SmRightRow { Id = 1, Label = "a" });
        db.Table<SmRightRow>().Add(new SmRightRow { Id = 2, Label = "b" });

        List<string> expected = db.Table<SmLeftRow>().AsEnumerable()
            .SelectMany(_ => db.Table<SmRightRow>().AsEnumerable(), (l, r) => r.Label)
            .OrderBy(x => x)
            .ToList();

        Assert.Equal(["a", "b"], expected);

        List<string> actual = db.Table<SmLeftRow>()
            .SelectMany(_ => db.Table<SmRightRow>(), (l, r) => r.Label)
            .OrderBy(x => x)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SelectManyWithMemberInitResultSelectorProjectsRows()
    {
        using TestDatabase db = new();
        db.Table<SmLeftRow>().Schema.CreateTable();
        db.Table<SmRightRow>().Schema.CreateTable();
        db.Table<SmLeftRow>().Add(new SmLeftRow { Id = 7 });
        db.Table<SmRightRow>().Add(new SmRightRow { Id = 1, Label = "a" });
        db.Table<SmRightRow>().Add(new SmRightRow { Id = 2, Label = "b" });

        List<string> expected = db.Table<SmLeftRow>().AsEnumerable()
            .SelectMany(_ => db.Table<SmRightRow>().AsEnumerable(), (l, r) => new SmPairResult { LeftId = l.Id, RightLabel = r.Label })
            .OrderBy(x => x.RightLabel)
            .Select(x => x.RightLabel)
            .ToList();

        Assert.Equal(["a", "b"], expected);

        List<string> actual = db.Table<SmLeftRow>()
            .SelectMany(_ => db.Table<SmRightRow>(), (l, r) => new SmPairResult { LeftId = l.Id, RightLabel = r.Label })
            .OrderBy(x => x.RightLabel)
            .Select(x => x.RightLabel)
            .ToList();

        Assert.Equal(expected, actual);
    }
}
