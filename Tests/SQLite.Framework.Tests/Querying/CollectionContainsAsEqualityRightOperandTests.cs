using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class CollectionContainsAsEqualityRightOperandTests
{
    internal sealed class FlagIdRow
    {
        [Key]
        public int Id { get; set; }

        public bool Flag { get; set; }

        public int Value { get; set; }
    }

    [Fact]
    public void BoolEqualsCollectionContainsResult()
    {
        using TestDatabase db = Seed();

        int[] ids = [1, 2, 3];

        List<int> expected = Rows()
            .Where(x => x.Flag == ids.Contains(x.Value))
            .Select(x => x.Id).OrderBy(i => i).ToList();

        List<int> actual = db.Table<FlagIdRow>()
            .Where(x => x.Flag == ids.Contains(x.Value))
            .Select(x => x.Id).OrderBy(i => i).ToList();

        Assert.Equal(expected, actual);
    }

    private static List<FlagIdRow> Rows() =>
    [
        new() { Id = 1, Flag = true, Value = 2 },
        new() { Id = 2, Flag = false, Value = 9 },
        new() { Id = 3, Flag = true, Value = 9 },
        new() { Id = 4, Flag = false, Value = 1 },
    ];

    private static TestDatabase Seed()
    {
        TestDatabase db = new();
        db.Table<FlagIdRow>().Schema.CreateTable();
        db.Table<FlagIdRow>().AddRange(Rows());
        return db;
    }
}
