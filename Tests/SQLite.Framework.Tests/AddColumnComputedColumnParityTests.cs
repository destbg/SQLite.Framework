using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("CompAddRows")]
internal sealed class CompAddRow
{
    [Key]
    public int Id { get; set; }

    public int Price { get; set; }

    public int Quantity { get; set; }

    public int Total { get; set; }
}

public class AddColumnComputedColumnParityTests
{
    [Fact]
    public void AddColumnForComputedProperty_ProducesComputedValue()
    {
        using ModelTestDatabase db = new(model => model.Entity<CompAddRow>()
            .Computed(r => r.Total, r => r.Price * r.Quantity));
        db.Execute("CREATE TABLE \"CompAddRows\" (\"Id\" INTEGER PRIMARY KEY, \"Price\" INTEGER NOT NULL, \"Quantity\" INTEGER NOT NULL)");

        db.Table<CompAddRow>().Schema.AddColumn(r => (object?)r.Total);

        db.Execute("INSERT INTO \"CompAddRows\" (\"Id\", \"Price\", \"Quantity\") VALUES (1, 5, 3)");

        Assert.Equal(15, db.Table<CompAddRow>().Single().Total);
    }

    [Fact]
    public void AddColumnForStoredComputedProperty_Throws()
    {
        using ModelTestDatabase db = new(model => model.Entity<CompAddRow>()
            .Computed(r => r.Total, r => r.Price * r.Quantity, stored: true));
        db.Execute("CREATE TABLE \"CompAddRows\" (\"Id\" INTEGER PRIMARY KEY, \"Price\" INTEGER NOT NULL, \"Quantity\" INTEGER NOT NULL)");

        Assert.Throws<InvalidOperationException>(() => db.Table<CompAddRow>().Schema.AddColumn(r => (object?)r.Total));
    }
}
