using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Exceptions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H24eRenameIndexRows")]
public class H24eRenameIndexRow
{
    [Key]
    public int Id { get; set; }

    public string Code { get; set; } = "";
}

[Table("H24eRenameCheckRows")]
public class H24eRenameCheckRow
{
    [Key]
    public int Id { get; set; }

    public int Amount { get; set; }
}

[Table("H24eRenameComputedRows")]
public class H24eRenameComputedRow
{
    [Key]
    public int Id { get; set; }

    public int Price { get; set; }

    public int Quantity { get; set; }

    public int Total { get; set; }
}

public class FluentDeclarationColumnRenameOrderTests
{
    [Fact]
    public void AnIndexFollowsALaterColumnRename()
    {
        using ModelTestDatabase db = new(model => model.Entity<H24eRenameIndexRow>()
            .Index(r => r.Code)
            .HasColumnName(r => r.Code, "cd"));

        db.Schema.CreateTable<H24eRenameIndexRow>();

        List<string> indexedColumns = db.Schema.ListIndexes("H24eRenameIndexRows")
            .SelectMany(name => db.Query<Dictionary<string, object?>>($"PRAGMA index_info('{name}')"))
            .Select(row => (string)row["name"]!)
            .ToList();

        Assert.Equal(new List<string> { "cd" }, indexedColumns);
    }

    [Fact]
    public void AnExpressionIndexWithAFilterFollowsALaterColumnRename()
    {
        using ModelTestDatabase db = new(model => model.Entity<H24eRenameIndexRow>()
            .Index(r => r.Code.ToUpper(), name: "IXH24eExprRename", filter: r => r.Code != "")
            .HasColumnName(r => r.Code, "cd"));

        db.Schema.CreateTable<H24eRenameIndexRow>();

        string indexSql = db.ExecuteScalar<string>(
            "SELECT sql FROM sqlite_master WHERE type = 'index' AND name = 'IXH24eExprRename'")!;

        Assert.Contains("\"cd\"", indexSql);
        Assert.DoesNotContain("\"Code\"", indexSql);
    }

    [Fact]
    public void ACheckConstraintFollowsALaterColumnRename()
    {
        using ModelTestDatabase db = new(model => model.Entity<H24eRenameCheckRow>()
            .Check(r => r.Amount > 0)
            .HasColumnName(r => r.Amount, "amt"));

        db.Schema.CreateTable<H24eRenameCheckRow>();
        db.Table<H24eRenameCheckRow>().Add(new H24eRenameCheckRow { Id = 1, Amount = 5 });

        Assert.Throws<SQLiteException>(() =>
            db.Table<H24eRenameCheckRow>().Add(new H24eRenameCheckRow { Id = 2, Amount = 0 }));
    }

    [Fact]
    public void AComputedColumnFollowsALaterColumnRename()
    {
        using ModelTestDatabase db = new(model => model.Entity<H24eRenameComputedRow>()
            .Computed(r => r.Total, r => r.Price * r.Quantity)
            .HasColumnName(r => r.Price, "prc"));

        db.Schema.CreateTable<H24eRenameComputedRow>();
        db.Table<H24eRenameComputedRow>().Add(new H24eRenameComputedRow { Id = 1, Price = 5, Quantity = 3 });

        int total = db.Table<H24eRenameComputedRow>().Select(r => r.Total).Single();

        Assert.Equal(15, total);
    }
}
