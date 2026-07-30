using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H25fBadges")]
public class H25fBadge
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public string? Tag { get; set; }
}

public class RepeatedFillAfterLegacyColumnFillTests
{
    [Fact]
    public void TheLastFillDeclaredForAColumnWinsOverAnEarlierLegacyColumnFill()
    {
        using TestDatabase db = new(useFile: true);
        Seed(db);

        db.Schema.Migrations()
            .Version(1, m => m.TableChanged<H25fBadge>(
                s => s
                    .Set(x => x.Tag, r => SQLiteColumn.Of<string?>(r, "Legacy"))
                    .Set(x => x.Tag, "override"),
                rebuild: true))
            .Migrate();

        Assert.Equal(["override", "override"], Tags(db));
    }

    private static void Seed(TestDatabase db)
    {
        db.Execute("CREATE TABLE \"H25fBadges\" (\"Id\" INTEGER PRIMARY KEY, \"Name\" TEXT, \"Tag\" TEXT, \"Legacy\" TEXT)");
        db.Execute("INSERT INTO \"H25fBadges\" (\"Id\", \"Name\", \"Tag\", \"Legacy\") VALUES (1, 'a', NULL, 'keepme'), (2, 'b', NULL, 'other')");
    }

    private static List<string> Tags(TestDatabase db)
    {
        return db.Query<string>("SELECT \"Tag\" FROM \"H25fBadges\" ORDER BY \"Id\"");
    }
}
