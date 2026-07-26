using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H22zPredicateRows")]
public class H22zPredicateRow
{
    [Key]
    public int Id { get; set; }

    public string? Tag { get; set; }

    public int A { get; set; }
}

public class ProjectedNullableMemberPredicateTests
{
    [Fact]
    public void NegatedGlobOverAProjectedNullableStringMemberKeepsTheNullRows()
    {
        using TestDatabase db = Setup();

        List<int> expected = Rows()
            .Select(r => new { r.Id, T = r.Tag })
            .Where(x => !(x.T != null && x.T.StartsWith("x")))
            .Select(x => x.Id)
            .OrderBy(v => v)
            .ToList();

        List<int> actual = db.Table<H22zPredicateRow>()
            .Select(r => new { r.Id, T = r.Tag })
            .Where(x => !SQLiteFunctions.Glob("x*", x.T!))
            .Select(x => x.Id)
            .AsEnumerable()
            .OrderBy(v => v)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NegatedInOverAProjectedNullableStringMemberKeepsTheNullRows()
    {
        using TestDatabase db = Setup();

        string[] wanted = ["x", "y"];

        List<int> expected = Rows()
            .Select(r => new { r.Id, T = r.Tag })
            .Where(x => !(x.T != null && wanted.Contains(x.T)))
            .Select(x => x.Id)
            .OrderBy(v => v)
            .ToList();

        List<int> actual = db.Table<H22zPredicateRow>()
            .Select(r => new { r.Id, T = r.Tag })
            .Where(x => !SQLiteFunctions.In(x.T, wanted))
            .Select(x => x.Id)
            .AsEnumerable()
            .OrderBy(v => v)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NegatedGlobOverANullableStringColumnKeepsTheNullRows()
    {
        using TestDatabase db = Setup();

        List<int> expected = Rows()
            .Where(r => !(r.Tag != null && r.Tag.StartsWith("x")))
            .Select(r => r.Id)
            .OrderBy(v => v)
            .ToList();

        List<int> actual = db.Table<H22zPredicateRow>()
            .Where(r => !SQLiteFunctions.Glob("x*", r.Tag!))
            .Select(r => r.Id)
            .AsEnumerable()
            .OrderBy(v => v)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H22zPredicateRow> Rows() =>
    [
        new H22zPredicateRow { Id = 1, Tag = "x", A = 10 },
        new H22zPredicateRow { Id = 2, Tag = null, A = 20 },
        new H22zPredicateRow { Id = 3, Tag = "y", A = 30 },
    ];

    private static TestDatabase Setup()
    {
        TestDatabase db = new();
        db.Table<H22zPredicateRow>().Schema.CreateTable();
        db.Table<H22zPredicateRow>().AddRange(Rows());
        return db;
    }
}
