using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class ValueTupleProjectionRow
{
    [Key]
    public int Id { get; set; }

    public required string Name { get; set; }
}

public class ValueTupleProjectionTests
{
    private static TestDatabase SetupDatabase()
    {
        TestDatabase db = new();
        db.Table<ValueTupleProjectionRow>().Schema.CreateTable();
        db.Table<ValueTupleProjectionRow>().Add(new ValueTupleProjectionRow { Id = 1, Name = "a" });
        db.Table<ValueTupleProjectionRow>().Add(new ValueTupleProjectionRow { Id = 2, Name = "b" });
        return db;
    }

    [Fact]
    public void TopLevelValueTupleConstructorProjectionMaterializes()
    {
        using TestDatabase db = SetupDatabase();

        List<(int, string)> expected = db.Table<ValueTupleProjectionRow>().AsEnumerable()
            .OrderBy(r => r.Id)
            .Select(r => new ValueTuple<int, string>(r.Id, r.Name))
            .ToList();

        Assert.Equal([(1, "a"), (2, "b")], expected);

        List<(int, string)> actual = db.Table<ValueTupleProjectionRow>()
            .OrderBy(r => r.Id)
            .Select(r => new ValueTuple<int, string>(r.Id, r.Name))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ValueTupleCreateProjectionMaterializes()
    {
        using TestDatabase db = SetupDatabase();

        List<(int, string)> expected = db.Table<ValueTupleProjectionRow>().AsEnumerable()
            .OrderBy(r => r.Id)
            .Select(r => ValueTuple.Create(r.Id, r.Name))
            .ToList();

        Assert.Equal([(1, "a"), (2, "b")], expected);

        List<(int, string)> actual = db.Table<ValueTupleProjectionRow>()
            .OrderBy(r => r.Id)
            .Select(r => ValueTuple.Create(r.Id, r.Name))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NestedValueTupleConstructorProjectionMaterializes()
    {
        using TestDatabase db = SetupDatabase();

        var expected = db.Table<ValueTupleProjectionRow>().AsEnumerable()
            .OrderBy(r => r.Id)
            .Select(r => new { r.Id, Pair = new ValueTuple<int, string>(r.Id, r.Name) })
            .ToList();

        var actual = db.Table<ValueTupleProjectionRow>()
            .OrderBy(r => r.Id)
            .Select(r => new { r.Id, Pair = new ValueTuple<int, string>(r.Id, r.Name) })
            .ToList();

        Assert.Equal(expected, actual);
    }
}
