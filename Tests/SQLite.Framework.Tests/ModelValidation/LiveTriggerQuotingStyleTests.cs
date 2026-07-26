using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Enums;
using SQLite.Framework.Internals.Helpers;
using SQLite.Framework.Models;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H22bQuoteAudits")]
public class H22bQuoteAudit
{
    [Key]
    public int Id { get; set; }

    public int ItemId { get; set; }
}

[Table("H22bQuoteSources")]
public class H22bQuoteSource
{
    [Key]
    public int Id { get; set; }
}

file sealed class H22bQuoteDb : TestDatabase
{
    protected override void OnModelCreating(SQLiteModelBuilder builder)
    {
        builder.Entity<H22bQuoteAudit>().HasKey(a => a.Id);
        builder.Entity<H22bQuoteSource>()
            .Trigger("trgH22bQuote", SQLiteTriggerTiming.After, SQLiteTriggerEvent.Insert,
                t => t.Insert(Table<H22bQuoteAudit>(), s => s.Set(a => a.ItemId, _ => t.New.Id)));
    }
}

public class LiveTriggerQuotingStyleTests
{
    [Fact]
    public void SingleQuotedIdentifiersInALiveTriggerAreEquivalent()
    {
        using H22bQuoteDb db = new();
        db.Schema.CreateTable<H22bQuoteAudit>();
        db.Schema.CreateTable<H22bQuoteSource>();

        string declared = db.ExecuteScalar<string>(
            "SELECT sql FROM sqlite_master WHERE type = 'trigger' AND name = 'trgH22bQuote'")!;
        string singleQuoted = declared.Replace("\"", "'");
        Assert.NotEqual(declared, singleQuoted);

        db.Schema.DropTrigger("trgH22bQuote");
        db.Execute(singleQuoted);
        db.Table<H22bQuoteSource>().Add(new H22bQuoteSource { Id = 5 });

        List<int> auditItemIds = db.Table<H22bQuoteAudit>().Select(a => a.ItemId).ToList();
        Assert.Equal(new List<int> { 5 }, auditItemIds);

        SQLiteModelValidationResult result = db.Schema.ValidateModel<H22bQuoteSource>();

        Assert.True(result.IsValid, string.Join(" | ", result.Issues));
    }

    [Theory]
    [InlineData("INSERT INTO \"H22bQuoteAudits\" (\"ItemId\") VALUES (1)", "INSERT INTO 'H22bQuoteAudits' ('ItemId') VALUES (1)")]
    [InlineData("NEW.\"Id\"", "NEW.'Id'")]
    public void SingleQuotedIdentifierPositionsMatchDoubleQuotedOnes(string expected, string actual)
    {
        Assert.True(SchemaSqlNormalizer.AreEquivalent(expected, actual));
    }
}
