using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Attributes;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H22cReplaceLedger")]
public class H22cReplaceLedger
{
    [Key]
    public int Id { get; set; }

    public required string Note { get; set; }
}

[Table("H22cReplaceLedgerCopy")]
public class H22cReplaceLedgerCopy
{
    [Key]
    public int Id { get; set; }

    public required string Note { get; set; }
}

[Table("H22cReplaceAudit")]
public class H22cReplaceAudit
{
    [Key]
    [AutoIncrement]
    public int Id { get; set; }

    public required string Operation { get; set; }

    public required string Source { get; set; }
}

public class ReplaceWriteDeleteTriggerParityTests
{
    [Fact]
    public void AddOrUpdateFiresTheSameDeleteTriggersAsARawInsertOrReplace()
    {
        using TestDatabase db = new();
        Setup(db);

        db.Execute("INSERT OR REPLACE INTO \"H22cReplaceLedgerCopy\" (\"Id\", \"Note\") VALUES (1, 'second')");
        db.Table<H22cReplaceLedger>().AddOrUpdate(new H22cReplaceLedger { Id = 1, Note = "second" });

        Assert.Equal(AuditCount(db, "raw"), AuditCount(db, "typed"));
    }

    [Fact]
    public void AddOrUpdateRangeFiresTheSameDeleteTriggersAsARawInsertOrReplace()
    {
        using TestDatabase db = new();
        Setup(db);

        db.Execute("INSERT OR REPLACE INTO \"H22cReplaceLedgerCopy\" (\"Id\", \"Note\") VALUES (1, 'second')");
        db.Table<H22cReplaceLedger>().AddOrUpdateRange([new H22cReplaceLedger { Id = 1, Note = "second" }]);

        Assert.Equal(AuditCount(db, "raw"), AuditCount(db, "typed"));
    }

    private static long AuditCount(TestDatabase db, string source)
    {
        return db.ExecuteScalar<long>(
            $"SELECT COUNT(*) FROM \"H22cReplaceAudit\" WHERE \"Source\" = '{source}'");
    }

    private static void Setup(TestDatabase db)
    {
        db.Table<H22cReplaceLedger>().Schema.CreateTable();
        db.Table<H22cReplaceLedgerCopy>().Schema.CreateTable();
        db.Table<H22cReplaceAudit>().Schema.CreateTable();

        db.Execute(
            "CREATE TRIGGER \"H22cReplaceLedgerAudit\" AFTER DELETE ON \"H22cReplaceLedger\" BEGIN " +
            "INSERT INTO \"H22cReplaceAudit\" (\"Operation\", \"Source\") VALUES('delete', 'typed'); END");
        db.Execute(
            "CREATE TRIGGER \"H22cReplaceLedgerCopyAudit\" AFTER DELETE ON \"H22cReplaceLedgerCopy\" BEGIN " +
            "INSERT INTO \"H22cReplaceAudit\" (\"Operation\", \"Source\") VALUES('delete', 'raw'); END");

        db.Table<H22cReplaceLedger>().Add(new H22cReplaceLedger { Id = 1, Note = "first" });
        db.Execute("INSERT INTO \"H22cReplaceLedgerCopy\" (\"Id\", \"Note\") VALUES (1, 'first')");
    }
}
