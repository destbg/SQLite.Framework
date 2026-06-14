using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
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

internal static class StaticSelectHolder
{
    public static int Value = 42;
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
    public void SelectManyProjectionWithRegisteredMaterializers()
    {
        using TestDatabase db = new(b => b.SelectMaterializers["__probe__"] = _ => null);
        db.Table<SmLeftRow>().Schema.CreateTable();
        db.Table<SmRightRow>().Schema.CreateTable();
        db.Table<SmLeftRow>().Add(new SmLeftRow { Id = 1 });
        db.Table<SmRightRow>().Add(new SmRightRow { Id = 1, Label = "a" });

        List<string> actual = db.Table<SmLeftRow>()
            .SelectMany(_ => db.Table<SmRightRow>(), (l, r) => new SmPairResult { LeftId = l.Id, RightLabel = r.Label })
            .Select(x => x.RightLabel)
            .ToList();

        Assert.Equal(["a"], actual);
    }

    [Fact]
    public void SelectProjectingMemberOfConstructedObjectReturnsThatMember()
    {
        using TestDatabase db = new();
        db.Table<SmRightRow>().Schema.CreateTable();
        db.Table<SmRightRow>().Add(new SmRightRow { Id = 1, Label = "a" });
        db.Table<SmRightRow>().Add(new SmRightRow { Id = 2, Label = "b" });

        List<SmRightRow> rows = [new SmRightRow { Id = 1, Label = "a" }, new SmRightRow { Id = 2, Label = "b" }];
        List<int> expected = rows
            .Select(r => new SmPairResult { LeftId = r.Id, RightLabel = "x" }.LeftId)
            .ToList();

        List<int> actual = db.Table<SmRightRow>()
            .OrderBy(r => r.Id)
            .Select(r => new SmPairResult { LeftId = r.Id, RightLabel = "x" }.LeftId)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SelectProjectingStaticMemberValue()
    {
        using TestDatabase db = new();
        db.Table<SmRightRow>().Schema.CreateTable();
        db.Table<SmRightRow>().Add(new SmRightRow { Id = 1, Label = "a" });
        db.Table<SmRightRow>().Add(new SmRightRow { Id = 2, Label = "b" });

        List<int> actual = db.Table<SmRightRow>()
            .Select(r => StaticSelectHolder.Value)
            .ToList();

        Assert.Equal([42, 42], actual);
    }

    [Fact]
    public void SelectWithCapturedSelectorExpression()
    {
        using TestDatabase db = new();
        db.Table<SmRightRow>().Schema.CreateTable();
        db.Table<SmRightRow>().Add(new SmRightRow { Id = 1, Label = "a" });
        db.Table<SmRightRow>().Add(new SmRightRow { Id = 2, Label = "b" });

        Expression<Func<SmRightRow, string>> selector = r => r.Label;

        List<string> actual = db.Table<SmRightRow>()
            .Select(selector)
            .OrderBy(x => x)
            .ToList();

        Assert.Equal(["a", "b"], actual);
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
