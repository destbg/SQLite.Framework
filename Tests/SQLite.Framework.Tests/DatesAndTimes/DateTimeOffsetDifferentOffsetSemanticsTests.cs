using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class DateTimeOffsetDifferentOffsetSemanticsTests
{
    internal sealed class DtoPairRow
    {
        [Key]
        public int Id { get; set; }

        public DateTimeOffset A { get; set; }

        public DateTimeOffset B { get; set; }
    }

    private static TestDatabase CreatePair(DateTimeOffset a, DateTimeOffset b)
    {
        TestDatabase db = new();
        db.Table<DtoPairRow>().Schema.CreateTable();
        db.Table<DtoPairRow>().Add(new DtoPairRow { Id = 1, A = a, B = b });
        return db;
    }

    [Fact]
    public void LessThanComparesStoredLocalTicks()
    {
        DateTimeOffset a = new(2024, 6, 30, 12, 0, 0, TimeSpan.FromHours(2));
        DateTimeOffset b = new(2024, 6, 30, 8, 0, 0, TimeSpan.FromHours(-3));
        using TestDatabase db = CreatePair(a, b);

        bool actual = db.Table<DtoPairRow>().Where(x => x.Id == 1).Select(x => x.A < x.B).First();

        Assert.False(actual);
    }

    [Fact]
    public void EqualityComparesStoredLocalTicks()
    {
        DateTimeOffset a = new(2024, 6, 30, 12, 0, 0, TimeSpan.FromHours(2));
        DateTimeOffset b = new(2024, 6, 30, 10, 0, 0, TimeSpan.Zero);
        using TestDatabase db = CreatePair(a, b);

        bool actual = db.Table<DtoPairRow>().Where(x => x.Id == 1).Select(x => x.A == x.B).First();

        Assert.False(actual);
    }

    [Fact]
    public void SubtractionUsesStoredLocalTicks()
    {
        DateTimeOffset a = new(2024, 6, 30, 12, 0, 0, TimeSpan.FromHours(2));
        DateTimeOffset b = new(2024, 6, 30, 8, 0, 0, TimeSpan.FromHours(-3));
        using TestDatabase db = CreatePair(a, b);

        TimeSpan actual = db.Table<DtoPairRow>().Where(x => x.Id == 1).Select(x => x.A - x.B).First();

        Assert.Equal(TimeSpan.FromHours(4), actual);
    }

    [Fact]
    public void OrderByUsesStoredLocalTicks()
    {
        using TestDatabase db = new();
        db.Table<DtoPairRow>().Schema.CreateTable();
        DateTimeOffset earlierInstant = new(2024, 6, 30, 12, 0, 0, TimeSpan.FromHours(2));
        DateTimeOffset laterInstant = new(2024, 6, 30, 8, 0, 0, TimeSpan.FromHours(-10));
        db.Table<DtoPairRow>().Add(new DtoPairRow { Id = 1, A = earlierInstant, B = earlierInstant });
        db.Table<DtoPairRow>().Add(new DtoPairRow { Id = 2, A = laterInstant, B = laterInstant });

        List<int> actual = db.Table<DtoPairRow>().OrderBy(x => x.A).Select(x => x.Id).ToList();

        Assert.Equal([2, 1], actual);
    }
}
