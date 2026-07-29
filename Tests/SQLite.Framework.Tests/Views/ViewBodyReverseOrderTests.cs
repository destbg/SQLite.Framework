using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H24sReverseSourceRows")]
public class H24sReverseSourceRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

[Table("H24sReverseViewRows")]
public class H24sReverseViewRow
{
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

[Table("H24sReverseDistinctViewRows")]
public class H24sReverseDistinctViewRow
{
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public class ViewBodyReverseOrderTests
{
    [Fact]
    public void ViewBodyEndingWithReverseReadsBackInTheReversedOrder()
    {
        using TestDatabase db = new();
        db.Table<H24sReverseSourceRow>().Schema.CreateTable();
        db.Table<H24sReverseSourceRow>().AddRange(SourceRows());

        List<int> expected = SourceRows()
            .OrderBy(r => r.Id)
            .Select(r => r.Id)
            .Reverse()
            .ToList();

        AssertMatchesOrIsRefused(expected, () =>
        {
            db.Schema.CreateView<H24sReverseViewRow>(() =>
                db.Table<H24sReverseSourceRow>()
                    .OrderBy(r => r.Id)
                    .Select(r => new H24sReverseViewRow { Id = r.Id, Name = r.Name })
                    .Reverse());

            return db.ReadOnlyTable<H24sReverseViewRow>().ToList().Select(v => v.Id).ToList();
        });
    }

    [Fact]
    public void ViewBodyWithReverseBeforeDistinctReadsBackInTheReversedOrder()
    {
        using TestDatabase db = new();
        db.Table<H24sReverseSourceRow>().Schema.CreateTable();
        db.Table<H24sReverseSourceRow>().AddRange(SourceRows());

        List<int> expected = SourceRows()
            .OrderBy(r => r.Id)
            .Select(r => r.Id)
            .Reverse()
            .Distinct()
            .ToList();

        AssertMatchesOrIsRefused(expected, () =>
        {
            db.Schema.CreateView<H24sReverseDistinctViewRow>(() =>
                db.Table<H24sReverseSourceRow>()
                    .OrderBy(r => r.Id)
                    .Select(r => new H24sReverseDistinctViewRow { Id = r.Id, Name = r.Name })
                    .Reverse()
                    .Distinct());

            return db.ReadOnlyTable<H24sReverseDistinctViewRow>().ToList().Select(v => v.Id).ToList();
        });
    }

    private static void AssertMatchesOrIsRefused(List<int> expected, Func<List<int>> run)
    {
        List<int> actual;
        try
        {
            actual = run();
        }
        catch (NotSupportedException)
        {
            return;
        }

        Assert.Equal(expected, actual);
    }

    private static List<H24sReverseSourceRow> SourceRows()
    {
        return
        [
            new H24sReverseSourceRow { Id = 1, Name = "alpha" },
            new H24sReverseSourceRow { Id = 2, Name = "beta" },
            new H24sReverseSourceRow { Id = 3, Name = "gamma" },
            new H24sReverseSourceRow { Id = 4, Name = "delta" }
        ];
    }
}
