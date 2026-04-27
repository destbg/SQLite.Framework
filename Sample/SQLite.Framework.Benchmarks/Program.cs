using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using SQLite.Framework;
using SQLite.Framework.Generated;
using SQLite.Framework.JsonB;

BenchmarkRunner.Run<MaterializerBenchmarks>(
    DefaultConfig.Instance.AddJob(Job.Default.WithWarmupCount(3).WithIterationCount(8)));

[JsonSerializable(typeof(List<string>))]
internal partial class BenchJsonContext : JsonSerializerContext;

[MemoryDiagnoser]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class MaterializerBenchmarks
{
    private SQLiteDatabase db = null!;

    [Params(false, true)]
    public bool UseSourceGenerator;

    [GlobalSetup]
    public void Setup()
    {
        SQLitePCL.Batteries_V2.Init();

        string path = $"bench_{(UseSourceGenerator ? "sg" : "rt")}.db3";
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        SQLiteOptionsBuilder builder = new(path);
        builder.AddJson();
        builder.TypeConverters[typeof(List<string>)] =
            new SQLiteJsonConverter<List<string>>(BenchJsonContext.Default.ListString);

        if (UseSourceGenerator)
        {
            builder.UseGeneratedMaterializers();
        }

        db = new SQLiteDatabase(builder.Build());
        db.Schema.CreateTable<Author>();
        db.Schema.CreateTable<Publisher>();
        db.Schema.CreateTable<Book>();
        db.Schema.CreateTable<Review>();

        Seed();
        Warmup();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        db.Dispose();
    }

    private void Seed()
    {
        Random rng = new(42);
        string[] countries = ["USA", "UK", "Germany", "Japan", "Brazil"];
        string[] tagPool = ["fiction", "non-fiction", "bestseller", "classic", "thriller", "romance", "history", "sci-fi"];

        List<Author> authors = [];
        for (int i = 1; i <= 100; i++)
        {
            authors.Add(new Author { Id = i, Name = $"Author {i}", Country = countries[i % countries.Length] });
        }

        List<Publisher> publishers = [];
        for (int i = 1; i <= 50; i++)
        {
            publishers.Add(new Publisher { Id = i, Name = (i % 3 == 0 ? "P-" : "X-") + $"Publisher {i}" });
        }

        List<Book> books = [];
        for (int i = 1; i <= 5000; i++)
        {
            int tagCount = rng.Next(1, 4);
            HashSet<string> tags = [];
            while (tags.Count < tagCount)
            {
                tags.Add(tagPool[rng.Next(tagPool.Length)]);
            }
            books.Add(new Book
            {
                Id = i,
                Title = $"Book {i}",
                AuthorId = (i % 100) + 1,
                PublisherId = (i % 50) + 1,
                Price = 5.0 + rng.NextDouble() * 50.0,
                Tags = [.. tags],
            });
        }

        List<Review> reviews = [];
        int reviewId = 1;
        foreach (Book b in books)
        {
            int n = rng.Next(2, 6);
            for (int j = 0; j < n; j++)
            {
                reviews.Add(new Review { Id = reviewId++, BookId = b.Id, Rating = rng.Next(1, 6) });
            }
        }

        using SQLiteTransaction tx = db.BeginTransaction();
        db.Table<Author>().AddRange(authors);
        db.Table<Publisher>().AddRange(publishers);
        db.Table<Book>().AddRange(books);
        db.Table<Review>().AddRange(reviews);
        tx.Commit();
    }

    private void Warmup()
    {
        _ = SimpleScalar();
        _ = SimpleList().Count;
        _ = ComplexScalar();
        _ = ComplexList().Count;
    }

    [Benchmark, BenchmarkCategory("SimpleScalar")]
    public int SimpleScalar()
    {
        return db.Table<Book>().Count(b => b.AuthorId == 5);
    }

    [Benchmark, BenchmarkCategory("SimpleList")]
    public List<Book> SimpleList()
    {
        return db.Table<Book>().Where(b => b.AuthorId == 5).ToList();
    }

    [Benchmark, BenchmarkCategory("ComplexScalar")]
    public int ComplexScalar()
    {
        return (
            from b in db.Table<Book>()
            join a in db.Table<Author>() on b.AuthorId equals a.Id
            join p in db.Table<Publisher>() on b.PublisherId equals p.Id
            where a.Country == "USA"
            where p.Name.StartsWith("P-")
            where b.Tags.Contains("fiction")
            where db.Table<Review>().Where(r => r.BookId == b.Id && r.Rating >= 4).Count() > 0
            select b.Id
        ).Count();
    }

    [Benchmark, BenchmarkCategory("ComplexList")]
    public List<ComplexRow> ComplexList()
    {
        return (
            from b in db.Table<Book>()
            join a in db.Table<Author>() on b.AuthorId equals a.Id
            join p in db.Table<Publisher>() on b.PublisherId equals p.Id
            where a.Country == "USA"
            where p.Name.StartsWith("P-")
            where b.Tags.Contains("fiction")
            where db.Table<Review>().Where(r => r.BookId == b.Id && r.Rating >= 4).Count() > 0
            select new ComplexRow
            {
                Title = b.Title,
                AuthorName = a.Name,
                PublisherName = p.Name,
                ReviewCount = db.Table<Review>().Where(r => r.BookId == b.Id).Count(),
                HighRatingCount = db.Table<Review>().Where(r => r.BookId == b.Id && r.Rating >= 4).Count(),
                TagCount = b.Tags.Count(),
            }
        ).ToList();
    }
}

public class Author
{
    [Key]
    public int Id { get; set; }

    public required string Name { get; set; }

    public required string Country { get; set; }
}

public class Publisher
{
    [Key]
    public int Id { get; set; }

    public required string Name { get; set; }
}

public class Book
{
    [Key]
    public int Id { get; set; }

    public required string Title { get; set; }

    public required int AuthorId { get; set; }

    public required int PublisherId { get; set; }

    public required double Price { get; set; }

    public required List<string> Tags { get; set; }
}

public class Review
{
    [Key]
    public int Id { get; set; }

    public required int BookId { get; set; }

    public required int Rating { get; set; }
}

public class ComplexRow
{
    public required string Title { get; set; }

    public required string AuthorName { get; set; }

    public required string PublisherName { get; set; }

    public required int ReviewCount { get; set; }

    public required int HighRatingCount { get; set; }

    public required int TagCount { get; set; }
}
