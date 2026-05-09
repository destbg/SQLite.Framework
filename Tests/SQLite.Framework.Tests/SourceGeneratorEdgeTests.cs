using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class SourceGeneratorEdgeTests
{
    [Fact]
    public void InitOnlyProperties_RoundTrip()
    {
        using TestDatabase db = new();
        db.Table<InitOnlyEntity>().Schema.CreateTable();

        db.Table<InitOnlyEntity>().AddRange([
            new InitOnlyEntity { Id = 1, Name = "first", Count = 10 },
            new InitOnlyEntity { Id = 2, Name = "second", Count = 20 },
        ]);

        List<InitOnlyEntity> rows = db.Table<InitOnlyEntity>().OrderBy(e => e.Id).ToList();

        Assert.Equal(2, rows.Count);
        Assert.Equal(1, rows[0].Id);
        Assert.Equal("first", rows[0].Name);
        Assert.Equal(10, rows[0].Count);
        Assert.Equal(2, rows[1].Id);
        Assert.Equal("second", rows[1].Name);
        Assert.Equal(20, rows[1].Count);
    }

    [Fact]
    public void RequiredMappedEntity_RoundTrip()
    {
        using TestDatabase db = new();
        db.Table<RequiredMappedEntity>().Schema.CreateTable();

        db.Table<RequiredMappedEntity>().AddRange([
            new RequiredMappedEntity { Id = 1, Name = "X", Value = 1.5 },
            new RequiredMappedEntity { Id = 2, Name = "Y", Value = 2.5 },
        ]);

        List<RequiredMappedEntity> rows = db.Table<RequiredMappedEntity>().OrderBy(e => e.Id).ToList();

        Assert.Equal(2, rows.Count);
        Assert.Equal("X", rows[0].Name);
        Assert.Equal(1.5, rows[0].Value);
        Assert.Equal("Y", rows[1].Name);
        Assert.Equal(2.5, rows[1].Value);
    }

    [Fact]
    public void RecordEntity_RoundTrip()
    {
        using TestDatabase db = new();
        db.Table<RecordEntity>().Schema.CreateTable();

        db.Table<RecordEntity>().AddRange([
            new RecordEntity { Id = 1, Name = "alpha" },
            new RecordEntity { Id = 2, Name = "beta" },
        ]);

        List<RecordEntity> rows = db.Table<RecordEntity>().OrderBy(e => e.Id).ToList();

        Assert.Equal(2, rows.Count);
        Assert.Equal("alpha", rows[0].Name);
        Assert.Equal("beta", rows[1].Name);
    }

    [Fact]
    public void Select_NestedTwoLevelAnonymous_Materializes()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "A", AuthorId = 7, Price = 9.5 },
        ]);

        var row = db.Table<Book>()
            .Select(b => new
            {
                b.Id,
                Inner = new { b.Title, b.AuthorId },
            })
            .Single();

        Assert.Equal(1, row.Id);
        Assert.Equal("A", row.Inner.Title);
        Assert.Equal(7, row.Inner.AuthorId);
    }

    [Fact]
    public void PrivateParameterlessCtor_RoundTrips()
    {
        using TestDatabase db = new();
        db.Table<PrivateCtorEntity>().Schema.CreateTable();

        db.Table<PrivateCtorEntity>().AddRange([
            new PrivateCtorEntity(1, "alpha"),
            new PrivateCtorEntity(2, "beta"),
        ]);

        List<PrivateCtorEntity> rows = db.Table<PrivateCtorEntity>().OrderBy(e => e.Id).ToList();

        Assert.Equal(2, rows.Count);
        Assert.Equal(1, rows[0].Id);
        Assert.Equal("alpha", rows[0].Name);
        Assert.Equal(2, rows[1].Id);
        Assert.Equal("beta", rows[1].Name);
    }

    [Fact]
    public void EntityWithComputedGetter_DoesNotPersistGetter()
    {
        using TestDatabase db = new();
        db.Table<EntityWithComputed>().Schema.CreateTable();

        db.Table<EntityWithComputed>().AddRange([
            new EntityWithComputed { Id = 1, FirstName = "Ada", LastName = "Lovelace" },
        ]);

        EntityWithComputed row = db.Table<EntityWithComputed>().Single();

        Assert.Equal("Ada", row.FirstName);
        Assert.Equal("Lovelace", row.LastName);
        Assert.Equal("Ada Lovelace", row.FullName);
    }

    [Fact]
    public void NestedEntityType_RoundTrips()
    {
        using TestDatabase db = new();
        db.Table<OuterContainer.NestedEntity>().Schema.CreateTable();

        db.Table<OuterContainer.NestedEntity>().AddRange([
            new OuterContainer.NestedEntity { Id = 1, Label = "alpha" },
        ]);

        OuterContainer.NestedEntity row = db.Table<OuterContainer.NestedEntity>().Single();

        Assert.Equal(1, row.Id);
        Assert.Equal("alpha", row.Label);
    }

    [Fact]
    public void Select_AnonymousFromJoin_BindsBothSides()
    {
        using TestDatabase db = new();
        db.Table<Author>().Schema.CreateTable();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Author>().AddRange([
            new Author { Id = 1, Name = "Asimov", Email = "a@x", BirthDate = new DateTime(2000, 1, 1) },
        ]);
        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "Foundation", AuthorId = 1, Price = 9.5 },
        ]);

        var rows = (
            from b in db.Table<Book>()
            join a in db.Table<Author>() on b.AuthorId equals a.Id
            select new { Book = b.Title, Author = a.Name, b.Price }
        ).ToList();

        Assert.Single(rows);
        Assert.Equal("Foundation", rows[0].Book);
        Assert.Equal("Asimov", rows[0].Author);
        Assert.Equal(9.5, rows[0].Price);
    }

    [Fact]
    public void Select_AnonymousWithSpread_FromEntity()
    {
        using TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();

        db.Table<Book>().AddRange([
            new Book { Id = 1, Title = "T", AuthorId = 5, Price = 1.5 },
        ]);

        var row = db.Table<Book>().Select(b => new { Entity = b, Doubled = b.Price * 2 }).Single();

        Assert.Equal(1, row.Entity.Id);
        Assert.Equal("T", row.Entity.Title);
        Assert.Equal(5, row.Entity.AuthorId);
        Assert.Equal(1.5, row.Entity.Price);
        Assert.Equal(3.0, row.Doubled);
    }

    [Fact]
    public void Select_AnonymousWithEveryPrimitive()
    {
        using TestDatabase db = new();
        db.Table<NumericType>().Schema.CreateTable();

        db.Table<NumericType>().AddRange([
            new NumericType
            {
                Id = 1,
                ByteValue = 1,
                SByteValue = -1,
                ShortValue = 2,
                UShortValue = 3,
                IntValue = 4,
                UIntValue = 5,
                LongValue = 6,
                ULongValue = 7,
                FloatValue = 1.5f,
                DoubleValue = 2.5,
                DecimalValue = 3.5m,
                CharValue = 'x',
            },
        ]);

        var row = db.Table<NumericType>().Select(n => new
        {
            n.Id,
            n.ByteValue,
            n.SByteValue,
            n.ShortValue,
            n.UShortValue,
            n.IntValue,
            n.UIntValue,
            n.LongValue,
            n.ULongValue,
            n.FloatValue,
            n.DoubleValue,
            n.DecimalValue,
            n.CharValue,
        }).Single();

        Assert.Equal(1, row.Id);
        Assert.Equal((byte)1, row.ByteValue);
        Assert.Equal((sbyte)-1, row.SByteValue);
        Assert.Equal((short)2, row.ShortValue);
        Assert.Equal((ushort)3, row.UShortValue);
        Assert.Equal(4, row.IntValue);
        Assert.Equal((uint)5, row.UIntValue);
        Assert.Equal((long)6, row.LongValue);
        Assert.Equal((ulong)7, row.ULongValue);
        Assert.Equal(1.5f, row.FloatValue);
        Assert.Equal(2.5, row.DoubleValue);
        Assert.Equal(3.5m, row.DecimalValue);
        Assert.Equal('x', row.CharValue);
    }
}
