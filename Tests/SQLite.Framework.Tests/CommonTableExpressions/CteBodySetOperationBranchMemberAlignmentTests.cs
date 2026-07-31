using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H26bBranchSources")]
public class H26bBranchSource
{
    [Key]
    public int Id { get; set; }

    public int X { get; set; }

    public int Y { get; set; }

    public int Z { get; set; }
}

public class H26bBranchPair
{
    public int A { get; set; }

    public int B { get; set; }

    public int C { get; set; }
}

public class CteBodySetOperationBranchMemberAlignmentTests
{
    [Fact]
    public void BranchesOfACteBodySetOperationThatSetDifferentMembersAreRejected()
    {
        using TestDatabase db = Setup(nameof(BranchesOfACteBodySetOperationThatSetDifferentMembersAreRejected));

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() => db.With(() => db.Table<H26bBranchSource>()
                .Where(r => r.Id == 1)
                .Select(r => new H26bBranchPair { A = r.X, B = r.Y })
                .Concat(db.Table<H26bBranchSource>()
                    .Where(r => r.Id == 2)
                    .Select(r => new H26bBranchPair { A = r.X, C = r.Z })))
            .Select(p => new { p.A, p.B })
            .ToList());

        Assert.Contains("same members in the same order", ex.Message, StringComparison.Ordinal);
    }

    private static List<H26bBranchSource> Rows()
    {
        return
        [
            new H26bBranchSource { Id = 1, X = 10, Y = 20, Z = 30 },
            new H26bBranchSource { Id = 2, X = 40, Y = 50, Z = 60 }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(null, methodName);
        db.Table<H26bBranchSource>().Schema.CreateTable();
        db.Table<H26bBranchSource>().AddRange(Rows());
        return db;
    }
}
