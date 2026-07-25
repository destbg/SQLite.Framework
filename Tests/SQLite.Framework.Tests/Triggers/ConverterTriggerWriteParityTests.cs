#if !SQLITECIPHER
using System.Linq;
using System.ComponentModel.DataAnnotations;
using SQLite.Framework;
using SQLite.Framework.Attributes;
using SQLite.Framework.Enums;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[StrictTable]
internal sealed class TriggerJsonbSource
{
    [Key]
    public int Id { get; set; }

    public Address Meta { get; set; } = new();
}

[StrictTable]
internal sealed class TriggerJsonbAudit
{
    [Key]
    [AutoIncrement]
    public int Id { get; set; }

    public Address Data { get; set; } = new();
}

public class ConverterTriggerWriteParityTests
{
    private static TestDatabase Db()
    {
        return new TestDatabase(b =>
            b.TypeConverters[typeof(Address)] = new SQLiteJsonbConverter<Address>(TestJsonContext.Default.Address));
    }

    [Fact]
    public void TriggerCopiesJsonbColumnFromNewRow_RoundTrips()
    {
        using TestDatabase db = Db();
        db.Table<TriggerJsonbSource>().Schema.CreateTable();
        db.Table<TriggerJsonbAudit>().Schema.CreateTable();

        db.Schema.CreateTrigger<TriggerJsonbSource>("trg_jn", SQLiteTriggerTiming.After, SQLiteTriggerEvent.Insert, t => t
            .Insert(db.Table<TriggerJsonbAudit>(), s => s
                .Set(a => a.Data, _ => t.New.Meta)));

        db.Table<TriggerJsonbSource>().Add(new TriggerJsonbSource { Id = 1, Meta = new Address { Street = "m", City = "n" } });

        Address copied = db.Table<TriggerJsonbAudit>().Select(a => a.Data).First();
        Assert.Equal(("m", "n"), (copied.Street, copied.City));
    }
}
#endif
