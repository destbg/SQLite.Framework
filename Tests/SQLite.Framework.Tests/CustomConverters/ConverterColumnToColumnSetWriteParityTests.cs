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
[Table("H21mJsonbCopyRows")]
public class H21mJsonbCopyRow
{
    [Key]
    public int Id { get; set; }

    public Address Data { get; set; } = new();

    public Address Backup { get; set; } = new();
}

public class ConverterColumnToColumnSetWriteParityTests
{
    [Fact]
    public void ExecuteUpdateCopyingConverterColumnStoresSameAsEntityWrite()
    {
        using TestDatabase db = Db();
        db.Table<H21mJsonbCopyRow>().AddRange(Rows());

        List<H21mJsonbCopyRow> local = Rows();
        foreach (H21mJsonbCopyRow row in local.Where(r => r.Id == 1))
        {
            row.Backup = row.Data;
        }

        (string Street, string City) expected = local
            .Where(r => r.Id == 1)
            .Select(r => (r.Backup.Street, r.Backup.City))
            .First();

        db.Table<H21mJsonbCopyRow>()
            .Where(r => r.Id == 1)
            .ExecuteUpdate(s => s.Set(r => r.Backup, r => r.Data));

        Address stored = db.Table<H21mJsonbCopyRow>().Where(r => r.Id == 1).Select(r => r.Backup).First();

        Assert.Equal(expected, (stored.Street, stored.City));
    }

    private static List<H21mJsonbCopyRow> Rows()
    {
        return
        [
            new H21mJsonbCopyRow
            {
                Id = 1,
                Data = new Address { Street = "1", City = "A" },
                Backup = new Address { Street = "0", City = "Z" }
            }
        ];
    }

    private static TestDatabase Db()
    {
        TestDatabase db = new(b =>
            b.TypeConverters[typeof(Address)] = new SQLiteJsonbConverter<Address>(TestJsonContext.Default.Address));
        db.Table<H21mJsonbCopyRow>().Schema.CreateTable();
        return db;
    }
}
#endif
