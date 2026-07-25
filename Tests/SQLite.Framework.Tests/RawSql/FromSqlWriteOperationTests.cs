using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("RawWriteBook")]
public class RawWriteBookRow
{
    [Key]
    public int Id { get; set; }

    public int Pages { get; set; }
}

public class FromSqlWriteOperationTests
{
    [Fact]
    public void ExecuteDeleteOnFromSqlThrows()
    {
        using TestDatabase db = new();
        db.Table<RawWriteBookRow>().Schema.CreateTable();

        Assert.Throws<NotSupportedException>(() =>
            db.FromSql<RawWriteBookRow>("SELECT * FROM \"RawWriteBook\"").ExecuteDelete());
    }

    [Fact]
    public void ExecuteUpdateOnFromSqlThrows()
    {
        using TestDatabase db = new();
        db.Table<RawWriteBookRow>().Schema.CreateTable();

        Assert.Throws<NotSupportedException>(() =>
            db.FromSql<RawWriteBookRow>("SELECT * FROM \"RawWriteBook\"").ExecuteUpdate(s => s.Set(b => b.Pages, 1)));
    }
}
