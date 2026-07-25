using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Attributes;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[FullTextSearch]
[Table("SwapSearch")]
public class SwapSearchRow
{
    [FullTextIndexed]
    [Column("Y")]
    public string X { get; set; } = "";

    [FullTextIndexed]
    [Column("Z")]
    public string Y { get; set; } = "";
}

public class FtsRenamedColumnResolutionTests
{
    [Fact]
    public void ColumnMatchScopesThePropertyNotAnotherPropertyRename()
    {
        using TestDatabase db = new();
        db.Table<SwapSearchRow>().Schema.CreateTable();
        db.Table<SwapSearchRow>().Add(new SwapSearchRow { X = "hello", Y = "world" });

        int matches = db.Table<SwapSearchRow>().Count(a => SQLiteFTS5Functions.Match(a.Y, "world"));

        Assert.Equal(1, matches);
    }
}
