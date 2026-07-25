using System.Linq;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class JsonGroupByTerminalPredicateParityTests
{
    private static readonly List<int> A = [5, 3, 5, 8, 3, 3];

    private static TestDatabase CreateDb()
    {
        TestDatabase db = new(b => b.TypeConverters[typeof(List<int>)] = new SQLiteJsonConverter<List<int>>(TestJsonContext.Default.ListInt32));
        db.Table<JgpRow>().Schema.CreateTable();
        db.Table<JgpRow>().Add(new JgpRow { Id = 1, Numbers = A });
        return db;
    }

    [Fact]
    public void GroupByCountWithPredicate_MatchesLinqToObjects()
    {
        using TestDatabase db = CreateDb();

        int oracle = A.GroupBy(x => x).Count(g => g.Count() > 1);
        int actual = db.Table<JgpRow>().Select(r => r.Numbers.GroupBy(x => x).Count(g => g.Count() > 1)).First();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void GroupByAnyWithPredicate_MatchesLinqToObjects()
    {
        using TestDatabase db = CreateDb();

        bool oracle = A.GroupBy(x => x).Any(g => g.Count() > 2);
        bool actual = db.Table<JgpRow>().Select(r => r.Numbers.GroupBy(x => x).Any(g => g.Count() > 2)).First();

        Assert.Equal(oracle, actual);
    }
}
