using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("CteSwapBook")]
public class CteSwapBookRow
{
    [Key]
    public int Id { get; set; }

    public int AuthorId { get; set; }
}

public class CteSwapSub
{
    public CteSwapSub(int p)
    {
        P = p;
    }

    public int P { get; }
}

public class CteSwapDto
{
    public CteSwapDto(CteSwapSub s)
    {
        S = s;
    }

    public CteSwapSub S { get; }

    public int B { get; set; }

    public int C { get; set; }
}

public class CteMemberInitOrderTests
{
    [Fact]
    public void CteMemberInitOutOfDeclarationOrder()
    {
        using TestDatabase db = new();
        db.Table<CteSwapBookRow>().Schema.CreateTable();
        db.Table<CteSwapBookRow>().Add(new CteSwapBookRow { Id = 1, AuthorId = 7 });

        SQLiteCte<CteSwapDto> cte = db.With(() => db.Table<CteSwapBookRow>()
            .Select(b => new CteSwapDto(new CteSwapSub(b.Id)) { C = b.AuthorId, B = b.Id }));

        var row = cte.Select(d => new { d.B, d.C }).Single();

        Assert.Equal(1, row.B);
        Assert.Equal(7, row.C);
    }
}
