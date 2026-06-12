using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class ExternalContentFtsRenamedColumnValueReadTests
{
    [Fact]
    public void MatchedRowColumnValueIsReadable()
    {
        using TestDatabase db = new();
        db.Table<FtsRenamedSource>().Schema.CreateTable();
        db.Table<FtsRenamedSourceSearch>().Schema.CreateTable();

        db.Table<FtsRenamedSource>().Add(new FtsRenamedSource { Body = "hello world" });

        List<string> bodies = db.Table<FtsRenamedSourceSearch>()
            .Where(s => SQLiteFTS5Functions.Match(s, "hello"))
            .Select(s => s.Body)
            .ToList();

        Assert.Equal(["hello world"], bodies);
    }
}
