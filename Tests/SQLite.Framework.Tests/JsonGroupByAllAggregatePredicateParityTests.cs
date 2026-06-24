using System.Collections.Generic;
using System.Linq;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class JsonGroupByAllAggregatePredicateParityTests
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
    public void GroupByAllWithCountPredicate_MatchesLinqToObjects()
    {
        using TestDatabase db = CreateDb();

        bool oracle = A.GroupBy(x => x).All(g => g.Count() > 1);
        bool actual = db.Table<JgpRow>().Select(r => r.Numbers.GroupBy(x => x).All(g => g.Count() > 1)).First();

        Assert.Equal(oracle, actual);
    }
}
