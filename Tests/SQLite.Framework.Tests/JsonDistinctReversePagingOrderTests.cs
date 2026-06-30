using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[JsonSerializable(typeof(List<int>))]
internal partial class JsonReversePagingCtx : JsonSerializerContext;

public class JsonDistinctReversePagingOrderTests
{
    internal sealed class JrpRow
    {
        [Key]
        public int Id { get; set; }

        public List<int> Numbers { get; set; } = [];
    }

    private static readonly List<int> Numbers = [5, 3, 5, 8, 3, 3];

    private static TestDatabase Create()
    {
        TestDatabase db = new(b =>
            b.TypeConverters[typeof(List<int>)] = new SQLiteJsonConverter<List<int>>(JsonReversePagingCtx.Default.ListInt32));
        db.Table<JrpRow>().Schema.CreateTable();
        db.Table<JrpRow>().Add(new JrpRow { Id = 1, Numbers = Numbers });
        return db;
    }

    [Fact]
    public void DistinctReverseTake()
    {
        using TestDatabase db = Create();

        List<int> expected = Numbers.Distinct().Reverse().Take(2).ToList();
        List<int> actual = db.Table<JrpRow>().Select(r => r.Numbers.Distinct().Reverse().Take(2).ToList()).First();

        Assert.Equal([8, 3], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void DistinctReverseSkip()
    {
        using TestDatabase db = Create();

        List<int> expected = Numbers.Distinct().Reverse().Skip(1).ToList();
        List<int> actual = db.Table<JrpRow>().Select(r => r.Numbers.Distinct().Reverse().Skip(1).ToList()).First();

        Assert.Equal([3, 5], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void DistinctReverseElementAt()
    {
        using TestDatabase db = Create();

        int expected = Numbers.Distinct().Reverse().ElementAt(0);
        int actual = db.Table<JrpRow>().Select(r => r.Numbers.Distinct().Reverse().ElementAt(0)).First();

        Assert.Equal(8, expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ReverseDistinctTake()
    {
        using TestDatabase db = Create();

        List<int> expected = Numbers.AsEnumerable().Reverse().Distinct().Take(2).ToList();
        List<int> actual = db.Table<JrpRow>().Select(r => r.Numbers.AsEnumerable().Reverse().Distinct().Take(2).ToList()).First();

        Assert.Equal([3, 8], expected);
        Assert.Equal(expected, actual);
    }
}
