#if !SQLITECIPHER
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H22mReadOwners")]
public class H22mReadOwner
{
    [Key]
    public int Id { get; set; }

    public int PayloadId { get; set; }
}

[Table("H22mReadPayloads")]
public class H22mReadPayload
{
    [Key]
    public int Id { get; set; }

    public Address Data { get; set; } = new();
}

public class ConverterColumnReadFromJoinedTableParityTests
{
    [Fact]
    public void WholeJoinedRowReadsItsConverterColumnBack()
    {
        using TestDatabase db = Db();

        List<(string? Street, string? City)> expected = Owners()
            .Join(Payloads(), o => o.PayloadId, p => p.Id, (o, p) => p)
            .Select(p => (p.Data?.Street, p.Data?.City))
            .ToList();

        List<(string? Street, string? City)> actual = db.Table<H22mReadOwner>()
            .Join(db.Table<H22mReadPayload>(), o => o.PayloadId, p => p.Id, (o, p) => p)
            .AsEnumerable()
            .Select(p => (p.Data?.Street, p.Data?.City))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ProjectedJoinedConverterColumnReadsItsValueBack()
    {
        using TestDatabase db = Db();

        List<(int Id, string? Street)> expected = Owners()
            .Join(Payloads(), o => o.PayloadId, p => p.Id, (o, p) => new { o.Id, Payload = p.Data })
            .Select(x => (x.Id, x.Payload?.Street))
            .ToList();

        List<(int Id, string? Street)> actual = db.Table<H22mReadOwner>()
            .Join(db.Table<H22mReadPayload>(), o => o.PayloadId, p => p.Id, (o, p) => new { o.Id, Payload = p.Data })
            .AsEnumerable()
            .Select(x => (x.Id, x.Payload?.Street))
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H22mReadOwner> Owners()
    {
        return
        [
            new H22mReadOwner { Id = 1, PayloadId = 7 }
        ];
    }

    private static List<H22mReadPayload> Payloads()
    {
        return
        [
            new H22mReadPayload
            {
                Id = 7,
                Data = new Address { Street = "Long", City = "Rome" }
            }
        ];
    }

    private static TestDatabase Db()
    {
        TestDatabase db = new(b =>
            b.TypeConverters[typeof(Address)] = new SQLiteJsonbConverter<Address>(TestJsonContext.Default.Address));
        db.Table<H22mReadOwner>().Schema.CreateTable();
        db.Table<H22mReadPayload>().Schema.CreateTable();
        db.Table<H22mReadOwner>().AddRange(Owners());
        db.Table<H22mReadPayload>().AddRange(Payloads());
        return db;
    }
}
#endif
