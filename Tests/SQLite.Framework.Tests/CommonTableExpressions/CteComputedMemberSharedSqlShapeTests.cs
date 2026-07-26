using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H22eShiftRows")]
public class H22eShiftRow
{
    [Key]
    public int Id { get; set; }

    public TimeOnly At { get; set; }

    public DateTime Stamp { get; set; }

    public int Extra { get; set; }
}

public class CteComputedMemberSharedSqlShapeTests
{
    [Fact]
    public void TwoTimeOnlyComparisonMembersBesideArrayMemberKeepTheirOwnValues()
    {
        using TestDatabase db = Setup();

        TimeOnly low = new(10, 30, 15);
        TimeOnly high = new(10, 30, 45);

        List<(int Id, bool Early, bool Late)> expected = Rows()
            .Select(r => new { r.Id, Early = r.At > low, Late = r.At > high, Tags = new[] { r.Extra } })
            .Select(x => (x.Id, x.Early, x.Late))
            .OrderBy(t => t.Id)
            .ToList();

        List<(int Id, bool Early, bool Late)> actual = db.With(() => db.Table<H22eShiftRow>()
                .Select(r => new { r.Id, Early = r.At > low, Late = r.At > high, Tags = new[] { r.Extra } }))
            .Select(x => new { x.Id, x.Early, x.Late })
            .ToList()
            .Select(x => (x.Id, x.Early, x.Late))
            .OrderBy(t => t.Id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TwoDateTimeComparisonMembersBesideArrayMemberKeepTheirOwnValues()
    {
        using TestDatabase db = Setup();

        DateTime low = new(2024, 1, 1, 12, 0, 0, 100);
        DateTime high = new(2024, 1, 1, 12, 0, 0, 900);

        List<(int Id, bool Early, bool Late)> expected = Rows()
            .Select(r => new { r.Id, Early = r.Stamp > low, Late = r.Stamp > high, Tags = new[] { r.Extra } })
            .Select(x => (x.Id, x.Early, x.Late))
            .OrderBy(t => t.Id)
            .ToList();

        List<(int Id, bool Early, bool Late)> actual = db.With(() => db.Table<H22eShiftRow>()
                .Select(r => new { r.Id, Early = r.Stamp > low, Late = r.Stamp > high, Tags = new[] { r.Extra } }))
            .Select(x => new { x.Id, x.Early, x.Late })
            .ToList()
            .Select(x => (x.Id, x.Early, x.Late))
            .OrderBy(t => t.Id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H22eShiftRow> Rows()
    {
        return
        [
            new H22eShiftRow
            {
                Id = 1,
                At = new TimeOnly(10, 30, 0),
                Stamp = new DateTime(2024, 1, 1, 12, 0, 0, 0),
                Extra = 11
            },
            new H22eShiftRow
            {
                Id = 2,
                At = new TimeOnly(10, 30, 30),
                Stamp = new DateTime(2024, 1, 1, 12, 0, 0, 500),
                Extra = 22
            },
            new H22eShiftRow
            {
                Id = 3,
                At = new TimeOnly(10, 31, 0),
                Stamp = new DateTime(2024, 1, 1, 12, 0, 1, 0),
                Extra = 33
            },
        ];
    }

    private static TestDatabase Setup()
    {
        TestDatabase db = new();
        db.Table<H22eShiftRow>().Schema.CreateTable();
        db.Table<H22eShiftRow>().AddRange(Rows());
        return db;
    }
}
