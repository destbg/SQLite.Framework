using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using SQLite.Framework.JsonB;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[JsonSerializable(typeof(Address))]
[JsonSerializable(typeof(TagList))]
internal partial class TestJsonContext : JsonSerializerContext;

public class JsonConverterTests
{
    [Fact]
    public void JsonConverter_RoundTrip()
    {
        using TestDatabase db = SetupJsonDatabase();
        db.Table<ContactEntity>().Add(new ContactEntity
        {
            Id = 1,
            Address = new Address { Street = "123 Main St", City = "Springfield" }
        });

        ContactEntity result = db.Table<ContactEntity>().First();

        Assert.Equal("123 Main St", result.Address.Street);
        Assert.Equal("Springfield", result.Address.City);
    }

    [Fact]
    public void JsonConverter_NullValue_RoundTrip()
    {
        using TestDatabase db = SetupJsonDatabase();
        db.Table<NullableContactEntity>().Add(new NullableContactEntity { Id = 1, Address = null });

        NullableContactEntity result = db.Table<NullableContactEntity>().First();

        Assert.Null(result.Address);
    }

    [Fact]
    public void JsonConverter_Multiple_RoundTrip()
    {
        using TestDatabase db = SetupJsonDatabase();
        db.Table<ContactEntity>().Add(new ContactEntity
        {
            Id = 1,
            Address = new Address { Street = "1 A St", City = "Alpha" }
        });
        db.Table<ContactEntity>().Add(new ContactEntity
        {
            Id = 2,
            Address = new Address { Street = "2 B St", City = "Beta" }
        });

        List<ContactEntity> results = db.Table<ContactEntity>().OrderBy(e => e.Id).ToList();

        Assert.Equal("Alpha", results[0].Address.City);
        Assert.Equal("Beta", results[1].Address.City);
    }

    [Fact]
    public void JsonConverter_Select_ProjectedColumn()
    {
        using TestDatabase db = SetupJsonDatabase();
        db.Table<ContactEntity>().Add(new ContactEntity
        {
            Id = 1,
            Address = new Address { Street = "99 Oak Ave", City = "Shelbyville" }
        });

        Address result = db.Table<ContactEntity>().Select(e => e.Address).First();

        Assert.Equal("99 Oak Ave", result.Street);
    }

    [Fact]
    public void JsonConverter_CollectionType_RoundTrip()
    {
        using TestDatabase db = SetupJsonTagDatabase();
        db.Table<TaggedEntity>().Add(new TaggedEntity
        {
            Id = 1,
            Tags = new TagList { Values = ["csharp", "dotnet", "sqlite"] }
        });

        TaggedEntity result = db.Table<TaggedEntity>().First();

        Assert.Equal(3, result.Tags.Values.Count);
        Assert.Contains("csharp", result.Tags.Values);
        Assert.Contains("sqlite", result.Tags.Values);
    }

    [Fact]
    public void JsonbConverter_RoundTrip()
    {
        using TestDatabase db = SetupJsonbDatabase();
        db.Table<ContactEntity>().Add(new ContactEntity
        {
            Id = 1,
            Address = new Address { Street = "456 Elm St", City = "Shelbyville" }
        });

        ContactEntity result = db.Table<ContactEntity>().First();

        Assert.Equal("456 Elm St", result.Address.Street);
        Assert.Equal("Shelbyville", result.Address.City);
    }

    [Fact]
    public void JsonbConverter_NullValue_RoundTrip()
    {
        using TestDatabase db = SetupJsonbDatabase();
        db.Table<NullableContactEntity>().Add(new NullableContactEntity { Id = 1, Address = null });

        NullableContactEntity result = db.Table<NullableContactEntity>().First();

        Assert.Null(result.Address);
    }

