#if !SQLITE_FRAMEWORK_BUNDLED && !SQLITECIPHER && !NO_SQLITEPCL_RAW_BATTERIES
using SQLite.Framework.Enums;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Entities;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class MinimumSqliteVersionTests
{
    [Fact]
    public void Unspecified_DoesNotGateAnyMethod()
    {
        using TestDatabase db = new();
        Assert.Equal(SQLiteMinimumVersion.Unspecified, db.Options.MinimumSqliteVersion);

        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "x", AuthorId = 1, Price = 1 });

        int hits = db.Table<Book>()
            .Where(b => SQLiteFunctions.Iif(b.Price > 0, 1, 0) == 1)
            .Count();
        Assert.Equal(1, hits);
    }

    [Fact]
    public void HighFloor_AllowsNewerFunction()
    {
        using TestDatabase db = new(b => b.UseMinimumSqliteVersion(SQLiteMinimumVersion.V3_38));
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "x", AuthorId = 1, Price = 1 });

        int hits = db.Table<Book>()
            .Where(b => SQLiteFunctions.Iif(b.Price > 0, 1, 0) == 1)
            .Count();
        Assert.Equal(1, hits);
    }

    [Fact]
    public void LowFloor_BlocksIif()
    {
        using TestDatabase db = new(b => b.UseMinimumSqliteVersion(SQLiteMinimumVersion.V3_22));
        db.Table<Book>().Schema.CreateTable();

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() =>
            db.Table<Book>()
                .Where(b => SQLiteFunctions.Iif(b.Price > 0, 1, 0) == 1)
                .ToList());

        Assert.Contains("Iif", ex.Message);
        Assert.Contains("3.32", ex.Message);
        Assert.Contains("3.22", ex.Message);
    }

    [Fact]
    public void LowFloor_BlocksFormat()
    {
        using TestDatabase db = new(b => b.UseMinimumSqliteVersion(SQLiteMinimumVersion.V3_32));
        db.Table<Book>().Schema.CreateTable();

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() =>
            db.Table<Book>()
                .Select(b => SQLiteFunctions.Format("%s", b.Title))
                .ToList());

        Assert.Contains("Format", ex.Message);
        Assert.Contains("3.38", ex.Message);
    }

    [Fact]
    public void LowFloor_BlocksUnixEpoch()
    {
        using TestDatabase db = new(b => b.UseMinimumSqliteVersion(SQLiteMinimumVersion.V3_32));
        db.Table<Book>().Schema.CreateTable();

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() =>
            db.Table<Book>()
                .Select(b => SQLiteFunctions.UnixEpoch())
                .ToList());

        Assert.Contains("UnixEpoch", ex.Message);
        Assert.Contains("3.38", ex.Message);
    }

#if !SQLITECIPHER
    [Fact]
    public void LowFloor_BlocksUnhex()
    {
        using TestDatabase db = new(b => b.UseMinimumSqliteVersion(SQLiteMinimumVersion.V3_38));
        db.Table<Book>().Schema.CreateTable();

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() =>
            db.Table<Book>()
                .Select(b => SQLiteFunctions.Unhex("48"))
                .ToList());

        Assert.Contains("Unhex", ex.Message);
        Assert.Contains("3.41", ex.Message);
    }

    [Fact]
    public void LowFloor_BlocksTimediff()
    {
        using TestDatabase db = new(b => b.UseMinimumSqliteVersion(SQLiteMinimumVersion.V3_41));
        db.Table<Book>().Schema.CreateTable();

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() =>
            db.Table<Book>()
                .Select(b => SQLiteDateFunctions.Timediff("2024-01-02", "2024-01-01"))
                .ToList());

        Assert.Contains("Timediff", ex.Message);
        Assert.Contains("3.43", ex.Message);
    }
