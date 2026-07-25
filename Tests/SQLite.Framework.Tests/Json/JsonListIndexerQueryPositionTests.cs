using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("JsonIdxRow")]
public class JsonIdxRow
{
    [Key]
    public int Id { get; set; }

    public List<int> Numbers { get; set; } = [];
}

public class JsonListIndexerQueryPositionTests
{
    private static List<(int Id, List<int> Numbers)> Data() =>
    [
        (1, [5, 3]),
        (2, [1]),
        (3, [4, 2]),
    ];

    private static TestDatabase Setup()
    {
        TestDatabase db = new(b => b.TypeConverters[typeof(List<int>)] = new SQLiteJsonConverter<List<int>>(TestJsonContext.Default.ListInt32));
        db.Table<JsonIdxRow>().Schema.CreateTable();
        foreach ((int id, List<int> numbers) in Data())
        {
            db.Table<JsonIdxRow>().Add(new JsonIdxRow { Id = id, Numbers = numbers });
        }

        return db;
    }

    [Fact]
    public void IndexerInSelectMatchesLinq()
    {
        using TestDatabase db = Setup();

        List<int> expected = Data().OrderBy(d => d.Id).Select(d => d.Numbers[0]).ToList();

        List<int> actual = db.Table<JsonIdxRow>()
            .OrderBy(r => r.Id)
            .Select(r => r.Numbers[0])
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void IndexerInWherePredicateMatchesLinq()
    {
        using TestDatabase db = Setup();

        List<int> expected = Data().Where(d => d.Numbers[0] > 3).Select(d => d.Id).OrderBy(i => i).ToList();

        List<int> actual = db.Table<JsonIdxRow>()
            .Where(r => r.Numbers[0] > 3)
            .Select(r => r.Id)
            .OrderBy(i => i)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void IndexerInOrderByMatchesLinq()
    {
        using TestDatabase db = Setup();

        List<int> expected = Data().OrderBy(d => d.Numbers[0]).Select(d => d.Id).ToList();

        List<int> actual = db.Table<JsonIdxRow>()
            .OrderBy(r => r.Numbers[0])
            .Select(r => r.Id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void IndexerInWindowOrderByMatchesLinq()
    {
        using TestDatabase db = Setup();

        List<int> expected = Data().OrderBy(d => d.Numbers[0]).Select(d => d.Id).ToList();

        var actual = db.Table<JsonIdxRow>()
            .Select(r => new
            {
                r.Id,
                Rn = SQLiteWindowFunctions.RowNumber().Over().OrderBy(r.Numbers[0]).AsValue(),
            })
            .OrderBy(x => x.Rn)
            .ToList();

        Assert.Equal(expected, actual.Select(x => x.Id).ToList());
    }

    [Fact]
    public void ElementAtInWherePredicateMatchesLinq()
    {
        using TestDatabase db = Setup();

        List<int> expected = Data().Where(d => d.Numbers.ElementAt(0) > 3).Select(d => d.Id).OrderBy(i => i).ToList();

        List<int> actual = db.Table<JsonIdxRow>()
            .Where(r => r.Numbers.ElementAt(0) > 3)
            .Select(r => r.Id)
            .OrderBy(i => i)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static TestDatabase SetupArray()
    {
        TestDatabase db = new(b => b.TypeConverters[typeof(int[])] = new SQLiteJsonConverter<int[]>(TestJsonContext.Default.Int32Array));
        db.Table<JsonIdxArrayRow>().Schema.CreateTable();
        foreach ((int id, List<int> numbers) in Data())
        {
            db.Table<JsonIdxArrayRow>().Add(new JsonIdxArrayRow { Id = id, Numbers = [.. numbers] });
        }

        return db;
    }

    [Fact]
    public void ArrayIndexerInWherePredicateMatchesLinq()
    {
        using TestDatabase db = SetupArray();

        List<int> expected = Data().Where(d => d.Numbers[0] > 3).Select(d => d.Id).OrderBy(i => i).ToList();

        List<int> actual = db.Table<JsonIdxArrayRow>()
            .Where(r => r.Numbers[0] > 3)
            .Select(r => r.Id)
            .OrderBy(i => i)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ArrayIndexerInOrderByMatchesLinq()
    {
        using TestDatabase db = SetupArray();

        List<int> expected = Data().OrderBy(d => d.Numbers[0]).Select(d => d.Id).ToList();

        List<int> actual = db.Table<JsonIdxArrayRow>()
            .OrderBy(r => r.Numbers[0])
            .Select(r => r.Id)
            .ToList();

        Assert.Equal(expected, actual);
    }
}

[Table("JsonIdxArrayRow")]
public class JsonIdxArrayRow
{
    [Key]
    public int Id { get; set; }

    public int[] Numbers { get; set; } = [];
}
