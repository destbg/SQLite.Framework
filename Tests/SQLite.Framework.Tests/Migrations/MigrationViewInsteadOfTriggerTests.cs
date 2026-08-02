using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("SecGViewBaseRows")]
public class SecGViewBaseRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public int Value { get; set; }
}

[Table("SecGBaseSummaryView")]
public class SecGBaseSummary
{
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public class MigrationViewInsteadOfTriggerTests
{
    [Fact]
    public void RedeclaredViewKeepsItsInsteadOfTriggers()
    {
        using TestDatabase db = new(useFile: true);
        db.Table<SecGViewBaseRow>().Schema.CreateTable();
        CreateSummaryView(db);
        db.Schema.CreateTrigger<SecGBaseSummary>(
            "SecGBaseSummaryInsert",
            SQLiteTriggerTiming.InsteadOf,
            SQLiteTriggerEvent.Insert,
            "INSERT INTO \"SecGViewBaseRows\" (\"Id\", \"Name\", \"Value\") VALUES (NEW.\"Id\", NEW.\"Name\", 0)");
        db.Pragmas.UserVersion = 1;

        db.Schema.Migrations()
            .Version(1, m => m.CreateTable<SecGViewBaseRow>())
            .Version(2, m => m.CreateView<SecGBaseSummary>(() =>
                from r in db.Table<SecGViewBaseRow>()
                select new SecGBaseSummary { Id = r.Id, Name = r.Name }))
            .Migrate();

        db.Execute("INSERT INTO \"SecGBaseSummaryView\" (\"Id\", \"Name\") VALUES (1, 'routed')");

        Assert.Equal("routed", db.Table<SecGViewBaseRow>().Single().Name);
    }

    [Fact]
    public void RedeclaredViewKeepsTheTriggerInSchema()
    {
        using TestDatabase db = new(useFile: true);
        db.Table<SecGViewBaseRow>().Schema.CreateTable();
        CreateSummaryView(db);
        db.Schema.CreateTrigger<SecGBaseSummary>(
            "SecGBaseSummaryInsert",
            SQLiteTriggerTiming.InsteadOf,
            SQLiteTriggerEvent.Insert,
            "INSERT INTO \"SecGViewBaseRows\" (\"Id\", \"Name\", \"Value\") VALUES (NEW.\"Id\", NEW.\"Name\", 0)");
        db.Pragmas.UserVersion = 1;

        db.Schema.Migrations()
            .Version(1, m => m.CreateTable<SecGViewBaseRow>())
            .Version(2, m => m.CreateView<SecGBaseSummary>(() =>
                from r in db.Table<SecGViewBaseRow>()
                select new SecGBaseSummary { Id = r.Id, Name = r.Name }))
            .Migrate();

        long count = db.ExecuteScalar<long>(
            "SELECT COUNT(*) FROM sqlite_master WHERE type = 'trigger' AND name = 'SecGBaseSummaryInsert'");
        Assert.Equal(1L, count);
    }

    [Fact]
    public void RedeclaredViewDropsATriggerReferencingARemovedColumn()
    {
        using TestDatabase db = new(useFile: true);
        db.Table<SecGViewBaseRow>().Schema.CreateTable();
        db.Execute("CREATE VIEW \"SecGBaseSummaryView\" AS SELECT \"Id\", \"Name\", \"Value\" AS \"Gone\" FROM \"SecGViewBaseRows\"");
        db.Schema.CreateTrigger<SecGBaseSummary>(
            "SecGBaseSummaryInsertGone",
            SQLiteTriggerTiming.InsteadOf,
            SQLiteTriggerEvent.Insert,
            "INSERT INTO \"SecGViewBaseRows\" (\"Id\", \"Name\", \"Value\") VALUES (NEW.\"Id\", NEW.\"Name\", NEW.\"Gone\")");
        db.Pragmas.UserVersion = 1;

        db.Schema.Migrations()
            .Version(1, m => m.CreateTable<SecGViewBaseRow>())
            .Version(2, m => m.CreateView<SecGBaseSummary>(() =>
                from r in db.Table<SecGViewBaseRow>()
                select new SecGBaseSummary { Id = r.Id, Name = r.Name }))
            .Migrate();

        Assert.Equal(0L, db.ExecuteScalar<long>(
            "SELECT COUNT(*) FROM sqlite_master WHERE type = 'trigger' AND name = 'SecGBaseSummaryInsertGone'"));
    }

    [Fact]
    public void RedeclaredViewOnAMissingViewCreatesIt()
    {
        using TestDatabase db = new(useFile: true);
        db.Table<SecGViewBaseRow>().Schema.CreateTable();
        db.Table<SecGViewBaseRow>().Add(new SecGViewBaseRow { Id = 1, Name = "fresh" });
        db.Pragmas.UserVersion = 1;

        db.Schema.Migrations()
            .Version(2, m => m.CreateView<SecGBaseSummary>(() =>
                from r in db.Table<SecGViewBaseRow>()
                select new SecGBaseSummary { Id = r.Id, Name = r.Name }))
            .Migrate();

        Assert.Equal("fresh", db.Table<SecGBaseSummary>().Single().Name);
    }

    private static void CreateSummaryView(TestDatabase db)
    {
        db.Schema.CreateView<SecGBaseSummary>(() =>
            from r in db.Table<SecGViewBaseRow>()
            select new SecGBaseSummary { Id = r.Id, Name = r.Name });
    }
}
