using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H24lIndexerFoldRows")]
public class H24lIndexerFoldRow
{
    [Key]
    public int Id { get; set; }

    public int Num { get; set; }
}

public class H24lThrowingHolder
{
    public int Boom => throw new InvalidOperationException("boom");
}

public class CapturedIndexerFoldingExceptionParityTests
{
    [Fact]
    public void ReportsADuplicateDictionaryInitializerKeyTheSameWayAsLinqToObjects()
    {
        using TestDatabase db = Seed();
        List<H24lIndexerFoldRow> local = Rows();

        Assert.Throws<ArgumentException>(() => local
            .Where(r => r.Num == new Dictionary<int, int> { { 1, 1 }, { 1, 2 } }[1])
            .Select(r => r.Id)
            .ToList());

        Assert.Throws<ArgumentException>(() => db.Table<H24lIndexerFoldRow>()
            .Where(r => r.Num == new Dictionary<int, int> { { 1, 1 }, { 1, 2 } }[1])
            .Select(r => r.Id)
            .ToList());
    }

    [Fact]
    public void ReportsAThrowingCapturedPropertyGetterTheSameWayAsLinqToObjects()
    {
        H24lThrowingHolder holder = new();
        using TestDatabase db = Seed();
        List<H24lIndexerFoldRow> local = Rows();

        Assert.Throws<InvalidOperationException>(() => local
            .Where(r => r.Num == holder.Boom)
            .Select(r => r.Id)
            .ToList());

        Assert.Throws<InvalidOperationException>(() => db.Table<H24lIndexerFoldRow>()
            .Where(r => r.Num == holder.Boom)
            .Select(r => r.Id)
            .ToList());
    }

    [Fact]
    public void ReportsAMissingDictionaryKeyTheSameWayAsLinqToObjects()
    {
        Dictionary<string, int> limits = new() { ["low"] = 1 };
        using TestDatabase db = Seed();
        List<H24lIndexerFoldRow> local = Rows();

        Assert.Throws<KeyNotFoundException>(() => local
            .Where(r => r.Num == limits["high"])
            .Select(r => r.Id)
            .ToList());

        Assert.Throws<KeyNotFoundException>(() => db.Table<H24lIndexerFoldRow>()
            .Where(r => r.Num == limits["high"])
            .Select(r => r.Id)
            .ToList());
    }

    [Fact]
    public void ReportsAnOutOfRangeListIndexTheSameWayAsLinqToObjects()
    {
        List<int> limits = [1];
        using TestDatabase db = Seed();
        List<H24lIndexerFoldRow> local = Rows();

        Assert.Throws<ArgumentOutOfRangeException>(() => local
            .Where(r => r.Num == limits[3])
            .Select(r => r.Id)
            .ToList());

        Assert.Throws<ArgumentOutOfRangeException>(() => db.Table<H24lIndexerFoldRow>()
            .Where(r => r.Num == limits[3])
            .Select(r => r.Id)
            .ToList());
    }

    [Fact]
    public void ReportsAnInvalidConstructorArgumentTheSameWayAsLinqToObjects()
    {
        int day = InvalidDay();
        using TestDatabase db = Seed();
        List<H24lIndexerFoldRow> local = Rows();

        Assert.Throws<ArgumentOutOfRangeException>(() => local
            .Where(r => r.Num == new DateTime(2020, 2, day).Day)
            .Select(r => r.Id)
            .ToList());

        Assert.Throws<ArgumentOutOfRangeException>(() => db.Table<H24lIndexerFoldRow>()
            .Where(r => r.Num == new DateTime(2020, 2, day).Day)
            .Select(r => r.Id)
            .ToList());
    }

    private static int InvalidDay()
    {
        return 30;
    }

    private static List<H24lIndexerFoldRow> Rows()
    {
        return
        [
            new H24lIndexerFoldRow { Id = 1, Num = 1 },
            new H24lIndexerFoldRow { Id = 2, Num = 2 }
        ];
    }

    private static TestDatabase Seed()
    {
        TestDatabase db = new();
        db.Table<H24lIndexerFoldRow>().Schema.CreateTable();
        db.Table<H24lIndexerFoldRow>().AddRange(Rows());
        return db;
    }
}
