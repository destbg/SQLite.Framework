using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H24gStageRows")]
public class H24gStageRow
{
    [Key]
    public int Id { get; set; }

    public int Amount { get; set; }
}

public class NestedCtePlaceholderColumnMappingTests
{
    [Fact]
    public void OuterCteMembersOverTwoPlaceholderColumnsKeepTheirOwnValues()
    {
        using TestDatabase db = Setup();

        var inMemoryInner = Rows()
            .Select(r => new { r.Id, First = r.Amount * 2, Second = r.Amount * 3, Tags = new[] { r.Amount } })
            .ToList();

        List<(int Id, int A, int B)> expected = inMemoryInner
            .Select(x => new { x.Id, A = x.First + 1, B = x.Second + 1, Tags = new[] { x.Id } })
            .Select(y => (y.Id, y.A, y.B))
            .OrderBy(t => t.Id)
            .ToList();

        var inner = db.With(() => db.Table<H24gStageRow>()
            .Select(r => new { r.Id, First = r.Amount * 2, Second = r.Amount * 3, Tags = new[] { r.Amount } }));

        var outer = db.With(() => inner
            .Select(x => new { x.Id, A = x.First + 1, B = x.Second + 1, Tags = new[] { x.Id } }));

        List<(int Id, int A, int B)> actual = outer
            .Select(y => new { y.Id, y.A, y.B })
            .AsEnumerable()
            .Select(y => (y.Id, y.A, y.B))
            .OrderBy(t => t.Id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H24gStageRow> Rows()
    {
        return
        [
            new H24gStageRow { Id = 1, Amount = 5 },
            new H24gStageRow { Id = 2, Amount = 11 },
            new H24gStageRow { Id = 3, Amount = 40 },
        ];
    }

    private static TestDatabase Setup()
    {
        TestDatabase db = new();
        db.Table<H24gStageRow>().Schema.CreateTable();
        db.Table<H24gStageRow>().AddRange(Rows());
        return db;
    }
}
