using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("FillExprReceipts")]
public class FillExprReceiptRow
{
    [Key]
    public int Id { get; set; }

    public int Price { get; set; }

    public int Quantity { get; set; }

    public int Total { get; set; }
}

[Table("FillExprNotes")]
public class FillExprNoteRow
{
    [Key]
    public int Id { get; set; }

    public string Body { get; set; } = "";
}

[Table("FillShadowNotes")]
public class FillShadowNoteRow
{
    [Key]
    public int Id { get; set; }

    public string Body { get; set; } = "";
}

public class MigrationFillTargetExpressionTests
{
    [Fact]
    public void FillExpressionOnAComputedColumnThrowsAClearError()
    {
        using ModelTestDatabase db = new(model => model.Entity<FillExprReceiptRow>()
            .Computed(r => r.Total, r => r.Price * r.Quantity));
        db.Execute("CREATE TABLE \"FillExprReceipts\" (\"Id\" INTEGER PRIMARY KEY, \"Price\" INTEGER NOT NULL, \"Quantity\" INTEGER NOT NULL)");

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => db.Schema.Migrations()
            .Version(1, m => m.TableChanged<FillExprReceiptRow>(s => s.Set(r => r.Total, r => r.Price + 1)))
            .Migrate());

        Assert.Contains("Total", ex.Message);
    }

    [Fact]
    public void FillExpressionOnAnUnknownColumnThrowsAClearError()
    {
        using TestDatabase db = new();
        db.Execute("CREATE TABLE \"FillExprNotes\" (\"Id\" INTEGER PRIMARY KEY, \"Body\" TEXT NOT NULL)");

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => db.Schema.Migrations()
            .Version(1, m => m.TableChanged<FillExprNoteRow>(s => s.Set(r => SQLiteColumn.Of<string>(r, "Nowhere"), r => r.Body)))
            .Migrate());

        Assert.Contains("Nowhere", ex.Message);
    }

    [Fact]
    public void FillOnADeclaredShadowColumnStillRuns()
    {
        using ModelTestDatabase db = new(model => model.Entity<FillShadowNoteRow>()
            .Column("Tag", SQLiteColumnType.Text, nullable: true));
        db.Execute("CREATE TABLE \"FillShadowNotes\" (\"Id\" INTEGER PRIMARY KEY, \"Body\" TEXT NOT NULL)");
        db.Execute("INSERT INTO \"FillShadowNotes\" (\"Id\", \"Body\") VALUES (1, 'x')");

        db.Schema.Migrations()
            .Version(1, m => m.TableChanged<FillShadowNoteRow>(s => s.Set(r => SQLiteColumn.Of<string?>(r, "Tag"), "t")))
            .Migrate();

        Assert.Equal("t", db.ExecuteScalar<string>("SELECT \"Tag\" FROM \"FillShadowNotes\""));
    }
}
