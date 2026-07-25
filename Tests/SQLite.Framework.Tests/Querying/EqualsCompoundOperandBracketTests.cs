using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class BoolTripleRow
{
    [Key]
    public int Id { get; set; }

    public bool A { get; set; }

    public bool B { get; set; }

    public bool C { get; set; }

    public required string Name { get; set; }

    public int Num { get; set; }
}

public class EqualsCompoundOperandBracketTests
{
    private static List<BoolTripleRow> Rows() =>
    [
        new() { Id = 1, A = false, B = false, C = false, Name = "cherry", Num = 5 },
        new() { Id = 2, A = false, B = false, C = true, Name = "banana", Num = 5 },
        new() { Id = 3, A = false, B = true, C = false, Name = "mango", Num = 5 },
        new() { Id = 4, A = false, B = true, C = true, Name = "banana", Num = 7 },
        new() { Id = 5, A = true, B = false, C = false, Name = "plum", Num = 9 },
        new() { Id = 6, A = true, B = false, C = true, Name = "banana", Num = 2 },
        new() { Id = 7, A = true, B = true, C = false, Name = "peach", Num = 5 },
        new() { Id = 8, A = true, B = true, C = true, Name = "banana", Num = 5 },
    ];

    private static TestDatabase Seed()
    {
        TestDatabase db = new();
        db.Table<BoolTripleRow>().Schema.CreateTable();
        db.Table<BoolTripleRow>().AddRange(Rows());
        return db;
    }

    [Fact]
    public void OrOperandEqualsColumn()
    {
        using TestDatabase db = Seed();

        List<int> expected = Rows().Where(x => (x.A || x.B).Equals(x.C)).Select(x => x.Id).OrderBy(id => id).ToList();
        Assert.Equal([1, 4, 6, 8], expected);

        List<int> actual = db.Table<BoolTripleRow>()
            .Where(x => (x.A || x.B).Equals(x.C)).Select(x => x.Id).OrderBy(id => id).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ColumnEqualsAndOperand()
    {
        using TestDatabase db = Seed();

        List<int> expected = Rows().Where(x => x.C.Equals(x.A && x.B)).Select(x => x.Id).OrderBy(id => id).ToList();
        Assert.Equal([1, 3, 5, 8], expected);

        List<int> actual = db.Table<BoolTripleRow>()
            .Where(x => x.C.Equals(x.A && x.B)).Select(x => x.Id).OrderBy(id => id).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ColumnEqualsStringContainsResult()
    {
        using TestDatabase db = Seed();

        List<int> expected = Rows().Where(x => x.C.Equals(x.Name.Contains("an"))).Select(x => x.Id).OrderBy(id => id).ToList();
        Assert.Equal([1, 2, 4, 5, 6, 7, 8], expected);

        List<int> actual = db.Table<BoolTripleRow>()
            .Where(x => x.C.Equals(x.Name.Contains("an"))).Select(x => x.Id).OrderBy(id => id).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ColumnEqualsComparisonResult()
    {
        using TestDatabase db = Seed();

        List<int> expected = Rows().Where(x => x.C.Equals(x.Num == 5)).Select(x => x.Id).OrderBy(id => id).ToList();
        Assert.Equal([2, 5, 8], expected);

        List<int> actual = db.Table<BoolTripleRow>()
            .Where(x => x.C.Equals(x.Num == 5)).Select(x => x.Id).OrderBy(id => id).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void StaticEqualsWithOrOperand()
    {
        using TestDatabase db = Seed();

        List<int> expected = Rows().Where(x => Equals(x.A || x.B, x.C)).Select(x => x.Id).OrderBy(id => id).ToList();
        Assert.Equal([1, 4, 6, 8], expected);

        List<int> actual = db.Table<BoolTripleRow>()
            .Where(x => Equals(x.A || x.B, x.C)).Select(x => x.Id).OrderBy(id => id).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ProjectedEqualsWithOrOperand()
    {
        using TestDatabase db = Seed();

        List<bool> expected = Rows().OrderBy(x => x.Id).Select(x => (x.A || x.B).Equals(x.C)).ToList();
        Assert.Equal([true, false, false, true, false, true, false, true], expected);

        List<bool> actual = db.Table<BoolTripleRow>()
            .OrderBy(x => x.Id).Select(x => (x.A || x.B).Equals(x.C)).ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ExecuteUpdateSetFromEqualsWithOrOperand()
    {
        using TestDatabase db = Seed();

        List<bool> expected = Rows().OrderBy(x => x.Id).Select(x => (x.A || x.B).Equals(x.C)).ToList();
        Assert.Equal([true, false, false, true, false, true, false, true], expected);

        db.Table<BoolTripleRow>().ExecuteUpdate(s => s.Set(x => x.C, x => (x.A || x.B).Equals(x.C)));

        List<bool> actual = db.Table<BoolTripleRow>().OrderBy(x => x.Id).Select(x => x.C).ToList();
        Assert.Equal(expected, actual);
    }
}
