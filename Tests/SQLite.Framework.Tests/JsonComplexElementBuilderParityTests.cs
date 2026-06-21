using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

file sealed class ComplexElementListRow
{
    [Key]
    public int Id { get; set; }

    public List<Address> Addresses { get; set; } = [];
}

public class JsonComplexElementBuilderParityTests
{
    [Fact]
    public void Concat_ComplexElements_MatchesObjects()
    {
        using TestDatabase db = Db();
        List<Address> other = [new Address { Street = "X", City = "Y" }];

        List<(string, string)> expected = db.Table<ComplexElementListRow>().AsEnumerable()
            .First(r => r.Id == 1).Addresses
            .Concat(other)
            .Select(a => (a.Street, a.City))
            .ToList();

        List<(string, string)> actual = db.Table<ComplexElementListRow>()
            .Where(r => r.Id == 1)
            .Select(r => r.Addresses.Concat(other).ToList())
            .First()
            .Select(a => (a.Street, a.City))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Append_ComplexElement_MatchesObjects()
    {
        using TestDatabase db = Db();
        Address extra = new() { Street = "X", City = "Y" };

        List<(string, string)> expected = db.Table<ComplexElementListRow>().AsEnumerable()
            .First(r => r.Id == 1).Addresses
            .Append(extra)
            .Select(a => (a.Street, a.City))
            .ToList();

        List<(string, string)> actual = db.Table<ComplexElementListRow>()
            .Where(r => r.Id == 1)
            .Select(r => r.Addresses.Append(extra).ToList())
            .First()
            .Select(a => (a.Street, a.City))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Prepend_ComplexElement_MatchesObjects()
    {
        using TestDatabase db = Db();
        Address extra = new() { Street = "X", City = "Y" };

        List<(string, string)> expected = db.Table<ComplexElementListRow>().AsEnumerable()
            .First(r => r.Id == 1).Addresses
            .Prepend(extra)
            .Select(a => (a.Street, a.City))
            .ToList();

        List<(string, string)> actual = db.Table<ComplexElementListRow>()
            .Where(r => r.Id == 1)
            .Select(r => r.Addresses.Prepend(extra).ToList())
            .First()
            .Select(a => (a.Street, a.City))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void GetRange_ComplexElements_MatchesObjects()
    {
        using TestDatabase db = Db();

        List<(string, string)> expected = db.Table<ComplexElementListRow>().AsEnumerable()
            .First(r => r.Id == 1).Addresses
            .GetRange(0, 2)
            .Select(a => (a.Street, a.City))
            .ToList();

        List<(string, string)> actual = db.Table<ComplexElementListRow>()
            .Where(r => r.Id == 1)
            .Select(r => r.Addresses.GetRange(0, 2))
            .First()
            .Select(a => (a.Street, a.City))
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static TestDatabase Db()
    {
        TestDatabase db = new(b =>
        {
            b.TypeConverters[typeof(List<Address>)] =
                new SQLiteJsonConverter<List<Address>>(TestJsonContext.Default.ListAddress);
            b.TypeConverters[typeof(Address)] =
                new SQLiteJsonConverter<Address>(TestJsonContext.Default.Address);
        });
        db.Table<ComplexElementListRow>().Schema.CreateTable();
        db.Table<ComplexElementListRow>().Add(new ComplexElementListRow
        {
            Id = 1,
            Addresses =
            [
                new Address { Street = "1", City = "A" },
                new Address { Street = "2", City = "B" },
                new Address { Street = "3", City = "B" },
            ],
        });
        return db;
    }
}
