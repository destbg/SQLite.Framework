using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("SecK_FillFromGenerated")]
public class SecKFillFromGeneratedRow
{
    [Key]
    public int Id { get; set; }

    public int Base { get; set; }

    public int? Copied { get; set; }
}

public class MigrationFillReadingGeneratedColumnTests
{
    [Fact]
    public void FillReadingALiveGeneratedColumnAppliesInPlace()
    {
        List<(int Id, int Base)> source = [(1, 21), (2, 4)];
        List<int?> expected = source
            .OrderBy(r => r.Id)
            .Select(r => (int?)(r.Base * 2))
            .ToList();

        using TestDatabase db = new(useFile: true);
        Seed(db);

        db.Schema.Migrations()
            .Version(1, m => m.TableChanged<SecKFillFromGeneratedRow>(
                s => s.Set(x => x.Copied, x => SQLiteColumn.Of<int?>(x, "Doubled"))))
            .Migrate();

        List<int?> values = db.Table<SecKFillFromGeneratedRow>()
            .OrderBy(x => x.Id)
            .Select(x => x.Copied)
            .ToList();
        Assert.Equal(expected, values);
    }

    [Fact]
    public void FillReadingALiveGeneratedColumnAppliesOnRebuild()
    {
        List<(int Id, int Base)> source = [(1, 21), (2, 4)];
        List<int?> expected = source
            .OrderBy(r => r.Id)
            .Select(r => (int?)(r.Base * 2))
            .ToList();

        using TestDatabase db = new(useFile: true);
        Seed(db);

        db.Schema.Migrations()
            .Version(1, m => m.TableChanged<SecKFillFromGeneratedRow>(
                s => s.Set(x => x.Copied, x => SQLiteColumn.Of<int?>(x, "Doubled")), rebuild: true))
            .Migrate();

        List<int?> values = db.Table<SecKFillFromGeneratedRow>()
            .OrderBy(x => x.Id)
            .Select(x => x.Copied)
            .ToList();
        Assert.Equal(expected, values);
    }

    private static void Seed(TestDatabase db)
    {
        db.Execute("CREATE TABLE \"SecK_FillFromGenerated\" (\"Id\" INTEGER PRIMARY KEY, \"Base\" INTEGER NOT NULL, \"Doubled\" INTEGER GENERATED ALWAYS AS (\"Base\" * 2) VIRTUAL)");
        db.Execute("INSERT INTO \"SecK_FillFromGenerated\" (\"Id\", \"Base\") VALUES (1, 21), (2, 4)");
    }
}
