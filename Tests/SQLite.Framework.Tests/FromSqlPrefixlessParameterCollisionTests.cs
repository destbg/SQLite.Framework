using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Attributes;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H20CmdPrefixRow")]
public class H20CmdPrefixRow
{
    [Key]
    [AutoIncrement]
    public int Id { get; set; }

    public int X { get; set; }
}

public class FromSqlPrefixlessParameterCollisionTests
{
    [Fact]
    public void PrefixlessParameterBindsRawSqlSlotOverGeneratedProjectionParameter()
    {
        using TestDatabase db = new();
        db.Table<H20CmdPrefixRow>().Schema.CreateTable();
        db.Table<H20CmdPrefixRow>().Add(new H20CmdPrefixRow { X = 1 });
        db.Table<H20CmdPrefixRow>().Add(new H20CmdPrefixRow { X = 2 });

        List<H20CmdPrefixRow> rows = [new() { X = 1 }, new() { X = 2 }];
        List<int> expected = rows.Where(r => r.X == 1).Select(r => r.X + 10).ToList();

        List<int> actual = db.FromSql<H20CmdPrefixRow>(
                "SELECT * FROM H20CmdPrefixRow WHERE X = :p1",
                new SQLiteParameter { Name = "p1", Value = 1 })
            .Select(r => r.X + 10)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void DifferentlyPrefixedParameterBindsRawSqlSlotOverGeneratedProjectionParameter()
    {
        using TestDatabase db = new();
        db.Table<H20CmdPrefixRow>().Schema.CreateTable();
        db.Table<H20CmdPrefixRow>().Add(new H20CmdPrefixRow { X = 1 });
        db.Table<H20CmdPrefixRow>().Add(new H20CmdPrefixRow { X = 2 });

        List<H20CmdPrefixRow> rows = [new() { X = 1 }, new() { X = 2 }];
        List<int> expected = rows.Where(r => r.X == 1).Select(r => r.X + 10).ToList();

        List<int> actual = db.FromSql<H20CmdPrefixRow>(
                "SELECT * FROM H20CmdPrefixRow WHERE X = :p1",
                new SQLiteParameter { Name = "$p1", Value = 1 })
            .Select(r => r.X + 10)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void PrefixlessParameterWithGeneratedWhereParameterBinds()
    {
        using TestDatabase db = new();
        db.Table<H20CmdPrefixRow>().Schema.CreateTable();
        db.Table<H20CmdPrefixRow>().Add(new H20CmdPrefixRow { X = 1 });
        db.Table<H20CmdPrefixRow>().Add(new H20CmdPrefixRow { X = 2 });

        List<H20CmdPrefixRow> rows = [new() { X = 1 }, new() { X = 2 }];
        List<int> expected = rows.Where(r => r.X == 1 && r.X < 100).Select(r => r.X).ToList();

        List<int> actual = db.FromSql<H20CmdPrefixRow>(
                "SELECT * FROM H20CmdPrefixRow WHERE X = :p1",
                new SQLiteParameter { Name = "p1", Value = 1 })
            .Where(r => r.X < 100)
            .Select(r => r.X)
            .ToList();

        Assert.Equal(expected, actual);
    }
}
