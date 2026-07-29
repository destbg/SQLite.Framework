using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Enums;
using SQLite.Framework.Internals.Helpers;
using SQLite.Framework.Models;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H24eClauseAudits")]
public class H24eClauseAudit
{
    [Key]
    public int Id { get; set; }

    public int ItemId { get; set; }
}

[Table("H24eClauseSources")]
public class H24eClauseSource
{
    [Key]
    public int Id { get; set; }
}

file sealed class H24eClauseDb : TestDatabase
{
    protected override void OnModelCreating(SQLiteModelBuilder builder)
    {
        builder.Entity<H24eClauseAudit>().HasKey(a => a.Id);
        builder.Entity<H24eClauseSource>()
            .Trigger("trgH24eClause", SQLiteTriggerTiming.After, SQLiteTriggerEvent.Insert,
                t => t.Insert(Table<H24eClauseAudit>(), s => s.Set(a => a.ItemId, _ => t.New.Id)));
    }
}

public class LiveTriggerOptionalRowClauseTests
{
    [Fact]
    public void ALiveTriggerWithoutTheOptionalForEachRowClauseIsEquivalent()
    {
        using H24eClauseDb db = new();
        db.Schema.CreateTable<H24eClauseAudit>();
        db.Schema.CreateTable<H24eClauseSource>();

        string declared = db.ExecuteScalar<string>(
            "SELECT sql FROM sqlite_master WHERE type = 'trigger' AND name = 'trgH24eClause'")!;
        string withoutRowClause = declared.Replace(" FOR EACH ROW", "", StringComparison.Ordinal);
        Assert.NotEqual(declared, withoutRowClause);

        db.Schema.DropTrigger("trgH24eClause");
        db.Execute(withoutRowClause);
        db.Table<H24eClauseSource>().Add(new H24eClauseSource { Id = 5 });

        List<int> auditItemIds = db.Table<H24eClauseAudit>().Select(a => a.ItemId).ToList();
        Assert.Equal(new List<int> { 5 }, auditItemIds);

        SQLiteModelValidationResult result = db.Schema.ValidateModel<H24eClauseSource>();

        Assert.True(result.IsValid, string.Join(" | ", result.Issues));
    }

    [Fact]
    public void ALiveTriggerWithASchemaQualifiedNameIsEquivalent()
    {
        using H24eClauseDb db = new();
        db.Schema.CreateTable<H24eClauseAudit>();
        db.Schema.CreateTable<H24eClauseSource>();

        string declared = db.ExecuteScalar<string>(
            "SELECT sql FROM sqlite_master WHERE type = 'trigger' AND name = 'trgH24eClause'")!;
        string qualified = declared.Replace(
            "\"trgH24eClause\" AFTER INSERT",
            "\"main\".\"trgH24eClause\" AFTER INSERT",
            StringComparison.Ordinal);
        Assert.NotEqual(declared, qualified);

        db.Schema.DropTrigger("trgH24eClause");
        db.Execute(qualified);
        db.Table<H24eClauseSource>().Add(new H24eClauseSource { Id = 7 });

        List<int> auditItemIds = db.Table<H24eClauseAudit>().Select(a => a.ItemId).ToList();
        Assert.Equal(new List<int> { 7 }, auditItemIds);

        SQLiteModelValidationResult result = db.Schema.ValidateModel<H24eClauseSource>();

        Assert.True(result.IsValid, string.Join(" | ", result.Issues));
    }

    [Theory]
    [InlineData(
        "CREATE TRIGGER \"t\" AFTER INSERT ON \"S\" FOR EACH ROW BEGIN DELETE FROM \"A\"; END",
        "CREATE TRIGGER \"t\" AFTER INSERT ON \"S\" BEGIN DELETE FROM \"A\"; END")]
    [InlineData(
        "CREATE TRIGGER \"t\" AFTER INSERT ON \"S\" FOR EACH ROW BEGIN DELETE FROM \"A\"; END",
        "CREATE TRIGGER \"t\" AFTER INSERT ON \"S\" FOR EACH ROW BEGIN DELETE FROM \"main\".\"A\"; END")]
    public void SemanticallyNeutralTriggerClausesMatchTheDeclaredDefinition(string expected, string actual)
    {
        Assert.True(SchemaSqlNormalizer.AreEquivalent(expected, actual));
    }
}
