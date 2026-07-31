using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Attributes;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H26kFlagDocs")]
[FullTextSearch]
public class H26kFlagDoc
{
    [FullTextRowId]
    public int Id { get; set; }

    [FullTextIndexed]
    public string Body { get; set; } = "";
}

public class FullTextMatchOutsideAFilterTests
{
    [Fact]
    public void MatchProjectedIntoTheResultTellsWhichRowsMatch()
    {
        using TestDatabase db = Setup(nameof(MatchProjectedIntoTheResultTellsWhichRowsMatch));

        List<bool> expected = Rows()
            .OrderBy(d => d.Id)
            .Select(d => d.Body.Split(' ').Contains("apple"))
            .ToList();

        List<bool> actual = db.Table<H26kFlagDoc>()
            .OrderBy(d => d.Id)
            .Select(d => SQLiteFTS5Functions.Match(d, "apple"))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void OrderingByMatchPutsTheMatchingRowsFirst()
    {
        using TestDatabase db = Setup(nameof(OrderingByMatchPutsTheMatchingRowsFirst));

        List<int> expected = Rows()
            .OrderByDescending(d => d.Body.Split(' ').Contains("apple"))
            .ThenBy(d => d.Id)
            .Select(d => d.Id)
            .ToList();

        List<int> actual = db.Table<H26kFlagDoc>()
            .OrderByDescending(d => SQLiteFTS5Functions.Match(d, "apple"))
            .ThenBy(d => d.Id)
            .Select(d => d.Id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void AllOverMatchReportsWhetherEveryRowMatches()
    {
        using TestDatabase db = Setup(nameof(AllOverMatchReportsWhetherEveryRowMatches));

        bool expectedApple = Rows().All(d => d.Body.Split(' ').Contains("apple"));
        bool expectedFruit = Rows().All(d => d.Body.Split(' ').Contains("fruit"));

        bool actualApple = db.Table<H26kFlagDoc>().All(d => SQLiteFTS5Functions.Match(d, "apple"));
        bool actualFruit = db.Table<H26kFlagDoc>().All(d => SQLiteFTS5Functions.Match(d, "fruit"));

        Assert.Equal(expectedApple, actualApple);
        Assert.Equal(expectedFruit, actualFruit);
    }

    private static List<H26kFlagDoc> Rows()
    {
        return
        [
            new H26kFlagDoc { Id = 1, Body = "apple fruit" },
            new H26kFlagDoc { Id = 2, Body = "banana fruit" },
            new H26kFlagDoc { Id = 3, Body = "apple tart fruit" }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(null, methodName);
        db.Table<H26kFlagDoc>().Schema.CreateTable();
        db.Table<H26kFlagDoc>().AddRange(Rows());
        return db;
    }
}
