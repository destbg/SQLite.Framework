#if !SQLITECIPHER
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class JsonbKeyRow
{
    [Key]
    public Address Key { get; set; } = new();

    public string Name { get; set; } = "";
}

public class ConverterKeyUpdateRemoveParityTests
{
    private static TestDatabase Db()
    {
        return new TestDatabase(b =>
            b.TypeConverters[typeof(Address)] = new SQLiteJsonbConverter<Address>(TestJsonContext.Default.Address));
    }

    [Fact]
    public void UpdateByJsonbKey_MatchesRowAndStoresNewValue()
    {
        using TestDatabase db = Db();
        db.Table<JsonbKeyRow>().Schema.CreateTable();
        Address key = new() { Street = "1", City = "A" };
        db.Table<JsonbKeyRow>().Add(new JsonbKeyRow { Key = key, Name = "before" });

        List<JsonbKeyRow> oracle = [new JsonbKeyRow { Key = key, Name = "before" }];
        JsonbKeyRow match = oracle.First(r => r.Key.Street == key.Street && r.Key.City == key.City);
        match.Name = "after";

        db.Table<JsonbKeyRow>().Update(new JsonbKeyRow { Key = key, Name = "after" });

        string actual = db.Table<JsonbKeyRow>().Select(r => r.Name).First();
        Assert.Equal(oracle[0].Name, actual);
    }

    [Fact]
    public void RemoveByJsonbKey_DeletesRow()
    {
        using TestDatabase db = Db();
        db.Table<JsonbKeyRow>().Schema.CreateTable();
        Address key = new() { Street = "1", City = "A" };
        db.Table<JsonbKeyRow>().Add(new JsonbKeyRow { Key = key, Name = "x" });

        db.Table<JsonbKeyRow>().Remove(new JsonbKeyRow { Key = key, Name = "x" });

        int remaining = db.Table<JsonbKeyRow>().Count();
        Assert.Equal(0, remaining);
    }
}
#endif
