using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Attributes;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[StrictTable]
[Table("ChnBAuditRows")]
public class ChnBAuditRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

[Table("ChnBEventRows")]
public class ChnBEventRow
{
    [Key]
    public int Id { get; set; }

    public string Payload { get; set; } = "";
}

public class MigrationCrossTableTriggerDropTests
{
    [Fact]
    public void ARebuildDroppingAColumnKeepsCrossTableWritesWorking()
    {
        using TestDatabase db = new();
        Seed(db);
        db.Pragmas.UserVersion = 1;

        db.Schema.Migrations()
            .Version(2, m => m.TableChanged<ChnBEventRow>())
            .Version(3, m => m.TableChanged<ChnBAuditRow>())
            .Migrate();

        db.Table<ChnBEventRow>().Add(new ChnBEventRow { Id = 2, Payload = "two" });

        Assert.Equal(1L, db.ExecuteScalar<long>("SELECT COUNT(*) FROM \"ChnBEventRows\""));
        Assert.Equal(1L, db.ExecuteScalar<long>("SELECT COUNT(*) FROM \"ChnBAuditRows\""));
    }

    [Fact]
    public void ADropColumnStepKeepsCrossTableWritesWorking()
    {
        using TestDatabase db = new();
        Seed(db);
        db.Pragmas.UserVersion = 1;

        db.Schema.Migrations()
            .Version(2, m => m.DropColumn<ChnBAuditRow>("Level"))
            .Migrate();

        db.Table<ChnBEventRow>().Add(new ChnBEventRow { Id = 2, Payload = "two" });

        Assert.Equal(1L, db.ExecuteScalar<long>("SELECT COUNT(*) FROM \"ChnBEventRows\""));
        Assert.Equal(1L, db.ExecuteScalar<long>("SELECT COUNT(*) FROM \"ChnBAuditRows\""));
    }

    [Fact]
    public void ARebuildKeepsACrossTableTriggerThatDoesNotReferenceTheDroppedColumn()
    {
        using TestDatabase db = new();
        db.Execute("CREATE TABLE \"ChnBAuditRows\" (\"Id\" INTEGER NOT NULL PRIMARY KEY, \"Name\" TEXT NOT NULL, \"Level\" INTEGER NOT NULL) STRICT");
        db.Execute("INSERT INTO \"ChnBAuditRows\" (\"Id\", \"Name\", \"Level\") VALUES (1, 'seed', 3)");
        db.Execute("CREATE TABLE \"ChnBEventRows\" (\"Id\" INTEGER NOT NULL PRIMARY KEY, \"Payload\" TEXT NOT NULL)");
        db.Execute("CREATE TABLE \"ChnBUnrelatedRows\" (\"Id\" INTEGER NOT NULL PRIMARY KEY)");
        db.Execute("CREATE TRIGGER \"ChnBEventAuditName\" AFTER INSERT ON \"ChnBEventRows\" BEGIN INSERT INTO \"ChnBAuditRows\" (\"Id\", \"Name\") VALUES (new.\"Id\", new.\"Payload\"); END");
        db.Execute("CREATE TRIGGER \"ChnBEventUnrelated\" AFTER INSERT ON \"ChnBEventRows\" BEGIN INSERT INTO \"ChnBUnrelatedRows\" (\"Id\") VALUES (new.\"Id\"); END");
        db.Pragmas.UserVersion = 1;

        db.Schema.Migrations()
            .Version(2, m => m.TableChanged<ChnBAuditRow>())
            .Migrate();

        db.Table<ChnBEventRow>().Add(new ChnBEventRow { Id = 2, Payload = "two" });

        Assert.Equal(2L, db.ExecuteScalar<long>("SELECT COUNT(*) FROM \"ChnBAuditRows\""));
        Assert.Equal(1L, db.ExecuteScalar<long>("SELECT COUNT(*) FROM \"ChnBUnrelatedRows\""));
    }

    private static void Seed(TestDatabase db)
    {
        db.Execute("CREATE TABLE \"ChnBAuditRows\" (\"Id\" INTEGER NOT NULL PRIMARY KEY, \"Name\" TEXT NOT NULL, \"Level\" INTEGER NOT NULL) STRICT");
        db.Execute("INSERT INTO \"ChnBAuditRows\" (\"Id\", \"Name\", \"Level\") VALUES (1, 'seed', 3)");
        db.Execute("CREATE TABLE \"ChnBEventRows\" (\"Id\" INTEGER NOT NULL PRIMARY KEY, \"Payload\" TEXT NOT NULL)");
        db.Execute("CREATE TRIGGER \"ChnBEventAudit\" AFTER INSERT ON \"ChnBEventRows\" BEGIN INSERT INTO \"ChnBAuditRows\" (\"Id\", \"Name\", \"Level\") VALUES (new.\"Id\", new.\"Payload\", 0); END");
    }
}
