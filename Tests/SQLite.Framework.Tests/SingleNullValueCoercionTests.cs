using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class SingleNullValueCoercionTests
{
    private static TestDatabase SeedWithNull()
    {
        TestDatabase db = new();
        db.Table<NullableEntity>().Schema.CreateTable();
        db.Table<NullableEntity>().Add(new NullableEntity { Id = 1, Value = null });
        return db;
    }

    [Fact]
    public void First_NullableValueUnwrapOnNull_ReturnsDefaultInsteadOfThrowing()
    {
        using TestDatabase db = SeedWithNull();

        int actual = db.Table<NullableEntity>()
            .Where(e => e.Id == 1)
            .Select(e => e.Value!.Value)
            .First();

        Assert.Equal(0, actual);
    }

    [Fact]
    public void Single_NullableValueUnwrapOnNull_ReturnsDefaultInsteadOfThrowing()
    {
        using TestDatabase db = SeedWithNull();

        int actual = db.Table<NullableEntity>()
            .Where(e => e.Id == 1)
            .Select(e => e.Value!.Value)
            .Single();

        Assert.Equal(0, actual);
    }

    [Fact]
    public void First_NullableProjectionWithoutUnwrap_ReturnsNull()
    {
        using TestDatabase db = SeedWithNull();

        int? actual = db.Table<NullableEntity>()
            .Where(e => e.Id == 1)
            .Select(e => e.Value)
            .First();

        Assert.Null(actual);
    }
}
