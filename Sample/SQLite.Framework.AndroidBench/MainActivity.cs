using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Android.App;
using Android.Database;
using Android.OS;
using Android.Util;
using Microsoft.EntityFrameworkCore;
using SQLite.Framework;
using SQLite.Framework.Enums;
using SQLite.Framework.Generated;
using AndroidSqlite = Android.Database.Sqlite;
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

    private static readonly Regex androidParamRegex = new("@p\\d+", RegexOptions.Compiled);

    public static void Run(string dir, string orm)
    {
        try
        {
            using (SQLiteDatabase init = new(new SQLiteOptionsBuilder(":memory:").Build()))
            {
                init.Execute("SELECT 1");
            }

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

            if (all || orm == "android")
            {
                RunAndroid(dir);
            }

            if (orm == "raw")
            {
                RunRaw(dir);
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

        string walPath = NewPath(dir, gen ? "insw-fwg" : "insw-fw");
        using (SQLiteDatabase db = NewDb(walPath, gen))
        {
            db.Schema.CreateTable<Book>();
            db.Pragmas.JournalMode = SQLiteJournalMode.Wal;
            Bench($"{label} insert wal",
                () => db.Table<Book>().AddRange(payload),
                () => db.Execute("DELETE FROM \"Book\""));
        }

        Del(walPath);

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

    private static void RunAndroid(string dir)
    {
        string readPath = NewPath(dir, "read-android");
        using (SQLiteDatabase db = NewDb(readPath, false))
        {
            db.Schema.CreateTable<Book>();
            db.Table<Book>().AddRange(MakeReadBooks());
        }

        using (SQLiteDatabase compiler = NewDb(":memory:", false))
        using (AndroidSqlite.SQLiteDatabase adb = AndroidSqlite.SQLiteDatabase.OpenOrCreateDatabase(readPath, null)!)
        {
            Bench("android read", () =>
            {
                (string sql, string[] args) = CompileForAndroid(compiler.Table<Book>().Where(b => b.AuthorId == 5));
                List<Book> rows = new(ReadRows);
                using ICursor cursor = adb.RawQuery(sql, args)!;
                while (cursor.MoveToNext())
                {
                    rows.Add(new Book
                    {
                        Id = cursor.GetInt(0),
                        Title = cursor.GetString(1)!,
                        AuthorId = cursor.GetInt(2),
                        PublisherId = cursor.GetInt(3),
                        Price = cursor.GetDouble(4),
                    });
                }

                cursor.Close();
            });

            adb.Close();
        }

        Del(readPath);

        string insPath = NewPath(dir, "ins-android");
        using (SQLiteDatabase db = NewDb(insPath, false))
        {
            db.Schema.CreateTable<Book>();
        }

        List<Book> payload = MakeReadBooks();
        using (AndroidSqlite.SQLiteDatabase adb = AndroidSqlite.SQLiteDatabase.OpenOrCreateDatabase(insPath, null)!)
        using (AndroidSqlite.SQLiteStatement insert = adb.CompileStatement(
                   "INSERT INTO \"Book\" (\"Id\", \"Title\", \"AuthorId\", \"PublisherId\", \"Price\") VALUES (?, ?, ?, ?, ?)")!)
        {
            Log.Info(Tag, $"android engine journal={QueryScalar(adb, "PRAGMA journal_mode")} sync={QueryScalar(adb, "PRAGMA synchronous")} version={QueryScalar(adb, "SELECT sqlite_version()")}");

            Bench("android insert",
                () =>
                {
                    adb.BeginTransaction();
                    try
                    {
                        foreach (Book book in payload)
                        {
                            insert.BindLong(1, book.Id);
                            insert.BindString(2, book.Title);
                            insert.BindLong(3, book.AuthorId);
                            insert.BindLong(4, book.PublisherId);
                            insert.BindDouble(5, book.Price);
                            insert.ExecuteInsert();
                        }

                        adb.SetTransactionSuccessful();
                    }
                    finally
                    {
                        adb.EndTransaction();
                    }
                },
                () => adb.ExecSQL("DELETE FROM \"Book\""));

            insert.Close();
            adb.Close();
        }

        Del(insPath);

        string walPath = NewPath(dir, "insw-android");
        using (SQLiteDatabase db = NewDb(walPath, false))
        {
            db.Schema.CreateTable<Book>();
        }

        using (AndroidSqlite.SQLiteDatabase adb = AndroidSqlite.SQLiteDatabase.OpenOrCreateDatabase(walPath, null)!)
        {
            adb.EnableWriteAheadLogging();
            using AndroidSqlite.SQLiteStatement insert = adb.CompileStatement(
                "INSERT INTO \"Book\" (\"Id\", \"Title\", \"AuthorId\", \"PublisherId\", \"Price\") VALUES (?, ?, ?, ?, ?)")!;
            Log.Info(Tag, $"android wal journal={QueryScalar(adb, "PRAGMA journal_mode")} sync={QueryScalar(adb, "PRAGMA synchronous")}");
            Bench("android insert wal",
                () =>
                {
                    adb.BeginTransaction();
                    try
                    {
                        foreach (Book book in payload)
                        {
                            insert.BindLong(1, book.Id);
                            insert.BindString(2, book.Title);
                            insert.BindLong(3, book.AuthorId);
                            insert.BindLong(4, book.PublisherId);
                            insert.BindDouble(5, book.Price);
                            insert.ExecuteInsert();
                        }

                        adb.SetTransactionSuccessful();
                    }
                    finally
                    {
                        adb.EndTransaction();
                    }
                },
                () => adb.ExecSQL("DELETE FROM \"Book\""));

            insert.Close();
            adb.Close();
        }

        Del(walPath);

        string joinPath = NewPath(dir, "join-android");
        using (SQLiteDatabase db = NewDb(joinPath, false))
        {
            db.Schema.CreateTable<Author>();
            db.Schema.CreateTable<Book>();
            db.Table<Author>().AddRange(MakeAuthors());
            db.Table<Book>().AddRange(MakeJoinBooks());
        }

        using (SQLiteDatabase compiler = NewDb(":memory:", false))
        using (AndroidSqlite.SQLiteDatabase adb = AndroidSqlite.SQLiteDatabase.OpenOrCreateDatabase(joinPath, null)!)
        {
            Bench("android join", () =>
            {
                (string sql, string[] args) = CompileForAndroid(
                    from book in compiler.Table<Book>()
                    join author in compiler.Table<Author>() on book.AuthorId equals author.Id
                    where book.Price > 50
                    orderby book.Price descending
                    select new BookSummary
                    {
                        Title = book.Title,
                        Author = author.Name,
                        Price = book.Price,
                        TotalBooks = compiler.Table<Book>().Count(),
                    });
                List<BookSummary> rows = [];
                using ICursor cursor = adb.RawQuery(sql, args)!;
                while (cursor.MoveToNext())
                {
                    rows.Add(new BookSummary
                    {
                        Title = cursor.GetString(0)!,
                        Author = cursor.GetString(1)!,
                        Price = cursor.GetDouble(2),
                        TotalBooks = cursor.GetInt(3),
                    });
                }

                cursor.Close();
            });

            adb.Close();
        }

        Del(joinPath);
    }

    private static void RunRaw(string dir)
    {
        string insPath = NewPath(dir, "ins-raw");
        using (SQLiteDatabase db = NewDb(insPath, false))
        {
            db.Schema.CreateTable<Book>();
        }

        List<Book> payload = MakeReadBooks();
        int rc = raw.sqlite3_open(insPath, out sqlite3 handle);
        if (rc != raw.SQLITE_OK)
        {
            throw new InvalidOperationException("open failed");
        }

        raw.sqlite3_prepare_v2(handle, "INSERT INTO \"Book\" (\"Id\", \"Title\", \"AuthorId\", \"PublisherId\", \"Price\") VALUES (?, ?, ?, ?, ?)", out sqlite3_stmt insert);
        raw.sqlite3_prepare_v2(handle, "BEGIN", out sqlite3_stmt begin);
        raw.sqlite3_prepare_v2(handle, "COMMIT", out sqlite3_stmt commit);
        raw.sqlite3_prepare_v2(handle, "DELETE FROM \"Book\"", out sqlite3_stmt delete);

        raw.sqlite3_exec(handle, "PRAGMA journal_mode=TRUNCATE");

        Bench("raw insert trunc", () =>
        {
            raw.sqlite3_step(begin);
            raw.sqlite3_reset(begin);
            foreach (Book b in payload)
            {
                raw.sqlite3_bind_int(insert, 1, b.Id);
                raw.sqlite3_bind_text(insert, 2, b.Title);
                raw.sqlite3_bind_int(insert, 3, b.AuthorId);
                raw.sqlite3_bind_int(insert, 4, b.PublisherId);
                raw.sqlite3_bind_double(insert, 5, b.Price);
                raw.sqlite3_step(insert);
                raw.sqlite3_reset(insert);
            }

            raw.sqlite3_step(commit);
            raw.sqlite3_reset(commit);
        }, () =>
        {
            raw.sqlite3_step(delete);
            raw.sqlite3_reset(delete);
        });

        raw.sqlite3_exec(handle, "PRAGMA journal_mode=WAL");

        Bench("raw insert wal", () =>
        {
            raw.sqlite3_step(begin);
            raw.sqlite3_reset(begin);
            foreach (Book b in payload)
            {
                raw.sqlite3_bind_int(insert, 1, b.Id);
                raw.sqlite3_bind_text(insert, 2, b.Title);
                raw.sqlite3_bind_int(insert, 3, b.AuthorId);
                raw.sqlite3_bind_int(insert, 4, b.PublisherId);
                raw.sqlite3_bind_double(insert, 5, b.Price);
                raw.sqlite3_step(insert);
                raw.sqlite3_reset(insert);
            }

            raw.sqlite3_step(commit);
            raw.sqlite3_reset(commit);
        }, () =>
        {
            raw.sqlite3_step(delete);
            raw.sqlite3_reset(delete);
        });

        raw.sqlite3_exec(handle, "PRAGMA journal_mode=DELETE");

        Bench("raw insert", () =>
        {
            raw.sqlite3_step(begin);
            raw.sqlite3_reset(begin);
            foreach (Book b in payload)
            {
                raw.sqlite3_bind_int(insert, 1, b.Id);
                raw.sqlite3_bind_text(insert, 2, b.Title);
                raw.sqlite3_bind_int(insert, 3, b.AuthorId);
                raw.sqlite3_bind_int(insert, 4, b.PublisherId);
                raw.sqlite3_bind_double(insert, 5, b.Price);
                raw.sqlite3_step(insert);
                raw.sqlite3_reset(insert);
            }

            raw.sqlite3_step(commit);
            raw.sqlite3_reset(commit);
        }, () =>
        {
            raw.sqlite3_step(delete);
            raw.sqlite3_reset(delete);
        });

        raw.sqlite3_finalize(insert);
        raw.sqlite3_finalize(begin);
        raw.sqlite3_finalize(commit);
        raw.sqlite3_finalize(delete);
        raw.sqlite3_close(handle);
        Del(insPath);
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

    private static void Bench(string label, Action action, Action? perIteration = null, int warmup = 20, int iterations = 200)
    {
        for (int i = 0; i < warmup; i++)
        {
            perIteration?.Invoke();
            action();
        }

        double[] samples = new double[iterations];
        for (int i = 0; i < iterations; i++)
        {
            perIteration?.Invoke();
            System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
            action();
            sw.Stop();
            samples[i] = sw.Elapsed.TotalMilliseconds * 1000.0;
        }

        Array.Sort(samples);
        double median = (samples[iterations / 2 - 1] + samples[iterations / 2]) / 2.0;
        double min = samples[0];
        Log.Info(Tag, $"{label,-22} median {median,11:F1} us   min {min,11:F1} us");
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

    private static string QueryScalar(AndroidSqlite.SQLiteDatabase adb, string sql)
    {
        using ICursor cursor = adb.RawQuery(sql, null)!;
        cursor.MoveToNext();
        string value = cursor.GetString(0)!;
        cursor.Close();
        return value;
    }

    private static (string Sql, string[] Args) CompileForAndroid<T>(IQueryable<T> query)
    {
        SQLiteCommand command = SQLite.Framework.Extensions.QueryableExtensions.ToSqlCommand(query);
        List<string> args = new(command.Parameters.Count);

        string sql = androidParamRegex.Replace(command.CommandText, match =>
        {
            SQLiteParameter parameter = command.Parameters.First(p => p.Name == match.Value);
            args.Add(Convert.ToString(parameter.Value, CultureInfo.InvariantCulture)!);
            return "?";
        });

        return (sql, args.ToArray());
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
