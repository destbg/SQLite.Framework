using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[FullTextSearch(ContentMode = FtsContentMode.Internal)]
public sealed class SecLNulMatchDoc
{
    [FullTextRowId]
    public int Id { get; set; }

    [FullTextIndexed]
    public string Body { get; set; } = "";
}

public class FtsMatchEmbeddedNulTests
{
    [Fact]
    public void StringMatchQueryWithEmbeddedNulIsRejected()
    {
        using TestDatabase db = new();
        db.Table<SecLNulMatchDoc>().Schema.CreateTable();
        db.Table<SecLNulMatchDoc>().Add(new SecLNulMatchDoc { Id = 1, Body = "alpha beta" });

        Assert.Throws<ArgumentException>(() => db.Table<SecLNulMatchDoc>()
            .Where(a => SQLiteFTS5Functions.Match(a, "alpha\0 OR beta"))
            .ToList());
    }

    [Fact]
    public void BuilderTermWithEmbeddedNulIsRejected()
    {
        using TestDatabase db = new();
        db.Table<SecLNulMatchDoc>().Schema.CreateTable();
        db.Table<SecLNulMatchDoc>().Add(new SecLNulMatchDoc { Id = 1, Body = "alpha beta" });

        Assert.Throws<ArgumentException>(() => db.Table<SecLNulMatchDoc>()
            .Where(a => SQLiteFTS5Functions.Match(a, f => f.Term("alp\0ha")))
            .ToList());
    }
}
