using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public enum H26bKind
{
    Small = 0,
    Large = 1
}

[Table("H26bKindSources")]
public class H26bKindSource
{
    [Key]
    public int Id { get; set; }

    public H26bKind Kind { get; set; }
}

[Table("H26bKindTargets")]
public class H26bKindTarget
{
    [Key]
    public int Id { get; set; }

    public H26bKind Kind { get; set; }
}

public class InsertFromQueryTextEnumStorageTests
{
    [Fact]
    public void InsertFromQueryWithPlainColumnsCopiesTextStoredEnumValues()
    {
        using TestDatabase db = new(b => b.UseEnumStorage(EnumStorageMode.Text), nameof(InsertFromQueryWithPlainColumnsCopiesTextStoredEnumValues));
        db.Table<H26bKindSource>().Schema.CreateTable();
        db.Table<H26bKindTarget>().Schema.CreateTable();
        db.Table<H26bKindSource>().AddRange(Sources());

        db.Table<H26bKindTarget>().InsertFromQuery(
            db.Table<H26bKindSource>().Select(s => new H26bKindTarget { Id = s.Id, Kind = s.Kind }));

        List<H26bKind> expected = Sources()
            .OrderBy(s => s.Id)
            .Select(s => s.Kind)
            .ToList();

        Assert.Equal([H26bKind.Large, H26bKind.Small], expected);

        List<H26bKind> actual = db.Table<H26bKindTarget>()
            .OrderBy(t => t.Id)
            .Select(t => t.Kind)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H26bKindSource> Sources()
    {
        return
        [
            new H26bKindSource { Id = 1, Kind = H26bKind.Large },
            new H26bKindSource { Id = 2, Kind = H26bKind.Small }
        ];
    }
}
