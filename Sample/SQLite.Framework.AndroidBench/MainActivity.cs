using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;
using Android.App;
using Android.OS;
using Android.Util;
using Microsoft.EntityFrameworkCore;
using SQLite.Framework;
using SQLite.Framework.Generated;
using SqliteNet = SQLite;
using SQLitePCL;

namespace SQLite.Framework.AndroidBench;

[Activity(Name = "com.sqliteframework.androidbench.MainActivity", Label = "AndroidBench", MainLauncher = true)]
public class MainActivity : Activity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        string orm = Intent?.GetStringExtra("orm") ?? "all";
        new Thread(() => Benchmarks.Run(FilesDir!.AbsolutePath, orm)) { IsBackground = true }.Start();
    }
}

internal static class Benchmarks
{
    private const string Tag = "ORMBENCH";
    private const int ReadRows = 100;
    private const int JoinBooks = 1000;
    private const int JoinAuthors = 50;

    public static void Run(string dir, string orm)
    {
        try
        {
            Batteries_V2.Init();
            Log.Info(Tag, $"runtime={RuntimeInformation.FrameworkDescription} sqlite={raw.sqlite3_libversion().utf8_to_string()} orm={orm}");

            bool all = orm == "all";
            if (all || orm == "framework")
            {
                RunFramework(dir, false);
            }

            if (all || orm == "frameworkgen")
            {
                RunFramework(dir, true);
            }

            if (all || orm == "ef")
            {
                RunEf(dir);
            }

            if (all || orm == "sqlitenet")
            {
                RunSqliteNet(dir);
            }

            Log.Info(Tag, "DONE");
        }
        catch (Exception e)
        {
            Log.Error(Tag, "FAILED: " + e);
        }
    }

    private static void RunFramework(string dir, bool gen)
    {
        string label = gen ? "framework+gen" : "framework";

        string readPath = NewPath(dir, gen ? "read-fwg" : "read-fw");
        using (SQLiteDatabase db = NewDb(readPath, gen))
        {
            db.Schema.CreateTable<Book>();
            db.Table<Book>().AddRange(MakeReadBooks());
            Bench($"{label} read", () => db.Table<Book>().Where(b => b.AuthorId == 5).ToList());
        }

        Del(readPath);

        string insPath = NewPath(dir, gen ? "ins-fwg" : "ins-fw");
        List<Book> payload = MakeReadBooks();
        using (SQLiteDatabase db = NewDb(insPath, gen))
        {
            db.Schema.CreateTable<Book>();
            Bench($"{label} insert",
                () => db.Table<Book>().AddRange(payload),
                () => db.Execute("DELETE FROM \"Book\""));
        }

        Del(insPath);

        string updPath = NewPath(dir, gen ? "upd-fwg" : "upd-fw");
        using (SQLiteDatabase db = NewDb(updPath, gen))
        {
            db.Schema.CreateTable<Book>();
            db.Table<Book>().AddRange(MakeReadBooks());
            Bench($"{label} update", () => SQLite.Framework.Extensions.QueryableExtensions.ExecuteUpdate(
                db.Table<Book>().Where(b => b.AuthorId == 5),
                s => s.Set(x => x.Price, x => x.Price + 1.0)));
        }

        Del(updPath);

        string joinPath = NewPath(dir, gen ? "join-fwg" : "join-fw");
        using (SQLiteDatabase db = NewDb(joinPath, gen))
        {
            db.Schema.CreateTable<Author>();
            db.Schema.CreateTable<Book>();
            db.Table<Author>().AddRange(MakeAuthors());
            db.Table<Book>().AddRange(MakeJoinBooks());
            Bench($"{label} join", () =>
                (from book in db.Table<Book>()
                 join author in db.Table<Author>() on book.AuthorId equals author.Id
                 where book.Price > 50
                 orderby book.Price descending
                 select new BookSummary
                 {
                     Title = book.Title,
                     Author = author.Name,
                     Price = book.Price,
                     TotalBooks = db.Table<Book>().Count(),
                 }).ToList());
        }

        Del(joinPath);
    }

    private static void RunSqliteNet(string dir)
    {
        string readPath = NewPath(dir, "read-snet");
        using (SqliteNet.SQLiteConnection conn = new(readPath))
        {
            conn.CreateTable<Book>();
            conn.InsertAll(MakeReadBooks(), runInTransaction: true);
            Bench("sqlitenet read", () => conn.Table<Book>().Where(b => b.AuthorId == 5).ToList());
        }

        Del(readPath);

        string insPath = NewPath(dir, "ins-snet");
        List<Book> payload = MakeReadBooks();
        using (SqliteNet.SQLiteConnection conn = new(insPath))
        {
            conn.CreateTable<Book>();
            Bench("sqlitenet insert",
                () => conn.InsertAll(payload, runInTransaction: true),
                () => conn.DeleteAll<Book>());
        }

        Del(insPath);

        string updPath = NewPath(dir, "upd-snet");
        using (SqliteNet.SQLiteConnection conn = new(updPath))
        {
            conn.CreateTable<Book>();
            conn.InsertAll(MakeReadBooks(), runInTransaction: true);
            Bench("sqlitenet update", () =>
            {
                List<Book> all = conn.Table<Book>().Where(b => b.AuthorId == 5).ToList();
                foreach (Book b in all)
                {
                    b.Price += 1.0;
                }

                conn.UpdateAll(all, runInTransaction: true);
            });
        }

        Del(updPath);

        string joinPath = NewPath(dir, "join-snet");
        using (SqliteNet.SQLiteConnection conn = new(joinPath))
        {
            conn.CreateTable<Author>();
            conn.CreateTable<Book>();
            conn.InsertAll(MakeAuthors(), runInTransaction: true);
            conn.InsertAll(MakeJoinBooks(), runInTransaction: true);
            Bench("sqlitenet join", () =>
                (from book in conn.Table<Book>()
                 join author in conn.Table<Author>() on book.AuthorId equals author.Id
                 where book.Price > 50
                 orderby book.Price descending
                 select new BookSummary
                 {
                     Title = book.Title,
                     Author = author.Name,
                     Price = book.Price,
                     TotalBooks = conn.Table<Book>().Count(),
                 }).ToList());
        }

        Del(joinPath);
    }

