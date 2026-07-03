using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class PredicateAsEqualityRightOperandTests
{
    internal sealed class FlagNameRow
    {
        [Key]
        public int Id { get; set; }

        public bool Flag { get; set; }

        public required string Name { get; set; }

        public char C { get; set; }
    }

    [Fact]
    public void BoolEqualsStartsWithResult()
    {
        using TestDatabase db = Seed();

        List<int> expected = Rows()
            .Where(x => x.Flag == x.Name.StartsWith("A"))
            .Select(x => x.Id).OrderBy(i => i).ToList();
        Assert.Equal([1, 2, 4], expected);

        List<int> actual = db.Table<FlagNameRow>()
            .Where(x => x.Flag == x.Name.StartsWith("A"))
            .Select(x => x.Id).OrderBy(i => i).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void BoolEqualsContainsResult()
    {
        using TestDatabase db = Seed();

        List<int> expected = Rows()
            .Where(x => x.Flag == x.Name.Contains("lp"))
            .Select(x => x.Id).OrderBy(i => i).ToList();

        List<int> actual = db.Table<FlagNameRow>()
            .Where(x => x.Flag == x.Name.Contains("lp"))
            .Select(x => x.Id).OrderBy(i => i).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void BoolEqualsIsWhiteSpaceResult()
    {
        using TestDatabase db = Seed();

        List<int> expected = Rows()
            .Where(x => x.Flag == char.IsWhiteSpace(x.C))
            .Select(x => x.Id).OrderBy(i => i).ToList();

        List<int> actual = db.Table<FlagNameRow>()
            .Where(x => x.Flag == char.IsWhiteSpace(x.C))
            .Select(x => x.Id).OrderBy(i => i).ToList();

        Assert.Equal(expected, actual);
    }

    private static List<FlagNameRow> Rows() =>
    [
        new() { Id = 1, Flag = true, Name = "Alpha", C = ' ' },
        new() { Id = 2, Flag = false, Name = "Beta", C = 'x' },
        new() { Id = 3, Flag = true, Name = "Gamma", C = 'y' },
        new() { Id = 4, Flag = false, Name = "Zeta", C = ' ' },
    ];

    private static TestDatabase Seed()
    {
        TestDatabase db = new();
        db.Table<FlagNameRow>().Schema.CreateTable();
        db.Table<FlagNameRow>().AddRange(Rows());
        return db;
    }
}
