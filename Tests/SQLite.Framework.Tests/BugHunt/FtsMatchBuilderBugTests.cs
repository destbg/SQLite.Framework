using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests.BugHunt;

[FullTextSearch(ContentMode = FtsContentMode.Internal)]
public class FtsRtreeHunt_Plain_Search
{
    [FullTextRowId]
    public int Id { get; set; }

    [FullTextIndexed]
    public required string Body { get; set; }
}

public class FtsMatchBuilderBugTests
{
    [Fact]
    public void NearWithNoTerms_DoesNotEmitInvalidSql()
    {
        using TestDatabase db = new();
        db.Table<FtsRtreeHunt_Plain_Search>().Schema.CreateTable();
        db.Table<FtsRtreeHunt_Plain_Search>().Add(new FtsRtreeHunt_Plain_Search { Id = 1, Body = "hello world" });

        Exception? ex = Record.Exception(() => db.Table<FtsRtreeHunt_Plain_Search>()
            .Where(a => SQLiteFTS5Functions.Match(a, f => f.Near(2)))
            .ToList());

        Assert.False(ex is SQLite.Framework.Exceptions.SQLiteException,
            "Near(2) with no terms produced invalid FTS5 SQL and SQLite raised a raw parser error: " + ex?.Message);
    }
}