#endif

    [Fact]
    public void LowFloor_BlocksVacuumInto()
    {
        string destPath = Path.Combine(Path.GetTempPath(), $"min_ver_{Guid.NewGuid():N}.db3");
        try
        {
            using TestDatabase db = new(b => b.UseMinimumSqliteVersion(SQLiteMinimumVersion.V3_22), useFile: true);

            NotSupportedException ex = Assert.Throws<NotSupportedException>(() => db.VacuumInto(destPath));
            Assert.Contains("VACUUM INTO", ex.Message);
            Assert.Contains("3.27", ex.Message);
        }
        finally
        {
            if (File.Exists(destPath)) File.Delete(destPath);
        }
    }

    [Fact]
    public void HighFloor_AllowsVacuumInto()
    {
        string destPath = Path.Combine(Path.GetTempPath(), $"min_ver_ok_{Guid.NewGuid():N}.db3");
        try
        {
            using TestDatabase db = new(b => b.UseMinimumSqliteVersion(SQLiteMinimumVersion.V3_27), useFile: true);
            db.Table<Book>().Schema.CreateTable();
            db.VacuumInto(destPath);
            Assert.True(File.Exists(destPath));
        }
        finally
        {
            if (File.Exists(destPath)) File.Delete(destPath);
        }
    }

    [Fact]
    public void LowFloor_BlocksReturningOnExecuteDelete()
    {
        using TestDatabase db = new(b => b.UseMinimumSqliteVersion(SQLiteMinimumVersion.V3_33));
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "x", AuthorId = 1, Price = 1 });

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() =>
            db.Table<Book>().Where(b => true).Returning().ExecuteDelete());

        Assert.Contains("RETURNING", ex.Message);
        Assert.Contains("3.35", ex.Message);
    }

    [Fact]
    public void HighFloor_AllowsReturning()
    {
        using TestDatabase db = new(b => b.UseMinimumSqliteVersion(SQLiteMinimumVersion.V3_35));
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "x", AuthorId = 1, Price = 1 });

        List<Book> deleted = db.Table<Book>().Where(b => true).Returning().ExecuteDelete();
        Assert.Single(deleted);
    }

    [Fact]
    public void Open_WhenLoadedVersionMeetsFloor_DoesNotThrow()
    {
        using TestDatabase db = new(b => b.UseMinimumSqliteVersion(SQLiteMinimumVersion.V3_8));
        db.Table<Book>().Schema.CreateTable();
        Assert.Empty(db.Table<Book>().ToList());
    }

#if !SQLITECIPHER && !SQLITE_FRAMEWORK_OS_BUNDLED_SQLITE
    [Fact]
    public void Open_WhenLoadedVersionBelowFloor_Throws()
    {
        SQLiteOptions options = new SQLiteOptionsBuilder(":memory:")
            .UseMinimumSqliteVersion((SQLiteMinimumVersion)9_999_000)
            .Build();
        using SQLiteDatabase db = new(options);

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() => db.Execute("SELECT 1"));
        Assert.Contains("below the configured minimum", ex.Message);
    }
