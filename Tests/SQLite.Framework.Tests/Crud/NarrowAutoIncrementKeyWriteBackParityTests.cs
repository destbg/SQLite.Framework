using System;
using System.Linq;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class NarrowAutoIncrementKeyWriteBackParityTests
{
    [Fact]
    public void SByteAutoIncrementKeyAboveRange_ThrowsOverflow()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<SByteKeyEntity>();
        db.Execute("INSERT INTO \"SByteKeyEntity\" (\"Id\", \"Name\") VALUES (127, 'seed')");

        SByteKeyEntity e = new() { Name = "target" };

        Assert.Throws<OverflowException>(() => db.Table<SByteKeyEntity>().Add(e));
    }

    [Fact]
    public void SByteAutoIncrementKeyInRange_WritesBackStoredKey()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<SByteKeyEntity>();

        SByteKeyEntity e = new() { Name = "target" };
        db.Table<SByteKeyEntity>().Add(e);

        long storedId = db.Query<long>("SELECT \"Id\" FROM \"SByteKeyEntity\" WHERE \"Name\" = 'target'").Single();

        Assert.Equal(1L, storedId);
        Assert.Equal(storedId, (long)e.Id);
    }
}
