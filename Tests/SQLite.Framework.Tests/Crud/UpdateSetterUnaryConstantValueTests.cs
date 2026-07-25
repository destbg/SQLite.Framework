using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H21aUnarySetter")]
public class H21aUnarySetterRow
{
    [Key]
    public int Id { get; set; }

    public int Val { get; set; }

    public bool Flag { get; set; }
}

public class UpdateSetterUnaryConstantValueTests
{
    [Fact]
    public void MigrationUpdateStepStoresTheNegatedAmount()
    {
        int amount = 3;
        using TestDatabase db = new();
        db.Table<H21aUnarySetterRow>().Schema.CreateTable();
        db.Table<H21aUnarySetterRow>().Add(new H21aUnarySetterRow { Id = 1, Val = 0, Flag = true });

        db.Schema.Migrations()
            .Version(1, m => m.Update<H21aUnarySetterRow>(s => s.Set(x => x.Val, x => -amount)))
            .Migrate();

        List<H21aUnarySetterRow> memory = [new H21aUnarySetterRow { Id = 1, Val = 0, Flag = true }];
        foreach (H21aUnarySetterRow row in memory)
        {
            row.Val = -amount;
        }

        List<int> expected = memory.Select(r => r.Val).ToList();
        List<int> actual = db.Table<H21aUnarySetterRow>().OrderBy(r => r.Id).Select(r => r.Val).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ExecuteUpdateStoresTheNegatedFlag()
    {
        bool flag = true;
        using TestDatabase db = new();
        db.Table<H21aUnarySetterRow>().Schema.CreateTable();
        db.Table<H21aUnarySetterRow>().Add(new H21aUnarySetterRow { Id = 1, Val = 0, Flag = true });

        db.Table<H21aUnarySetterRow>().ExecuteUpdate(s => s.Set(x => x.Flag, x => !flag));

        List<H21aUnarySetterRow> memory = [new H21aUnarySetterRow { Id = 1, Val = 0, Flag = true }];
        foreach (H21aUnarySetterRow row in memory)
        {
            row.Flag = !flag;
        }

        List<bool> expected = memory.Select(r => r.Flag).ToList();
        List<bool> actual = db.Table<H21aUnarySetterRow>().OrderBy(r => r.Id).Select(r => r.Flag).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ExecuteUpdateStoresTheComplementedAmount()
    {
        int amount = 5;
        using TestDatabase db = new();
        db.Table<H21aUnarySetterRow>().Schema.CreateTable();
        db.Table<H21aUnarySetterRow>().Add(new H21aUnarySetterRow { Id = 1, Val = 0, Flag = true });

        db.Table<H21aUnarySetterRow>().ExecuteUpdate(s => s.Set(x => x.Val, x => ~amount));

        List<H21aUnarySetterRow> memory = [new H21aUnarySetterRow { Id = 1, Val = 0, Flag = true }];
        foreach (H21aUnarySetterRow row in memory)
        {
            row.Val = ~amount;
        }

        List<int> expected = memory.Select(r => r.Val).ToList();
        List<int> actual = db.Table<H21aUnarySetterRow>().OrderBy(r => r.Id).Select(r => r.Val).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void UpsertDoUpdateStoresTheNegatedAmount()
    {
        int amount = 3;
        using TestDatabase db = new();
        db.Table<H21aUnarySetterRow>().Schema.CreateTable();
        db.Table<H21aUnarySetterRow>().Add(new H21aUnarySetterRow { Id = 1, Val = 100, Flag = true });

        db.Table<H21aUnarySetterRow>().Upsert(
            new H21aUnarySetterRow { Id = 1, Val = 7, Flag = false },
            c => c.OnConflict(x => x.Id).DoUpdate(s => s.Set(x => x.Val, x => -amount)));

        List<H21aUnarySetterRow> memory = [new H21aUnarySetterRow { Id = 1, Val = 100, Flag = true }];
        foreach (H21aUnarySetterRow row in memory)
        {
            row.Val = -amount;
        }

        List<int> expected = memory.Select(r => r.Val).ToList();
        List<int> actual = db.Table<H21aUnarySetterRow>().OrderBy(r => r.Id).Select(r => r.Val).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void MigrationSetStepStoresTheNegatedAmount()
    {
        int amount = 3;
        using TestDatabase db = new();
        db.Table<H21aUnarySetterRow>().Schema.CreateTable();
        db.Table<H21aUnarySetterRow>().Add(new H21aUnarySetterRow { Id = 1, Val = 0, Flag = true });

        db.Schema.Migrations()
            .Version(1, m => m.TableChanged<H21aUnarySetterRow>(s => s.Set(x => x.Val, x => -amount)))
            .Migrate();

        List<H21aUnarySetterRow> memory = [new H21aUnarySetterRow { Id = 1, Val = 0, Flag = true }];
        foreach (H21aUnarySetterRow row in memory)
        {
            row.Val = -amount;
        }

        List<int> expected = memory.Select(r => r.Val).ToList();
        List<int> actual = db.Table<H21aUnarySetterRow>().OrderBy(r => r.Id).Select(r => r.Val).ToList();

        Assert.Equal(expected, actual);
    }
}
