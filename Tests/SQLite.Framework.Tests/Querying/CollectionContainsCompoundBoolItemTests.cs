using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class BoolPairRow
{
    [Key]
    public int Id { get; set; }

    public bool A { get; set; }

    public bool B { get; set; }
}

public class CollectionContainsCompoundBoolItemTests
{
    private static List<BoolPairRow> Rows() =>
    [
        new() { Id = 1, A = false, B = false },
        new() { Id = 2, A = false, B = true },
        new() { Id = 3, A = true, B = false },
        new() { Id = 4, A = true, B = true },
    ];

    private static TestDatabase Seed()
    {
        TestDatabase db = new();
        db.Table<BoolPairRow>().Schema.CreateTable();
        db.Table<BoolPairRow>().AddRange(Rows());
        return db;
    }

    [Fact]
    public void ContainsWithOrItemMatchesOnlyFalseRows()
    {
        using TestDatabase db = Seed();

        List<int> expected = Rows().Where(x => new[] { false }.Contains(x.A || x.B)).Select(x => x.Id).OrderBy(id => id).ToList();
        Assert.Equal([1], expected);

        List<int> actual = db.Table<BoolPairRow>()
            .Where(x => new[] { false }.Contains(x.A || x.B)).Select(x => x.Id).OrderBy(id => id).ToList();
        Assert.Equal(expected, actual);
    }
}
