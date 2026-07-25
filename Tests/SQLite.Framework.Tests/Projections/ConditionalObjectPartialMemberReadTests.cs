using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H21eCondRows")]
public class H21eCondRow
{
    [Key]
    public int Id { get; set; }

    public bool Flag { get; set; }

    public int A { get; set; }

    public int B { get; set; }
}

public class H21eCondBox
{
    public int First { get; set; }

    public int Second { get; set; }
}

public record H21eCondPair(int Left)
{
    public int Doubled => Left * 2;
}

public class ConditionalObjectPartialMemberReadTests
{
    private static List<H21eCondRow> Rows()
    {
        return
        [
            new H21eCondRow { Id = 1, Flag = true, A = 5, B = 7 },
            new H21eCondRow { Id = 2, Flag = false, A = 11, B = 13 },
            new H21eCondRow { Id = 3, Flag = true, A = 0, B = 4 }
        ];
    }

    private static TestDatabase Setup()
    {
        TestDatabase db = new();
        db.Table<H21eCondRow>().Schema.CreateTable();
        db.Table<H21eCondRow>().AddRange(Rows());
        return db;
    }

    [Fact]
    public void MemberMissingFromOneBranchReadsTheClrDefault()
    {
        using TestDatabase db = Setup();
        List<H21eCondRow> local = Rows();

        List<int> expected = local
            .OrderBy(r => r.Id)
            .Select(r => (r.Flag ? new H21eCondBox { First = r.A, Second = r.A } : new H21eCondBox { First = r.B }).Second)
            .ToList();

        List<int> actual = db.Table<H21eCondRow>()
            .OrderBy(r => r.Id)
            .Select(r => (r.Flag ? new H21eCondBox { First = r.A, Second = r.A } : new H21eCondBox { First = r.B }).Second)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ComputedMemberOfPositionalBranchesReadsTheComputedValue()
    {
        using TestDatabase db = Setup();
        List<H21eCondRow> local = Rows();

        List<int> expected = local
            .OrderBy(r => r.Id)
            .Select(r => (r.Flag ? new H21eCondPair(r.A) : new H21eCondPair(r.B)).Doubled)
            .ToList();

        List<int> actual = db.Table<H21eCondRow>()
            .OrderBy(r => r.Id)
            .Select(r => (r.Flag ? new H21eCondPair(r.A) : new H21eCondPair(r.B)).Doubled)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void MemberMissingFromOneBranchFiltersOnTheClrDefault()
    {
        using TestDatabase db = Setup();
        List<H21eCondRow> local = Rows();

        List<int> expected = local
            .Where(r => (r.Flag ? new H21eCondBox { First = r.A, Second = r.A } : new H21eCondBox { First = r.B }).Second > 0)
            .OrderBy(r => r.Id)
            .Select(r => r.Id)
            .ToList();

        List<int> actual = db.Table<H21eCondRow>()
            .Where(r => (r.Flag ? new H21eCondBox { First = r.A, Second = r.A } : new H21eCondBox { First = r.B }).Second > 0)
            .OrderBy(r => r.Id)
            .Select(r => r.Id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void MemberMissingFromOneBranchOrdersOnTheClrDefault()
    {
        using TestDatabase db = Setup();
        List<H21eCondRow> local = Rows();

        List<int> expected = local
            .OrderBy(r => (r.Flag ? new H21eCondBox { First = r.A, Second = r.A } : new H21eCondBox { First = r.B }).Second)
            .ThenBy(r => r.Id)
            .Select(r => r.Id)
            .ToList();

        List<int> actual = db.Table<H21eCondRow>()
            .OrderBy(r => (r.Flag ? new H21eCondBox { First = r.A, Second = r.A } : new H21eCondBox { First = r.B }).Second)
            .ThenBy(r => r.Id)
            .Select(r => r.Id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void MemberPresentInBothBranchesReadsTheBranchValue()
    {
        using TestDatabase db = Setup();
        List<H21eCondRow> local = Rows();

        List<int> expected = local
            .OrderBy(r => r.Id)
            .Select(r => (r.Flag ? new H21eCondBox { First = r.A, Second = r.A } : new H21eCondBox { First = r.B, Second = r.B }).Second)
            .ToList();

        List<int> actual = db.Table<H21eCondRow>()
            .OrderBy(r => r.Id)
            .Select(r => (r.Flag ? new H21eCondBox { First = r.A, Second = r.A } : new H21eCondBox { First = r.B, Second = r.B }).Second)
            .ToList();

        Assert.Equal(expected, actual);
    }
}
