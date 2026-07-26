using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H22ePartRows")]
public class H22ePartRow
{
    [Key]
    public int Id { get; set; }

    public int A { get; set; }
}

public class H22ePartShape
{
    public int X { get; set; }

    public int Y { get; set; }
}

public class H22ePartWrap
{
    public int X { get; set; }

    public H22ePartShape? N { get; set; }
}

public class CteUnassignedElementMemberReadTests
{
    [Fact]
    public void SimpleMemberTheCteBodyNeverAssignsReadsAsItsClrDefault()
    {
        using TestDatabase db = Setup();

        List<(int X, int Y)> expected = Rows()
            .Select(r => new H22ePartShape { X = r.A })
            .Select(s => (s.X, s.Y))
            .OrderBy(t => t.X)
            .ToList();

        List<(int X, int Y)> actual = db.With(() => db.Table<H22ePartRow>()
                .Select(r => new H22ePartShape { X = r.A }))
            .AsEnumerable()
            .Select(s => (s.X, s.Y))
            .OrderBy(t => t.X)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NestedMemberTheCteBodyNeverAssignsReadsAsNull()
    {
        using TestDatabase db = Setup();

        List<(int X, bool Missing)> expected = Rows()
            .Select(r => new H22ePartWrap { X = r.A })
            .Select(s => (s.X, s.N == null))
            .OrderBy(t => t.X)
            .ToList();

        List<(int X, bool Missing)> actual = db.With(() => db.Table<H22ePartRow>()
                .Select(r => new H22ePartWrap { X = r.A }))
            .AsEnumerable()
            .Select(s => (s.X, s.N == null))
            .OrderBy(t => t.X)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H22ePartRow> Rows()
    {
        return
        [
            new H22ePartRow { Id = 1, A = 10 },
            new H22ePartRow { Id = 2, A = 20 },
        ];
    }

    private static TestDatabase Setup()
    {
        TestDatabase db = new();
        db.Table<H22ePartRow>().Schema.CreateTable();
        db.Table<H22ePartRow>().AddRange(Rows());
        return db;
    }
}
