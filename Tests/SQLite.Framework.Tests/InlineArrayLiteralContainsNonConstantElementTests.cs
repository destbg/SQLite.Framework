using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class InlineArrayLiteralContainsNonConstantElementTests
{
    internal sealed class IalRow
    {
        [Key]
        public int Id { get; set; }

        public int V { get; set; }
    }

    private static readonly IalRow[] Data =
    [
        new IalRow { Id = 1, V = 10 },
        new IalRow { Id = 2, V = 20 },
        new IalRow { Id = 3, V = 30 },
    ];

    private static TestDatabase Create()
    {
        TestDatabase db = new();
        db.Table<IalRow>().Schema.CreateTable();
        foreach (IalRow r in Data)
        {
            db.Table<IalRow>().Add(r);
        }

        return db;
    }

    [Fact]
    public void ContainsOverArrayWithComputedElementsThrows()
    {
        using TestDatabase db = Create();

        Assert.Throws<NotSupportedException>(() =>
            db.Table<IalRow>().Where(x => new[] { int.Parse("10"), int.Parse("30") }.Contains(x.V)).Select(x => x.Id).ToList());
    }

    [Fact]
    public void ContainsOverCapturedArrayWithComputedElementsWorks()
    {
        using TestDatabase db = Create();
        int[] values = [int.Parse("10"), int.Parse("30")];

        List<int> expected = Data.Where(x => values.Contains(x.V)).Select(x => x.Id).OrderBy(i => i).ToList();
        List<int> actual = db.Table<IalRow>().Where(x => values.Contains(x.V)).Select(x => x.Id).OrderBy(i => i).ToList();

        Assert.Equal([1, 3], expected);
        Assert.Equal(expected, actual);
    }
}
