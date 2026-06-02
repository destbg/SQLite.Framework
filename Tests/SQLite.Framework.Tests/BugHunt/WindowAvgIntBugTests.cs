using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests.BugHunt;

internal sealed class WindowAvgRow
{
    [Key]
    public int Id { get; set; }

    public int Amt { get; set; }
}

internal sealed class WindowAvgResult
{
    public int Id { get; set; }

    public int Avg { get; set; }
}

public class WindowAvgIntBugTests
{
    [Fact]
    public void WindowAvgOverIntColumn_MatchesLinqToObjects()
    {
        using TestDatabase db = new();
        db.Table<WindowAvgRow>().Schema.CreateTable();
        db.Table<WindowAvgRow>().Add(new WindowAvgRow { Id = 1, Amt = 2 });
        db.Table<WindowAvgRow>().Add(new WindowAvgRow { Id = 2, Amt = 3 });

        List<WindowAvgResult> rows = db.Table<WindowAvgRow>()
            .Select(r => new WindowAvgResult
            {
                Id = r.Id,
                Avg = SQLiteWindowFunctions.Avg(r.Amt).Over().AsValue(),
            })
            .OrderBy(r => r.Id)
            .ToList();

        double expected = new[] { 2, 3 }.Average();

        Assert.Equal(expected, (double)rows[0].Avg);
    }
}
