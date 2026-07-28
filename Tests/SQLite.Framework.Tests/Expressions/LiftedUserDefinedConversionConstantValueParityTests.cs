using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H23aLiftedConversionRows")]
public class H23aLiftedConversionRow
{
    [Key]
    public int Id { get; set; }

    public int Num { get; set; }
}

public readonly struct H23aLiftedCode
{
    private readonly int value;

    public H23aLiftedCode(int value)
    {
        this.value = value;
    }

    public int Value => value;

    public static implicit operator int(H23aLiftedCode code)
    {
        return code.Value;
    }
}

public class LiftedUserDefinedConversionConstantValueParityTests
{
    [Fact]
    public void ProjectsANullCapturedValueThroughALiftedUserDefinedOperator()
    {
        H23aLiftedCode? code = null;
        using TestDatabase db = Setup();

        List<int?> expected = Rows()
            .OrderBy(r => r.Id)
            .Select(_ => (int?)code)
            .ToList();

        List<int?> actual = db.Table<H23aLiftedConversionRow>()
            .OrderBy(r => r.Id)
            .Select(_ => (int?)code)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FiltersOnANullCapturedValueThroughALiftedUserDefinedOperator()
    {
        H23aLiftedCode? code = null;
        using TestDatabase db = Setup();

        List<int> expected = Rows()
            .Where(r => r.Num == (int?)code)
            .OrderBy(r => r.Id)
            .Select(r => r.Id)
            .ToList();

        List<int> actual = db.Table<H23aLiftedConversionRow>()
            .Where(r => r.Num == (int?)code)
            .OrderBy(r => r.Id)
            .Select(r => r.Id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ProjectsANonNullCapturedValueThroughALiftedUserDefinedOperator()
    {
        H23aLiftedCode? code = new H23aLiftedCode(7);
        using TestDatabase db = Setup();

        List<int?> expected = Rows()
            .OrderBy(r => r.Id)
            .Select(_ => (int?)code)
            .ToList();

        List<int?> actual = db.Table<H23aLiftedConversionRow>()
            .OrderBy(r => r.Id)
            .Select(_ => (int?)code)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H23aLiftedConversionRow> Rows()
    {
        return
        [
            new H23aLiftedConversionRow { Id = 1, Num = 0 },
            new H23aLiftedConversionRow { Id = 2, Num = 20 }
        ];
    }

    private static TestDatabase Setup()
    {
        TestDatabase db = new();
        db.Table<H23aLiftedConversionRow>().Schema.CreateTable();
        db.Table<H23aLiftedConversionRow>().AddRange(Rows());
        return db;
    }
}
