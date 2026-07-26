using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H22iConversionRows")]
public class H22iConversionRow
{
    [Key]
    public int Id { get; set; }

    public int Num { get; set; }
}

public readonly struct H22iCode
{
    private readonly int value;

    public H22iCode(int value)
    {
        this.value = value;
    }

    public int Value => value;

    public static implicit operator int(H22iCode code)
    {
        return code.Value;
    }
}

public class UserDefinedConversionConstantValueParityTests
{
    [Fact]
    public void FiltersOnACapturedValueConvertedByAUserDefinedOperator()
    {
        H22iCode code = new(20);
        using TestDatabase db = Setup();

        List<int> expected = Rows()
            .Where(r => r.Num == code)
            .OrderBy(r => r.Id)
            .Select(r => r.Id)
            .ToList();

        List<int> actual = db.Table<H22iConversionRow>()
            .Where(r => r.Num == code)
            .OrderBy(r => r.Id)
            .Select(r => r.Id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ProjectsACapturedValueConvertedByAUserDefinedOperator()
    {
        H22iCode code = new(7);
        using TestDatabase db = Setup();

        List<int> expected = Rows()
            .OrderBy(r => r.Id)
            .Select(_ => (int)code)
            .ToList();

        List<int> actual = db.Table<H22iConversionRow>()
            .OrderBy(r => r.Id)
            .Select(_ => (int)code)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H22iConversionRow> Rows()
    {
        return
        [
            new H22iConversionRow { Id = 1, Num = 10 },
            new H22iConversionRow { Id = 2, Num = 20 }
        ];
    }

    private static TestDatabase Setup()
    {
        TestDatabase db = new();
        db.Table<H22iConversionRow>().Schema.CreateTable();
        db.Table<H22iConversionRow>().AddRange(Rows());
        return db;
    }
}
