using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class AsyncAggregateOverloadTests
{
    private static TestDatabase NewDb()
    {
        TestDatabase db = new();
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().AddRange(new[]
        {
            new Book { Id = 1, Title = "A", AuthorId = 1, Price = 10 },
            new Book { Id = 2, Title = "B", AuthorId = 2, Price = 20 },
        });
        return db;
    }

    [Fact]
    public async Task SumAsync_Int_OverSqlite()
    {
        using TestDatabase db = NewDb();
        int total = await db.Table<Book>().Select(b => b.Id).SumAsync();
        Assert.Equal(3, total);
    }

    [Fact]
    public async Task SumAsync_IntNullable_OverSqlite()
    {
        using TestDatabase db = NewDb();
        int? total = await db.Table<Book>().Select(b => (int?)b.Id).SumAsync();
        Assert.Equal(3, total);
    }

    [Fact]
    public async Task SumAsync_Long_OverSqlite()
    {
        using TestDatabase db = NewDb();
        long total = await db.Table<Book>().Select(b => (long)b.Id).SumAsync();
        Assert.Equal(3L, total);
    }

    [Fact]
    public async Task SumAsync_LongNullable_OverSqlite()
    {
        using TestDatabase db = NewDb();
        long? total = await db.Table<Book>().Select(b => (long?)b.Id).SumAsync();
        Assert.Equal(3L, total);
    }

    [Fact]
    public async Task SumAsync_Float_OverSqlite()
    {
        using TestDatabase db = NewDb();
        float total = await db.Table<Book>().Select(b => (float)b.Price).SumAsync();
        Assert.Equal(30f, total);
    }

    [Fact]
    public async Task SumAsync_FloatNullable_OverSqlite()
    {
        using TestDatabase db = NewDb();
        float? total = await db.Table<Book>().Select(b => (float?)b.Price).SumAsync();
        Assert.Equal(30f, total);
    }

    [Fact]
    public async Task SumAsync_Double_OverSqlite()
    {
        using TestDatabase db = NewDb();
        double total = await db.Table<Book>().Select(b => b.Price).SumAsync();
        Assert.Equal(30d, total);
    }

    [Fact]
    public async Task SumAsync_DoubleNullable_OverSqlite()
    {
        using TestDatabase db = NewDb();
        double? total = await db.Table<Book>().Select(b => (double?)b.Price).SumAsync();
        Assert.Equal(30d, total);
    }

    [Fact]
    public async Task SumAsync_Decimal_OverSqlite()
    {
        using TestDatabase db = NewDb();
        decimal total = await db.Table<Book>().Select(b => (decimal)b.Price).SumAsync();
        Assert.Equal(30m, total);
    }

    [Fact]
    public async Task SumAsync_DecimalNullable_OverSqlite()
    {
        using TestDatabase db = NewDb();
        decimal? total = await db.Table<Book>().Select(b => (decimal?)b.Price).SumAsync();
        Assert.Equal(30m, total);
    }

    [Fact]
    public async Task SumAsync_IntSelector_OverSqlite()
    {
        using TestDatabase db = NewDb();
        int total = await db.Table<Book>().SumAsync(b => b.Id);
        Assert.Equal(3, total);
    }

    [Fact]
    public async Task SumAsync_IntNullSelector_OverSqlite()
    {
        using TestDatabase db = NewDb();
        int? total = await db.Table<Book>().SumAsync(b => (int?)b.Id);
        Assert.Equal(3, total);
    }

    [Fact]
    public async Task SumAsync_LongSelector_OverSqlite()
    {
        using TestDatabase db = NewDb();
        long total = await db.Table<Book>().SumAsync(b => (long)b.Id);
        Assert.Equal(3L, total);
    }

    [Fact]
    public async Task SumAsync_LongNullSelector_OverSqlite()
    {
        using TestDatabase db = NewDb();
        long? total = await db.Table<Book>().SumAsync(b => (long?)b.Id);
        Assert.Equal(3L, total);
    }

    [Fact]
    public async Task SumAsync_FloatSelector_OverSqlite()
    {
        using TestDatabase db = NewDb();
        float total = await db.Table<Book>().SumAsync(b => (float)b.Price);
        Assert.Equal(30f, total);
    }

    [Fact]
    public async Task SumAsync_FloatNullSelector_OverSqlite()
    {
        using TestDatabase db = NewDb();
        float? total = await db.Table<Book>().SumAsync(b => (float?)b.Price);
        Assert.Equal(30f, total);
    }

    [Fact]
    public async Task SumAsync_DoubleNullSelector_OverSqlite()
    {
        using TestDatabase db = NewDb();
        double? total = await db.Table<Book>().SumAsync(b => (double?)b.Price);
        Assert.Equal(30d, total);
    }

    [Fact]
    public async Task SumAsync_DecimalSelector_OverSqlite()
    {
        using TestDatabase db = NewDb();
        decimal total = await db.Table<Book>().SumAsync(b => (decimal)b.Price);
        Assert.Equal(30m, total);
    }

    [Fact]
    public async Task SumAsync_DecimalNullSelector_OverSqlite()
    {
        using TestDatabase db = NewDb();
        decimal? total = await db.Table<Book>().SumAsync(b => (decimal?)b.Price);
        Assert.Equal(30m, total);
    }

    [Fact]
    public async Task AverageAsync_Int_OverSqlite()
    {
        using TestDatabase db = NewDb();
        double avg = await db.Table<Book>().Select(b => b.Id).AverageAsync();
        Assert.Equal(1.5d, avg);
    }

    [Fact]
    public async Task AverageAsync_IntNullable_OverSqlite()
    {
        using TestDatabase db = NewDb();
        double? avg = await db.Table<Book>().Select(b => (int?)b.Id).AverageAsync();
        Assert.Equal(1.5d, avg);
    }

    [Fact]
    public async Task AverageAsync_Long_OverSqlite()
    {
        using TestDatabase db = NewDb();
        double avg = await db.Table<Book>().Select(b => (long)b.Id).AverageAsync();
        Assert.Equal(1.5d, avg);
    }

    [Fact]
    public async Task AverageAsync_LongNullable_OverSqlite()
    {
        using TestDatabase db = NewDb();
        double? avg = await db.Table<Book>().Select(b => (long?)b.Id).AverageAsync();
        Assert.Equal(1.5d, avg);
    }

    [Fact]
    public async Task AverageAsync_Float_OverSqlite()
    {
        using TestDatabase db = NewDb();
        float avg = await db.Table<Book>().Select(b => (float)b.Price).AverageAsync();
        Assert.Equal(15f, avg);
    }

    [Fact]
    public async Task AverageAsync_FloatNullable_OverSqlite()
    {
        using TestDatabase db = NewDb();
        float? avg = await db.Table<Book>().Select(b => (float?)b.Price).AverageAsync();
        Assert.Equal(15f, avg);
    }

    [Fact]
    public async Task AverageAsync_Double_OverSqlite()
    {
        using TestDatabase db = NewDb();
        double avg = await db.Table<Book>().Select(b => b.Price).AverageAsync();
        Assert.Equal(15d, avg);
    }

    [Fact]
    public async Task AverageAsync_DoubleNullable_OverSqlite()
    {
        using TestDatabase db = NewDb();
        double? avg = await db.Table<Book>().Select(b => (double?)b.Price).AverageAsync();
        Assert.Equal(15d, avg);
    }

    [Fact]
    public async Task AverageAsync_Decimal_OverSqlite()
    {
        using TestDatabase db = NewDb();
        decimal avg = await db.Table<Book>().Select(b => (decimal)b.Price).AverageAsync();
        Assert.Equal(15m, avg);
    }

    [Fact]
    public async Task AverageAsync_DecimalNullable_OverSqlite()
    {
        using TestDatabase db = NewDb();
        decimal? avg = await db.Table<Book>().Select(b => (decimal?)b.Price).AverageAsync();
        Assert.Equal(15m, avg);
    }

    [Fact]
    public async Task AverageAsync_IntSelector_OverSqlite()
    {
        using TestDatabase db = NewDb();
        double avg = await db.Table<Book>().AverageAsync(b => b.Id);
        Assert.Equal(1.5d, avg);
    }

    [Fact]
    public async Task AverageAsync_IntNullSelector_OverSqlite()
    {
        using TestDatabase db = NewDb();
        double? avg = await db.Table<Book>().AverageAsync(b => (int?)b.Id);
        Assert.Equal(1.5d, avg);
    }

    [Fact]
    public async Task AverageAsync_FloatSelector_OverSqlite()
    {
        using TestDatabase db = NewDb();
        float avg = await db.Table<Book>().AverageAsync(b => (float)b.Price);
        Assert.Equal(15f, avg);
    }

    [Fact]
    public async Task AverageAsync_FloatNullSelector_OverSqlite()
    {
        using TestDatabase db = NewDb();
        float? avg = await db.Table<Book>().AverageAsync(b => (float?)b.Price);
        Assert.Equal(15f, avg);
    }

    [Fact]
    public async Task AverageAsync_LongSelector_OverSqlite()
    {
        using TestDatabase db = NewDb();
        double avg = await db.Table<Book>().AverageAsync(b => (long)b.Id);
        Assert.Equal(1.5d, avg);
    }

    [Fact]
    public async Task AverageAsync_LongNullSelector_OverSqlite()
    {
        using TestDatabase db = NewDb();
        double? avg = await db.Table<Book>().AverageAsync(b => (long?)b.Id);
        Assert.Equal(1.5d, avg);
    }

    [Fact]
    public async Task AverageAsync_DoubleNullSelector_OverSqlite()
    {
        using TestDatabase db = NewDb();
        double? avg = await db.Table<Book>().AverageAsync(b => (double?)b.Price);
        Assert.Equal(15d, avg);
    }

    [Fact]
    public async Task AverageAsync_DecimalSelector_OverSqlite()
    {
        using TestDatabase db = NewDb();
        decimal avg = await db.Table<Book>().AverageAsync(b => (decimal)b.Price);
        Assert.Equal(15m, avg);
    }

    [Fact]
    public async Task AverageAsync_DecimalNullSelector_OverSqlite()
    {
        using TestDatabase db = NewDb();
        decimal? avg = await db.Table<Book>().AverageAsync(b => (decimal?)b.Price);
        Assert.Equal(15m, avg);
    }

    [Fact]
    public async Task MinAsync_NoSelector_OverSqlite()
    {
        using TestDatabase db = NewDb();
        int min = await db.Table<Book>().Select(b => b.Id).MinAsync();
        Assert.Equal(1, min);
    }

    [Fact]
    public async Task MaxAsync_NoSelector_OverSqlite()
    {
        using TestDatabase db = NewDb();
        int max = await db.Table<Book>().Select(b => b.Id).MaxAsync();
        Assert.Equal(2, max);
    }

    [Fact]
    public async Task ToLookupAsync_OverSqlite()
    {
        using TestDatabase db = NewDb();
        ILookup<int, string> lookup = await ((IEnumerable<Book>)db.Table<Book>()).ToLookupAsync(b => b.AuthorId, b => b.Title);
        Assert.Equal(2, lookup.Count);
    }

    [Fact]
    public async Task FirstOrDefaultAsync_DefaultValue_OverSqlite()
    {
        using TestDatabase db = NewDb();
        Book def = new() { Id = -1, Title = "x", AuthorId = 0, Price = 0 };
        Book first = await db.Table<Book>().FirstOrDefaultAsync(def);
        Assert.Equal(1, first.Id);
    }

    [Fact]
    public async Task FirstOrDefaultAsync_DefaultValue_OnEmpty_ReturnsDefault()
    {
        using TestDatabase db = NewDb();
        Book def = new() { Id = -1, Title = "x", AuthorId = 0, Price = 0 };
        Book first = await db.Table<Book>().Where(b => b.Id == -100).FirstOrDefaultAsync(def);
        Assert.Equal(-1, first.Id);
    }

    [Fact]
    public async Task SingleOrDefaultAsync_DefaultValue_OverSqlite()
    {
        using TestDatabase db = NewDb();
        Book def = new() { Id = -1, Title = "x", AuthorId = 0, Price = 0 };
        Book single = await db.Table<Book>().Where(b => b.Id == 1).SingleOrDefaultAsync(def);
        Assert.Equal(1, single.Id);
    }

    [Fact]
    public async Task SingleOrDefaultAsync_DefaultValue_OnEmpty_ReturnsDefault()
    {
        using TestDatabase db = NewDb();
        Book def = new() { Id = -1, Title = "x", AuthorId = 0, Price = 0 };
        Book single = await db.Table<Book>().Where(b => b.Id == -100).SingleOrDefaultAsync(def);
        Assert.Equal(-1, single.Id);
    }
}
