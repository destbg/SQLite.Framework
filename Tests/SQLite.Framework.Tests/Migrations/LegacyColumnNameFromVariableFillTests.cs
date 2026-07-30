using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H25fCarriedTags")]
public class H25fCarriedTag
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public string? Tag { get; set; }
}

public class LegacyColumnNameFromVariableFillTests
{
    [Fact]
    public void AFillReadingALegacyColumnNamedByAVariableCarriesTheOldValues()
    {
        string legacyColumn = "Legacy";
        using TestDatabase db = new(useFile: true);
        Seed(db);

        db.Schema.Migrations()
            .Version(1, m => m.TableChanged<H25fCarriedTag>(
                s => s.Set(x => x.Tag, r => SQLiteColumn.Of<string?>(r, legacyColumn)),
                rebuild: true))
            .Migrate();

        Assert.Equal(["keepme", "other"], Tags(db));
    }

    private static void Seed(TestDatabase db)
    {
        db.Execute("CREATE TABLE \"H25fCarriedTags\" (\"Id\" INTEGER PRIMARY KEY, \"Name\" TEXT, \"Tag\" TEXT, \"Legacy\" TEXT)");
        db.Execute("INSERT INTO \"H25fCarriedTags\" (\"Id\", \"Name\", \"Tag\", \"Legacy\") VALUES (1, 'a', NULL, 'keepme'), (2, 'b', NULL, 'other')");
    }

    private static List<string> Tags(TestDatabase db)
    {
        return db.Query<string>("SELECT \"Tag\" FROM \"H25fCarriedTags\" ORDER BY \"Id\"");
    }
}
