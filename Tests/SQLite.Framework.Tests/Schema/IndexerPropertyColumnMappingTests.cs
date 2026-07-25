using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("IndexedLookup")]
public class IndexedLookupRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public char this[int position]
    {
        get => Name[position];
        set => Name = Name.Remove(position, 1).Insert(position, value.ToString());
    }
}

public class IndexerPropertyColumnMappingTests
{
    [Fact]
    public void EntityWithIndexerRoundTrips()
    {
        List<IndexedLookupRow> memory = [new IndexedLookupRow { Id = 1, Name = "abc" }];
        string expected = memory.Single(r => r.Id == 1).Name;
        Assert.Equal("abc", expected);

        using TestDatabase db = new();
        db.Schema.CreateTable<IndexedLookupRow>();
        db.Table<IndexedLookupRow>().Add(new IndexedLookupRow { Id = 1, Name = "abc" });
        string actual = db.Table<IndexedLookupRow>().Single(r => r.Id == 1).Name;

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void EntityWithIndexerMapsOnlyPlainColumns()
    {
        using TestDatabase db = new();
        db.Schema.CreateTable<IndexedLookupRow>();

        List<string> columns = db.Schema.ListColumns("IndexedLookup").Select(c => c.Name).ToList();

        Assert.Equal(["Id", "Name"], columns);
    }
}
