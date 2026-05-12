using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using SQLite.Framework.Enums;
using SQLite.Framework.Internals.JSON;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[JsonSerializable(typeof(Person))]
internal partial class PersonRootJsonContext : JsonSerializerContext;

[JsonSerializable(typeof(LoopA))]
[JsonSerializable(typeof(LoopB))]
[JsonSerializable(typeof(SelfLoop))]
internal partial class LoopJsonContext : JsonSerializerContext;

public class NestedJsonConverterTests
{
    [Fact]
    public void AddJsonContext_RegistersDeclaredAndNestedTypes()
    {
        SQLiteOptionsBuilder builder = new(":memory:");
        builder.AddJsonContext(PersonRootJsonContext.Default);

        SQLiteOptions options = builder.Build();

        Assert.True(options.TypeConverters.ContainsKey(typeof(Person)));
        Assert.True(options.TypeConverters.ContainsKey(typeof(Address)));
    }

    [Fact]
    public void AddJsonContext_DoesNotOverwriteExplicitlyRegistered()
    {
        SQLiteJsonConverter<Address> explicitAddress = new(TestJsonContext.Default.Address);

        SQLiteOptionsBuilder builder = new(":memory:");
        builder.AddTypeConverter<Address>(explicitAddress);
        builder.AddJsonContext(PersonRootJsonContext.Default);

        SQLiteOptions options = builder.Build();

        Assert.Same(explicitAddress, options.TypeConverters[typeof(Address)]);
    }

    [Fact]
    public void AddJsonContext_DoesNotOverwriteRootWhenAlreadyRegistered()
    {
        SQLiteJsonConverter<Person> explicitPerson = new(TestJsonContext.Default.Person);

        SQLiteOptionsBuilder builder = new(":memory:");
        builder.AddTypeConverter<Person>(explicitPerson);
        builder.AddJsonContext(PersonRootJsonContext.Default);

        SQLiteOptions options = builder.Build();

        Assert.Same(explicitPerson, options.TypeConverters[typeof(Person)]);
        Assert.True(options.TypeConverters.ContainsKey(typeof(Address)));
    }

#if !SQLITECIPHER
    [Fact]
    public void AddJsonbContext_RegistersEverythingAsJsonb()
    {
        SQLiteOptionsBuilder builder = new(":memory:");
        builder.AddJsonbContext(PersonRootJsonContext.Default);

        SQLiteOptions options = builder.Build();

        ISQLiteTypeConverter person = options.TypeConverters[typeof(Person)];
        Assert.Equal(SQLiteColumnType.Blob, person.ColumnType);

        ISQLiteTypeConverter address = options.TypeConverters[typeof(Address)];
        Assert.Equal(SQLiteColumnType.Blob, address.ColumnType);
        Assert.Equal("jsonb({0})", address.ParameterSqlExpression);
        Assert.Equal("json({0})", address.ColumnSqlExpression);
    }
#endif

    [Fact]
    public void Projection_OfNestedType_DeserializesWithoutExplicitConverter()
    {
        using TestDatabase db = SetupNestedDatabase();
        db.Table<PersonEntity>().Add(new PersonEntity
        {
            Id = 1,
            Person = new Person
            {
                Name = "Alice",
                Home = new Address { Street = "1 Oak", City = "Shelbyville" }
            }
        });

        Address result = db.Table<PersonEntity>()
            .Select(p => p.Person.Home)
            .First();

        Assert.Equal("1 Oak", result.Street);
        Assert.Equal("Shelbyville", result.City);
    }

    [Fact]
    public void Walk_TerminatesOnMutualCycle()
    {
        SQLiteOptionsBuilder builder = new(":memory:");
        builder.AddJsonContext(LoopJsonContext.Default);

        SQLiteOptions options = builder.Build();

        Assert.True(options.TypeConverters.ContainsKey(typeof(LoopA)));
        Assert.True(options.TypeConverters.ContainsKey(typeof(LoopB)));
    }

    [Fact]
    public void Walk_TerminatesOnSelfReference()
    {
        SQLiteOptionsBuilder builder = new(":memory:");
        builder.AddJsonContext(LoopJsonContext.Default);

        SQLiteOptions options = builder.Build();

        Assert.True(options.TypeConverters.ContainsKey(typeof(SelfLoop)));
    }

    [Fact]
    public void JsonObjectConverter_ToDatabase_WithNull_ReturnsNull()
    {
        SQLiteJsonObjectConverter converter = new(TestJsonContext.Default.Address, isJsonb: false);
        Assert.Null(converter.ToDatabase(null));
    }

    [Fact]
    public void JsonObjectConverter_FromDatabase_WithNonString_ReturnsNull()
    {
        SQLiteJsonObjectConverter converter = new(TestJsonContext.Default.Address, isJsonb: false);
        Assert.Null(converter.FromDatabase(null));
        Assert.Null(converter.FromDatabase(42));
    }

    [Fact]
    public void Cycle_RoundTrip_PreservesValues()
    {
        using TestDatabase db = new(b =>
        {
            b.AddJsonContext(LoopJsonContext.Default);
        });
        db.Table<LoopEntity>().Schema.CreateTable();
        db.Table<LoopEntity>().Add(new LoopEntity
        {
            Id = 1,
            Value = new LoopA
            {
                Name = "first",
                Next = new LoopB
                {
                    Label = "second",
                    Next = null
                }
            }
        });

        LoopEntity result = db.Table<LoopEntity>().First();

        Assert.Equal("first", result.Value.Name);
        Assert.Equal("second", result.Value.Next!.Label);
        Assert.Null(result.Value.Next.Next);
    }

    private static TestDatabase SetupNestedDatabase([CallerMemberName] string? methodName = null)
    {
        TestDatabase db = new(b => b.AddJsonContext(PersonRootJsonContext.Default), methodName);
        db.Table<PersonEntity>().Schema.CreateTable();
        return db;
    }
}

public class PersonEntity
{
    [Key]
    public required int Id { get; set; }

    public required Person Person { get; set; }
}

public class LoopEntity
{
    [Key]
    public required int Id { get; set; }

    public required LoopA Value { get; set; }
}

public class LoopA
{
    public string Name { get; set; } = "";
    public LoopB? Next { get; set; }
}

public class LoopB
{
    public string Label { get; set; } = "";
    public LoopA? Next { get; set; }
}

public class SelfLoop
{
    public string Name { get; set; } = "";
    public SelfLoop? Parent { get; set; }
}
