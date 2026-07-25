using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("AcComputed")]
public sealed class AcComputedRow
{
    [Key]
    public int Id { get; set; }

    public int Price { get; set; }

    public int Quantity { get; set; }

    public int Total { get; set; }
}

public class SchemaAddColumnComputedParityTests
{
    [Fact]
    public void AddColumnExpressionDefault_ForComputedProperty_StaysComputed()
    {
        using ModelTestDatabase db = new(model => model.Entity<AcComputedRow>()
            .Computed(r => r.Total, r => r.Price * r.Quantity));
        db.Execute("CREATE TABLE \"AcComputed\" (\"Id\" INTEGER PRIMARY KEY, \"Price\" INTEGER NOT NULL, \"Quantity\" INTEGER NOT NULL)");

        db.Schema.AddColumn<AcComputedRow>(r => (object?)r.Total, () => 0);

        db.Execute("INSERT INTO \"AcComputed\" (\"Id\", \"Price\", \"Quantity\") VALUES (1, 5, 3)");
        Assert.Equal(15, db.Table<AcComputedRow>().Single().Total);
    }
}
