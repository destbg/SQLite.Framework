#if !SQLITECIPHER
using System.Linq;
using System.ComponentModel.DataAnnotations;
using SQLite.Framework;
using SQLite.Framework.Attributes;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[StrictTable]
file sealed class StrictJsonbRow
{
    [Key]
    public int Id { get; set; }

    public Address Data { get; set; } = new();
}

public class JsonbSetBasedWriteParityTests
{
    [Fact]
    public void ExecuteUpdate_SetJsonbColumn_StoresSameAsEntityUpdate()
    {
        using TestDatabase db = Db();
        db.Table<StrictJsonbRow>().Add(new StrictJsonbRow { Id = 1, Data = new Address { Street = "1", City = "A" } });

        db.Table<StrictJsonbRow>()
            .Where(r => r.Id == 1)
            .ExecuteUpdate(s => s.Set(r => r.Data, new Address { Street = "2", City = "B" }));

        Address actual = db.Table<StrictJsonbRow>().Where(r => r.Id == 1).Select(r => r.Data).First();

        Assert.Equal(("2", "B"), (actual.Street, actual.City));
    }

    [Fact]
    public void WithColumnsSetJsonbColumn_StoresSameAsEntityUpdate()
    {
        using TestDatabase db = Db();

        db.Table<StrictJsonbRow>()
            .WithColumns(c => c.Set(r => r.Data, new Address { Street = "3", City = "C" }))
            .Add(new StrictJsonbRow { Id = 1 });

        Address actual = db.Table<StrictJsonbRow>().Where(r => r.Id == 1).Select(r => r.Data).First();

        Assert.Equal(("3", "C"), (actual.Street, actual.City));
    }

    private static TestDatabase Db()
    {
        TestDatabase db = new(b =>
            b.TypeConverters[typeof(Address)] = new SQLiteJsonbConverter<Address>(TestJsonContext.Default.Address));
        db.Table<StrictJsonbRow>().Schema.CreateTable();
        return db;
    }
}
#endif
