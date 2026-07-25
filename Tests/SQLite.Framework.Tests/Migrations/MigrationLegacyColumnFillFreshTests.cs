using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("SluggedBook")]
public class SluggedBookRow
{
    [Key]
    public int Id { get; set; }

    public string? Slug { get; set; }
}

public class MigrationLegacyColumnFillFreshTests
{
    [Fact]
    public void FreshInstallRunsAFillThatReadsALegacyColumn()
    {
        using TestDatabase db = new(useFile: true);

        db.Schema.Migrations()
            .Version(1, m => m.CreateTable<SluggedBookRow>().Insert(new SluggedBookRow { Id = 1 }))
            .Version(2, m => m.TableChanged<SluggedBookRow>(s => s.Set(x => x.Slug, x => SQLiteColumn.Of<string?>(x, "LegacySlug"))))
            .Migrate();

        Assert.Null(db.Table<SluggedBookRow>().Single().Slug);
    }
}
