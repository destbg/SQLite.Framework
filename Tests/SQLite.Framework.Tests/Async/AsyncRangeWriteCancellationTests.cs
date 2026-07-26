using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Attributes;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H22dRangeCancelRows")]
public class H22dRangeCancelRow
{
    [Key]
    [AutoIncrement]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public class AsyncRangeWriteCancellationTests
{
    [Fact]
    public async Task AddRangeAsyncWritesNothingWhenTheTokenIsCancelledDuringTheRange()
    {
        using TestDatabase db = new();
        db.Table<H22dRangeCancelRow>().Schema.CreateTable();
        using CancellationTokenSource cts = new();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await db.Table<H22dRangeCancelRow>().AddRangeAsync(CancelAfterFirst(cts, Rows()), ct: cts.Token));

        Assert.Equal(0, db.Table<H22dRangeCancelRow>().Count());
    }

    [Fact]
    public async Task UpdateRangeAsyncKeepsTheStoredValuesWhenTheTokenIsCancelledDuringTheRange()
    {
        using TestDatabase db = new();
        db.Table<H22dRangeCancelRow>().Schema.CreateTable();
        List<H22dRangeCancelRow> stored = Rows();
        db.Table<H22dRangeCancelRow>().AddRange(stored);
        foreach (H22dRangeCancelRow row in stored)
        {
            row.Name += "-changed";
        }

        using CancellationTokenSource cts = new();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await db.Table<H22dRangeCancelRow>().UpdateRangeAsync(CancelAfterFirst(cts, stored), ct: cts.Token));

        List<string> names = db.Table<H22dRangeCancelRow>().OrderBy(r => r.Id).Select(r => r.Name).ToList();
        Assert.Equal(["a", "b", "c"], names);
    }

    [Fact]
    public async Task RemoveRangeAsyncKeepsTheRowsWhenTheTokenIsCancelledDuringTheRange()
    {
        using TestDatabase db = new();
        db.Table<H22dRangeCancelRow>().Schema.CreateTable();
        List<H22dRangeCancelRow> stored = Rows();
        db.Table<H22dRangeCancelRow>().AddRange(stored);

        using CancellationTokenSource cts = new();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await db.Table<H22dRangeCancelRow>().RemoveRangeAsync(CancelAfterFirst(cts, stored), ct: cts.Token));

        Assert.Equal(3, db.Table<H22dRangeCancelRow>().Count());
    }

    private static List<H22dRangeCancelRow> Rows()
    {
        return
        [
            new H22dRangeCancelRow { Name = "a" },
            new H22dRangeCancelRow { Name = "b" },
            new H22dRangeCancelRow { Name = "c" }
        ];
    }

    private static IEnumerable<H22dRangeCancelRow> CancelAfterFirst(CancellationTokenSource cts, List<H22dRangeCancelRow> rows)
    {
        for (int i = 0; i < rows.Count; i++)
        {
            yield return rows[i];

            if (i == 0)
            {
                cts.Cancel();
            }
        }
    }
}
