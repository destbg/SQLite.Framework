using System.ComponentModel.DataAnnotations;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using Microsoft.EntityFrameworkCore;
using SqliteNet = SQLite;
using SQLite.Framework;
using SQLite.Framework.Generated;

SQLitePCL.Batteries_V2.Init();

BenchmarkRunner.Run(new[]
{
    typeof(ReadBenchmarks),
    typeof(InsertBenchmarks),
    typeof(UpdateBenchmarks),
    typeof(JoinBenchmarks),
}, DefaultConfig.Instance.AddJob(Job.Default.WithWarmupCount(3).WithIterationCount(8)));

internal static class BenchHelpers
{
    public const int RowsPerQuery = 100;

    static BenchHelpers()
    {
        SQLitePCL.Batteries_V2.Init();
    }

    public static string NewDb(string label)
    {
        string path = Path.Combine(Path.GetTempPath(), $"orm-bench-{label}-{Guid.NewGuid():N}.db");
        TryDelete(path);
        return path;
    }

    public static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch { }
    }

    public static void SeedFramework(SQLiteDatabase db)
    {
        for (int i = 0; i < RowsPerQuery; i++)
        {
            db.Table<Book>().Add(NewBook(i));
        }
    }

    public static void SeedSqliteNet(SqliteNet.SQLiteConnection conn)
    {
        for (int i = 0; i < RowsPerQuery; i++)
        {
            conn.Insert(NewBook(i));
        }
    }

    public static void SeedEf(BookContext ctx)
    {
        for (int i = 0; i < RowsPerQuery; i++)
        {
            ctx.Books.Add(NewBook(i));
        }
        ctx.SaveChanges();
    }

    public static Book NewBook(int i) => new()
    {
        Id = i + 1,
        Title = $"Book {i}",
        AuthorId = 5,
        PublisherId = (i % 7) + 1,
        Price = 9.99 + i,
    };

    public static List<Book> NewBooks(int count, int idOffset = 0)
    {
        List<Book> books = new(count);
        for (int i = 0; i < count; i++)
        {
            Book b = NewBook(i);
            b.Id = i + 1 + idOffset;
            books.Add(b);
        }
        return books;
    }
}

[MemoryDiagnoser]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class ReadBenchmarks
{
    private SQLiteDatabase frameworkDb = null!;
    private SQLiteDatabase frameworkDbGen = null!;
    private SqliteNet.SQLiteConnection sqliteNet = null!;
    private BookContext ef = null!;
    private string a = null!, b = null!, c = null!, d = null!;

    [GlobalSetup]
    public void Setup()
    {
        _ = BenchHelpers.RowsPerQuery;
        a = BenchHelpers.NewDb("read-fw");
        b = BenchHelpers.NewDb("read-fwg");
        c = BenchHelpers.NewDb("read-snet");
        d = BenchHelpers.NewDb("read-ef");

        frameworkDb = new SQLiteDatabase(new SQLiteOptionsBuilder(a).Build());
        frameworkDb.Schema.CreateTable<Book>();
        BenchHelpers.SeedFramework(frameworkDb);

        frameworkDbGen = new SQLiteDatabase(new SQLiteOptionsBuilder(b).UseGeneratedMaterializers().Build());
        frameworkDbGen.Schema.CreateTable<Book>();
        BenchHelpers.SeedFramework(frameworkDbGen);

        sqliteNet = new SqliteNet.SQLiteConnection(c);
        sqliteNet.CreateTable<Book>();
        BenchHelpers.SeedSqliteNet(sqliteNet);

        ef = new BookContext(d);
        ef.Database.EnsureCreated();
        BenchHelpers.SeedEf(ef);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        frameworkDb.Dispose();
        frameworkDbGen.Dispose();
        sqliteNet.Dispose();
        ef.Dispose();
        BenchHelpers.TryDelete(a);
        BenchHelpers.TryDelete(b);
        BenchHelpers.TryDelete(c);
        BenchHelpers.TryDelete(d);
    }

    [Benchmark(Baseline = true), BenchmarkCategory("List")]
    public List<Book> Framework_List() =>
        frameworkDb.Table<Book>().Where(b => b.AuthorId == 5).ToList();

    [Benchmark, BenchmarkCategory("List")]
    public List<Book> FrameworkGen_List() =>
        frameworkDbGen.Table<Book>().Where(b => b.AuthorId == 5).ToList();

    [Benchmark, BenchmarkCategory("List")]
    public List<Book> SqliteNet_List() =>
        sqliteNet.Table<Book>().Where(b => b.AuthorId == 5).ToList();

    [Benchmark, BenchmarkCategory("List")]
    public List<Book> Ef_List() =>
        ef.Books.AsNoTracking().Where(b => b.AuthorId == 5).ToList();

    [Benchmark(Baseline = true), BenchmarkCategory("Scalar")]
    public int Framework_Scalar() =>
        frameworkDb.Table<Book>().Count(b => b.AuthorId == 5);

    [Benchmark, BenchmarkCategory("Scalar")]
    public int FrameworkGen_Scalar() =>
        frameworkDbGen.Table<Book>().Count(b => b.AuthorId == 5);

    [Benchmark, BenchmarkCategory("Scalar")]
    public int SqliteNet_Scalar() =>
        sqliteNet.Table<Book>().Count(b => b.AuthorId == 5);

    [Benchmark, BenchmarkCategory("Scalar")]
    public int Ef_Scalar() =>
        ef.Books.AsNoTracking().Count(b => b.AuthorId == 5);
}