    private static void RunEf(string dir)
    {
        string readPath = NewPath(dir, "read-ef");
        using (BookContext ctx = new(readPath))
        {
            ctx.Database.EnsureCreated();
            ctx.Books.AddRange(MakeReadBooks());
            ctx.SaveChanges();
            Bench("ef read", () => ctx.Books.AsNoTracking().Where(b => b.AuthorId == 5).ToList());
        }

        Del(readPath);

        string insPath = NewPath(dir, "ins-ef");
        using (BookContext ctx = new(insPath))
        {
            ctx.Database.EnsureCreated();
            List<Book> payload = MakeReadBooks();
            Bench("ef insert",
                () =>
                {
                    ctx.Books.AddRange(payload);
                    ctx.SaveChanges();
                },
                () =>
                {
                    ctx.Database.ExecuteSqlRaw("DELETE FROM \"Books\"");
                    ctx.ChangeTracker.Clear();
                    payload = MakeReadBooks();
                });
        }

        Del(insPath);

        string updPath = NewPath(dir, "upd-ef");
        using (BookContext ctx = new(updPath))
        {
            ctx.Database.EnsureCreated();
            ctx.Books.AddRange(MakeReadBooks());
            ctx.SaveChanges();
            Bench("ef update", () => ctx.Books.Where(b => b.AuthorId == 5)
                .ExecuteUpdate(s => s.SetProperty(b => b.Price, b => b.Price + 1.0)));
        }

        Del(updPath);

        string joinPath = NewPath(dir, "join-ef");
        using (BookContext ctx = new(joinPath))
        {
            ctx.Database.EnsureCreated();
            ctx.Authors.AddRange(MakeAuthors());
            ctx.Books.AddRange(MakeJoinBooks());
            ctx.SaveChanges();
            Bench("ef join", () =>
                (from book in ctx.Books.AsNoTracking()
                 join author in ctx.Authors.AsNoTracking() on book.AuthorId equals author.Id
                 where book.Price > 50
                 orderby book.Price descending
                 select new BookSummary
                 {
                     Title = book.Title,
                     Author = author.Name,
                     Price = book.Price,
                     TotalBooks = ctx.Books.Count(),
                 }).ToList());
        }

        Del(joinPath);
    }

    private static void Bench(string label, Action action, Action? perIteration = null, int warmup = 3, int iterations = 30)
    {
        for (int i = 0; i < warmup; i++)
        {
            perIteration?.Invoke();
            action();
        }

        double min = double.MaxValue;
        double sum = 0;
        for (int i = 0; i < iterations; i++)
        {
            perIteration?.Invoke();
            System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
            action();
            sw.Stop();
            double us = sw.Elapsed.TotalMilliseconds * 1000.0;
            sum += us;
            if (us < min)
            {
                min = us;
            }
        }

        Log.Info(Tag, $"{label,-22} mean {sum / iterations,11:F1} us   min {min,11:F1} us");
    }

    private static SQLiteDatabase NewDb(string path, bool gen)
    {
        SQLiteOptionsBuilder builder = new(path);
        if (gen)
        {
            builder.UseGeneratedMaterializers();
        }

        return new SQLiteDatabase(builder.Build());
    }

    private static List<Book> MakeReadBooks()
    {
        List<Book> list = new(ReadRows);
        for (int i = 0; i < ReadRows; i++)
        {
            list.Add(new Book { Id = i + 1, Title = $"Book {i}", AuthorId = 5, PublisherId = (i % 7) + 1, Price = 9.99 + i });
        }

        return list;
    }

    private static List<Author> MakeAuthors()
    {
        List<Author> list = new(JoinAuthors);
        for (int i = 0; i < JoinAuthors; i++)
        {
            list.Add(new Author { Id = i + 1, Name = $"Author {i}" });
        }

        return list;
    }

    private static List<Book> MakeJoinBooks()
    {
        List<Book> list = new(JoinBooks);
        for (int i = 0; i < JoinBooks; i++)
        {
            list.Add(new Book
            {
                Id = i + 1,
                Title = $"Book {i}",
                AuthorId = (i % JoinAuthors) + 1,
                PublisherId = (i % 7) + 1,
                Price = i % 10 == 0 ? 60.0 + (i % 50) : 5.0 + (i % 40),
            });
        }

        return list;
    }

    private static string NewPath(string dir, string label)
    {
        string path = Path.Combine(dir, $"orm-bench-{label}.db");
        Del(path);
        return path;
    }

    private static void Del(string path)
    {
        foreach (string p in new[] { path, path + "-wal", path + "-shm" })
        {
            try
            {
                if (File.Exists(p))
                {
                    File.Delete(p);
                }
            }
            catch (IOException)
            {
            }
        }
    }
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
    public int TotalBooks { get; set; }
}

public class BookContext : DbContext
{
    private readonly string path;

    public BookContext(string path)
    {
        this.path = path;
    }

    public DbSet<Book> Books => Set<Book>();
    public DbSet<Author> Authors => Set<Author>();

    protected override void OnConfiguring(DbContextOptionsBuilder options) =>
        options.UseSqlite($"Data Source={path};Pooling=False");
}
