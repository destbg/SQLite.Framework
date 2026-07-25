using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class CollectionInitializerNullableColumnProjectionTests
{
    internal sealed class CinRow
    {
        [Key]
        public int Id { get; set; }

        public int? NB { get; set; }
    }

    internal sealed class CinDto
    {
        public int Id { get; set; }

        public List<int?> Items { get; set; } = new();
    }

    private static readonly CinRow[] Data =
    [
        new CinRow { Id = 1, NB = null },
        new CinRow { Id = 2, NB = 40 },
    ];

    private static TestDatabase Create()
    {
        TestDatabase db = new();
        db.Table<CinRow>().Schema.CreateTable();
        foreach (CinRow r in Data)
        {
            db.Table<CinRow>().Add(r);
        }

        return db;
    }

    [Fact]
    public void NullableColumnInListInitializer()
    {
        using TestDatabase db = Create();

        List<CinDto> expected = Data.Select(x => new CinDto { Id = x.Id, Items = new List<int?> { x.NB } }).OrderBy(d => d.Id).ToList();
        List<CinDto> actual = db.Table<CinRow>().Select(x => new CinDto { Id = x.Id, Items = new List<int?> { x.NB } }).OrderBy(d => d.Id).ToList();

        Assert.Equal([null], expected[0].Items);
        Assert.Equal([40], expected[1].Items);
        Assert.Equal(expected.Count, actual.Count);
        for (int i = 0; i < expected.Count; i++)
        {
            Assert.Equal(expected[i].Id, actual[i].Id);
            Assert.Equal(expected[i].Items, actual[i].Items);
        }
    }
}