[MemoryDiagnoser]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class InsertBenchmarks
{
    private SQLiteDatabase frameworkDb = null!;
    private SQLiteDatabase frameworkDbGen = null!;
    private SqliteNet.SQLiteConnection sqliteNet = null!;
    private BookContext ef = null!;
    private string a = null!, b = null!, c = null!, d = null!;
    private List<Book> payload = null!;

    [GlobalSetup]
    public void Setup()
    {
        _ = BenchHelpers.RowsPerQuery;
        payload = BenchHelpers.NewBooks(BenchHelpers.RowsPerQuery);
    }

    [IterationSetup]
    public void IterSetup()
    {
        a = BenchHelpers.NewDb("ins-fw");
        b = BenchHelpers.NewDb("ins-fwg");
        c = BenchHelpers.NewDb("ins-snet");
        d = BenchHelpers.NewDb("ins-ef");

        frameworkDb = new SQLiteDatabase(new SQLiteOptionsBuilder(a).Build());
        frameworkDb.Schema.CreateTable<Book>();

        frameworkDbGen = new SQLiteDatabase(new SQLiteOptionsBuilder(b).UseGeneratedMaterializers().Build());
        frameworkDbGen.Schema.CreateTable<Book>();

        sqliteNet = new SqliteNet.SQLiteConnection(c);
        sqliteNet.CreateTable<Book>();

        ef = new BookContext(d);
        ef.Database.EnsureCreated();
    }

    [IterationCleanup]
    public void IterCleanup()
    {
        frameworkDb.Dispose();
        frameworkDbGen.Dispose();
        sqliteNet.Dispose();
        ef.Dispose();
        BenchHelpers.TryDelete(a);
        BenchHelpers.TryDelete(b);
        BenchHelpers.TryDelete(c);
        BenchHelpers.TryDelete(d);
    }

    [Benchmark(Baseline = true), BenchmarkCategory("Insert100")]
    public void Framework_Insert100() =>
        frameworkDb.Table<Book>().AddRange(payload);

    [Benchmark, BenchmarkCategory("Insert100")]
    public void FrameworkGen_Insert100() =>
        frameworkDbGen.Table<Book>().AddRange(payload);

    [Benchmark, BenchmarkCategory("Insert100")]
    public int SqliteNet_Insert100() =>
        sqliteNet.InsertAll(payload, runInTransaction: true);

    [Benchmark, BenchmarkCategory("Insert100")]
    public void Ef_Insert100()
    {
        ef.Books.AddRange(payload);
        ef.SaveChanges();
    }
}

