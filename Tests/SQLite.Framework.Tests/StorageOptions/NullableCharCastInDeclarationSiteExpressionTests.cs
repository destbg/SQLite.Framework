using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H26pCharCodeRows")]
public class H26pCharCodeRow
{
    [Key]
    public int Id { get; set; }

    public int Code { get; set; }

    public char? Initial { get; set; }
}

public class NullableCharCastInDeclarationSiteExpressionTests
{
    [Fact]
    public void AComputedColumnCastingANumberToANullableCharMatchesLinq()
    {
        using ModelTestDatabase db = new(
            mb => mb.Entity<H26pCharCodeRow>().Computed(r => r.Initial, r => (char?)r.Code),
            nameof(AComputedColumnCastingANumberToANullableCharMatchesLinq));
        db.Table<H26pCharCodeRow>().Schema.CreateTable();

        List<H26pCharCodeRow> rows = Rows();
        db.Table<H26pCharCodeRow>().AddRange(rows);

        List<char?> expected = rows.OrderBy(r => r.Id).Select(r => (char?)r.Code).ToList();
        List<char?> actual = db.Table<H26pCharCodeRow>()
            .OrderBy(r => r.Id)
            .Select(r => r.Initial)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H26pCharCodeRow> Rows()
    {
        return
        [
            new H26pCharCodeRow { Id = 1, Code = 65 },
            new H26pCharCodeRow { Id = 2, Code = 98 }
        ];
    }
}
