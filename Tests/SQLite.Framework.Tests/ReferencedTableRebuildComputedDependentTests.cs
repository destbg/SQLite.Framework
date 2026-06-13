using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Attributes;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("RebuildCategory")]
internal sealed class RebuildCategoryV1
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

[Table("RebuildCategory")]
internal sealed class RebuildCategoryV2
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public string? Extra { get; set; }
}

internal sealed class ComputedPriceItem
{
    [Key]
    public int Id { get; set; }

    [ReferencesTable(typeof(RebuildCategoryV1))]
    public int CategoryId { get; set; }

    public double Price { get; set; }

    public double Doubled { get; set; }
}

public class ReferencedTableRebuildComputedDependentTests
{
    [Fact]
    public void RebuildOfReferencedTableSucceedsWithComputedColumnDependent()
    {
        using ModelTestDatabase db = new(model =>
            model.Entity<ComputedPriceItem>().Computed(i => i.Doubled, i => i.Price * 2, stored: true));
        db.Pragmas.ForeignKeys = true;
        db.Schema.CreateTable<RebuildCategoryV1>();
        db.Schema.CreateTable<ComputedPriceItem>();
        db.Table<RebuildCategoryV1>().Add(new RebuildCategoryV1 { Id = 1, Name = "tools" });
        db.Table<ComputedPriceItem>().Add(new ComputedPriceItem { Id = 1, CategoryId = 1, Price = 4 });

        using (SQLiteTransaction transaction = db.BeginTransaction())
        {
            db.Schema.MigrateByRebuild<RebuildCategoryV2>();
            transaction.Commit();
        }

        ComputedPriceItem item = db.Table<ComputedPriceItem>().Single();

        Assert.Equal(4, item.Price);
        Assert.Equal(8, item.Doubled);
    }
}
