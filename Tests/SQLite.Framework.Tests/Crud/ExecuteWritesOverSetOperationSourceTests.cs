using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H26oSetOperationRows")]
public class H26oSetOperationRow
{
    [Key]
    public int Id { get; set; }

    public int Score { get; set; }
}

public class ExecuteWritesOverSetOperationSourceTests
{
    [Fact]
    public void ExecuteDeleteOverAUnionReportsAnUnsupportedSource()
    {
        using TestDatabase db = Setup(nameof(ExecuteDeleteOverAUnionReportsAnUnsupportedSource));

        Assert.Throws<NotSupportedException>(() =>
            db.Table<H26oSetOperationRow>().Where(r => r.Score < 10)
                .Union(db.Table<H26oSetOperationRow>().Where(r => r.Score > 90))
                .ExecuteDelete());
    }

    [Fact]
    public void ExecuteUpdateOverAUnionReportsAnUnsupportedSource()
    {
        using TestDatabase db = Setup(nameof(ExecuteUpdateOverAUnionReportsAnUnsupportedSource));

        Assert.Throws<NotSupportedException>(() =>
            db.Table<H26oSetOperationRow>().Where(r => r.Score < 10)
                .Union(db.Table<H26oSetOperationRow>().Where(r => r.Score > 90))
                .ExecuteUpdate(s => s.Set(r => r.Score, 50)));
    }

    private static List<H26oSetOperationRow> Rows()
    {
        return
        [
            new H26oSetOperationRow { Id = 1, Score = 5 },
            new H26oSetOperationRow { Id = 2, Score = 50 },
            new H26oSetOperationRow { Id = 3, Score = 95 }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(null, methodName);
        db.Table<H26oSetOperationRow>().Schema.CreateTable();
        db.Table<H26oSetOperationRow>().AddRange(Rows());
        return db;
    }
}
