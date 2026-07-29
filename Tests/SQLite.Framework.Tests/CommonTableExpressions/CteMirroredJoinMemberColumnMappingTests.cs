using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H24gLedgerRows")]
public class H24gLedgerRow
{
    [Key]
    public int Id { get; set; }

    public int Partner { get; set; }

    public int Amount { get; set; }

    public string Code { get; set; } = "";
}

public class CteMirroredJoinMemberColumnMappingTests
{
    [Fact]
    public void MirroredDifferenceMembersBesideArrayMemberKeepTheirOwnValues()
    {
        using TestDatabase db = Setup();

        List<(int Id, int Forward, int Backward)> expected = (
                from a in Rows()
                join b in Rows() on a.Partner equals b.Id
                where a.Id > 0
                select new
                {
                    a.Id,
                    Forward = a.Amount - b.Amount,
                    Backward = b.Amount - a.Amount,
                    Tags = new[] { a.Amount }
                })
            .Select(x => (x.Id, x.Forward, x.Backward))
            .OrderBy(t => t.Id)
            .ToList();

        List<(int Id, int Forward, int Backward)> actual = db.With(() =>
                from a in db.Table<H24gLedgerRow>()
                join b in db.Table<H24gLedgerRow>() on a.Partner equals b.Id
                where a.Id > 0
                select new
                {
                    a.Id,
                    Forward = a.Amount - b.Amount,
                    Backward = b.Amount - a.Amount,
                    Tags = new[] { a.Amount }
                })
            .Select(x => new { x.Id, x.Forward, x.Backward })
            .AsEnumerable()
            .Select(x => (x.Id, x.Forward, x.Backward))
            .OrderBy(t => t.Id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void MirroredConcatenationMembersBesideArrayMemberKeepTheirOwnValues()
    {
        using TestDatabase db = Setup();

        List<(int Id, string Forward, string Backward)> expected = (
                from a in Rows()
                join b in Rows() on a.Partner equals b.Id
                where a.Id > 0
                select new
                {
                    a.Id,
                    Forward = a.Code + b.Code,
                    Backward = b.Code + a.Code,
                    Tags = new[] { a.Amount }
                })
            .Select(x => (x.Id, x.Forward, x.Backward))
            .OrderBy(t => t.Id)
            .ToList();

        List<(int Id, string Forward, string Backward)> actual = db.With(() =>
                from a in db.Table<H24gLedgerRow>()
                join b in db.Table<H24gLedgerRow>() on a.Partner equals b.Id
                where a.Id > 0
                select new
                {
                    a.Id,
                    Forward = a.Code + b.Code,
                    Backward = b.Code + a.Code,
                    Tags = new[] { a.Amount }
                })
            .Select(x => new { x.Id, x.Forward, x.Backward })
            .AsEnumerable()
            .Select(x => (x.Id, x.Forward, x.Backward))
            .OrderBy(t => t.Id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H24gLedgerRow> Rows()
    {
        return
        [
            new H24gLedgerRow { Id = 1, Partner = 2, Amount = 10, Code = "aa" },
            new H24gLedgerRow { Id = 2, Partner = 3, Amount = 40, Code = "bb" },
            new H24gLedgerRow { Id = 3, Partner = 1, Amount = 90, Code = "cc" },
        ];
    }

    private static TestDatabase Setup()
    {
        TestDatabase db = new();
        db.Table<H24gLedgerRow>().Schema.CreateTable();
        db.Table<H24gLedgerRow>().AddRange(Rows());
        return db;
    }
}
