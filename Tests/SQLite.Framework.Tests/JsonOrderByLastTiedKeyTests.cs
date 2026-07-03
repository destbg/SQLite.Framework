using System.ComponentModel.DataAnnotations;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class JsonOrderByLastTiedKeyTests
{
    internal sealed class OrderByLastRow
    {
        [Key]
        public int Id { get; set; }

        public List<int> Nums { get; set; } = [];
    }

    private static TestDatabase NumbersDb()
    {
        return new TestDatabase(b => b.TypeConverters[typeof(List<int>)] =
            new SQLiteJsonConverter<List<int>>(TestJsonContext.Default.ListInt32));
    }

    [Fact]
    public void OrderByThenLastKeepsLastSeenAmongTiedKeys()
    {
        using TestDatabase db = NumbersDb();
        db.Table<OrderByLastRow>().Schema.CreateTable();
        db.Table<OrderByLastRow>().Add(new OrderByLastRow { Id = 1, Nums = [15, 25, 3] });

        int expected = new List<int> { 15, 25, 3 }.OrderBy(n => n % 10).Last();
        Assert.Equal(25, expected);

        int actual = db.Table<OrderByLastRow>()
            .Select(r => r.Nums.OrderBy(n => n % 10).Last())
            .First();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void OrderByDescendingThenLastKeepsLastSeenAmongTiedKeys()
    {
        using TestDatabase db = NumbersDb();
        db.Table<OrderByLastRow>().Schema.CreateTable();
        db.Table<OrderByLastRow>().Add(new OrderByLastRow { Id = 1, Nums = [3, 13, 25] });

        int expected = new List<int> { 3, 13, 25 }.OrderByDescending(n => n % 10).Last();
        Assert.Equal(13, expected);

        int actual = db.Table<OrderByLastRow>()
            .Select(r => r.Nums.OrderByDescending(n => n % 10).Last())
            .First();

        Assert.Equal(expected, actual);
    }
}
