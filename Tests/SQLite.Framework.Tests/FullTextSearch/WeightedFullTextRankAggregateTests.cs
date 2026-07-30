using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Attributes;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H25rWeightedDocs")]
[FullTextSearch]
public class H25rWeightedDoc
{
    [FullTextRowId]
    public int Id { get; set; }

    [FullTextIndexed(Weight = 10.0)]
    public string Title { get; set; } = "";

    [FullTextIndexed]
    public string Body { get; set; } = "";
}

public class WeightedFullTextRankAggregateTests
{
    [Fact]
    public void MinimumRankOverAWeightedIndexEqualsTheLowestRowScore()
    {
        using TestDatabase db = Setup();

        List<double> scores = db.Table<H25rWeightedDoc>()
            .Where(d => SQLiteFTS5Functions.Match(d, "apple"))
            .Select(d => SQLiteFTS5Functions.Rank(d))
            .ToList();

        double expected = scores.Min();
        double actual = db.Table<H25rWeightedDoc>()
            .Where(d => SQLiteFTS5Functions.Match(d, "apple"))
            .Min(d => SQLiteFTS5Functions.Rank(d));

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void MaximumRankOverAWeightedIndexEqualsTheHighestRowScore()
    {
        using TestDatabase db = Setup();

        List<double> scores = db.Table<H25rWeightedDoc>()
            .Where(d => SQLiteFTS5Functions.Match(d, "apple"))
            .Select(d => SQLiteFTS5Functions.Rank(d))
            .ToList();

        double expected = scores.Max();
        double actual = db.Table<H25rWeightedDoc>()
            .Where(d => SQLiteFTS5Functions.Match(d, "apple"))
            .Max(d => SQLiteFTS5Functions.Rank(d));

        Assert.Equal(expected, actual);
    }

    private static List<H25rWeightedDoc> Rows()
    {
        return
        [
            new H25rWeightedDoc { Id = 1, Title = "apple", Body = "pie" },
            new H25rWeightedDoc { Id = 2, Title = "banana", Body = "bread" },
            new H25rWeightedDoc { Id = 3, Title = "cherry", Body = "apple tart" }
        ];
    }

    private static TestDatabase Setup()
    {
        TestDatabase db = new();
        db.Table<H25rWeightedDoc>().Schema.CreateTable();
        db.Table<H25rWeightedDoc>().AddRange(Rows());
        return db;
    }
}
