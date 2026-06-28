using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class ElementAtOffsetOverflowParityTests
{
    public class Row
    {
        [System.ComponentModel.DataAnnotations.Key]
        public int Id { get; set; }
        public string Value { get; set; } = "";
    }

    private const int Skip = 1000000001;
    private const int Index = 1147483647;

    private static (TestDatabase db, Row[] seed) Create()
    {
        TestDatabase db = new();
        db.Table<Row>().Schema.CreateTable();
        Row[] seed = Enumerable.Range(1, 5).Select(i => new Row { Id = i, Value = "v" + i }).ToArray();
        foreach (Row r in seed)
        {
            db.Table<Row>().Add(r);
        }
        return (db, seed);
    }

    [Fact]
    public void ElementAtAfterSkip_OffsetOverflows_MatchesLinqToObjects()
    {
        (TestDatabase db, Row[] seed) = Create();
        using (db)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                seed.OrderBy(r => r.Id).Select(r => r.Id).Skip(Skip).ElementAt(Index));

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                db.Table<Row>().OrderBy(r => r.Id).Select(r => r.Id).Skip(Skip).ElementAt(Index));
        }
    }

    [Fact]
    public void ElementAtOrDefaultAfterSkip_OffsetOverflows_MatchesLinqToObjects()
    {
        (TestDatabase db, Row[] seed) = Create();
        using (db)
        {
            int oracle = seed.OrderBy(r => r.Id).Select(r => r.Id).Skip(Skip).ElementAtOrDefault(Index);
            int actual = db.Table<Row>().OrderBy(r => r.Id).Select(r => r.Id).Skip(Skip).ElementAtOrDefault(Index);

            Assert.Equal(oracle, actual);
        }
    }
}
