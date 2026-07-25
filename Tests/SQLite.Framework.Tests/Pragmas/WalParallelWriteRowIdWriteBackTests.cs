using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Attributes;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class WalParallelDocRow
{
    [Key]
    [AutoIncrement]
    public int Id { get; set; }

    public string Text { get; set; } = "";
}

public class WalParallelWriteRowIdWriteBackTests
{
    [Fact]
    public void ParallelAddsWriteBackTheirOwnRowIds()
    {
        using TestDatabase db = new(b => b.UseWalMode());
        db.Table<WalParallelDocRow>().Schema.CreateTable();

        const int writers = 8;
        const int perWriter = 250;
        ConcurrentBag<(string Text, int AssignedId)> assigned = [];

        using (Barrier barrier = new(writers))
        {
            Task[] tasks = Enumerable.Range(0, writers).Select(w => Task.Run(async () =>
            {
                barrier.SignalAndWait();
                for (int i = 0; i < perWriter; i++)
                {
                    WalParallelDocRow row = new() { Text = $"w{w}-{i}" };
                    await db.Table<WalParallelDocRow>().AddAsync(row);
                    assigned.Add((row.Text, row.Id));
                }
            })).ToArray();
            Task.WaitAll(tasks);
        }

        Dictionary<string, int> stored = db.Table<WalParallelDocRow>().ToList().ToDictionary(r => r.Text, r => r.Id);

        Assert.Equal(writers * perWriter, stored.Count);
        Assert.Equal(writers * perWriter, assigned.Select(a => a.AssignedId).Distinct().Count());
        Assert.All(assigned, a => Assert.Equal(stored[a.Text], a.AssignedId));
    }
}
