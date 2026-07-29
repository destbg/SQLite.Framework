using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H24dArchivedTags")]
public class H24dArchivedTag
{
    [Key]
    public int Id { get; set; }

    [SQLite.Framework.Attributes.Indexed(IsUnique = true)]
    public string Name { get; set; } = "";

    public string? Tag { get; set; }
}

[Table("H24dPendingSeeds")]
public class H24dPendingSeed
{
    [Key]
    public int Id { get; set; }

    [SQLite.Framework.Attributes.Indexed(IsUnique = true)]
    public string Name { get; set; } = "";

    public string Tag { get; set; } = "";
}

public class PendingFillBeforeLaterRebuildTests
{
    [Fact]
    public void AFillReadingAColumnOutsideTheModelRunsBeforeALaterRebuildDropsThatColumn()
    {
        using TestDatabase stepwise = new(useFile: true);
        SeedArchived(stepwise);
        ArchivedChain(stepwise.Schema.Migrations(), 2).Migrate();
        ArchivedChain(stepwise.Schema.Migrations(), 3).Migrate();

        using TestDatabase collapsed = new(useFile: true);
        SeedArchived(collapsed);
        ArchivedChain(collapsed.Schema.Migrations(), 3).Migrate();

        List<string> stepwiseNames = Column(stepwise, "H24dArchivedTags", "Name");
        List<string> stepwiseTags = Column(stepwise, "H24dArchivedTags", "Tag");

        Assert.Equal(["a-filled", "b-filled"], stepwiseNames);
        Assert.Equal(["keepme", "other"], stepwiseTags);
        Assert.Equal(stepwiseNames, Column(collapsed, "H24dArchivedTags", "Name"));
        Assert.Equal(stepwiseTags, Column(collapsed, "H24dArchivedTags", "Tag"));
    }

    [Fact]
    public void AConstantFillOnANullableColumnRunsBeforeALaterRebuildMakesThatColumnRequired()
    {
        using TestDatabase stepwise = new(useFile: true);
        SeedPending(stepwise);
        PendingChain(stepwise.Schema.Migrations(), 2).Migrate();
        PendingChain(stepwise.Schema.Migrations(), 3).Migrate();

        using TestDatabase collapsed = new(useFile: true);
        SeedPending(collapsed);
        PendingChain(collapsed.Schema.Migrations(), 3).Migrate();

        List<string> stepwiseNames = Column(stepwise, "H24dPendingSeeds", "Name");
        List<string> stepwiseTags = Column(stepwise, "H24dPendingSeeds", "Tag");

        Assert.Equal(["a-filled", "b-filled"], stepwiseNames);
        Assert.Equal(["seed", "seed"], stepwiseTags);
        Assert.Equal(stepwiseNames, Column(collapsed, "H24dPendingSeeds", "Name"));
        Assert.Equal(stepwiseTags, Column(collapsed, "H24dPendingSeeds", "Tag"));
    }

    private static SQLiteMigrationRunner ArchivedChain(SQLiteMigrationRunner runner, int upTo)
    {
        runner.Version(1, m => m.Sql("SELECT 1"));
        runner.Version(2, m => m.TableChanged<H24dArchivedTag>(
            s => s.Set(x => x.Tag, r => SQLiteColumn.Of<string?>(r, "Legacy"))));
        if (upTo >= 3)
        {
            runner.Version(3, m => m.TableChanged<H24dArchivedTag>(s => s.Set(x => x.Name, r => r.Name + "-filled")));
        }

        return runner;
    }

    private static SQLiteMigrationRunner PendingChain(SQLiteMigrationRunner runner, int upTo)
    {
        runner.Version(1, m => m.Sql("SELECT 1"));
        runner.Version(2, m => m.TableChanged<H24dPendingSeed>(s => s.Set(x => x.Tag, "seed")));
        if (upTo >= 3)
        {
            runner.Version(3, m => m.TableChanged<H24dPendingSeed>(s => s.Set(x => x.Name, r => r.Name + "-filled")));
        }

        return runner;
    }

    private static void SeedArchived(TestDatabase db)
    {
        db.Execute("CREATE TABLE \"H24dArchivedTags\" (\"Id\" INTEGER PRIMARY KEY, \"Name\" TEXT, \"Tag\" TEXT, \"Legacy\" TEXT)");
        db.Execute("INSERT INTO \"H24dArchivedTags\" (\"Id\", \"Name\", \"Tag\", \"Legacy\") VALUES (1, 'a', NULL, 'keepme'), (2, 'b', NULL, 'other')");
    }

    private static void SeedPending(TestDatabase db)
    {
        db.Execute("CREATE TABLE \"H24dPendingSeeds\" (\"Id\" INTEGER PRIMARY KEY, \"Name\" TEXT, \"Tag\" TEXT)");
        db.Execute("INSERT INTO \"H24dPendingSeeds\" (\"Id\", \"Name\", \"Tag\") VALUES (1, 'a', NULL), (2, 'b', NULL)");
    }

    private static List<string> Column(TestDatabase db, string table, string column)
    {
        return db.Query<string>($"SELECT \"{column}\" FROM \"{table}\" ORDER BY \"Id\"");
    }
}
