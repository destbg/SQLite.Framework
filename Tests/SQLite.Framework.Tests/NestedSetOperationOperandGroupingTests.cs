using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class SetOperandValueRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public int Value { get; set; }
}

public class NestedSetOperationOperandGroupingTests
{
    private static TestDatabase SetupDatabase()
    {
        TestDatabase db = new();
        db.Table<SetOperandValueRow>().Schema.CreateTable();
        db.Table<SetOperandValueRow>().Add(new SetOperandValueRow { Id = 1, Name = "x", Value = 1 });
        db.Table<SetOperandValueRow>().Add(new SetOperandValueRow { Id = 2, Name = "y", Value = 2 });
        return db;
    }

    [Fact]
    public void ConcatWithUnionOperandKeepsTheOperandGrouping()
    {
        using TestDatabase db = SetupDatabase();
        List<SetOperandValueRow> rows = db.Table<SetOperandValueRow>().AsEnumerable().ToList();

        List<int> expected = rows.Where(r => r.Value == 1).Select(r => r.Value)
            .Concat(rows.Where(r => r.Value == 1).Select(r => r.Value)
                .Union(rows.Where(r => r.Value == 99).Select(r => r.Value)))
            .ToList();

        Assert.Equal([1, 1], expected);

        List<int> actual = db.Table<SetOperandValueRow>().Where(r => r.Value == 1).Select(r => r.Value)
            .Concat(db.Table<SetOperandValueRow>().Where(r => r.Value == 1).Select(r => r.Value)
                .Union(db.Table<SetOperandValueRow>().Where(r => r.Value == 99).Select(r => r.Value)))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void UnionWithConcatOperandKeepsTheOperandGrouping()
    {
        using TestDatabase db = SetupDatabase();
        List<SetOperandValueRow> rows = db.Table<SetOperandValueRow>().AsEnumerable().ToList();

        List<int> expected = rows.Select(r => r.Value)
            .Union(rows.Where(r => r.Value == 2).Select(r => r.Value)
                .Concat(rows.Where(r => r.Value == 1).Select(r => r.Value)))
            .OrderBy(v => v)
            .ToList();

        Assert.Equal([1, 2], expected);

        List<int> actual = db.Table<SetOperandValueRow>().Select(r => r.Value)
            .Union(db.Table<SetOperandValueRow>().Where(r => r.Value == 2).Select(r => r.Value)
                .Concat(db.Table<SetOperandValueRow>().Where(r => r.Value == 1).Select(r => r.Value)))
            .ToList()
            .OrderBy(v => v)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ExceptWithExceptOperandKeepsTheOperandGrouping()
    {
        using TestDatabase db = SetupDatabase();
        List<SetOperandValueRow> rows = db.Table<SetOperandValueRow>().AsEnumerable().ToList();

        List<int> expected = rows.Select(r => r.Value)
            .Except(rows.Select(r => r.Value)
                .Except(rows.Where(r => r.Value == 1).Select(r => r.Value)))
            .ToList();

        Assert.Equal([1], expected);

        List<int> actual = db.Table<SetOperandValueRow>().Select(r => r.Value)
            .Except(db.Table<SetOperandValueRow>().Select(r => r.Value)
                .Except(db.Table<SetOperandValueRow>().Where(r => r.Value == 1).Select(r => r.Value)))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ReverseOnConcatOperandIsApplied()
    {
        using TestDatabase db = SetupDatabase();
        List<SetOperandValueRow> rows = db.Table<SetOperandValueRow>().AsEnumerable().ToList();

        List<string> expected = rows.Where(r => r.Value == 99).Select(r => r.Name)
            .Concat(rows.Select(r => r.Name).Reverse())
            .ToList();

        Assert.Equal(["y", "x"], expected);

        List<string> actual = db.Table<SetOperandValueRow>().Where(r => r.Value == 99).Select(r => r.Name)
            .Concat(db.Table<SetOperandValueRow>().Select(r => r.Name).Reverse())
            .ToList();

        Assert.Equal(expected, actual);
    }
}
