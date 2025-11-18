using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class NumericTypeTests
{
    [Fact]
    public void IntComparison()
    {
        using TestDatabase db = new();

        db.Table<NumericType>().CreateTable();
        db.Table<NumericType>().AddRange(new[]
        {
            new NumericType
            {
                Id = 1,
                IntValue = 100,
                LongValue = 1000,
                ShortValue = 10,
                ByteValue = 5,
                SByteValue = -5,
                UIntValue = 200,
                ULongValue = 2000,
                UShortValue = 20,
                DoubleValue = 100.5,
                FloatValue = 50.25f,
                DecimalValue = 75.75m,
                CharValue = 'A',
                BlobValue = new byte[] { 1, 2, 3 }
            },
            new NumericType
            {
                Id = 2,
                IntValue = 200,
                LongValue = 2000,
                ShortValue = 20,
                ByteValue = 10,
                SByteValue = -10,
                UIntValue = 400,
                ULongValue = 4000,
                UShortValue = 40,
                DoubleValue = 200.5,
                FloatValue = 100.25f,
                DecimalValue = 150.75m,
                CharValue = 'B',
                BlobValue = new byte[] { 4, 5, 6 }
            }
        });

        IQueryable<NumericType> query =
            from n in db.Table<NumericType>()
            where n.IntValue > 150
            select n;

        List<NumericType> results = query.ToList();
        Assert.Single(results);
        Assert.Equal(200, results[0].IntValue);
    }

    [Fact]
    public void LongComparison()
    {
        using TestDatabase db = new();

        db.Table<NumericType>().CreateTable();
        db.Table<NumericType>().AddRange(new[]
        {
            new NumericType
            {
                Id = 1,
                IntValue = 100,
                LongValue = 1000,
                ShortValue = 10,
                ByteValue = 5,
                SByteValue = -5,
                UIntValue = 200,
                ULongValue = 2000,
                UShortValue = 20,
                DoubleValue = 100.5,
                FloatValue = 50.25f,
                DecimalValue = 75.75m,
                CharValue = 'A'
            },
            new NumericType
            {
                Id = 2,
                IntValue = 200,
                LongValue = 5000,
                ShortValue = 20,
                ByteValue = 10,
                SByteValue = -10,
                UIntValue = 400,
                ULongValue = 4000,
                UShortValue = 40,
                DoubleValue = 200.5,
                FloatValue = 100.25f,
                DecimalValue = 150.75m,
                CharValue = 'B'
            }
        });

        IQueryable<NumericType> query =
            from n in db.Table<NumericType>()
            where n.LongValue > 3000
            select n;

        List<NumericType> results = query.ToList();
        Assert.Single(results);
        Assert.Equal(5000, results[0].LongValue);
    }

    [Fact]
    public void DoubleComparison()
    {
        using TestDatabase db = new();

        db.Table<NumericType>().CreateTable();
        db.Table<NumericType>().AddRange(new[]
        {
            new NumericType
            {
                Id = 1,
                IntValue = 100,
                LongValue = 1000,
                ShortValue = 10,
                ByteValue = 5,
                SByteValue = -5,
                UIntValue = 200,
                ULongValue = 2000,
                UShortValue = 20,
                DoubleValue = 100.5,
                FloatValue = 50.25f,
                DecimalValue = 75.75m,
                CharValue = 'A'
            },
            new NumericType
            {
                Id = 2,
                IntValue = 200,
                LongValue = 2000,
                ShortValue = 20,
                ByteValue = 10,
                SByteValue = -10,
                UIntValue = 400,
                ULongValue = 4000,
                UShortValue = 40,
                DoubleValue = 300.75,
                FloatValue = 100.25f,
                DecimalValue = 150.75m,
                CharValue = 'B'
            }
        });

        IQueryable<NumericType> query =
            from n in db.Table<NumericType>()
            where n.DoubleValue > 200
            select n;

        List<NumericType> results = query.ToList();
        Assert.Single(results);
        Assert.Equal(300.75, results[0].DoubleValue);
    }

    [Fact]
    public void DecimalComparison()
    {
        using TestDatabase db = new();

        db.Table<NumericType>().CreateTable();
        db.Table<NumericType>().AddRange(new[]
        {
            new NumericType
            {
                Id = 1,
                IntValue = 100,
                LongValue = 1000,
                ShortValue = 10,
                ByteValue = 5,
                SByteValue = -5,
                UIntValue = 200,
                ULongValue = 2000,
                UShortValue = 20,
                DoubleValue = 100.5,
                FloatValue = 50.25f,
                DecimalValue = 75.75m,
                CharValue = 'A'
            },
            new NumericType
            {
                Id = 2,
                IntValue = 200,
                LongValue = 2000,
                ShortValue = 20,
                ByteValue = 10,
                SByteValue = -10,
                UIntValue = 400,
                ULongValue = 4000,
                UShortValue = 40,
                DoubleValue = 200.5,
                FloatValue = 100.25f,
                DecimalValue = 200.25m,
                CharValue = 'B'
            }
        });

        IQueryable<NumericType> query =
            from n in db.Table<NumericType>()
            where n.DecimalValue > 100m
            select n;

        List<NumericType> results = query.ToList();
        Assert.Single(results);
        Assert.Equal(200.25m, results[0].DecimalValue);
    }

    [Fact]
    public void ByteArrayComparison()
    {
        using TestDatabase db = new();

        db.Table<NumericType>().CreateTable();
        db.Table<NumericType>().AddRange(new[]
        {
            new NumericType
            {
                Id = 1,
                IntValue = 100,
                LongValue = 1000,
                ShortValue = 10,
                ByteValue = 5,
                SByteValue = -5,
                UIntValue = 200,
                ULongValue = 2000,
                UShortValue = 20,
                DoubleValue = 100.5,
                FloatValue = 50.25f,
                DecimalValue = 75.75m,
                CharValue = 'A',
                BlobValue = new byte[] { 1, 2, 3 }
            },
            new NumericType
            {
                Id = 2,
                IntValue = 200,
                LongValue = 2000,
                ShortValue = 20,
                ByteValue = 10,
                SByteValue = -10,
                UIntValue = 400,
                ULongValue = 4000,
                UShortValue = 40,
                DoubleValue = 200.5,
                FloatValue = 100.25f,
                DecimalValue = 150.75m,
                CharValue = 'B',
                BlobValue = new byte[] { 4, 5, 6, 7 }
            }
        });

        IQueryable<NumericType> query =
            from n in db.Table<NumericType>()
            where n.BlobValue != null
            select n;

        List<NumericType> results = query.ToList();
        Assert.Equal(2, results.Count);
        Assert.Equal(new byte[] { 1, 2, 3 }, results[0].BlobValue);
        Assert.Equal(new byte[] { 4, 5, 6, 7 }, results[1].BlobValue);
    }

    [Fact]
    public void NullableIntComparison()
    {
        using TestDatabase db = new();

        db.Table<NumericType>().CreateTable();
        db.Table<NumericType>().AddRange(new[]
        {
            new NumericType
            {
                Id = 1,
                IntValue = 100,
                LongValue = 1000,
                ShortValue = 10,
                ByteValue = 5,
                SByteValue = -5,
                UIntValue = 200,
                ULongValue = 2000,
                UShortValue = 20,
                DoubleValue = 100.5,
                FloatValue = 50.25f,
                DecimalValue = 75.75m,
                CharValue = 'A'
            }
        });

        int? nullableValue = 100;
        IQueryable<NumericType> query =
            from n in db.Table<NumericType>()
            where n.IntValue == nullableValue
            select n;

        List<NumericType> results = query.ToList();
        Assert.Single(results);
        Assert.Equal(100, results[0].IntValue);
    }

    [Fact]
    public void MixedNumericOperations()
    {
        using TestDatabase db = new();

        db.Table<NumericType>().CreateTable();
        db.Table<NumericType>().AddRange(new[]
        {
            new NumericType
            {
                Id = 1,
                IntValue = 100,
                LongValue = 1000,
                ShortValue = 10,
                ByteValue = 5,
                SByteValue = -5,
                UIntValue = 200,
                ULongValue = 2000,
                UShortValue = 20,
                DoubleValue = 100.5,
                FloatValue = 50.25f,
                DecimalValue = 75.75m,
                CharValue = 'A'
            },
            new NumericType
            {
                Id = 2,
                IntValue = 50,
                LongValue = 500,
                ShortValue = 5,
                ByteValue = 2,
                SByteValue = -2,
                UIntValue = 100,
                ULongValue = 1000,
                UShortValue = 10,
                DoubleValue = 50.25,
                FloatValue = 25.125f,
                DecimalValue = 37.875m,
                CharValue = 'B'
            }
        });

        IQueryable<NumericType> query =
            from n in db.Table<NumericType>()
            where n.IntValue + n.ShortValue > 100
            select n;

        List<NumericType> results = query.ToList();
        Assert.Single(results);
        Assert.Equal(100, results[0].IntValue);
        Assert.Equal(10, results[0].ShortValue);
    }

    [Fact]
    public void IntParse()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();
        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "123", AuthorId = 1, Price = 10.0 },
            new Book { Id = 2, Title = "456", AuthorId = 2, Price = 20.0 },
            new Book { Id = 3, Title = "789", AuthorId = 3, Price = 30.0 }
        });

        IQueryable<Book> query =
            from b in db.Table<Book>()
            where int.Parse(b.Title) > 200
            select b;

        List<Book> results = query.ToList();
        Assert.Equal(2, results.Count);
        Assert.Equal("456", results[0].Title);
        Assert.Equal("789", results[1].Title);
    }

    [Fact]
    public void DoubleParse()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();
        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "10.5", AuthorId = 1, Price = 10.0 },
            new Book { Id = 2, Title = "20.75", AuthorId = 2, Price = 20.0 },
            new Book { Id = 3, Title = "5.25", AuthorId = 3, Price = 30.0 }
        });

        IQueryable<Book> query =
            from b in db.Table<Book>()
            where double.Parse(b.Title) > 15.0
            select b;

        List<Book> results = query.ToList();
        Assert.Single(results);
        Assert.Equal("20.75", results[0].Title);
    }

    [Fact]
    public void DoubleToString()
    {
        using TestDatabase db = new();

        db.Table<Book>().CreateTable();
        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "Book 1", AuthorId = 1, Price = 10.5 },
            new Book { Id = 2, Title = "Book 2", AuthorId = 2, Price = 20.75 },
            new Book { Id = 3, Title = "Book 3", AuthorId = 3, Price = 30.25 }
        });

        var query =
            from b in db.Table<Book>()
            where b.Price > 15.0
            select new { b.Id, PriceString = b.Price.ToString() };

        var results = query.ToList();
        Assert.Equal(2, results.Count);
        Assert.Equal("20.75", results[0].PriceString);
        Assert.Equal("30.25", results[1].PriceString);
    }
}
