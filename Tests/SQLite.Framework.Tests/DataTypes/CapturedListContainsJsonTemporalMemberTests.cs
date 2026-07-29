using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class H24rTemporalPayload
{
    public DateTime When { get; set; }

    public DateOnly Day { get; set; }
}

[JsonSerializable(typeof(H24rTemporalPayload))]
public partial class H24rTemporalContext : JsonSerializerContext;

[Table("H24rTemporalDocs")]
public class H24rTemporalDoc
{
    [Key]
    public int Id { get; set; }

    public H24rTemporalPayload Data { get; set; } = new();
}

public class H24rTemporalPair
{
    public int Id { get; set; }

    public DateTime When { get; set; }
}

public class CapturedListContainsJsonTemporalMemberTests
{
    private static List<H24rTemporalDoc> Rows()
    {
        return
        [
            new H24rTemporalDoc
            {
                Id = 1,
                Data = new H24rTemporalPayload { When = new DateTime(2024, 5, 6, 7, 8, 9), Day = new DateOnly(2024, 5, 6) },
            },
            new H24rTemporalDoc
            {
                Id = 2,
                Data = new H24rTemporalPayload { When = new DateTime(2024, 1, 15, 0, 0, 0), Day = new DateOnly(2024, 1, 15) },
            },
            new H24rTemporalDoc
            {
                Id = 3,
                Data = new H24rTemporalPayload { When = new DateTime(2023, 9, 1, 12, 0, 0), Day = new DateOnly(2023, 9, 1) },
            },
        ];
    }

    [Fact]
    public void CapturedListContainsJsonDateTimeMemberMatchesLinqToObjects()
    {
        using TestDatabase db = new(b => b.AddJsonContext(H24rTemporalContext.Default));
        db.Table<H24rTemporalDoc>().Schema.CreateTable();
        List<H24rTemporalDoc> docs = Rows();
        db.Table<H24rTemporalDoc>().AddRange(docs);

        List<DateTime> wanted = [new DateTime(2024, 5, 6, 7, 8, 9), new DateTime(2024, 1, 15, 0, 0, 0)];

        List<int> expected = docs.Where(d => wanted.Contains(d.Data.When)).Select(d => d.Id).OrderBy(i => i).ToList();
        List<int> actual = db.Table<H24rTemporalDoc>()
            .Where(r => wanted.Contains(r.Data.When))
            .Select(r => r.Id)
            .OrderBy(i => i)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CapturedTupleListAnyJsonDateTimeMemberThroughCteMatchesLinqToObjects()
    {
        using TestDatabase db = new(b => b.AddJsonContext(H24rTemporalContext.Default));
        db.Table<H24rTemporalDoc>().Schema.CreateTable();
        List<H24rTemporalDoc> docs = Rows();
        db.Table<H24rTemporalDoc>().AddRange(docs);

        List<(int, DateTime)> wanted =
        [
            (1, new DateTime(2024, 5, 6, 7, 8, 9)),
            (3, new DateTime(2023, 9, 1, 12, 0, 0)),
        ];

        List<int> expected = docs
            .Select(d => new H24rTemporalPair { Id = d.Id, When = d.Data.When })
            .Where(p => wanted.Any(w => w.Item1 == p.Id && w.Item2 == p.When))
            .Select(p => p.Id)
            .OrderBy(i => i)
            .ToList();

        SQLiteCte<H24rTemporalPair> cte = db.With(() => db.Table<H24rTemporalDoc>()
            .Select(d => new H24rTemporalPair { Id = d.Id, When = d.Data.When }));
        List<int> actual = cte
            .Where(p => wanted.Any(w => w.Item1 == p.Id && w.Item2 == p.When))
            .Select(p => p.Id)
            .OrderBy(i => i)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CapturedListContainsJsonDateOnlyMemberMatchesLinqToObjects()
    {
        using TestDatabase db = new(b => b.AddJsonContext(H24rTemporalContext.Default));
        db.Table<H24rTemporalDoc>().Schema.CreateTable();
        List<H24rTemporalDoc> docs = Rows();
        db.Table<H24rTemporalDoc>().AddRange(docs);

        List<DateOnly> wanted = [new DateOnly(2024, 5, 6), new DateOnly(2023, 9, 1)];

        List<int> expected = docs.Where(d => wanted.Contains(d.Data.Day)).Select(d => d.Id).OrderBy(i => i).ToList();
        List<int> actual = db.Table<H24rTemporalDoc>()
            .Where(r => wanted.Contains(r.Data.Day))
            .Select(r => r.Id)
            .OrderBy(i => i)
            .ToList();

        Assert.Equal(expected, actual);
    }
}
