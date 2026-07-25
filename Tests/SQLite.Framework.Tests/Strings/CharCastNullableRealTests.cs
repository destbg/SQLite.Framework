using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("NullableRealCoded")]
public class NullableRealCodedRow
{
    [Key]
    public int Id { get; set; }

    public double? RealCode { get; set; }
}

public class CharCastNullableRealTests
{
    [Fact]
    public void CastOfANullableDoubleColumnToCharMatchesLinq()
    {
        using TestDatabase db = new();
        db.Table<NullableRealCodedRow>().Schema.CreateTable();
        List<NullableRealCodedRow> rows =
        [
            new NullableRealCodedRow { Id = 1, RealCode = 66.0 },
            new NullableRealCodedRow { Id = 2, RealCode = null },
        ];
        db.Table<NullableRealCodedRow>().AddRange(rows);

        List<char?> expected = rows.OrderBy(r => r.Id).Select(r => (char?)r.RealCode).ToList();
        List<char?> actual = db.Table<NullableRealCodedRow>().OrderBy(r => r.Id).Select(r => (char?)r.RealCode).ToList();

        Assert.Equal(expected, actual);
    }
}