[MemoryDiagnoser]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class UpdateBenchmarks
{
    private SQLiteDatabase frameworkDb = null!;
    private SQLiteDatabase frameworkDbGen = null!;
    private SqliteNet.SQLiteConnection sqliteNet = null!;
    private BookContext ef = null!;
    private string a = null!, b = null!, c = null!, d = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _ = BenchHelpers.RowsPerQuery;
    }

    [IterationSetup]
    public void IterSetup()
    {
        a = BenchHelpers.NewDb("upd-fw");
        b = BenchHelpers.NewDb("upd-fwg");
        c = BenchHelpers.NewDb("upd-snet");
        d = BenchHelpers.NewDb("upd-ef");

        frameworkDb = new SQLiteDatabase(new SQLiteOptionsBuilder(a).Build());
        frameworkDb.Schema.CreateTable<Book>();
        BenchHelpers.SeedFramework(frameworkDb);

        frameworkDbGen = new SQLiteDatabase(new SQLiteOptionsBuilder(b).UseGeneratedMaterializers().Build());
        frameworkDbGen.Schema.CreateTable<Book>();
        BenchHelpers.SeedFramework(frameworkDbGen);

        sqliteNet = new SqliteNet.SQLiteConnection(c);
        sqliteNet.CreateTable<Book>();
        BenchHelpers.SeedSqliteNet(sqliteNet);

        ef = new BookContext(d);
        ef.Database.EnsureCreated();
        BenchHelpers.SeedEf(ef);
    }

    [IterationCleanup]
    public void IterCleanup()
    {
        frameworkDb.Dispose();
        frameworkDbGen.Dispose();
        sqliteNet.Dispose();
        ef.Dispose();
        BenchHelpers.TryDelete(a);
        BenchHelpers.TryDelete(b);
        BenchHelpers.TryDelete(c);
        BenchHelpers.TryDelete(d);
    }

    [Benchmark(Baseline = true), BenchmarkCategory("Update100")]
    public int Framework_Update100() =>
        SQLite.Framework.Extensions.QueryableExtensions.ExecuteUpdate(
            frameworkDb.Table<Book>().Where(b => b.AuthorId == 5),
            b => b.Set(x => x.Price, x => x.Price + 1.0));

    [Benchmark, BenchmarkCategory("Update100")]
    public int FrameworkGen_Update100() =>
        SQLite.Framework.Extensions.QueryableExtensions.ExecuteUpdate(
            frameworkDbGen.Table<Book>().Where(b => b.AuthorId == 5),
            b => b.Set(x => x.Price, x => x.Price + 1.0));

    [Benchmark, BenchmarkCategory("Update100")]
    public int SqliteNet_Update100()
    {
        List<Book> all = sqliteNet.Table<Book>().Where(b => b.AuthorId == 5).ToList();
        foreach (Book b in all) b.Price += 1.0;
        return sqliteNet.UpdateAll(all, runInTransaction: true);
    }

    [Benchmark, BenchmarkCategory("Update100")]
    public int Ef_Update100() =>
        ef.Books.Where(b => b.AuthorId == 5)
            .ExecuteUpdate(s => s.SetProperty(b => b.Price, b => b.Price + 1.0));
}

