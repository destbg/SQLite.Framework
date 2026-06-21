using System.Collections.Generic;
using System.Linq;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class DictionaryIndexerMissingKeyParityTests
{
    private static TestDatabase Seed()
    {
        TestDatabase db = new(b => b.TypeConverters[typeof(Dictionary<string, int>)] =
            new SQLiteJsonConverter<Dictionary<string, int>>(JdEdgeJsonContext.Default.DictionaryStringInt32));
        db.Table<JdEdgeMapRow>().Schema.CreateTable();
        db.Table<JdEdgeMapRow>().Add(new JdEdgeMapRow { Id = 1, Map = new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 } });
        db.Table<JdEdgeMapRow>().Add(new JdEdgeMapRow { Id = 2, Map = new Dictionary<string, int> { ["a"] = 5 } });
        db.Table<JdEdgeMapRow>().Add(new JdEdgeMapRow { Id = 3, Map = new Dictionary<string, int>() });
        return db;
    }

    [Fact]
    public void IndexerOnMissingKeyInProjection_ReturnsDefault()
    {
        using TestDatabase db = Seed();

        List<Dictionary<string, int>> src =
        [
            new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 },
            new Dictionary<string, int> { ["a"] = 5 },
            new Dictionary<string, int>()
        ];

        Assert.Throws<KeyNotFoundException>(() => src.Select(m => m["b"]).ToList());

        List<int> actual = db.Table<JdEdgeMapRow>().OrderBy(m => m.Id).Select(m => m.Map["b"]).ToList();

        Assert.Equal([2, 0, 0], actual);
    }
}
