using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H26uJsonMomentRows")]
public class H26uJsonMomentRow
{
    [Key]
    public int Id { get; set; }

    public List<DateTime> Moments { get; set; } = [];
}

public class H26uJsonMomentProjection
{
    public int Id { get; set; }

    public DateTime First { get; set; }
}

public class JsonElementComparedAfterPagingSubqueryTests
{
    [Fact]
    public void AScalarJsonDateElementStillMatchesAConstantAfterPaging()
    {
        using TestDatabase db = Setup(nameof(AScalarJsonDateElementStillMatchesAConstantAfterPaging));
        DateTime sought = new(2023, 6, 1);

        int expected = Rows()
            .OrderBy(r => r.Id)
            .Select(r => r.Moments.First())
            .Take(2)
            .Count(d => d == sought);

        int actual = db.Table<H26uJsonMomentRow>()
            .OrderBy(r => r.Id)
            .Select(r => r.Moments.First())
            .Take(2)
            .Count(d => d == sought);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void AProjectedJsonDateMemberStillMatchesAConstantAfterPaging()
    {
        using TestDatabase db = Setup(nameof(AProjectedJsonDateMemberStillMatchesAConstantAfterPaging));
        DateTime sought = new(2023, 6, 1);

        int expected = Rows()
            .OrderBy(r => r.Id)
            .Select(r => new H26uJsonMomentProjection { Id = r.Id, First = r.Moments.First() })
            .Take(2)
            .Count(x => x.First == sought);

        int actual = db.Table<H26uJsonMomentRow>()
            .OrderBy(r => r.Id)
            .Select(r => new H26uJsonMomentProjection { Id = r.Id, First = r.Moments.First() })
            .Take(2)
            .Count(x => x.First == sought);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void AJsonDateElementFromAProjectedCrossJoinSourceStillMatchesAConstant()
    {
        using TestDatabase db = Setup(nameof(AJsonDateElementFromAProjectedCrossJoinSourceStillMatchesAConstant));
        DateTime sought = new(2023, 6, 1);

        List<H26uJsonMomentRow> rows = Rows();
        int expected = rows
            .SelectMany(_ => rows.Select(x => x.Moments.First()))
            .Count(d => d == sought);

        int actual = db.Table<H26uJsonMomentRow>()
            .SelectMany(_ => db.Table<H26uJsonMomentRow>().Select(x => x.Moments.First()))
            .Count(d => d == sought);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void AJsonDateElementReadThroughACorrelatedSubqueryStillMatchesAConstant()
    {
        using TestDatabase db = Setup(nameof(AJsonDateElementReadThroughACorrelatedSubqueryStillMatchesAConstant));
        DateTime sought = new(2023, 6, 1);

        List<H26uJsonMomentRow> rows = Rows();
        int expected = rows.Count(r => rows
            .Where(x => x.Id == r.Id)
            .Select(x => x.Moments.First())
            .First() == sought);

        int actual = db.Table<H26uJsonMomentRow>()
            .Count(r => db.Table<H26uJsonMomentRow>()
                .Where(x => x.Id == r.Id)
                .Select(x => x.Moments.First())
                .First() == sought);

        Assert.Equal(expected, actual);
    }

    private static List<H26uJsonMomentRow> Rows()
    {
        return
        [
            new H26uJsonMomentRow
            {
                Id = 1,
                Moments = [new DateTime(2023, 6, 1), new DateTime(2024, 1, 15)]
            },
            new H26uJsonMomentRow
            {
                Id = 2,
                Moments = [new DateTime(2024, 3, 20), new DateTime(2025, 4, 2)]
            },
            new H26uJsonMomentRow
            {
                Id = 3,
                Moments = [new DateTime(2023, 6, 1), new DateTime(2026, 2, 2)]
            }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(b => b.TypeConverters[typeof(List<DateTime>)] =
            new SQLiteJsonConverter<List<DateTime>>(TestJsonContext.Default.ListDateTime), methodName);
        db.Table<H26uJsonMomentRow>().Schema.CreateTable();
        db.Table<H26uJsonMomentRow>().AddRange(Rows());
        return db;
    }
}