[MemoryDiagnoser]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class JoinBenchmarks
{
    private const int AuthorCount = 50;
    private const int BookCount = 1000;

    private SQLiteDatabase frameworkDb = null!;
    private SQLiteDatabase frameworkDbGen = null!;
    private SqliteNet.SQLiteConnection sqliteNet = null!;
    private BookContext ef = null!;
    private string a = null!, b = null!, c = null!, d = null!;

    [GlobalSetup]
    public void Setup()
    {
        _ = BenchHelpers.RowsPerQuery;
        a = BenchHelpers.NewDb("join-fw");
        b = BenchHelpers.NewDb("join-fwg");
        c = BenchHelpers.NewDb("join-snet");
        d = BenchHelpers.NewDb("join-ef");

        frameworkDb = new SQLiteDatabase(new SQLiteOptionsBuilder(a).Build());
        frameworkDb.Schema.CreateTable<Author>();
        frameworkDb.Schema.CreateTable<Book>();
        SeedAuthorsFramework(frameworkDb);
        SeedBooksFramework(frameworkDb);

        frameworkDbGen = new SQLiteDatabase(new SQLiteOptionsBuilder(b).UseGeneratedMaterializers().Build());
        frameworkDbGen.Schema.CreateTable<Author>();
        frameworkDbGen.Schema.CreateTable<Book>();
        SeedAuthorsFramework(frameworkDbGen);
        SeedBooksFramework(frameworkDbGen);

        sqliteNet = new SqliteNet.SQLiteConnection(c);
        sqliteNet.CreateTable<Author>();
        sqliteNet.CreateTable<Book>();
        for (int i = 0; i < AuthorCount; i++)
        {
            sqliteNet.Insert(new Author { Id = i + 1, Name = $"Author {i}" });
        }
        for (int i = 0; i < BookCount; i++)
        {
            sqliteNet.Insert(MakeBook(i));
        }

        ef = new BookContext(d);
        ef.Database.EnsureCreated();
        for (int i = 0; i < AuthorCount; i++)
        {
            ef.Authors.Add(new Author { Id = i + 1, Name = $"Author {i}" });
        }
        for (int i = 0; i < BookCount; i++)
        {
            ef.Books.Add(MakeBook(i));
        }
        ef.SaveChanges();
    }

    private static void SeedAuthorsFramework(SQLiteDatabase db)
    {
        for (int i = 0; i < AuthorCount; i++)
        {
            db.Table<Author>().Add(new Author { Id = i + 1, Name = $"Author {i}" });
        }
    }

    private static void SeedBooksFramework(SQLiteDatabase db)
    {
        for (int i = 0; i < BookCount; i++)
        {
            db.Table<Book>().Add(MakeBook(i));
        }
    }

    // Most prices are well below 50; only ~10% of rows match the Price > 50 filter,
    // so the SQL-side filter avoids materializing the rest. This stresses sqlite-net-pcl's
    // in-memory join path, which has to load every row before it can filter.
    private static Book MakeBook(int i) => new()
    {
        Id = i + 1,
        Title = $"Book {i}",
        AuthorId = (i % AuthorCount) + 1,
        PublisherId = (i % 7) + 1,
        Price = i % 10 == 0 ? 60.0 + (i % 50) : 5.0 + (i % 40),
    };

    [GlobalCleanup]
    public void Cleanup()
    {
        frameworkDb.Dispose();
        frameworkDbGen.Dispose();
        sqliteNet.Dispose();
        ef.Dispose();
        BenchHelpers.TryDelete(a);
        BenchHelpers.TryDelete(b);
        BenchHelpers.TryDelete(c);
        BenchHelpers.TryDelete(d);
    }

    [Benchmark(Baseline = true), BenchmarkCategory("JoinProject")]
    public List<BookSummary> Framework_JoinProject() =>
        (from book in frameworkDb.Table<Book>()
         join author in frameworkDb.Table<Author>() on book.AuthorId equals author.Id
         where book.Price > 50
         orderby book.Price descending
         select new BookSummary { Title = book.Title, Author = author.Name, Price = book.Price })
        .ToList();

    [Benchmark, BenchmarkCategory("JoinProject")]
    public List<BookSummary> FrameworkGen_JoinProject() =>
        (from book in frameworkDbGen.Table<Book>()
         join author in frameworkDbGen.Table<Author>() on book.AuthorId equals author.Id
         where book.Price > 50
         orderby book.Price descending
         select new BookSummary { Title = book.Title, Author = author.Name, Price = book.Price })
        .ToList();

    [Benchmark, BenchmarkCategory("JoinProject")]
    public List<BookSummary> SqliteNet_JoinProject() =>
        (from book in sqliteNet.Table<Book>()
         join author in sqliteNet.Table<Author>() on book.AuthorId equals author.Id
         where book.Price > 50
         orderby book.Price descending
         select new BookSummary { Title = book.Title, Author = author.Name, Price = book.Price })
        .ToList();

    [Benchmark, BenchmarkCategory("JoinProject")]
    public List<BookSummary> Ef_JoinProject() =>
        (from book in ef.Books.AsNoTracking()
         join author in ef.Authors.AsNoTracking() on book.AuthorId equals author.Id
         where book.Price > 50
         orderby book.Price descending
         select new BookSummary { Title = book.Title, Author = author.Name, Price = book.Price })
        .ToList();
}

public class Book
{
    [Key]
    [SqliteNet.PrimaryKey]
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public int AuthorId { get; set; }

    public int PublisherId { get; set; }

    public double Price { get; set; }
}

public class Author
{
    [Key]
    [SqliteNet.PrimaryKey]
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
}

public class BookSummary
{
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public double Price { get; set; }
}

public class BookContext : DbContext
{
    private readonly string path;

    public BookContext(string path) { this.path = path; }

    public DbSet<Book> Books => Set<Book>();
    public DbSet<Author> Authors => Set<Author>();

    protected override void OnConfiguring(DbContextOptionsBuilder options) =>
        options.UseSqlite($"Data Source={path}");
}
