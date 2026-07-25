using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H21bUnsetRow")]
public class H21bUnsetRow
{
    [Key]
    public int Id { get; set; }

    public int A { get; set; }
}

public class H21bUnsetSide
{
    public int X { get; set; }

    public int Y { get; set; }
}

public class ProjectedUnsetNestedMemberReadTests
{
    private static List<H21bUnsetRow> Rows() =>
    [
        new H21bUnsetRow { Id = 1, A = 10 },
        new H21bUnsetRow { Id = 2, A = 20 },
    ];

    private static TestDatabase Setup()
    {
        TestDatabase db = new();
        db.Table<H21bUnsetRow>().Schema.CreateTable();
        db.Table<H21bUnsetRow>().AddRange(Rows());
        return db;
    }

    [Fact]
    public void ChainedSelectReadsUnsetNestedMemberAsDefault()
    {
        using TestDatabase db = Setup();

        List<int> expected = Rows()
            .Select(r => new { r.Id, N = new H21bUnsetSide { X = r.A } })
            .Select(x => x.N.Y)
            .ToList();

        List<int> actual = db.Table<H21bUnsetRow>()
            .Select(r => new { r.Id, N = new H21bUnsetSide { X = r.A } })
            .Select(x => x.N.Y)
            .AsEnumerable()
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CteReadsUnsetNestedMemberAsDefault()
    {
        using TestDatabase db = Setup();

        List<int> expected = Rows()
            .Select(r => new { r.Id, N = new H21bUnsetSide { X = r.A } })
            .Select(x => x.N.Y)
            .ToList();

        List<int> actual = db.With(() => db.Table<H21bUnsetRow>()
                .Select(r => new { r.Id, N = new H21bUnsetSide { X = r.A } }))
            .Select(x => x.N.Y)
            .AsEnumerable()
            .ToList();

        Assert.Equal(expected, actual);
    }
}
