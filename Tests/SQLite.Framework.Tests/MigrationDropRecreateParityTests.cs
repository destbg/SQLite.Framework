using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("ArchivedNote")]
public class ArchivedNoteRow
{
    [Key]
    public int Id { get; set; }

    public string Note { get; set; } = "";
}

public class MigrationDropRecreateParityTests
{
    [Fact]
    public void SingleRunMatchesIncrementalRunsForDropThenCreate()
    {
        using TestDatabase incremental = new(useFile: true);
        incremental.Execute("CREATE TABLE \"ArchivedNote\" (\"Id\" INTEGER PRIMARY KEY)");
        incremental.Execute("INSERT INTO \"ArchivedNote\" (\"Id\") VALUES (7)");
        incremental.Schema.Migrations()
            .Version(1, m => m.DropTable("ArchivedNote"))
            .Migrate();
        incremental.Schema.Migrations()
            .Version(1, m => m.DropTable("ArchivedNote"))
            .Version(2, m => m.CreateTable<ArchivedNoteRow>())
            .Migrate();

        using TestDatabase single = new(useFile: true);
        single.Execute("CREATE TABLE \"ArchivedNote\" (\"Id\" INTEGER PRIMARY KEY)");
        single.Execute("INSERT INTO \"ArchivedNote\" (\"Id\") VALUES (7)");
        single.Schema.Migrations()
            .Version(1, m => m.DropTable("ArchivedNote"))
            .Version(2, m => m.CreateTable<ArchivedNoteRow>())
            .Migrate();

        List<string> expectedColumns = incremental.Pragmas.TableInfo("ArchivedNote").Select(c => c.Name).ToList();
        List<string> actualColumns = single.Pragmas.TableInfo("ArchivedNote").Select(c => c.Name).ToList();
        Assert.Equal(expectedColumns, actualColumns);
        Assert.Equal(incremental.Table<ArchivedNoteRow>().Count(), single.Table<ArchivedNoteRow>().Count());
    }
}
