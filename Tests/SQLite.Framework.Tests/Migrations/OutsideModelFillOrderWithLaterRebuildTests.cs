using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H23dLegacyTags")]
public class H23dLegacyTag
{
    [Key]
    public int Id { get; set; }

    [SQLite.Framework.Attributes.Indexed(IsUnique = true)]
    public string Name { get; set; } = "";

    public string? Tag { get; set; }
}

public class OutsideModelFillOrderWithLaterRebuildTests
{
    [Fact]
    public void ALaterUpdateSurvivesAFillThatReadsAColumnOutsideTheModel()
    {
        using TestDatabase stepwise = new(useFile: true);
        Seed(stepwise);
        Chain(stepwise.Schema.Migrations(), 2).Migrate();
        Chain(stepwise.Schema.Migrations(), 3).Migrate();
        Chain(stepwise.Schema.Migrations(), 4).Migrate();

        using TestDatabase collapsed = new(useFile: true);
        Seed(collapsed);
        Chain(collapsed.Schema.Migrations(), 4).Migrate();

        List<(string Name, string? Tag)> stepwiseRows = Rows(stepwise);
        List<(string Name, string? Tag)> collapsedRows = Rows(collapsed);

        Assert.Equal([("a-filled", "t3"), ("b-filled", "t3")], stepwiseRows);
        Assert.Equal(stepwiseRows, collapsedRows);
    }

    private static SQLiteMigrationRunner Chain(SQLiteMigrationRunner runner, int upTo)
    {
        runner.Version(1, m => m.Sql("SELECT 1"));
        runner.Version(2, m => m.TableChanged<H23dLegacyTag>(
            s => s.Set(x => x.Tag, r => SQLiteColumn.Of<string?>(r, "Legacy"))));
        if (upTo >= 3)
        {
            runner.Version(3, m => m.Update<H23dLegacyTag>(s => s.Set(x => x.Tag, "t3")));
        }

        if (upTo >= 4)
        {
            runner.Version(4, m => m.TableChanged<H23dLegacyTag>(s => s.Set(x => x.Name, r => r.Name + "-filled")));
        }

        return runner;
    }

    private static void Seed(TestDatabase db)
    {
        db.Execute("CREATE TABLE \"H23dLegacyTags\" (\"Id\" INTEGER PRIMARY KEY, \"Name\" TEXT, \"Tag\" TEXT, \"Legacy\" TEXT)");
        db.Execute("INSERT INTO \"H23dLegacyTags\" (\"Id\", \"Name\", \"Tag\", \"Legacy\") VALUES (1, 'a', NULL, 'keepme'), (2, 'b', NULL, 'other')");
    }

    private static List<(string Name, string? Tag)> Rows(TestDatabase db)
    {
        return db.Table<H23dLegacyTag>()
            .OrderBy(r => r.Id)
            .Select(r => new { r.Name, r.Tag })
            .ToList()
            .Select(r => (r.Name, r.Tag))
            .ToList();
    }
}
