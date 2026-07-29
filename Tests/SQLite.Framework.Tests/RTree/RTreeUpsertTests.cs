using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Attributes;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[RTreeIndex]
[Table("H24oUpsertRegions")]
public class H24oUpsertRegion
{
    [Key]
    public int Id { get; set; }

    [RTreeMin("X")]
    public float MinX { get; set; }

    [RTreeMax("X")]
    public float MaxX { get; set; }
}

public class RTreeUpsertTests
{
    [Fact]
    public void UpsertOnAnRTreeTableReportsAClearUnsupportedError()
    {
        using TestDatabase db = new();
        db.Table<H24oUpsertRegion>().Schema.CreateTable();
        db.Table<H24oUpsertRegion>().Add(new H24oUpsertRegion { Id = 1, MinX = 0, MaxX = 10 });

        Assert.Throws<NotSupportedException>(() =>
            db.Table<H24oUpsertRegion>().Upsert(
                new H24oUpsertRegion { Id = 1, MinX = 5, MaxX = 15 },
                c => c.OnConflict(x => x.Id).DoUpdateAll()));
    }
}
