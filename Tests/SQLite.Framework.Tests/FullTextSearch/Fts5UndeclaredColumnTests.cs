using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[FullTextSearch(ContentMode = FtsContentMode.Internal)]
public class FtsEntityWithUnindexedColumn
{
    [FullTextRowId]
    public int Id { get; set; }

    [FullTextIndexed]
    public required string Title { get; set; }

    public string? Note { get; set; }
}

public class Fts5UndeclaredColumnTests
{
    [Fact]
    public void MatchOnUndeclaredColumnThrows()
    {
        using TestDatabase db = new();
        db.Table<FtsEntityWithUnindexedColumn>().Schema.CreateTable();

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() =>
            db.Table<FtsEntityWithUnindexedColumn>()
                .Where(a => SQLiteFTS5Functions.Match(a.Note!, "hello"))
                .ToList());

        Assert.Contains("is not declared", ex.Message);
    }
}
