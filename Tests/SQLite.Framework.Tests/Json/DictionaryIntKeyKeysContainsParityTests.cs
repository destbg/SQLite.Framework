using System.Collections.Generic;
using System.Linq;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class DictionaryIntKeyKeysContainsParityTests
{
    private static TestDatabase Seed()
    {
        TestDatabase db = new(b => b.TypeConverters[typeof(Dictionary<int, int>)] =
            new SQLiteJsonConverter<Dictionary<int, int>>(JdEdgeIntJsonContext.Default.DictionaryInt32Int32));
        db.Table<JdEdgeIntMapRow>().Schema.CreateTable();
        db.Table<JdEdgeIntMapRow>().Add(new JdEdgeIntMapRow { Id = 1, Map = new Dictionary<int, int> { [1] = 10, [2] = 20 } });
        return db;
    }

    [Fact]
    public void IntKeyedKeysContainsPresent_MatchesLinqToObjects()
    {
        using TestDatabase db = Seed();

        bool oracle = new Dictionary<int, int> { [1] = 10, [2] = 20 }.Keys.Contains(1);
        bool actual = db.Table<JdEdgeIntMapRow>().Select(d => d.Map.Keys.Contains(1)).First();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void IntKeyedKeysContainsAbsent_MatchesLinqToObjects()
    {
        using TestDatabase db = Seed();

        bool oracle = new Dictionary<int, int> { [1] = 10, [2] = 20 }.Keys.Contains(99);
        bool actual = db.Table<JdEdgeIntMapRow>().Select(d => d.Map.Keys.Contains(99)).First();

        Assert.Equal(oracle, actual);
    }
}
