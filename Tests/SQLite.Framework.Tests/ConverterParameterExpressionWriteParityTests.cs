#if !SQLITECIPHER
using System.Linq;
using System.ComponentModel.DataAnnotations;
using SQLite.Framework;
using SQLite.Framework.Attributes;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[StrictTable]
file sealed class StrictJsonbUpsertRow
{
    [Key]
    public int Id { get; set; }

    public Address Data { get; set; } = new();
}

[StrictTable]
internal sealed class StrictJsonbMigrateRow
{
    [Key]
    public int Id { get; set; }

    public Address Data { get; set; } = new();
}

internal sealed class JsonbDefaultRow
{
    [Key]
    public int Id { get; set; }

    public Address Data { get; set; } = new();
}

public class ConverterParameterExpressionWriteParityTests
{
    private static TestDatabase Db()
    {
        return new TestDatabase(b =>
            b.TypeConverters[typeof(Address)] = new SQLiteJsonbConverter<Address>(TestJsonContext.Default.Address));
    }

    [Fact]
    public void UpsertDoUpdateSetJsonbColumn_StoresSameAsEntityUpdate()
    {
        using TestDatabase db = Db();
        db.Table<StrictJsonbUpsertRow>().Schema.CreateTable();
        db.Table<StrictJsonbUpsertRow>().Add(new StrictJsonbUpsertRow { Id = 1, Data = new Address { Street = "1", City = "A" } });

        db.Table<StrictJsonbUpsertRow>().Upsert(
            new StrictJsonbUpsertRow { Id = 1, Data = new Address { Street = "9", City = "Z" } },
            c => c.OnConflict(x => x.Id).DoUpdate(s => s.Set(x => x.Data, new Address { Street = "2", City = "B" })));

        Address actual = db.Table<StrictJsonbUpsertRow>().Where(r => r.Id == 1).Select(r => r.Data).First();

        Assert.Equal(("2", "B"), (actual.Street, actual.City));
    }

    [Fact]
    public void MigrateSetJsonbColumn_StoresSameAsEntityWrite()
    {
        using TestDatabase db = Db();
        db.CreateCommand("CREATE TABLE \"StrictJsonbMigrateRow\" (\"Id\" INTEGER PRIMARY KEY) STRICT", []).ExecuteNonQuery();
        db.CreateCommand("INSERT INTO \"StrictJsonbMigrateRow\" (\"Id\") VALUES (1)", []).ExecuteNonQuery();

        db.Schema.Migrate<StrictJsonbMigrateRow>(m => m.Set(e => e.Data, new Address { Street = "2", City = "B" }));

        Address actual = db.Table<StrictJsonbMigrateRow>().Select(r => r.Data).First();

        Assert.Equal(("2", "B"), (actual.Street, actual.City));
    }

    [Fact]
    public void DefaultJsonbColumn_WrapsWithConverterExpression()
    {
        using ModelTestDatabase db = new(
            model => model.Entity<JsonbDefaultRow>().Default(e => e.Data, new Address { Street = "d", City = "c" }),
            options => options.TypeConverters[typeof(Address)] = new SQLiteJsonbConverter<Address>(TestJsonContext.Default.Address));
        db.Schema.CreateTable<JsonbDefaultRow>();

        string sql = db.QueryFirst<string>(
            "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = 'JsonbDefaultRow'");

        Assert.Equal(
            "CREATE TABLE \"JsonbDefaultRow\" (\"Id\" INTEGER PRIMARY KEY, \"Data\" BLOB NOT NULL DEFAULT (jsonb('{\"Street\":\"d\",\"City\":\"c\"}')))",
            sql);
    }
}
#endif
