using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("CnfRow")]
public class CnfRow
{
    [Key]
    public int Id { get; set; }

    public int Amount { get; set; }

    public bool Flag { get; set; }
}

public class CnfInitDto
{
    public int A { get; set; }
}

public class CnfCtorDto
{
    public CnfCtorDto(int a)
    {
        A = a;
    }

    public int A { get; }
}

public class ConditionalCompositeNullFoldParityTests
{
    private static List<CnfRow> Rows() =>
    [
        new CnfRow { Id = 1, Amount = 10, Flag = true },
        new CnfRow { Id = 2, Amount = 3, Flag = true },
        new CnfRow { Id = 3, Amount = 7, Flag = false },
        new CnfRow { Id = 4, Amount = 0, Flag = false },
    ];

    private static TestDatabase Setup()
    {
        TestDatabase db = new();
        db.Table<CnfRow>().Schema.CreateTable();
        db.Table<CnfRow>().AddRange(Rows());
        return db;
    }

    [Fact]
    public void MemberInitArmNullCheckMatchesLinq()
    {
        using TestDatabase db = Setup();

        List<bool> expected = Rows().OrderBy(r => r.Id)
            .Select(r => (r.Flag ? new CnfInitDto { A = r.Amount } : null) == null)
            .ToList();

        List<bool> actual = db.Table<CnfRow>().OrderBy(r => r.Id)
            .Select(r => (r.Flag ? new CnfInitDto { A = r.Amount } : null) == null)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ConstructorArmNotNullCheckMatchesLinq()
    {
        using TestDatabase db = Setup();

        List<bool> expected = Rows().OrderBy(r => r.Id)
            .Select(r => (r.Flag ? new CnfCtorDto(r.Amount) : null) != null)
            .ToList();

        List<bool> actual = db.Table<CnfRow>().OrderBy(r => r.Id)
            .Select(r => (r.Flag ? new CnfCtorDto(r.Amount) : null) != null)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NestedIfTrueArmNullCheckMatchesLinq()
    {
        using TestDatabase db = Setup();

        List<bool> expected = Rows().OrderBy(r => r.Id)
            .Select(r => (r.Flag ? (r.Amount > 5 ? new CnfInitDto { A = r.Amount } : null) : null) == null)
            .ToList();

        List<bool> actual = db.Table<CnfRow>().OrderBy(r => r.Id)
            .Select(r => (r.Flag ? (r.Amount > 5 ? new CnfInitDto { A = r.Amount } : null) : null) == null)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NestedIfFalseArmNotNullCheckMatchesLinq()
    {
        using TestDatabase db = Setup();

        List<bool> expected = Rows().OrderBy(r => r.Id)
            .Select(r => (r.Flag ? new CnfInitDto { A = r.Amount } : (r.Amount > 5 ? new CnfInitDto { A = r.Id } : null)) != null)
            .ToList();

        List<bool> actual = db.Table<CnfRow>().OrderBy(r => r.Id)
            .Select(r => (r.Flag ? new CnfInitDto { A = r.Amount } : (r.Amount > 5 ? new CnfInitDto { A = r.Id } : null)) != null)
            .ToList();

        Assert.Equal(expected, actual);
    }
}
