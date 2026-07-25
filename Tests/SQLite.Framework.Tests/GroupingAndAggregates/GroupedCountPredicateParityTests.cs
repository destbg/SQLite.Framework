using System.Linq;
using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class GroupedCountRow
{
    [Key]
    public int Id { get; set; }

    public string Category { get; set; } = "";

    public int Value { get; set; }
}

public class GroupedCountPredicateParityTests
{
    [Fact]
    public void WhereThenCountWithPredicate_MatchesLinqToObjects()
    {
        using TestDatabase db = new();
        db.Table<GroupedCountRow>().Schema.CreateTable();
        db.Table<GroupedCountRow>().Add(new GroupedCountRow { Id = 1, Category = "A", Value = 50 });
        db.Table<GroupedCountRow>().Add(new GroupedCountRow { Id = 2, Category = "A", Value = 150 });
        db.Table<GroupedCountRow>().Add(new GroupedCountRow { Id = 3, Category = "A", Value = 200 });
        db.Table<GroupedCountRow>().Add(new GroupedCountRow { Id = 4, Category = "B", Value = -10 });
        db.Table<GroupedCountRow>().Add(new GroupedCountRow { Id = 5, Category = "B", Value = 300 });

        var expected = db.Table<GroupedCountRow>().AsEnumerable()
            .GroupBy(r => r.Category)
            .Select(g => new { g.Key, Count = g.Where(x => x.Value > 0).Count(x => x.Value > 100) })
            .OrderBy(x => x.Key)
            .ToList();

        var actual = db.Table<GroupedCountRow>()
            .GroupBy(r => r.Category)
            .Select(g => new { g.Key, Count = g.Where(x => x.Value > 0).Count(x => x.Value > 100) })
            .OrderBy(x => x.Key)
            .ToList();

        Assert.Equal(expected, actual);
    }
}
