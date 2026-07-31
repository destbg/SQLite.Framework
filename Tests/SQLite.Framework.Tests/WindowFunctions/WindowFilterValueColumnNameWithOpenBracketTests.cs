using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H26rBracketOverRows")]
public class H26rBracketOverRow
{
    [Key]
    public int Id { get; set; }

    [Column("total OVER (limit)")]
    public int Amount { get; set; }
}

public class WindowFilterValueColumnNameWithOpenBracketTests
{
    [Fact]
    public void FilterLandsBeforeTheRealOverKeywordWhenTheValueColumnNameHoldsAnOpenBracket()
    {
        using TestDatabase db = Setup();

        List<H26rBracketOverRow> local = Rows();
        List<int> expected = local
            .OrderBy(r => r.Id)
            .Select(_ => local.Where(o => o.Id > 1).Sum(o => o.Amount))
            .ToList();

        Assert.Equal([50, 50, 50], expected);

        List<int> actual = db.Table<H26rBracketOverRow>()
            .OrderBy(r => r.Id)
            .Select(r => SQLiteWindowFunctions.Sum(r.Amount).Filter(r.Id > 1).AsValue())
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H26rBracketOverRow> Rows()
    {
        return
        [
            new H26rBracketOverRow { Id = 1, Amount = 10 },
            new H26rBracketOverRow { Id = 2, Amount = 20 },
            new H26rBracketOverRow { Id = 3, Amount = 30 }
        ];
    }

    private static TestDatabase Setup()
    {
        TestDatabase db = new();
        db.Table<H26rBracketOverRow>().Schema.CreateTable();
        db.Table<H26rBracketOverRow>().AddRange(Rows());
        return db;
    }
}
