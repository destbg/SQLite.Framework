using System.ComponentModel.DataAnnotations;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class JsonTakeSkipBeforeGroupByParityTests
{
    public class JtgRow
    {
        [Key]
        public int Id { get; set; }
        public List<int> Numbers { get; set; } = [];
    }

    private static TestDatabase Create(List<int> numbers)
    {
        TestDatabase db = new(b =>
            b.TypeConverters[typeof(List<int>)] = new SQLiteJsonConverter<List<int>>(TestJsonContext.Default.ListInt32));
        db.Table<JtgRow>().Schema.CreateTable();
        db.Table<JtgRow>().Add(new JtgRow { Id = 1, Numbers = numbers });
        return db;
    }

    [Fact]
    public void TakeThenGroupByCount_MatchesLinqToObjects()
    {
        List<int> numbers = [5, 3, 5, 8, 3, 3];
        using TestDatabase db = Create(numbers);

        List<int> oracle = numbers.Take(3).GroupBy(x => x).Select(g => g.Count()).OrderBy(c => c).ToList();
        List<int> actual = db.Table<JtgRow>()
            .Select(r => r.Numbers.Take(3).GroupBy(x => x).Select(g => g.Count()).ToList())
            .First()
            .OrderBy(c => c)
            .ToList();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void SkipThenGroupByCount_MatchesLinqToObjects()
    {
        List<int> numbers = [5, 3, 5, 8, 3, 3];
        using TestDatabase db = Create(numbers);

        List<int> oracle = numbers.Skip(2).GroupBy(x => x).Select(g => g.Count()).OrderBy(c => c).ToList();
        List<int> actual = db.Table<JtgRow>()
            .Select(r => r.Numbers.Skip(2).GroupBy(x => x).Select(g => g.Count()).ToList())
            .First()
            .OrderBy(c => c)
            .ToList();

        Assert.Equal(oracle, actual);
    }
}
