using System.ComponentModel.DataAnnotations;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class JsonRangeNegativeArgumentParityTests
{
    public class JrRow
    {
        [Key]
        public int Id { get; set; }
        public List<int> Numbers { get; set; } = [];
    }

    private static TestDatabase Create()
    {
        TestDatabase db = new(b =>
            b.TypeConverters[typeof(List<int>)] = new SQLiteJsonConverter<List<int>>(TestJsonContext.Default.ListInt32));
        db.Table<JrRow>().Schema.CreateTable();
        db.Table<JrRow>().Add(new JrRow { Id = 1, Numbers = [10, 20, 30, 40] });
        return db;
    }

    [Fact]
    public void GetRangeNegativeCount_ReturnsWholeListInsteadOfThrowing()
    {
        using TestDatabase db = Create();

        Assert.Throws<ArgumentOutOfRangeException>(() => new List<int> { 10, 20, 30, 40 }.GetRange(0, -1));

        List<int> actual = db.Table<JrRow>().Select(r => r.Numbers.GetRange(0, -1)).First();
        Assert.Equal([10, 20, 30, 40], actual);
    }

    [Fact]
    public void GetRangeNegativeIndex_TreatsIndexAsZeroInsteadOfThrowing()
    {
        using TestDatabase db = Create();

        Assert.Throws<ArgumentOutOfRangeException>(() => new List<int> { 10, 20, 30, 40 }.GetRange(-1, 2));

        List<int> actual = db.Table<JrRow>().Select(r => r.Numbers.GetRange(-1, 2)).First();
        Assert.Equal([10, 20], actual);
    }
}
