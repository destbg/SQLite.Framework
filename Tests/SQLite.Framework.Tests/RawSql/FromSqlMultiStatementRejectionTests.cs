using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("FdMultiBook")]
public class FdMultiBook
{
    [Key]
    public int Id { get; set; }

    public string Title { get; set; } = "";

    public int Pages { get; set; }
}

public class FromSqlMultiStatementRejectionTests
{
    [Fact]
    public void MultiStatementFromSqlThrowsClearError()
    {
        using TestDatabase db = new();
        db.Table<FdMultiBook>().Schema.CreateTable();
        db.Table<FdMultiBook>().Add(new FdMultiBook { Id = 1, Title = "a", Pages = 80 });
        db.Table<FdMultiBook>().Add(new FdMultiBook { Id = 2, Title = "b", Pages = 120 });

        Exception? ex = Record.Exception(() =>
            db.FromSql<FdMultiBook>("SELECT * FROM FdMultiBook; DELETE FROM FdMultiBook").ToList());

        Assert.NotNull(ex);
        Assert.True(ex is NotSupportedException or ArgumentException, ex.GetType().Name + ": " + ex.Message);
        Assert.Equal(2, db.Table<FdMultiBook>().Count());
    }
}
