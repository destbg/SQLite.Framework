using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H22zBranchRows")]
public class H22zBranchRow
{
    [Key]
    public int Id { get; set; }

    public int A { get; set; }
}

public class H22zBranchBox
{
    public int Id { get; set; }

    public int Plain { get; set; }
}

public class ConditionalBranchAssignedMemberReadTests
{
    [Fact]
    public void AMemberAssignedInOnlyOneBranchReadsItsDefaultInTheOtherBranch()
    {
        using TestDatabase db = Setup();

        List<int> expected = Rows()
            .Select(r => r.A > 15
                ? new H22zBranchBox { Id = r.Id }
                : new H22zBranchBox { Id = r.Id, Plain = 5 })
            .Select(x => x.Plain)
            .ToList();

        List<int> actual = db.Table<H22zBranchRow>()
            .Select(r => r.A > 15
                ? new H22zBranchBox { Id = r.Id }
                : new H22zBranchBox { Id = r.Id, Plain = 5 })
            .Select(x => x.Plain)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void AMemberAssignedInOnlyOneBranchFiltersOnItsDefaultInTheOtherBranch()
    {
        using TestDatabase db = Setup();

        List<int> expected = Rows()
            .Select(r => r.A > 15
                ? new H22zBranchBox { Id = r.Id }
                : new H22zBranchBox { Id = r.Id, Plain = 5 })
            .Where(x => x.Plain == 0)
            .Select(x => x.Id)
            .OrderBy(v => v)
            .ToList();

        List<int> actual = db.Table<H22zBranchRow>()
            .Select(r => r.A > 15
                ? new H22zBranchBox { Id = r.Id }
                : new H22zBranchBox { Id = r.Id, Plain = 5 })
            .Where(x => x.Plain == 0)
            .Select(x => x.Id)
            .AsEnumerable()
            .OrderBy(v => v)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H22zBranchRow> Rows() =>
    [
        new H22zBranchRow { Id = 1, A = 10 },
        new H22zBranchRow { Id = 2, A = 20 },
        new H22zBranchRow { Id = 3, A = 30 },
    ];

    private static TestDatabase Setup()
    {
        TestDatabase db = new();
        db.Table<H22zBranchRow>().Schema.CreateTable();
        db.Table<H22zBranchRow>().AddRange(Rows());
        return db;
    }
}
