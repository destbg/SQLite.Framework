using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("FkCaseDoc")]
public class FkCaseDocRow
{
    [Key]
    public int Id { get; set; }

    public int Keep { get; set; }
}

[Table("IndexCaseDoc")]
public class IndexCaseDocRow
{
    [Key]
    public int Id { get; set; }

    public int Keep { get; set; }
}

[Table("TriggerCaseDoc")]
public class TriggerCaseDocRow
{
    [Key]
    public int Id { get; set; }

    public int Keep { get; set; }
}

public class MigrationDropColumnReferenceCaseTests
{
    [Fact]
    public void DropsAColumnWhoseForeignKeySpellsItInAnotherCase()
    {
        using TestDatabase db = new(useFile: true);
        db.Execute("CREATE TABLE \"FkCaseDoc\" (\"Id\" INTEGER PRIMARY KEY, \"Keep\" INTEGER NOT NULL, \"Extra\" INTEGER, FOREIGN KEY(\"EXTRA\") REFERENCES \"FkCaseDoc\"(\"Id\"))");
        db.Execute("INSERT INTO \"FkCaseDoc\" (\"Id\", \"Keep\", \"Extra\") VALUES (1, 5, NULL)");

        db.Schema.Migrations()
            .Version(1, m => m.DropColumn<FkCaseDocRow>("Extra"))
            .Migrate();

        Assert.DoesNotContain("Extra", db.Pragmas.TableInfo("FkCaseDoc").Select(c => c.Name));
        Assert.Equal(5, db.Table<FkCaseDocRow>().Single().Keep);
    }

    [Fact]
    public void DropsAColumnWhoseIndexSpellsItInAnotherCase()
    {
        using TestDatabase db = new(useFile: true);
        db.Execute("CREATE TABLE \"IndexCaseDoc\" (\"Id\" INTEGER PRIMARY KEY, \"Keep\" INTEGER NOT NULL, \"Extra\" TEXT)");
        db.Execute("CREATE INDEX \"IndexCaseDocExtra\" ON \"IndexCaseDoc\"(\"EXTRA\")");
        db.Execute("INSERT INTO \"IndexCaseDoc\" (\"Id\", \"Keep\", \"Extra\") VALUES (1, 7, 'x')");

        db.Schema.Migrations()
            .Version(1, m => m.DropColumn<IndexCaseDocRow>("Extra"))
            .Migrate();

        Assert.DoesNotContain("Extra", db.Pragmas.TableInfo("IndexCaseDoc").Select(c => c.Name));
        Assert.Equal(0L, db.ExecuteScalar<long>("SELECT COUNT(*) FROM sqlite_master WHERE name = 'IndexCaseDocExtra'"));
        Assert.Equal(7, db.Table<IndexCaseDocRow>().Single().Keep);
    }

    [Fact]
    public void DropsAColumnWhoseTriggerSpellsItInAnotherCase()
    {
        using TestDatabase db = new(useFile: true);
        db.Execute("CREATE TABLE \"TriggerCaseDoc\" (\"Id\" INTEGER PRIMARY KEY, \"Keep\" INTEGER NOT NULL, \"Extra\" TEXT)");
        db.Execute("CREATE TRIGGER \"TriggerCaseDocTrg\" AFTER INSERT ON \"TriggerCaseDoc\" BEGIN UPDATE \"TriggerCaseDoc\" SET \"Keep\" = \"Keep\" + 1 WHERE \"EXTRA\" IS NOT NULL; END");
        db.Execute("INSERT INTO \"TriggerCaseDoc\" (\"Id\", \"Keep\", \"Extra\") VALUES (1, 3, NULL)");

        db.Schema.Migrations()
            .Version(1, m => m.DropColumn<TriggerCaseDocRow>("Extra"))
            .Migrate();

        Assert.DoesNotContain("Extra", db.Pragmas.TableInfo("TriggerCaseDoc").Select(c => c.Name));
        Assert.Equal(0L, db.ExecuteScalar<long>("SELECT COUNT(*) FROM sqlite_master WHERE name = 'TriggerCaseDocTrg'"));
        db.Execute("INSERT INTO \"TriggerCaseDoc\" (\"Id\", \"Keep\") VALUES (2, 4)");
        Assert.Equal(2, db.Table<TriggerCaseDocRow>().Count());
    }
}
