using System.Linq;
using System.ComponentModel.DataAnnotations;
using SQLite.Framework;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

file sealed class JsonConverterSetRow
{
    [Key]
    public int Id { get; set; }

    public Address Data { get; set; } = new();
}

public class JsonConverterSetBasedWriteParityTests
{
    [Fact]
    public void ExecuteUpdate_SetJsonConverterColumn_RoundTrips()
    {
        using TestDatabase db = Db();
        db.Table<JsonConverterSetRow>().Add(new JsonConverterSetRow { Id = 1, Data = new Address { Street = "1", City = "A" } });

        db.Table<JsonConverterSetRow>()
            .Where(r => r.Id == 1)
            .ExecuteUpdate(s => s.Set(r => r.Data, new Address { Street = "2", City = "B" }));

        Address actual = db.Table<JsonConverterSetRow>().Where(r => r.Id == 1).Select(r => r.Data).First();

        Assert.Equal(("2", "B"), (actual.Street, actual.City));
    }

    [Fact]
    public void WithColumns_SetJsonConverterColumn_RoundTrips()
    {
        using TestDatabase db = Db();

        db.Table<JsonConverterSetRow>()
            .WithColumns(c => c.Set(r => r.Data, new Address { Street = "3", City = "C" }))
            .Add(new JsonConverterSetRow { Id = 1 });

        Address actual = db.Table<JsonConverterSetRow>().Where(r => r.Id == 1).Select(r => r.Data).First();

        Assert.Equal(("3", "C"), (actual.Street, actual.City));
    }

    private static TestDatabase Db()
    {
        TestDatabase db = new(b =>
            b.TypeConverters[typeof(Address)] = new SQLiteJsonConverter<Address>(TestJsonContext.Default.Address));
        db.Table<JsonConverterSetRow>().Schema.CreateTable();
        return db;
    }
}
