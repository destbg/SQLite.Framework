using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class GroupByKeyMemberNameCollisionTests
{
    internal sealed class Sale
    {
        [Key]
        public int Id { get; set; }
        public int Bucket { get; set; }
        public int Amount { get; set; }
    }

    private static List<Sale> Data() =>
    [
        new() { Id = 1, Bucket = 7, Amount = 50 },
        new() { Id = 2, Bucket = 9, Amount = 150 },
        new() { Id = 3, Bucket = 7, Amount = 120 },
    ];

    private static TestDatabase Seed()
    {
        TestDatabase db = new();
        db.Table<Sale>().Schema.CreateTable();
        db.Table<Sale>().AddRange(Data());
        return db;
    }

    [Fact]
    public void AggregateOverColumnWhoseNameMatchesKeyMember()
    {
        using TestDatabase db = Seed();

        List<(int Bucket, int Total)> expected = Data()
            .GroupBy(s => new { Bucket = s.Amount / 100 })
            .Select(g => new { g.Key.Bucket, Total = g.Sum(x => x.Bucket) })
            .OrderBy(r => r.Bucket)
            .Select(r => (r.Bucket, r.Total))
            .ToList();
        Assert.Equal([(0, 7), (1, 16)], expected);

        List<(int Bucket, int Total)> actual = db.Table<Sale>()
            .GroupBy(s => new { Bucket = s.Amount / 100 })
            .Select(g => new { g.Key.Bucket, Total = g.Sum(x => x.Bucket) })
            .OrderBy(r => r.Bucket)
            .ToList()
            .Select(r => (r.Bucket, r.Total))
            .ToList();

        Assert.Equal(expected, actual);
    }
}
