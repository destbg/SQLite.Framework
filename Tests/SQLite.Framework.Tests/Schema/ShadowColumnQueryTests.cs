using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using SQLite.Framework.Enums;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public class ShadowColumnQueryTests
{
    private static ModelTestDatabase NewDatabase([CallerMemberName] string? methodName = null)
    {
        ModelTestDatabase db = new(model =>
        {
            model.Entity<ShQ>()
                .Column("Rank", SQLiteColumnType.Integer, nullable: true)
                .Column("Tag", SQLiteColumnType.Text, nullable: true);
            model.Entity<ShQChild>()
                .Column("Note", SQLiteColumnType.Text, nullable: true);
        }, methodName);

        db.Schema.CreateTable<ShQ>();
        db.Schema.CreateTable<ShQChild>();

        db.Execute("INSERT INTO \"ShQ\" (\"Id\", \"Name\", \"Score\", \"Rank\", \"Tag\") VALUES (1, 'a', 5, 30, 'x')");
        db.Execute("INSERT INTO \"ShQ\" (\"Id\", \"Name\", \"Score\", \"Rank\", \"Tag\") VALUES (2, 'b', 6, 10, 'y')");
        db.Execute("INSERT INTO \"ShQ\" (\"Id\", \"Name\", \"Score\", \"Rank\", \"Tag\") VALUES (3, 'c', 7, 20, 'x')");
        return db;
    }

    [Fact]
    public void Where_FiltersByShadowIntColumn()
    {
        using ModelTestDatabase db = NewDatabase();

        List<int> ids = db.Table<ShQ>()
            .Where(x => SQLiteColumn.Of<int>(x, "Rank") >= 20)
            .OrderBy(x => x.Id)
            .Select(x => x.Id)
            .ToList();

        Assert.Equal([1, 3], ids);
    }

    [Fact]
    public void Where_FiltersByShadowStringColumn()
    {
        using ModelTestDatabase db = NewDatabase();

        List<int> ids = db.Table<ShQ>()
            .Where(x => SQLiteColumn.Of<string>(x, "Tag") == "x")
            .OrderBy(x => x.Id)
            .Select(x => x.Id)
            .ToList();

        Assert.Equal([1, 3], ids);
    }

    [Fact]
    public void Select_ScalarShadowColumn()
    {
        using ModelTestDatabase db = NewDatabase();

        List<int> ranks = db.Table<ShQ>()
            .OrderBy(x => x.Id)
            .Select(x => SQLiteColumn.Of<int>(x, "Rank"))
            .ToList();

        Assert.Equal([30, 10, 20], ranks);
    }

    [Fact]
    public void Select_ProjectionWithShadowColumn()
    {
        using ModelTestDatabase db = NewDatabase();

        var rows = db.Table<ShQ>()
            .OrderBy(x => x.Id)
            .Select(x => new { x.Id, Rank = SQLiteColumn.Of<int>(x, "Rank"), Tag = SQLiteColumn.Of<string>(x, "Tag") })
            .ToList();

        Assert.Equal(3, rows.Count);
        Assert.Equal(30, rows[0].Rank);
        Assert.Equal("x", rows[0].Tag);
    }

    [Fact]
    public void OrderBy_ShadowColumn()
    {
        using ModelTestDatabase db = NewDatabase();

        List<int> ids = db.Table<ShQ>()
            .OrderBy(x => SQLiteColumn.Of<int>(x, "Rank"))
            .Select(x => x.Id)
            .ToList();

        Assert.Equal([2, 3, 1], ids);
    }

    [Fact]
    public void OrderByDescending_ShadowColumn()
    {
        using ModelTestDatabase db = NewDatabase();

        List<int> ids = db.Table<ShQ>()
            .OrderByDescending(x => SQLiteColumn.Of<int>(x, "Rank"))
            .Select(x => x.Id)
            .ToList();

        Assert.Equal([1, 3, 2], ids);
    }

    [Fact]
    public void ThenBy_ShadowColumn()
    {
        using ModelTestDatabase db = NewDatabase();

        List<int> ids = db.Table<ShQ>()
            .OrderBy(x => SQLiteColumn.Of<string>(x, "Tag"))
            .ThenByDescending(x => SQLiteColumn.Of<int>(x, "Rank"))
            .Select(x => x.Id)
            .ToList();

        Assert.Equal([1, 3, 2], ids);
    }

    [Fact]
    public void GroupBy_ShadowColumnKey()
    {
        using ModelTestDatabase db = NewDatabase();

        var counts = db.Table<ShQ>()
            .GroupBy(x => SQLiteColumn.Of<string>(x, "Tag"))
            .Select(g => new { Tag = g.Key, Count = g.Count() })
            .OrderBy(r => r.Tag)
            .ToList();

        Assert.Equal(2, counts.Count);
        Assert.Equal("x", counts[0].Tag);
        Assert.Equal(2, counts[0].Count);
        Assert.Equal("y", counts[1].Tag);
        Assert.Equal(1, counts[1].Count);
    }

    [Fact]
    public void Join_ResultSelector_ShadowColumnsOnBothSides()
    {
        using ModelTestDatabase db = NewDatabase();
        db.Execute("INSERT INTO \"ShQChild\" (\"Id\", \"ShQId\", \"Note\") VALUES (100, 1, 'first')");
        db.Execute("INSERT INTO \"ShQChild\" (\"Id\", \"ShQId\", \"Note\") VALUES (101, 3, 'third')");

        var rows = (
            from p in db.Table<ShQ>()
            join c in db.Table<ShQChild>() on p.Id equals c.ShQId
            select new { Tag = SQLiteColumn.Of<string>(p, "Tag"), Note = SQLiteColumn.Of<string>(c, "Note") }).ToList();

        Assert.Equal(2, rows.Count);
        Assert.All(rows, r => Assert.Equal("x", r.Tag));
        Assert.Contains(rows, r => r.Note == "first");
        Assert.Contains(rows, r => r.Note == "third");
    }

    [Fact]
    public void Join_Key_ShadowColumnOnBothSides()
    {
        using ModelTestDatabase db = NewDatabase();

        int pairs = (
            from a in db.Table<ShQ>()
            join b in db.Table<ShQ>() on SQLiteColumn.Of<string>(a, "Tag") equals SQLiteColumn.Of<string>(b, "Tag")
            select new { A = a.Id, B = b.Id }).Count();

        Assert.Equal(5, pairs);
    }

    [Fact]
    public void CorrelatedSubquery_OuterShadowColumn()
    {
        using ModelTestDatabase db = NewDatabase();

        List<int> ids = db.Table<ShQ>()
            .Where(x => db.Table<ShQ>().Any(y => SQLiteColumn.Of<int>(y, "Rank") > SQLiteColumn.Of<int>(x, "Rank")))
            .OrderBy(x => x.Id)
            .Select(x => x.Id)
            .ToList();

        Assert.Equal([2, 3], ids);
    }

    [Fact]
    public void Returning_ProjectsShadowColumn_Unqualified()
    {
        using ModelTestDatabase db = NewDatabase();

        int rank = db.Table<ShQ>()
            .WithColumns(c => c.Set(x => SQLiteColumn.Of<int>(x, "Rank"), 42))
            .Returning(x => SQLiteColumn.Of<int>(x, "Rank"))
            .Add(new ShQ { Id = 9, Name = "z", Score = 1 });

        Assert.Equal(42, rank);
    }

    [Fact]
    public async Task Where_ShadowColumn_Async()
    {
        using ModelTestDatabase db = NewDatabase();

        List<int> ids = await db.Table<ShQ>()
            .Where(x => SQLiteColumn.Of<int>(x, "Rank") == 10)
            .Select(x => x.Id)
            .ToListAsync(TestContext.Current.CancellationToken);

        Assert.Equal([2], ids);
    }

    [Fact]
    public void TypeConverter_PlainJsonShadowColumn_ReferencesColumnDirectly()
    {
        using ModelTestDatabase db = new(
            model => model.Entity<ShQ>().Column("Data", SQLiteColumnType.Text, nullable: true),
            options => options.TypeConverters[typeof(Address)] = new SQLiteJsonConverter<Address>(ShadowJsonContext.Default.Address));
        db.Schema.CreateTable<ShQ>();

        string sql = db.Table<ShQ>()
            .Select(x => SQLiteColumn.Of<Address>(x, "Data"))
            .ToSql();

        Assert.Equal("SELECT s0.\"Data\" AS \"4\"\nFROM \"ShQ\" AS s0", sql);
        Assert.Equal("SELECT s0.\"Data\" AS \"4\"\nFROM \"ShQ\" AS s0", sql);
    }

#if !SQLITECIPHER
    [Fact]
    public void TypeConverter_JsonbShadowColumn_WrapsValueWithJson()
    {
        using ModelTestDatabase db = new(
            model => model.Entity<ShQ>().Column("Data", SQLiteColumnType.Blob, nullable: true),
            options => options.TypeConverters[typeof(Address)] = new SQLiteJsonbConverter<Address>(ShadowJsonContext.Default.Address));
        db.Schema.CreateTable<ShQ>();

        string sql = db.Table<ShQ>()
            .Select(x => SQLiteColumn.Of<Address>(x, "Data"))
            .ToSql();

        Assert.Equal("SELECT json(s0.\"Data\") AS \"4\"\nFROM \"ShQ\" AS s0", sql);
    }
#endif

    [Fact]
    public void ProjectionParameter_Throws()
    {
        using ModelTestDatabase db = NewDatabase();

        Assert.Throws<NotSupportedException>(() => db.Table<ShQ>()
            .Select(x => new { x.Id })
            .Where(p => SQLiteColumn.Of<int>(p, "Rank") > 0)
            .ToList());
    }

    [Fact]
    public void InvalidMemberPath_Throws()
    {
        using ModelTestDatabase db = NewDatabase();

        Assert.Throws<NotSupportedException>(() => db.Table<ShQ>()
            .Where(x => SQLiteColumn.Of<int>(x.Name, "Rank") > 0)
            .ToList());
    }

    [Fact]
    public void NonRowReceiver_Throws()
    {
        using ModelTestDatabase db = NewDatabase();

        Assert.Throws<NotSupportedException>(() => db.Table<ShQ>()
            .Where(x => SQLiteColumn.Of<int>(x.Name.Trim(), "Rank") > 0)
            .ToList());
    }
}

[Table("ShQ")]
public class ShQ
{
    [Key]
    public int Id { get; set; }
    public required string Name { get; set; }
    public int Score { get; set; }
}

[Table("ShQChild")]
public class ShQChild
{
    [Key]
    public int Id { get; set; }
    public int ShQId { get; set; }
}

[JsonSerializable(typeof(Address))]
internal partial class ShadowJsonContext : JsonSerializerContext;
