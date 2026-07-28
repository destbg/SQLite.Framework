using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Enums;
using SQLite.Framework.Internals.Helpers;
using SQLite.Framework.Models;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H23eStockMoves")]
public class H23eStockMove
{
    [Key]
    public int Id { get; set; }

    public int ProductId { get; set; }

    public int Quantity { get; set; }
}

[Table("H23eStockTotals")]
public class H23eStockTotal
{
    [Key]
    public int Id { get; set; }

    public int OnHand { get; set; }
}

file sealed class H23eStockDb : TestDatabase
{
    protected override void OnModelCreating(SQLiteModelBuilder builder)
    {
        builder.Entity<H23eStockTotal>().HasKey(t => t.Id);
        builder.Entity<H23eStockMove>()
            .Trigger("trgH23eStock", SQLiteTriggerTiming.After, SQLiteTriggerEvent.Insert,
                t => t.Update(
                    Table<H23eStockTotal>(),
                    x => x.Id == t.New.ProductId,
                    s => s.Set(x => x.OnHand, _ => t.New.Quantity)));
    }
}

public class LiveTriggerUpdateStatementQuotingTests
{
    [Fact]
    public void SingleQuotedUpdateTargetAndSetColumnInALiveTriggerAreEquivalent()
    {
        using H23eStockDb db = new();
        db.Schema.CreateTable<H23eStockTotal>();
        db.Schema.CreateTable<H23eStockMove>();

        string declared = db.ExecuteScalar<string>(
            "SELECT sql FROM sqlite_master WHERE type = 'trigger' AND name = 'trgH23eStock'")!;
        string singleQuoted = declared.Replace(
            "UPDATE \"H23eStockTotals\" SET \"OnHand\"",
            "UPDATE 'H23eStockTotals' SET 'OnHand'",
            StringComparison.Ordinal);
        Assert.NotEqual(declared, singleQuoted);

        db.Schema.DropTrigger("trgH23eStock");
        db.Execute(singleQuoted);

        db.Table<H23eStockTotal>().AddRange(Totals());
        db.Table<H23eStockMove>().AddRange(Moves());

        int expected = Moves().Single(m => m.ProductId == 7).Quantity;
        int actual = db.Table<H23eStockTotal>().Single(t => t.Id == 7).OnHand;
        Assert.Equal(expected, actual);

        SQLiteModelValidationResult result = db.Schema.ValidateModel<H23eStockMove>();

        Assert.True(result.IsValid, string.Join(" | ", result.Issues));
    }

    [Theory]
    [InlineData("UPDATE \"H23eStockTotals\" SET \"OnHand\" = 1", "UPDATE 'H23eStockTotals' SET \"OnHand\" = 1")]
    [InlineData("UPDATE \"H23eStockTotals\" SET \"OnHand\" = 1", "UPDATE \"H23eStockTotals\" SET 'OnHand' = 1")]
    public void SingleQuotedIdentifierPositionsInAnUpdateStatementMatchDoubleQuotedOnes(string expected, string actual)
    {
        Assert.True(SchemaSqlNormalizer.AreEquivalent(expected, actual));
    }

    private static List<H23eStockTotal> Totals()
    {
        return
        [
            new H23eStockTotal { Id = 7, OnHand = 2 }
        ];
    }

    private static List<H23eStockMove> Moves()
    {
        return
        [
            new H23eStockMove { Id = 1, ProductId = 7, Quantity = 5 }
        ];
    }
}
