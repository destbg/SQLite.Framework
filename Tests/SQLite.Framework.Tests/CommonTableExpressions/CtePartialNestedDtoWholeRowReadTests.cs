using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H21bCtePartRow")]
public class H21bCtePartRow
{
    [Key]
    public int Id { get; set; }

    public int A { get; set; }
}

public class H21bCtePartSide
{
    public int X { get; set; }

    public int Y { get; set; }
}

public class CtePartialNestedDtoWholeRowReadTests
{
    private static List<H21bCtePartRow> Rows() =>
    [
        new H21bCtePartRow { Id = 1, A = 10 },
        new H21bCtePartRow { Id = 2, A = 20 },
    ];

    private static TestDatabase Setup()
    {
        TestDatabase db = new();
        db.Table<H21bCtePartRow>().Schema.CreateTable();
        db.Table<H21bCtePartRow>().AddRange(Rows());
        return db;
    }

    [Fact]
    public void CteBodyPartialNestedDtoWholeRowReadMatchesLinq()
    {
        using TestDatabase db = Setup();

        List<(int Id, int X, int Y)> expected = Rows()
            .Select(r => new { r.Id, N = new H21bCtePartSide { X = r.A } })
            .Select(x => (x.Id, x.N.X, x.N.Y))
            .OrderBy(t => t.Id)
            .ToList();

        List<(int Id, int X, int Y)> actual = db.With(() => db.Table<H21bCtePartRow>()
                .Select(r => new { r.Id, N = new H21bCtePartSide { X = r.A } }))
            .AsEnumerable()
            .Select(x => (x.Id, x.N.X, x.N.Y))
            .OrderBy(t => t.Id)
            .ToList();

        Assert.Equal(expected, actual);
    }
}
