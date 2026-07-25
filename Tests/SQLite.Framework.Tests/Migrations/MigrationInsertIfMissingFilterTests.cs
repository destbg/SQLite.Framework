using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Attributes;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("FilteredSeedItem")]
public class FilteredSeedItemRow
{
    [Key]
    [AutoIncrement]
    public int Id { get; set; }

    public string? Name { get; set; }

    public bool IsDeleted { get; set; }
}

public class MigrationInsertIfMissingFilterTests
{
    [Fact]
    public void SkipsRowWhoseKeyExistsOnAFilteredEntity()
    {
        using TestDatabase db = new(o => o.AddQueryFilter<FilteredSeedItemRow>(r => !r.IsDeleted), useFile: true);

        db.Schema.Migrations()
            .Version(1, m => m
                .CreateTable<FilteredSeedItemRow>()
                .Insert(new FilteredSeedItemRow { Name = "fiction", IsDeleted = true }))
            .Migrate();

        db.Schema.Migrations()
            .Version(1, m => m
                .CreateTable<FilteredSeedItemRow>()
                .Insert(new FilteredSeedItemRow { Name = "fiction", IsDeleted = true }))
            .Version(2, m => m.InsertIfMissing(x => x.Name, new FilteredSeedItemRow { Name = "fiction" }))
            .Migrate();

        Assert.Equal(1, db.Table<FilteredSeedItemRow>().IgnoreQueryFilters().Count(r => r.Name == "fiction"));
    }
}
