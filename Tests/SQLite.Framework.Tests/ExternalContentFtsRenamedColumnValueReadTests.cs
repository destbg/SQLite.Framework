using SQLite.Framework.Exceptions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class ExternalContentFtsRenamedColumnValueReadTests
{
    [Fact]
    public void MatchedRowColumnValueIsNotReadableWhenContentColumnRenamed()
    {
        using TestDatabase db = new();
        db.Table<FtsRenamedSource>().Schema.CreateTable();
        db.Table<FtsRenamedSourceSearch>().Schema.CreateTable();

        db.Table<FtsRenamedSource>().Add(new FtsRenamedSource { Body = "hello world" });

        Assert.Throws<SQLiteException>(() => db.Table<FtsRenamedSourceSearch>()
            .Where(s => SQLiteFTS5Functions.Match(s, "hello"))
            .Select(s => s.Body)
            .ToList());
    }
}
