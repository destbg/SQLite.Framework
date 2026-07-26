#if !SQLITECIPHER
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using SQLite.Framework;
using SQLite.Framework.Attributes;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[StrictTable]
[Table("H22mJoinCopyTargets")]
public class H22mJoinCopyTarget
{
    [Key]
    public int Id { get; set; }

    public int SourceId { get; set; }

    public Address Data { get; set; } = new();

    public Address Backup { get; set; } = new();
}

[Table("H22mJoinCopySources")]
public class H22mJoinCopySource
{
    [Key]
    public int Id { get; set; }

    public Address Payload { get; set; } = new();
}

public class ConverterColumnCopyFromJoinedRowWriteParityTests
{
    [Fact]
    public void CopyingATargetConverterColumnThroughAJoinStoresTheSameValue()
    {
        using TestDatabase db = Db();
        db.Table<H22mJoinCopyTarget>().AddRange(Targets());
        db.Table<H22mJoinCopySource>().AddRange(Sources());

        (string Street, string City) expected = Targets()
            .Join(Sources(), t => t.SourceId, s => s.Id, (t, s) => (t.Data.Street, t.Data.City))
            .First();

        db.Table<H22mJoinCopyTarget>()
            .Join(db.Table<H22mJoinCopySource>(), t => t.SourceId, s => s.Id, (t, s) => new { t, s })
            .ExecuteUpdate(u => u.Set(x => x.t.Backup, x => x.t.Data));

        Address stored = db.Table<H22mJoinCopyTarget>().Where(r => r.Id == 1).Select(r => r.Backup).First();

        Assert.Equal(expected, (stored.Street, stored.City));
    }

    private static List<H22mJoinCopyTarget> Targets()
    {
        return
        [
            new H22mJoinCopyTarget
            {
                Id = 1,
                SourceId = 7,
                Data = new Address { Street = "1", City = "A" },
                Backup = new Address { Street = "0", City = "Z" }
            }
        ];
    }

    private static List<H22mJoinCopySource> Sources()
    {
        return
        [
            new H22mJoinCopySource
            {
                Id = 7,
                Payload = new Address { Street = "9", City = "Q" }
            }
        ];
    }

    private static TestDatabase Db()
    {
        TestDatabase db = new(b =>
            b.TypeConverters[typeof(Address)] = new SQLiteJsonbConverter<Address>(TestJsonContext.Default.Address));
        db.Table<H22mJoinCopyTarget>().Schema.CreateTable();
        db.Table<H22mJoinCopySource>().Schema.CreateTable();
        return db;
    }
}
#endif
