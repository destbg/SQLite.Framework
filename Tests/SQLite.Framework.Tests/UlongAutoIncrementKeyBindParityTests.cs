using System.Linq;
using System.ComponentModel.DataAnnotations;
using SQLite.Framework;
using SQLite.Framework.Attributes;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class UlongAutoKeyRow
{
    [Key]
    [AutoIncrement]
    public ulong Id { get; set; }

    public string Name { get; set; } = "";
}

public class UlongAutoIncrementKeyBindParityTests
{
    [Fact]
    public void ExplicitUlongKeyAboveSignedRange_AddDoesNotThrow()
    {
        using TestDatabase db = new(b => b.ExplicitAutoIncrementKeysPreserved = true);
        db.Table<UlongAutoKeyRow>().Schema.CreateTable();

        UlongAutoKeyRow item = new() { Id = ulong.MaxValue, Name = "a" };
        int affected = db.Table<UlongAutoKeyRow>().Add(item);

        Assert.Equal(1, affected);
    }

    [Fact]
    public void UnsetUlongKey_Add_AutoAssigns()
    {
        using TestDatabase db = new();
        db.Table<UlongAutoKeyRow>().Schema.CreateTable();

        UlongAutoKeyRow item = new() { Name = "a" };
        db.Table<UlongAutoKeyRow>().Add(item);

        Assert.True(item.Id > 0UL);
    }
}

