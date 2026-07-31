using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H26cSingleMemberBranchRows")]
public class H26cSingleMemberBranchRow
{
    [Key]
    public int Id { get; set; }

    public string? Left { get; set; }

    public string? Right { get; set; }
}

public class SingleMemberSetOperationBranchAlignmentTests
{
    [Fact]
    public void ConcatBranchesThatEachBindOneDifferentMemberAreRejected()
    {
        using TestDatabase db = Setup(nameof(ConcatBranchesThatEachBindOneDifferentMemberAreRejected));

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() => db.Table<H26cSingleMemberBranchRow>()
            .Where(r => r.Id <= 2)
            .Select(r => new H26cSingleMemberBranchRow { Left = r.Left })
            .Concat(db.Table<H26cSingleMemberBranchRow>()
                .Where(r => r.Id > 2)
                .Select(r => new H26cSingleMemberBranchRow { Right = r.Right }))
            .ToList());

        Assert.Contains("same members in the same order", ex.Message, StringComparison.Ordinal);
    }

    private static List<H26cSingleMemberBranchRow> Rows()
    {
        return
        [
            new H26cSingleMemberBranchRow { Id = 1, Left = "alpha", Right = "a" },
            new H26cSingleMemberBranchRow { Id = 2, Left = "beta", Right = "b" },
            new H26cSingleMemberBranchRow { Id = 3, Left = "gamma", Right = "g" },
            new H26cSingleMemberBranchRow { Id = 4, Left = "delta", Right = "d" }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(null, methodName);
        db.Table<H26cSingleMemberBranchRow>().Schema.CreateTable();
        db.Table<H26cSingleMemberBranchRow>().AddRange(Rows());
        return db;
    }
}
