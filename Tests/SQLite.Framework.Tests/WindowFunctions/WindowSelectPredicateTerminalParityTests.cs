using System.ComponentModel.DataAnnotations;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class WindowSelectPredicateTerminalParityTests
{
    public class WspRow
    {
        [Key]
        public int Id { get; set; }
        public int Value { get; set; }
    }

    private static TestDatabase Create()
    {
        TestDatabase db = new();
        db.Table<WspRow>().Schema.CreateTable();
        for (int i = 1; i <= 6; i++)
        {
            db.Table<WspRow>().Add(new WspRow { Id = i, Value = i * 10 });
        }
        return db;
    }

    [Fact]
    public void FirstWithPredicateAfterWindowSelect_MatchesLinqToObjects()
    {
        using TestDatabase db = Create();
        List<WspRow> rows = db.Table<WspRow>().AsEnumerable().ToList();

        long oracle = rows.OrderBy(r => r.Id).Select((r, i) => new { r.Id, Rn = (long)i + 1 }).First(x => x.Id == 4).Rn;
        long actual = db.Table<WspRow>()
            .Select(r => new { r.Id, Rn = SQLiteWindowFunctions.RowNumber().OrderBy(r.Id).AsValue() })
            .First(x => x.Id == 4).Rn;

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void FirstOrDefaultReadingWindowColumnAfterWindowSelect_MatchesLinqToObjects()
    {
        using TestDatabase db = Create();
        List<WspRow> rows = db.Table<WspRow>().AsEnumerable().ToList();

        int? oracle = rows.OrderBy(r => r.Id).Select((r, i) => new { r.Id, Rn = (long)i + 1 }).FirstOrDefault(x => x.Rn == 2)?.Id;
        int? actual = db.Table<WspRow>()
            .Select(r => new { r.Id, Rn = SQLiteWindowFunctions.RowNumber().OrderBy(r.Id).AsValue() })
            .FirstOrDefault(x => x.Rn == 2)?.Id;

        Assert.Equal(oracle, actual);
    }
}
