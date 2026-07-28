using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Attributes;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H23iGrowRows")]
public class H23iGrowRow
{
    [Key]
    [AutoIncrement]
    public int Id { get; set; }

    public int Value { get; set; }
}

public class AsyncRangeWriteLiveSourceSnapshotTests
{
    [Fact]
    public async Task AddRangeAsyncFromQueryOverSameTableMatchesSnapshotSemantics()
    {
        using TestDatabase db = new();
        Seed(db);

        List<H23iGrowRow> snapshot = db.Table<H23iGrowRow>().Where(x => x.Value < 3).ToList();
        List<int> expected = snapshot.Select(x => x.Value)
            .Concat(snapshot.Select(x => x.Value + 1))
            .OrderBy(v => v)
            .ToList();

        await db.Table<H23iGrowRow>().AddRangeAsync(
            db.Table<H23iGrowRow>().Where(x => x.Value < 3).Select(x => new H23iGrowRow { Value = x.Value + 1 }),
            ct: TestContext.Current.CancellationToken);

        Assert.Equal(expected, StoredValues(db));
    }

    [Fact]
    public async Task AddRangeAsyncFromQueryOverSameTableStoresWhatAddRangeStores()
    {
        using TestDatabase syncDb = new();
        using TestDatabase asyncDb = new();
        Seed(syncDb);
        Seed(asyncDb);

        syncDb.Table<H23iGrowRow>().AddRange(
            syncDb.Table<H23iGrowRow>().Where(x => x.Value < 3).Select(x => new H23iGrowRow { Value = x.Value + 1 }));

        await asyncDb.Table<H23iGrowRow>().AddRangeAsync(
            asyncDb.Table<H23iGrowRow>().Where(x => x.Value < 3).Select(x => new H23iGrowRow { Value = x.Value + 1 }),
            ct: TestContext.Current.CancellationToken);

        Assert.Equal(StoredValues(syncDb), StoredValues(asyncDb));
    }

    [Fact]
    public async Task AddRangeAsyncWithoutItsOwnTransactionStoresWhatAddRangeStores()
    {
        using TestDatabase syncDb = new();
        using TestDatabase asyncDb = new();
        Seed(syncDb);
        Seed(asyncDb);

        syncDb.Table<H23iGrowRow>().AddRange(
            syncDb.Table<H23iGrowRow>().Where(x => x.Value < 3).Select(x => new H23iGrowRow { Value = x.Value + 1 }),
            runInTransaction: false);

        await asyncDb.Table<H23iGrowRow>().AddRangeAsync(
            asyncDb.Table<H23iGrowRow>().Where(x => x.Value < 3).Select(x => new H23iGrowRow { Value = x.Value + 1 }),
            runInTransaction: false,
            ct: TestContext.Current.CancellationToken);

        Assert.Equal(StoredValues(syncDb), StoredValues(asyncDb));
    }

    private static void Seed(TestDatabase db)
    {
        db.Table<H23iGrowRow>().Schema.CreateTable();
        db.Table<H23iGrowRow>().Add(new H23iGrowRow { Value = 1 });
    }

    private static List<int> StoredValues(TestDatabase db)
    {
        return db.Table<H23iGrowRow>().Select(x => x.Value).OrderBy(v => v).ToList();
    }
}
