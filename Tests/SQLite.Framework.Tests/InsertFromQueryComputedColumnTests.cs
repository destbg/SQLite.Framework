using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class InsertFromQueryComputedColumnTests
{
    [Fact]
    public void InsertFromQueryBareSourceExcludesComputedColumn()
    {
        using ModelTestDatabase db = new(model => model.Entity<ProductLine>()
            .Computed(p => p.Total, p => p.Price * p.Quantity));
        db.Schema.CreateTable<ProductLine>();
        db.Execute("INSERT INTO ProductLines (\"Id\", \"Price\", \"Quantity\") VALUES (1, 5.0, 3)");

        int inserted = db.Table<ProductLine>().InsertFromQuery(db.Table<ProductLine>().Where(p => p.Id < 0));

        Assert.Equal(0, inserted);
    }
}
