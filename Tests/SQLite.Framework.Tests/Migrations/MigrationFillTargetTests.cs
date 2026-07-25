using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("FillTargetReceipts")]
public class FillTargetReceiptRow
{
    [Key]
    public int Id { get; set; }

    public int Price { get; set; }

    public int Quantity { get; set; }

    public int Total { get; set; }
}

[Table("FillTargetNotes")]
public class FillTargetNoteRow
{
    [Key]
    public int Id { get; set; }

    public string Body { get; set; } = "";
}

public class MigrationFillTargetTests
{
    [Fact]
    public void FillOnAComputedColumnThrowsAClearError()
    {
        using ModelTestDatabase db = new(model => model.Entity<FillTargetReceiptRow>()
            .Computed(r => r.Total, r => r.Price * r.Quantity));
        db.Execute("CREATE TABLE \"FillTargetReceipts\" (\"Id\" INTEGER PRIMARY KEY, \"Price\" INTEGER NOT NULL, \"Quantity\" INTEGER NOT NULL)");
        db.Execute("INSERT INTO \"FillTargetReceipts\" (\"Id\", \"Price\", \"Quantity\") VALUES (1, 5, 3)");

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => db.Schema.Migrations()
            .Version(1, m => m.TableChanged<FillTargetReceiptRow>(s => s.Set(r => r.Total, 99)))
            .Migrate());

        Assert.Contains("Total", ex.Message);
    }

    [Fact]
    public void FillOnAnUnknownColumnThrowsAClearError()
    {
        using TestDatabase db = new();
        db.Execute("CREATE TABLE \"FillTargetNotes\" (\"Id\" INTEGER PRIMARY KEY, \"Body\" TEXT NOT NULL)");
        db.Execute("INSERT INTO \"FillTargetNotes\" (\"Id\", \"Body\") VALUES (1, 'x')");

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => db.Schema.Migrations()
            .Version(1, m => m.TableChanged<FillTargetNoteRow>(s => s.Set(r => SQLiteColumn.Of<string>(r, "Missing"), "v")))
            .Migrate());

        Assert.Contains("Missing", ex.Message);
    }
}
