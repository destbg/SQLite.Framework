#if !SQLITECIPHER
using System.Linq;
using System.ComponentModel.DataAnnotations;
using SQLite.Framework;
using SQLite.Framework.Attributes;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[StrictTable]
internal sealed class JxbExprStrictRow
{
    [Key]
    public int Id { get; set; }

    public Address Data { get; set; } = new();
}

public class JsonbConverterSetExpressionOverloadParityTests
{
    private static TestDatabase Db()
    {
        TestDatabase db = new(b =>
            b.TypeConverters[typeof(Address)] = new SQLiteJsonbConverter<Address>(TestJsonContext.Default.Address));
        db.Table<JxbExprStrictRow>().Schema.CreateTable();
        return db;
    }

    [Fact]
    public void ExecuteUpdateSetExpression_JsonbColumn_StoresSameAsValueOverload()
    {
        using TestDatabase db = Db();
        db.Table<JxbExprStrictRow>().Add(new JxbExprStrictRow { Id = 1, Data = new Address { Street = "1", City = "A" } });

        Address payload = new() { Street = "2", City = "B" };
        db.Table<JxbExprStrictRow>().Where(r => r.Id == 1).ExecuteUpdate(s => s.Set(r => r.Data, r => payload));

        Address actual = db.Table<JxbExprStrictRow>().Where(r => r.Id == 1).Select(r => r.Data).First();

        Assert.Equal(("2", "B"), (actual.Street, actual.City));
    }

    [Fact]
    public void WithColumnsSetExpression_JsonbColumn_StoresSameAsValueOverload()
    {
        using TestDatabase db = Db();

        Address payload = new() { Street = "3", City = "C" };
        db.Table<JxbExprStrictRow>().WithColumns(c => c.Set(r => r.Data, r => payload)).Add(new JxbExprStrictRow { Id = 1 });

        Address actual = db.Table<JxbExprStrictRow>().Where(r => r.Id == 1).Select(r => r.Data).First();

        Assert.Equal(("3", "C"), (actual.Street, actual.City));
    }
}
#endif
