using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("concat_nullable_arith_rows")]
public class ConcatNullableArithRow
{
    [Key]
    public int Id { get; set; }

    public required string Name { get; set; }

    public int? A { get; set; }

    public int? B { get; set; }

    public int C { get; set; }
}

public class StringConcatNullableArithmeticParityTests
{
    private static readonly ConcatNullableArithRow[] Rows =
    [
        new ConcatNullableArithRow { Id = 1, Name = "Bob", A = 2, B = 3, C = 7 },
        new ConcatNullableArithRow { Id = 2, Name = "Amy", A = null, B = 3, C = 8 },
        new ConcatNullableArithRow { Id = 3, Name = "Cid", A = 4, B = null, C = 9 },
        new ConcatNullableArithRow { Id = 4, Name = "Dot", A = null, B = null, C = 0 },
    ];

    [Fact]
    public void ConcatWithNullableSumMatchesDotNet()
    {
        using TestDatabase db = new();
        db.Table<ConcatNullableArithRow>().Schema.CreateTable();
        db.Table<ConcatNullableArithRow>().AddRange(Rows);

        List<string> expected = Rows.OrderBy(x => x.Id).Select(x => x.Name + (x.A + x.B)).ToList();
        List<string> actual = db.Table<ConcatNullableArithRow>().OrderBy(x => x.Id).Select(x => x.Name + (x.A + x.B)).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ConcatWithNullableProductMatchesDotNet()
    {
        using TestDatabase db = new();
        db.Table<ConcatNullableArithRow>().Schema.CreateTable();
        db.Table<ConcatNullableArithRow>().AddRange(Rows);

        List<string> expected = Rows.OrderBy(x => x.Id).Select(x => x.Name + (x.A * 2)).ToList();
        List<string> actual = db.Table<ConcatNullableArithRow>().OrderBy(x => x.Id).Select(x => x.Name + (x.A * 2)).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ConcatWithNullableDifferenceMatchesDotNet()
    {
        using TestDatabase db = new();
        db.Table<ConcatNullableArithRow>().Schema.CreateTable();
        db.Table<ConcatNullableArithRow>().AddRange(Rows);

        List<string> expected = Rows.OrderBy(x => x.Id).Select(x => x.Name + (x.A - x.B)).ToList();
        List<string> actual = db.Table<ConcatNullableArithRow>().OrderBy(x => x.Id).Select(x => x.Name + (x.A - x.B)).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ConcatWithNonNullableProductMatchesDotNet()
    {
        using TestDatabase db = new();
        db.Table<ConcatNullableArithRow>().Schema.CreateTable();
        db.Table<ConcatNullableArithRow>().AddRange(Rows);

        List<string> expected = Rows.OrderBy(x => x.Id).Select(x => x.Name + (x.C * 2)).ToList();
        List<string> actual = db.Table<ConcatNullableArithRow>().OrderBy(x => x.Id).Select(x => x.Name + (x.C * 2)).ToList();

        Assert.Equal(expected, actual);
    }
}
