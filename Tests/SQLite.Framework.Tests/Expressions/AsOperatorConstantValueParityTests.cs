using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H22iAsCastRows")]
public class H22iAsCastRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public string? Tag { get; set; }
}

public class AsOperatorConstantValueParityTests
{
    [Fact]
    public void ProjectsNullWhenTheAsOperatorDoesNotMatchTheRuntimeType()
    {
        object boxed = 42;
        using TestDatabase db = Setup();

        List<string?> expected = Rows()
            .OrderBy(r => r.Id)
            .Select(_ => boxed as string)
            .ToList();

        List<string?> actual = db.Table<H22iAsCastRow>()
            .OrderBy(r => r.Id)
            .Select(_ => boxed as string)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void UpdateStoresNullWhenTheAsOperatorDoesNotMatchTheRuntimeType()
    {
        object boxed = 42;
        using TestDatabase db = Setup();

        db.Table<H22iAsCastRow>().ExecuteUpdate(s => s.Set(x => x.Tag, _ => boxed as string));

        List<string?> expected = Rows()
            .OrderBy(r => r.Id)
            .Select(_ => boxed as string)
            .ToList();

        List<string?> actual = db.Table<H22iAsCastRow>()
            .OrderBy(r => r.Id)
            .Select(r => r.Tag)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ConcatenatesAValueReachedThroughTheAsOperator()
    {
        object boxed = "-x";
        using TestDatabase db = Setup();

        List<string> expected = Rows()
            .OrderBy(r => r.Id)
            .Select(r => r.Name + (boxed as string))
            .ToList();

        List<string> actual = db.Table<H22iAsCastRow>()
            .OrderBy(r => r.Id)
            .Select(r => r.Name + (boxed as string))
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H22iAsCastRow> Rows()
    {
        return
        [
            new H22iAsCastRow { Id = 1, Name = "a", Tag = "seed" },
            new H22iAsCastRow { Id = 2, Name = "b", Tag = "seed" }
        ];
    }

    private static TestDatabase Setup()
    {
        TestDatabase db = new();
        db.Table<H22iAsCastRow>().Schema.CreateTable();
        db.Table<H22iAsCastRow>().AddRange(Rows());
        return db;
    }
}
