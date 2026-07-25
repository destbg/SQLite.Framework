using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("GateNote")]
public class GateNoteRow
{
    [Key]
    public int Id { get; set; }
}

public class AsyncMigrateReadGateTests
{
    [Fact]
    public async Task MigrateAsyncBlocksReadsWhenTheOptionIsOn()
    {
        using TestDatabase db = new(b => b.UseBlockReadsDuringTransaction(), useFile: true);
        using ManualResetEventSlim inMigration = new();
        Task reader = Task.Run(() =>
        {
            inMigration.Wait(TimeSpan.FromSeconds(5));
            db.Table<GateNoteRow>().Count();
        });

        bool readCompletedDuringMigration = false;
        await db.Schema.Migrations()
            .Version(1, m => m
                .CreateTable<GateNoteRow>()
                .Run(_ =>
                {
                    inMigration.Set();
                    Thread.Sleep(1500);
                    readCompletedDuringMigration = reader.IsCompleted;
                }))
            .MigrateAsync();

        Assert.True(reader.Wait(TimeSpan.FromSeconds(5)));
        Assert.False(readCompletedDuringMigration);
    }
}
