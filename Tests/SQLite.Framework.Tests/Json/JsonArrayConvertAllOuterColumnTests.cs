using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H22fConvertAllRows")]
public class H22fConvertAllRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int Score { get; set; }

    public string[] Tags { get; set; } = [];
}

public class JsonArrayConvertAllOuterColumnTests
{
    [Fact]
    public void ConvertAllToATextColumnOfTheSameRowStaysPerRow()
    {
        using TestDatabase db = Setup(nameof(ConvertAllToATextColumnOfTheSameRowStaysPerRow));

        List<List<string>> expected = Rows().OrderBy(r => r.Id)
            .Select(r => Array.ConvertAll(r.Tags, t => r.Name))
            .Select(a => a.ToList())
            .ToList();
        List<List<string>> actual = db.Table<H22fConvertAllRow>().OrderBy(r => r.Id)
            .Select(r => Array.ConvertAll(r.Tags, t => r.Name))
            .ToList()
            .Select(a => a.ToList())
            .ToList();

        Assert.Equal(2, expected.Count);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ConvertAllToANumericColumnOfTheSameRowStaysPerRow()
    {
        using TestDatabase db = Setup(nameof(ConvertAllToANumericColumnOfTheSameRowStaysPerRow));

        List<List<int>> expected = Rows().OrderBy(r => r.Id)
            .Select(r => Array.ConvertAll(r.Tags, t => r.Score))
            .Select(a => a.ToList())
            .ToList();
        List<List<int>> actual = db.Table<H22fConvertAllRow>().OrderBy(r => r.Id)
            .Select(r => Array.ConvertAll(r.Tags, t => r.Score))
            .ToList()
            .Select(a => a.ToList())
            .ToList();

        Assert.Equal(2, expected.Count);
        Assert.Equal(expected, actual);
    }

    private static List<H22fConvertAllRow> Rows()
    {
        return
        [
            new H22fConvertAllRow { Id = 1, Name = "alpha", Score = 7, Tags = ["a", "b"] },
            new H22fConvertAllRow { Id = 2, Name = "beta", Score = 8, Tags = ["c"] }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(b =>
        {
            b.TypeConverters[typeof(string[])] = new SQLiteJsonConverter<string[]>(TestJsonContext.Default.StringArray);
            b.TypeConverters[typeof(int[])] = new SQLiteJsonConverter<int[]>(TestJsonContext.Default.Int32Array);
        }, methodName);
        db.Table<H22fConvertAllRow>().Schema.CreateTable();
        db.Table<H22fConvertAllRow>().AddRange(Rows());
        return db;
    }
}
