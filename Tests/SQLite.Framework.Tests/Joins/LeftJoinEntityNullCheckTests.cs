using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class NjLeftRow
{
    [Key]
    public int Id { get; set; }
    public int Fk { get; set; }
    public string Label { get; set; } = "";
}

internal sealed class NjRightRow
{
    [Key]
    public int Marker { get; set; }
    public int Fk { get; set; }
    public int? X { get; set; }
}

public class LeftJoinEntityNullCheckTests
{
    private static readonly NjLeftRow[] Lefts =
    [
        new NjLeftRow { Id = 1, Fk = 100, Label = "matched" },
        new NjLeftRow { Id = 2, Fk = 200, Label = "orphan" },
    ];

    private static readonly NjRightRow[] Rights =
    [
        new NjRightRow { Marker = 7, Fk = 100, X = null },
    ];

    private static TestDatabase CreateDb()
    {
        TestDatabase db = new();
        db.Table<NjLeftRow>().Schema.CreateTable();
        db.Table<NjRightRow>().Schema.CreateTable();
        db.Table<NjLeftRow>().AddRange(Lefts);
        db.Table<NjRightRow>().AddRange(Rights);
        return db;
    }

    [Fact]
    public void Projection_RightNotNull_MatchesLinqToObjects()
    {
        using TestDatabase db = CreateDb();

        List<bool> expected = (from l in Lefts
            join r in Rights on l.Fk equals r.Fk into g
            from r in g.DefaultIfEmpty()
            orderby l.Id
            select r != null).ToList();
        List<bool> actual = (from l in db.Table<NjLeftRow>()
            join r in db.Table<NjRightRow>() on l.Fk equals r.Fk into g
            from r in g.DefaultIfEmpty()
            orderby l.Id
            select r != null).ToList();

        Assert.Equal([true, false], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Projection_RightEqualNull_MatchesLinqToObjects()
    {
        using TestDatabase db = CreateDb();

        List<bool> expected = (from l in Lefts
            join r in Rights on l.Fk equals r.Fk into g
            from r in g.DefaultIfEmpty()
            orderby l.Id
            select r == null).ToList();
        List<bool> actual = (from l in db.Table<NjLeftRow>()
            join r in db.Table<NjRightRow>() on l.Fk equals r.Fk into g
            from r in g.DefaultIfEmpty()
            orderby l.Id
            select r == null).ToList();

        Assert.Equal([false, true], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Filter_WhereRightNotNull_MatchesLinqToObjects()
    {
        using TestDatabase db = CreateDb();

        List<int> expected = (from l in Lefts
            join r in Rights on l.Fk equals r.Fk into g
            from r in g.DefaultIfEmpty()
            where r != null
            orderby l.Id
            select l.Id).ToList();
        List<int> actual = (from l in db.Table<NjLeftRow>()
            join r in db.Table<NjRightRow>() on l.Fk equals r.Fk into g
            from r in g.DefaultIfEmpty()
            where r != null
            orderby l.Id
            select l.Id).ToList();

        Assert.Equal([1], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Filter_WhereRightEqualNull_MatchesLinqToObjects()
    {
        using TestDatabase db = CreateDb();

        List<int> expected = (from l in Lefts
            join r in Rights on l.Fk equals r.Fk into g
            from r in g.DefaultIfEmpty()
            where r == null
            orderby l.Id
            select l.Id).ToList();
        List<int> actual = (from l in db.Table<NjLeftRow>()
            join r in db.Table<NjRightRow>() on l.Fk equals r.Fk into g
            from r in g.DefaultIfEmpty()
            where r == null
            orderby l.Id
            select l.Id).ToList();

        Assert.Equal([2], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Conditional_ReadsMatchedColumn_MatchesLinqToObjects()
    {
        using TestDatabase db = CreateDb();

        List<int> expected = (from l in Lefts
            join r in Rights on l.Fk equals r.Fk into g
            from r in g.DefaultIfEmpty()
            orderby l.Id
            select r != null ? r.Marker : -1).ToList();
        List<int> actual = (from l in db.Table<NjLeftRow>()
            join r in db.Table<NjRightRow>() on l.Fk equals r.Fk into g
            from r in g.DefaultIfEmpty()
            orderby l.Id
            select r != null ? r.Marker : -1).ToList();

        Assert.Equal([7, -1], expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NullCheck_UsesPrimaryKeyColumn_NotShortestNullableColumn()
    {
        using TestDatabase db = CreateDb();

        var query = from l in db.Table<NjLeftRow>()
            join r in db.Table<NjRightRow>() on l.Fk equals r.Fk into g
            from r in g.DefaultIfEmpty()
            select r != null;
        string sql = query.ToSqlCommand().CommandText;

        Assert.Equal("SELECT n1.\"Marker\" IS NOT NULL AS \"8\"\nFROM \"NjLeftRow\" AS n0\nLEFT JOIN \"NjRightRow\" AS n1 ON n0.\"Fk\" = n1.\"Fk\"", sql);
        Assert.Equal("SELECT n1.\"Marker\" IS NOT NULL AS \"8\"\nFROM \"NjLeftRow\" AS n0\nLEFT JOIN \"NjRightRow\" AS n1 ON n0.\"Fk\" = n1.\"Fk\"", sql);
    }
}
