using System.Text.Json.Serialization;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[JsonSerializable(typeof(List<int>))]
internal partial class JsonDistinctPagingCtx : JsonSerializerContext;

public class JsonDistinctPagingOrderParityTests
{
    public class JdpRow
    {
        [System.ComponentModel.DataAnnotations.Key]
        public int Id { get; set; }
        public List<int> Numbers { get; set; } = [];
    }

    private static TestDatabase Create(List<int> numbers)
    {
        TestDatabase db = new(b =>
            b.TypeConverters[typeof(List<int>)] = new SQLiteJsonConverter<List<int>>(JsonDistinctPagingCtx.Default.ListInt32));
        db.Table<JdpRow>().Schema.CreateTable();
        db.Table<JdpRow>().Add(new JdpRow { Id = 1, Numbers = numbers });
        return db;
    }

    [Fact]
    public void DistinctThenSkipThenFirst_MatchesLinqToObjects()
    {
        List<int> numbers = [5, 5, 7];
        using TestDatabase db = Create(numbers);

        int oracle = numbers.Distinct().Skip(1).First();
        int actual = db.Table<JdpRow>().Select(r => r.Numbers.Distinct().Skip(1).First()).First();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void DistinctThenSkipThenElementAt_MatchesLinqToObjects()
    {
        List<int> numbers = [5, 5, 7];
        using TestDatabase db = Create(numbers);

        int oracle = numbers.Distinct().Skip(1).ElementAt(0);
        int actual = db.Table<JdpRow>().Select(r => r.Numbers.Distinct().Skip(1).ElementAt(0)).First();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void DistinctThenTakeThenGroupByCount_MatchesLinqToObjects()
    {
        List<int> numbers = [5, 5, 7];
        using TestDatabase db = Create(numbers);

        int oracle = numbers.Distinct().Take(2).GroupBy(x => x).Count();
        int actual = db.Table<JdpRow>().Select(r => r.Numbers.Distinct().Take(2).GroupBy(x => x).Count()).First();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void DistinctThenElementAt_MatchesLinqToObjects()
    {
        List<int> numbers = [5, 5, 7];
        using TestDatabase db = Create(numbers);

        int oracle = numbers.Distinct().ElementAt(1);
        int actual = db.Table<JdpRow>().Select(r => r.Numbers.Distinct().ElementAt(1)).First();

        Assert.Equal(oracle, actual);
    }
}
