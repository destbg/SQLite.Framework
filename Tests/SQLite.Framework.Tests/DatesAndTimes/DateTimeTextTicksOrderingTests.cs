using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class DateTimeTextTicksOrderingTests
{
    internal sealed class TtRow
    {
        [Key]
        public int Id { get; set; }

        public DateTime When { get; set; }
    }

    [Fact]
    public void OrderByUnderTextTicksSortsByStoredTickString()
    {
        using TestDatabase db = new(b => b.DateTimeStorage = DateTimeStorageMode.TextTicks);
        db.Table<TtRow>().Schema.CreateTable();

        List<TtRow> rows =
        [
            new() { Id = 1, When = new DateTime(300, 6, 15) },
            new() { Id = 2, When = new DateTime(400, 6, 15) },
        ];
        db.Table<TtRow>().AddRange(rows);

        List<int> inMemory = rows.OrderBy(x => x.When).Select(x => x.Id).ToList();
        Assert.Equal([1, 2], inMemory);

        List<int> actual = db.Table<TtRow>().OrderBy(x => x.When).Select(x => x.Id).ToList();

        Assert.Equal([2, 1], actual);
    }

    [Fact]
    public void ComparisonUnderTextTicksComparesStoredTickString()
    {
        using TestDatabase db = new(b => b.DateTimeStorage = DateTimeStorageMode.TextTicks);
        db.Table<TtRow>().Schema.CreateTable();

        List<TtRow> rows =
        [
            new() { Id = 1, When = new DateTime(300, 6, 15) },
            new() { Id = 2, When = new DateTime(400, 6, 15) },
        ];
        db.Table<TtRow>().AddRange(rows);

        DateTime cutoff = new(350, 1, 1);
        List<int> inMemory = rows.Where(x => x.When < cutoff).Select(x => x.Id).OrderBy(i => i).ToList();
        Assert.Equal([1], inMemory);

        List<int> actual = db.Table<TtRow>().Where(x => x.When < cutoff).Select(x => x.Id).OrderBy(i => i).ToList();

        Assert.Equal([], actual);
    }
}
