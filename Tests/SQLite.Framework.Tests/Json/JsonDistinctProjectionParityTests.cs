using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class JsonDistinctProjectionParityTests
{
    private static TestDatabase Create(List<int> numbers)
    {
        TestDatabase db = new(b =>
            b.TypeConverters[typeof(List<int>)] = new SQLiteJsonConverter<List<int>>(TestJsonContext.Default.ListInt32));
        db.Table<JdpRow>().Schema.CreateTable();
        db.Table<JdpRow>().Add(new JdpRow { Id = 1, Numbers = numbers });
        return db;
    }

    [Fact]
    public void DistinctThenSelectCollapsing_MatchesLinqToObjects()
    {
        List<int> numbers = [1, 2, 3, 4];
        using TestDatabase db = Create(numbers);

        List<int> oracle = numbers.Distinct().Select(x => x % 2).ToList();
        List<int> actual = db.Table<JdpRow>()
            .Select(r => r.Numbers.Distinct().Select(x => x % 2).ToList())
            .First();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void WhereThenDistinctThenSelectCollapsing_MatchesLinqToObjects()
    {
        List<int> numbers = [1, 2, 3, 4, 5];
        using TestDatabase db = Create(numbers);

        List<int> oracle = numbers.Where(x => x > 1).Distinct().Select(x => x % 2).ToList();
        List<int> actual = db.Table<JdpRow>()
            .Select(r => r.Numbers.Where(x => x > 1).Distinct().Select(x => x % 2).ToList())
            .First();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void DistinctThenGroupBySum_MatchesLinqToObjects()
    {
        List<int> numbers = [1, 2, 3, 4, 2, 4];
        using TestDatabase db = Create(numbers);

        List<int> oracle = numbers.Distinct().GroupBy(x => x % 2).Select(g => g.Sum()).OrderBy(s => s).ToList();
        List<int> actual = db.Table<JdpRow>()
            .Select(r => r.Numbers.Distinct().GroupBy(x => x % 2).Select(g => g.Sum()).ToList())
            .First()
            .OrderBy(s => s)
            .ToList();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void DistinctThenGroupByCount_MatchesLinqToObjects()
    {
        List<int> numbers = [1, 1, 2, 2, 3];
        using TestDatabase db = Create(numbers);

        List<int> oracle = numbers.Distinct().GroupBy(x => x % 2).Select(g => g.Count()).OrderBy(c => c).ToList();
        List<int> actual = db.Table<JdpRow>()
            .Select(r => r.Numbers.Distinct().GroupBy(x => x % 2).Select(g => g.Count()).ToList())
            .First()
            .OrderBy(c => c)
            .ToList();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void ReversedInputDistinctThenSelect_PreservesFirstAppearanceOrder()
    {
        List<int> numbers = [4, 3, 2, 1];
        using TestDatabase db = Create(numbers);

        List<int> oracle = numbers.Distinct().Select(x => x % 2).ToList();
        List<int> actual = db.Table<JdpRow>().Select(r => r.Numbers.Distinct().Select(x => x % 2).ToList()).First();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void DuplicatesDistinctThenSelect_PreservesFirstAppearanceOrder()
    {
        List<int> numbers = [5, 1, 5, 3, 1, 2];
        using TestDatabase db = Create(numbers);

        List<int> oracle = numbers.Distinct().Select(x => x + 100).ToList();
        List<int> actual = db.Table<JdpRow>().Select(r => r.Numbers.Distinct().Select(x => x + 100).ToList()).First();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void DistinctThenNonCollapsingSelect_PreservesFirstAppearanceOrder()
    {
        List<int> numbers = [3, 1, 2, 1, 3];
        using TestDatabase db = Create(numbers);

        List<int> oracle = numbers.Distinct().Select(x => x * 10).ToList();
        List<int> actual = db.Table<JdpRow>().Select(r => r.Numbers.Distinct().Select(x => x * 10).ToList()).First();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void WhereThenDistinctThenSelect_PreservesFirstAppearanceOrder()
    {
        List<int> numbers = [9, 2, 7, 2, 9, 4];
        using TestDatabase db = Create(numbers);

        List<int> oracle = numbers.Where(x => x > 2).Distinct().Select(x => x % 5).ToList();
        List<int> actual = db.Table<JdpRow>().Select(r => r.Numbers.Where(x => x > 2).Distinct().Select(x => x % 5).ToList()).First();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void DistinctThenSelectThenOrderBy_MatchesLinqToObjects()
    {
        List<int> numbers = [4, 3, 2, 1, 2];
        using TestDatabase db = Create(numbers);

        List<int> oracle = numbers.Distinct().Select(x => x % 3).OrderBy(x => x).ToList();
        List<int> actual = db.Table<JdpRow>().Select(r => r.Numbers.Distinct().Select(x => x % 3).OrderBy(x => x).ToList()).First();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void DistinctThenSelectThenSum_MatchesLinqToObjects()
    {
        List<int> numbers = [5, 5, 3, 1];
        using TestDatabase db = Create(numbers);

        int oracle = numbers.Distinct().Select(x => x * 2).Sum();
        int actual = db.Table<JdpRow>().Select(r => r.Numbers.Distinct().Select(x => x * 2).Sum()).First();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void PlainDistinct_PreservesFirstAppearanceOrder()
    {
        List<int> numbers = [4, 1, 4, 2, 1];
        using TestDatabase db = Create(numbers);

        List<int> oracle = numbers.Distinct().ToList();
        List<int> actual = db.Table<JdpRow>().Select(r => r.Numbers.Distinct().ToList()).First();

        Assert.Equal(oracle, actual);
    }

    [Fact]
    public void ReversedInputDistinctThenGroupBySum_MatchesLinqToObjects()
    {
        List<int> numbers = [40, 30, 20, 10, 20, 40];
        using TestDatabase db = Create(numbers);

        List<int> oracle = numbers.Distinct().GroupBy(x => x % 30).Select(g => g.Sum()).OrderBy(s => s).ToList();
        List<int> actual = db.Table<JdpRow>()
            .Select(r => r.Numbers.Distinct().GroupBy(x => x % 30).Select(g => g.Sum()).ToList())
            .First()
            .OrderBy(s => s)
            .ToList();

        Assert.Equal(oracle, actual);
    }
}

[Table("JdpRow")]
public class JdpRow
{
    [Key]
    public int Id { get; set; }

    public List<int> Numbers { get; set; } = [];
}
