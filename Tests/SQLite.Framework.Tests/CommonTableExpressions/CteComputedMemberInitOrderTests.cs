using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class CteComputedMemberInitOrderTests
{
    [Fact]
    public void ComputedMemberInitOutOfDeclarationOrderKeepsValues()
    {
        using TestDatabase db = new();
        db.Table<CteSwapBookRow>().Schema.CreateTable();
        db.Table<CteSwapBookRow>().Add(new CteSwapBookRow { Id = 1, AuthorId = 7 });

        SQLiteCte<CteSwapDto> cte = db.With(() => db.Table<CteSwapBookRow>()
            .Select(b => new CteSwapDto(new CteSwapSub(b.Id)) { C = b.AuthorId + 1, B = b.Id }));

        var row = cte.Select(d => new { d.B, d.C }).Single();

        Assert.Equal(1, row.B);
        Assert.Equal(8, row.C);
    }
}
