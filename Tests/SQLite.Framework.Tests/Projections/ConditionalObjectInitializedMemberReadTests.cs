using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H22hCondPresetRows")]
public class H22hCondPresetRow
{
    [Key]
    public int Id { get; set; }

    public bool Flag { get; set; }

    public int A { get; set; }

    public int B { get; set; }
}

public class H22hCondPresetBox
{
    public int First { get; set; }

    public int Second { get; set; } = 7;
}

public class ConditionalObjectInitializedMemberReadTests
{
    [Fact]
    public void BranchMemberWithAPropertyInitializerReadsItsInitialValue()
    {
        using TestDatabase db = Setup(nameof(BranchMemberWithAPropertyInitializerReadsItsInitialValue));

        List<int> expected = Rows()
            .OrderBy(r => r.Id)
            .Select(r => (r.Flag ? new H22hCondPresetBox { First = r.A } : new H22hCondPresetBox { First = r.B }).Second)
            .ToList();

        List<int> actual = db.Table<H22hCondPresetRow>()
            .OrderBy(r => r.Id)
            .Select(r => (r.Flag ? new H22hCondPresetBox { First = r.A } : new H22hCondPresetBox { First = r.B }).Second)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FilterOnAMemberWithAPropertyInitializerKeepsTheMatchingRows()
    {
        using TestDatabase db = Setup(nameof(FilterOnAMemberWithAPropertyInitializerKeepsTheMatchingRows));

        List<int> expected = Rows()
            .Where(r => (r.Flag ? new H22hCondPresetBox { First = r.A } : new H22hCondPresetBox { First = r.B }).Second > 0)
            .OrderBy(r => r.Id)
            .Select(r => r.Id)
            .ToList();

        List<int> actual = db.Table<H22hCondPresetRow>()
            .Where(r => (r.Flag ? new H22hCondPresetBox { First = r.A } : new H22hCondPresetBox { First = r.B }).Second > 0)
            .OrderBy(r => r.Id)
            .Select(r => r.Id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void MemberAssignedInBothBranchesStillReadsTheBranchColumn()
    {
        using TestDatabase db = Setup(nameof(MemberAssignedInBothBranchesStillReadsTheBranchColumn));

        List<int> expected = Rows()
            .OrderBy(r => r.Id)
            .Select(r => (r.Flag ? new H22hCondPresetBox { First = r.A } : new H22hCondPresetBox { First = r.B }).First)
            .ToList();

        List<int> actual = db.Table<H22hCondPresetRow>()
            .OrderBy(r => r.Id)
            .Select(r => (r.Flag ? new H22hCondPresetBox { First = r.A } : new H22hCondPresetBox { First = r.B }).First)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H22hCondPresetRow> Rows()
    {
        return
        [
            new H22hCondPresetRow { Id = 1, Flag = true, A = 5, B = 7 },
            new H22hCondPresetRow { Id = 2, Flag = false, A = 11, B = 13 }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(null, methodName);
        db.Table<H22hCondPresetRow>().Schema.CreateTable();
        db.Table<H22hCondPresetRow>().AddRange(Rows());
        return db;
    }
}
