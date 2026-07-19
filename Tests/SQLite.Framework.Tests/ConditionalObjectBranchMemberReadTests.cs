using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H20PrjRow")]
public class H20PrjRow
{
    [Key]
    public int Id { get; set; }

    public string? Name { get; set; }

    public int Amount { get; set; }

    public bool Flag { get; set; }
}

public class H20PrjDto
{
    public int A { get; set; }
}

public class ConditionalObjectBranchMemberReadTests
{
    private static List<H20PrjRow> Rows() =>
    [
        new H20PrjRow { Id = 1, Name = "one", Amount = 2, Flag = true },
        new H20PrjRow { Id = 2, Name = null, Amount = 0, Flag = false },
        new H20PrjRow { Id = 3, Name = "three", Amount = 30, Flag = true },
    ];

    private static TestDatabase Setup()
    {
        TestDatabase db = new();
        db.Table<H20PrjRow>().Schema.CreateTable();
        db.Table<H20PrjRow>().AddRange(Rows());
        return db;
    }

    [Fact]
    public void BothConstructedBranchesMemberReadMatchesLinq()
    {
        using TestDatabase db = Setup();

        List<int> expected = Rows().OrderBy(r => r.Id)
            .Select(r => (r.Flag ? new H20PrjDto { A = r.Amount } : new H20PrjDto { A = r.Id }).A)
            .ToList();

        List<int> actual = db.Table<H20PrjRow>().OrderBy(r => r.Id)
            .Select(r => (r.Flag ? new H20PrjDto { A = r.Amount } : new H20PrjDto { A = r.Id }).A)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ConstructedAndCapturedBranchesMemberReadMatchesLinq()
    {
        using TestDatabase db = Setup();

        H20PrjDto captured = new() { A = 7 };

        List<int> expected = Rows().OrderBy(r => r.Id)
            .Select(r => (r.Flag ? new H20PrjDto { A = r.Amount } : captured).A)
            .ToList();

        List<int> actual = db.Table<H20PrjRow>().OrderBy(r => r.Id)
            .Select(r => (r.Flag ? new H20PrjDto { A = r.Amount } : captured).A)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CapturedBranchesMemberReadMatchesLinq()
    {
        using TestDatabase db = Setup();

        H20PrjDto first = new() { A = 100 };
        H20PrjDto second = new() { A = 200 };

        List<int> expected = Rows().OrderBy(r => r.Id)
            .Select(r => (r.Flag ? first : second).A + r.Id)
            .ToList();

        List<int> actual = db.Table<H20PrjRow>().OrderBy(r => r.Id)
            .Select(r => (r.Flag ? first : second).A + r.Id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CapturedOrNullBranchGuardedMemberReadMatchesLinq()
    {
        using TestDatabase db = Setup();

        H20PrjDto captured = new() { A = 7 };

        List<int> expected = Rows().OrderBy(r => r.Id)
            .Select(r => (r.Flag ? captured : null) == null ? -1 : (r.Flag ? captured : null)!.A)
            .ToList();

        List<int> actual = db.Table<H20PrjRow>().OrderBy(r => r.Id)
            .Select(r => (r.Flag ? captured : null) == null ? -1 : (r.Flag ? captured : null)!.A)
            .ToList();

        Assert.Equal(expected, actual);
    }
}