    [Fact]
    public void JsonbConverter_Multiple_RoundTrip()
    {
        using TestDatabase db = SetupJsonbDatabase();
        db.Table<ContactEntity>().Add(new ContactEntity
        {
            Id = 1,
            Address = new Address { Street = "1 A St", City = "Alpha" }
        });
        db.Table<ContactEntity>().Add(new ContactEntity
        {
            Id = 2,
            Address = new Address { Street = "2 B St", City = "Beta" }
        });

        List<ContactEntity> results = db.Table<ContactEntity>().OrderBy(e => e.Id).ToList();

        Assert.Equal("Alpha", results[0].Address.City);
        Assert.Equal("Beta", results[1].Address.City);
    }

    [Fact]
    public void JsonbConverter_Select_ProjectedColumn()
    {
        using TestDatabase db = SetupJsonbDatabase();
        db.Table<ContactEntity>().Add(new ContactEntity
        {
            Id = 1,
            Address = new Address { Street = "7 Pine Rd", City = "Ogdenville" }
        });

        Address result = db.Table<ContactEntity>().Select(e => e.Address).First();

        Assert.Equal("7 Pine Rd", result.Street);
    }

    [Fact]
    public void JsonbConverter_CollectionType_RoundTrip()
    {
        using TestDatabase db = SetupJsonbTagDatabase();
        db.Table<TaggedEntity>().Add(new TaggedEntity
        {
            Id = 1,
            Tags = new TagList { Values = ["x", "y", "z"] }
        });

        TaggedEntity result = db.Table<TaggedEntity>().First();

        Assert.Equal(3, result.Tags.Values.Count);
        Assert.Contains("x", result.Tags.Values);
        Assert.Contains("z", result.Tags.Values);
    }

    [Fact]
    public void JsonbConverter_SpecialCharacters_RoundTrip()
    {
        using TestDatabase db = SetupJsonbDatabase();
        db.Table<ContactEntity>().Add(new ContactEntity
        {
            Id = 1,
            Address = new Address { Street = "O'Brien & \"Co\"", City = "New\nLine" }
        });

        ContactEntity result = db.Table<ContactEntity>().First();

        Assert.Equal("O'Brien & \"Co\"", result.Address.Street);
        Assert.Equal("New\nLine", result.Address.City);
    }

    private static TestDatabase SetupJsonDatabase([CallerMemberName] string? methodName = null)
    {
        TestDatabase db = new(methodName);
        db.StorageOptions.TypeConverters[typeof(Address)] =
            new SQLiteJsonConverter<Address>(TestJsonContext.Default.Address);
        db.Table<ContactEntity>().CreateTable();
        db.Table<NullableContactEntity>().CreateTable();
        return db;
    }

    private static TestDatabase SetupJsonTagDatabase([CallerMemberName] string? methodName = null)
    {
        TestDatabase db = new(methodName);
        db.StorageOptions.TypeConverters[typeof(TagList)] =
            new SQLiteJsonConverter<TagList>(TestJsonContext.Default.TagList);
        db.Table<TaggedEntity>().CreateTable();
        return db;
    }

    private static TestDatabase SetupJsonbDatabase([CallerMemberName] string? methodName = null)
    {
        TestDatabase db = new(methodName);
        db.StorageOptions.TypeConverters[typeof(Address)] =
            new SQLiteJsonbConverter<Address>(TestJsonContext.Default.Address);
        db.Table<ContactEntity>().CreateTable();
        db.Table<NullableContactEntity>().CreateTable();
        return db;
    }

    private static TestDatabase SetupJsonbTagDatabase([CallerMemberName] string? methodName = null)
    {
        TestDatabase db = new(methodName);
        db.StorageOptions.TypeConverters[typeof(TagList)] =
            new SQLiteJsonbConverter<TagList>(TestJsonContext.Default.TagList);
        db.Table<TaggedEntity>().CreateTable();
        return db;
    }
}

public class Address
{
    public string Street { get; set; } = "";
    public string City { get; set; } = "";
}

public class TagList
{
    public List<string> Values { get; set; } = [];
}

file class ContactEntity
{
    [Key]
    public required int Id { get; set; }

    public required Address Address { get; set; }
}

file class NullableContactEntity
{
    [Key]
    public required int Id { get; set; }

    public Address? Address { get; set; }
}

file class TaggedEntity
{
    [Key]
    public required int Id { get; set; }

    public required TagList Tags { get; set; }
}
