using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests.BugHunt;

public class GeneratedColumnAndStringKeyBugTests
{
    [Fact]
    public void Computed_Add_OmitsGeneratedColumn()
    {
        using ModelTestDatabase db = new(model => model.Entity<ProductLine>()
            .Computed(p => p.Total, p => p.Price * p.Quantity));
        db.Schema.CreateTable<ProductLine>();

        db.Table<ProductLine>().Add(new ProductLine { Id = 1, Price = 5m, Quantity = 3 });

        ProductLine row = db.Table<ProductLine>().Single();
        Assert.Equal(15.0m, row.Total);
    }

    [Fact]
    public void Computed_Update_OmitsGeneratedColumn()
    {
        using ModelTestDatabase db = new(model => model.Entity<ProductLine>()
            .Computed(p => p.Total, p => p.Price * p.Quantity));
        db.Schema.CreateTable<ProductLine>();

        db.Execute("INSERT INTO ProductLines (\"Id\", \"Price\", \"Quantity\") VALUES (1, 5.0, 3)");

        ProductLine row = db.Table<ProductLine>().Single();
        row.Quantity = 4;
        db.Table<ProductLine>().Update(row);

        ProductLine updated = db.Table<ProductLine>().Single();
        Assert.Equal(20.0m, updated.Total);
    }

    [Fact]
    public void StringKey_Ddl_HasNotNull()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<StrKeyProbe>();

        string? ddl = db.ExecuteScalar<string?>(
            "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = 'StrKeyProbe'");

        Assert.NotNull(ddl);
        Assert.Contains("NOT NULL", ddl);
    }

    [Fact]
    public void StringKey_RejectsNullKeyInsert()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<StrKeyProbe>();

        Assert.ThrowsAny<Exception>(() =>
            db.Execute("INSERT INTO StrKeyProbe (\"Tag\") VALUES (NULL)"));
    }
}

public class StrKeyProbe
{
    [Key]
    public required string Tag { get; set; }
}
