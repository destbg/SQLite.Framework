using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H26cNestedBranchRows")]
public class H26cNestedBranchRow
{
    [Key]
    public int Id { get; set; }

    public string? First { get; set; }

    public string? Second { get; set; }

    public string? Third { get; set; }
}

public class NestedSetOperationBranchAlignmentTests
{
    [Fact]
    public void ConcatWhoseNestedOperandBindsADifferentMemberIsRejected()
    {
        using TestDatabase db = Setup(nameof(ConcatWhoseNestedOperandBindsADifferentMemberIsRejected));

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() => db.Table<H26cNestedBranchRow>()
            .Where(r => r.Id == 1)
            .Select(r => new H26cNestedBranchRow { First = r.First, Second = r.Second })
            .Concat(db.Table<H26cNestedBranchRow>()
                .Where(r => r.Id == 2)
                .Select(r => new H26cNestedBranchRow { First = r.First, Second = r.Second })
                .Concat(db.Table<H26cNestedBranchRow>()
                    .Where(r => r.Id == 3)
                    .Select(r => new H26cNestedBranchRow { First = r.First, Third = r.Third })))
            .ToList());

        Assert.Contains("same members in the same order", ex.Message, StringComparison.Ordinal);
    }

    private static List<H26cNestedBranchRow> Rows()
    {
        return
        [
            new H26cNestedBranchRow { Id = 1, First = "f1", Second = "s1", Third = "t1" },
            new H26cNestedBranchRow { Id = 2, First = "f2", Second = "s2", Third = "t2" },
            new H26cNestedBranchRow { Id = 3, First = "f3", Second = "s3", Third = "t3" }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(null, methodName);
        db.Table<H26cNestedBranchRow>().Schema.CreateTable();
        db.Table<H26cNestedBranchRow>().AddRange(Rows());
        return db;
    }
}