#endif

    [Fact]
    public void LowFloor_BlocksStrictTables()
    {
        using TestDatabase db = new(b => b.UseMinimumSqliteVersion(SQLiteMinimumVersion.V3_22));

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() =>
            db.Schema.CreateTable<StrictBookEntity>());

        Assert.Contains("STRICT", ex.Message);
        Assert.Contains("3.37", ex.Message);
    }

    [Fact]
    public void LowFloor_BlocksComputedColumns()
    {
        using ModelTestDatabase db = new(
            model => model.Entity<Book>().Computed(b => b.Price, b => b.AuthorId * 10),
            b => b.UseMinimumSqliteVersion(SQLiteMinimumVersion.V3_22));

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() =>
            db.Schema.CreateTable<Book>());

        Assert.Contains("Computed", ex.Message);
        Assert.Contains("3.31", ex.Message);
    }

    [Fact]
    public void LowFloor_BlocksExpressionIndex()
    {
        using ModelTestDatabase db = new(
            model => model.Entity<Book>().Index(b => b.Title.ToLower(), name: "ix_low"),
            b => b.UseMinimumSqliteVersion(SQLiteMinimumVersion.V3_8));

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() =>
            db.Schema.CreateTable<Book>());

        Assert.Contains("Expression indexes", ex.Message);
        Assert.Contains("3.9", ex.Message);
    }

    [Fact]
    public void LowFloor_BlocksUpdateFrom()
    {
        using TestDatabase db = new(b => b.UseMinimumSqliteVersion(SQLiteMinimumVersion.V3_32));
        db.Table<Author>().Schema.CreateTable();
        db.Table<Book>().Schema.CreateTable();

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() =>
            db.Table<Book>()
                .Join(db.Table<Author>(), b => b.AuthorId, a => a.Id, (b, a) => new { b, a })
                .ExecuteUpdate(s => s.Set(x => x.b.Title, x => x.a.Name)));

        Assert.Contains("UPDATE FROM", ex.Message);
        Assert.Contains("3.33", ex.Message);
    }

    [Fact]
    public void LowFloor_BlocksMaterializedCte()
    {
        using TestDatabase db = new(b => b.UseMinimumSqliteVersion(SQLiteMinimumVersion.V3_32));
        db.Table<Book>().Schema.CreateTable();

        SQLiteCte<Book> cte = db.With(
            () => db.Table<Book>(),
            SQLiteCteMaterialization.Materialized);

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() =>
            (from b in cte select b).ToList());

        Assert.Contains("MATERIALIZED", ex.Message);
        Assert.Contains("3.35", ex.Message);
    }

    [Fact]
    public void LowFloor_BlocksDropColumn()
    {
        using TestDatabase db = new(b => b.UseMinimumSqliteVersion(SQLiteMinimumVersion.V3_32));
        db.Table<Book>().Schema.CreateTable();

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() =>
            db.Schema.DropColumn<Book>("BookPrice"));

        Assert.Contains("DROP COLUMN", ex.Message);
        Assert.Contains("3.35", ex.Message);
    }

    [Fact]
    public void LowFloor_BlocksOptimize()
    {
        using TestDatabase db = new(b => b.UseMinimumSqliteVersion(SQLiteMinimumVersion.V3_14));

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() => db.Pragmas.Optimize());

        Assert.Contains("optimize", ex.Message);
        Assert.Contains("3.18", ex.Message);
    }

    [Fact]
    public void LowFloor_BlocksDistinctFrom()
    {
        using TestDatabase db = new(b => b.UseMinimumSqliteVersion(SQLiteMinimumVersion.V3_38));
        db.Table<Book>().Schema.CreateTable();

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() =>
            db.Table<Book>()
                .Where(b => SQLiteFunctions.DistinctFrom(b.Title, "x"))
                .ToList());

        Assert.Contains("DistinctFrom", ex.Message);
        Assert.Contains("3.39", ex.Message);
    }

    [Fact]
    public void LowFloor_BlocksFullOuterJoin()
    {
        using TestDatabase db = new(b => b.UseMinimumSqliteVersion(SQLiteMinimumVersion.V3_38));
        db.Table<Book>().Schema.CreateTable();
        db.Table<Author>().Schema.CreateTable();

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() =>
            db.Table<Book>()
                .FullOuterJoin(db.Table<Author>(), b => b.AuthorId, a => a.Id, (b, a) => b!.Id)
                .ToList());

        Assert.Contains("FULL OUTER JOIN", ex.Message);
        Assert.Contains("3.39", ex.Message);
    }

    [Fact]
    public void LowFloor_BlocksNullsOrder()
    {
        using TestDatabase db = new(b => b.UseMinimumSqliteVersion(SQLiteMinimumVersion.V3_29));
        db.Table<Book>().Schema.CreateTable();

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() =>
            db.Table<Book>().OrderBy(b => b.Title, SQLiteNullsOrder.Last).ToList());

        Assert.Contains("NULLS", ex.Message);
        Assert.Contains("3.30", ex.Message);
    }

    [Fact]
    public void LowFloor_BlocksUpsert()
    {
        using TestDatabase db = new(b => b.UseMinimumSqliteVersion(SQLiteMinimumVersion.V3_22));
        db.Table<Book>().Schema.CreateTable();

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() =>
            db.Table<Book>().Upsert(
                new Book { Id = 1, Title = "x", AuthorId = 1, Price = 1 },
                b => b.OnConflict(x => x.Id).DoNothing()));

        Assert.Contains("UPSERT", ex.Message);
        Assert.Contains("3.24", ex.Message);
    }

    [Fact]
    public void LowFloor_BlocksWindowFunctions()
    {
        using TestDatabase db = new(b => b.UseMinimumSqliteVersion(SQLiteMinimumVersion.V3_24));
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "x", AuthorId = 1, Price = 1 });

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() =>
            db.Table<Book>().Select(b => SQLiteWindowFunctions.RowNumber().AsValue()).ToList());

        Assert.Contains("Window functions", ex.Message);
        Assert.Contains("3.25", ex.Message);
    }

    [Fact]
    public void LowFloor_BlocksMathExtension()
    {
        using TestDatabase db = new(b => b.UseMinimumSqliteVersion(SQLiteMinimumVersion.V3_32));
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "x", AuthorId = 1, Price = 4 });

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() =>
            db.Table<Book>().Select(b => Math.Sqrt(b.Price)).ToList());

        Assert.Contains("Math.Sqrt", ex.Message);
        Assert.Contains("3.35", ex.Message);
    }

    [Fact]
    public void LowFloor_AllowsMathRoundAndAbs()
    {
        using TestDatabase db = new(b => b.UseMinimumSqliteVersion(SQLiteMinimumVersion.V3_8));
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "x", AuthorId = 1, Price = 4.5 });

        List<double> rounded = db.Table<Book>().Select(b => Math.Round(b.Price)).ToList();
        Assert.Single(rounded);

        List<double> abs = db.Table<Book>().Select(b => Math.Abs(b.Price)).ToList();
        Assert.Single(abs);
    }

    [Fact]
    public void LowFloor_BlocksJsonFunctions()
    {
        using TestDatabase db = new(b => b.UseMinimumSqliteVersion(SQLiteMinimumVersion.V3_8));
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "{\"k\":1}", AuthorId = 1, Price = 1 });

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() =>
            db.Table<Book>().Select(b => SQLiteJsonFunctions.Valid(b.Title)).ToList());

        Assert.Contains("SQLiteJsonFunctions.Valid", ex.Message);
        Assert.Contains("3.9", ex.Message);
    }

    [Fact]
    public void LowFloor_BlocksRenameColumn()
    {
        using TestDatabase db = new(b => b.UseMinimumSqliteVersion(SQLiteMinimumVersion.V3_22));
        db.Table<Book>().Schema.CreateTable();

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() =>
            db.Schema.RenameColumn<Book>("BookTitle", "BookTitle2"));

        Assert.Contains("RENAME COLUMN", ex.Message);
        Assert.Contains("3.25", ex.Message);
    }

    [Fact]
    public void LowFloor_BlocksFTS5()
    {
        using TestDatabase db = new(b => b.UseMinimumSqliteVersion(SQLiteMinimumVersion.V3_8));

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() =>
            db.Schema.CreateTable<CategorisedSearch>());

        Assert.Contains("FTS5", ex.Message);
        Assert.Contains("3.9", ex.Message);
    }

    [Fact]
    public void LowFloor_BlocksRTree()
    {
        using TestDatabase db = new(b => b.UseMinimumSqliteVersion(SQLiteMinimumVersion.V3_8));

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() =>
            db.Schema.CreateTable<Region2D>());

        Assert.Contains("R-Tree", ex.Message);
        Assert.Contains("3.8.5", ex.Message);
    }

    [Fact]
    public void LowFloor_BlocksCte()
    {
        using TestDatabase db = new(b => b.UseMinimumSqliteVersion(SQLiteMinimumVersion.V3_8));
        db.Table<Book>().Schema.CreateTable();

        SQLiteCte<Book> cte = db.With(() => db.Table<Book>());

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() =>
            (from b in cte select b).ToList());

        Assert.Contains("Common table expressions", ex.Message);
        Assert.Contains("3.8.3", ex.Message);
    }

    [Fact]
    public void LowFloor_BlocksExplainQueryPlan()
    {
        using TestDatabase db = new(b => b.UseMinimumSqliteVersion(SQLiteMinimumVersion.V3_22));
        db.Table<Book>().Schema.CreateTable();

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() =>
            db.Table<Book>().ExplainQueryPlan());

        Assert.Contains("EXPLAIN QUERY PLAN", ex.Message);
        Assert.Contains("3.24", ex.Message);
    }

    [Fact]
    public void LowFloor_BlocksPragmaTableInfo()
    {
        using TestDatabase db = new(b => b.UseMinimumSqliteVersion(SQLiteMinimumVersion.V3_14));
        db.Table<Book>().Schema.CreateTable();

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() =>
            db.Pragmas.TableInfo("Books").ToList());

        Assert.Contains("pragma_table_info", ex.Message);
        Assert.Contains("3.16", ex.Message);
    }

    [Fact]
    public void LowFloor_BlocksPrintf()
    {
        using TestDatabase db = new(b => b.UseMinimumSqliteVersion(SQLiteMinimumVersion.V3_8));
        db.Table<Book>().Schema.CreateTable();

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() =>
            db.Table<Book>().Select(b => SQLiteFunctions.Printf("%s", b.Title)).ToList());

        Assert.Contains("Printf", ex.Message);
        Assert.Contains("3.8.3", ex.Message);
    }

    [Fact]
    public void LowFloor_BlocksExpressionDefaultOnTableBuilder()
    {
        using ModelTestDatabase db = new(
            model => model.Entity<Book>().Default(b => b.Price, () => SQLiteFunctions.Random()),
            b => b.UseMinimumSqliteVersion(SQLiteMinimumVersion.V3_22));

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() =>
            db.Schema.CreateTable<Book>());

        Assert.Contains("Column DEFAULT with computed expression", ex.Message);
        Assert.Contains("3.31", ex.Message);
    }

    [Fact]
    public void LowFloor_BlocksAddColumnWithExpressionDefault()
    {
        using TestDatabase db = new(b => b.UseMinimumSqliteVersion(SQLiteMinimumVersion.V3_22));
        db.Table<Book>().Schema.CreateTable();

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() =>
            db.Schema.AddColumn<Book>("Title", () => "x"));

        Assert.Contains("ALTER TABLE ADD COLUMN with computed DEFAULT", ex.Message);
        Assert.Contains("3.31", ex.Message);
    }

    [Fact]
    public void LowFloor_BlocksLastIndexOf()
    {
        using TestDatabase db = new(b => b.UseMinimumSqliteVersion(SQLiteMinimumVersion.V3_8));
        db.Table<Book>().Schema.CreateTable();
        db.Table<Book>().Add(new Book { Id = 1, Title = "hello world", AuthorId = 1, Price = 1 });

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() =>
            db.Table<Book>().Select(b => b.Title.LastIndexOf("o")).ToList());

        Assert.Contains("LastIndexOf", ex.Message);
        Assert.Contains("3.8.3", ex.Message);
    }

    [Fact]
    public void LowFloor_BlocksReturningOnSQLiteReturningTable()
    {
        using TestDatabase db = new(b => b.UseMinimumSqliteVersion(SQLiteMinimumVersion.V3_33));
        db.Table<Book>().Schema.CreateTable();

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() =>
            db.Table<Book>().Returning().Add(new Book { Id = 1, Title = "x", AuthorId = 1, Price = 1 }));

        Assert.Contains("RETURNING", ex.Message);
        Assert.Contains("3.35", ex.Message);
    }
}

[System.ComponentModel.DataAnnotations.Schema.Table("StrictBookEntity")]
[SQLite.Framework.Attributes.StrictTable]
public class StrictBookEntity
{
    [System.ComponentModel.DataAnnotations.Key]
    public int Id { get; set; }
}
#endif
