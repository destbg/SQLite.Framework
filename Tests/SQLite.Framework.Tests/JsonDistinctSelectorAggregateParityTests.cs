using System.ComponentModel.DataAnnotations;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class JsonDistinctSelectorAggregateParityTests
{
    public class JdsRow
    {
        [Key]
        public int Id { get; set; }
        public List<int> Numbers { get; set; } = [];
    }

    private static TestDatabase Create(List<int> numbers)
    {
        TestDatabase db = new(b =>
            b.TypeConverters[typeof(List<int>)] = new SQLiteJsonConverter<List<int>>(TestJsonContext.Default.ListInt32));
        db.Table<JdsRow>().Schema.CreateTable();
        db.Table<JdsRow>().Add(new JdsRow { Id = 1, Numbers = numbers });
        return db;
    }

    [Fact]
    public void DistinctThenSumWithSelector_MatchesLinqToObjects()
    {
        List<int> numbers = [1, 2, 3];
        using TestDatabase db = Create(numbers);

        int oracle = numbers.Distinct().Sum(x => x % 2);
        int actual = db.Table<JdsRow>().Select(r => r.Numbers.Distinct().Sum(x => x % 2)).First();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void DistinctThenAverageWithSelector_MatchesLinqToObjects()
    {
        List<int> numbers = [1, 2, 3];
        using TestDatabase db = Create(numbers);

        double oracle = numbers.Distinct().Average(x => x % 2);
        double actual = db.Table<JdsRow>().Select(r => r.Numbers.Distinct().Average(x => x % 2)).First();

        Assert.Equal(oracle, actual, 6);
    }

    [Fact]
    public void DistinctThenMaxWithSelector_MatchesLinqToObjects()
    {
        List<int> numbers = [1, 2, 3, 4, 2, 4];
        using TestDatabase db = Create(numbers);

        int oracle = numbers.Distinct().Max(x => x % 3);
        int actual = db.Table<JdsRow>().Select(r => r.Numbers.Distinct().Max(x => x % 3)).First();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void DistinctThenSumWithSelectorOverDuplicates_MatchesLinqToObjects()
    {
        List<int> numbers = [1, 2, 3, 4, 2, 4];
        using TestDatabase db = Create(numbers);

        int oracle = numbers.Distinct().Sum(x => x % 3);
        int actual = db.Table<JdsRow>().Select(r => r.Numbers.Distinct().Sum(x => x % 3)).First();

        Assert.Equal(oracle, actual);
    }
}
