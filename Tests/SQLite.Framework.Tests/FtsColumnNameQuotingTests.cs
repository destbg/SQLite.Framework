using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[FullTextSearch(ContentMode = FtsContentMode.Internal)]
file sealed class HyphenColumnDoc
{
    [FullTextRowId]
    public int Id { get; set; }

    [FullTextIndexed]
    [Column("title-text")]
    public required string Title { get; set; }

    [FullTextIndexed]
    public required string Body { get; set; }
}

public class FtsColumnNameQuotingTests
{
    private static readonly (int Id, string Title, string Body)[] Seed =
    [
        (1, "native", "unrelated body text"),
        (2, "boring", "native appears only in the body"),
    ];

    [Fact]
    public void ColumnScopedMatchOnMappedNameWithHyphenMatchesOnlyThatColumn()
    {
        using TestDatabase db = new();
        db.Table<HyphenColumnDoc>().Schema.CreateTable();
        foreach ((int id, string title, string body) in Seed)
        {
            db.Table<HyphenColumnDoc>().Add(new HyphenColumnDoc { Id = id, Title = title, Body = body });
        }

        List<int> oracle = Seed
            .Where(d => d.Title.Split(' ').Contains("native"))
            .Select(d => d.Id)
            .OrderBy(x => x)
            .ToList();

        List<int> actual = db.Table<HyphenColumnDoc>()
            .Where(d => SQLiteFTS5Functions.Match(d.Title, "native"))
            .Select(d => d.Id)
            .OrderBy(x => x)
            .ToList();

        Assert.Equal([1], oracle);
        Assert.Equal(oracle, actual);
    }
}
