using System.Linq;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class ReturningUlongAutoIncrementKeyParityTests
{
    [Fact]
    public void ReturningAdd_ExplicitUlongKeyAboveSignedRange_PreservesKey()
    {
        using TestDatabase db = new(b => b.ExplicitAutoIncrementKeysPreserved = true);
        db.Table<UlongAutoKeyRow>().Schema.CreateTable();

        UlongAutoKeyRow item = new() { Id = ulong.MaxValue, Name = "a" };
        UlongAutoKeyRow? returned = db.Table<UlongAutoKeyRow>().Returning().Add(item);

        Assert.NotNull(returned);
        Assert.Equal(ulong.MaxValue, returned!.Id);
    }

    [Fact]
    public void ReturningAdd_UnsetUlongKey_AutoAssigns()
    {
        using TestDatabase db = new(b => b.ExplicitAutoIncrementKeysPreserved = true);
        db.Table<UlongAutoKeyRow>().Schema.CreateTable();

        UlongAutoKeyRow item = new() { Name = "a" };
        UlongAutoKeyRow? returned = db.Table<UlongAutoKeyRow>().Returning().Add(item);

        Assert.NotNull(returned);
        Assert.True(returned!.Id > 0UL);
    }

    [Fact]
    public void ReturningUpsert_ExplicitUlongKeyAboveSignedRange_PreservesKey()
    {
        using TestDatabase db = new(b => b.ExplicitAutoIncrementKeysPreserved = true);
        db.Table<UlongAutoKeyRow>().Schema.CreateTable();

        UlongAutoKeyRow item = new() { Id = ulong.MaxValue, Name = "a" };
        UlongAutoKeyRow? returned = db.Table<UlongAutoKeyRow>()
            .Returning()
            .Upsert(item, c => c.OnConflict(x => x.Id).DoNothing());

        Assert.NotNull(returned);
        Assert.Equal(ulong.MaxValue, returned!.Id);
    }
}
