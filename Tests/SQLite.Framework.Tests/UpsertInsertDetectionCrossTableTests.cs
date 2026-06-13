using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Attributes;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class UpsertAutoRow
{
    [Key]
    [AutoIncrement]
    public int Id { get; set; }

    public string Text { get; set; } = "";
}

internal sealed class UpsertSideRow
{
    [Key]
    public int Id { get; set; }

    public string Tag { get; set; } = "";
}

public class UpsertInsertDetectionCrossTableTests
{
    [Fact]
    public void UpsertInsertDoesNotWriteBackKeyWhenRowIdMatchesEarlierInsert()
    {
        using TestDatabase db = new();
        db.Table<UpsertAutoRow>().Schema.CreateTable();
        db.Table<UpsertSideRow>().Schema.CreateTable();

        for (int i = 0; i < 3; i++)
        {
            db.Table<UpsertAutoRow>().Add(new UpsertAutoRow { Text = $"seed{i}" });
        }

        db.Table<UpsertSideRow>().Add(new UpsertSideRow { Id = 4, Tag = "side4" });

        UpsertAutoRow fresh = new() { Text = "upserted" };
        int changes = db.Table<UpsertAutoRow>().Upsert(fresh, u => u.OnConflict(x => x.Id).DoUpdateAll());

        UpsertAutoRow stored = db.Table<UpsertAutoRow>().Single(x => x.Text == "upserted");

        Assert.Equal(1, changes);
        Assert.Equal(4, stored.Id);
        Assert.Equal(0, fresh.Id);
    }
}
