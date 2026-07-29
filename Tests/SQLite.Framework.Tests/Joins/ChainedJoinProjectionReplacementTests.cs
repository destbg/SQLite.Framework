using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H24jChainHeads")]
public class H24jChainHead
{
    [Key]
    public int Id { get; set; }

    public int K { get; set; }
}

[Table("H24jChainMids")]
public class H24jChainMid
{
    [Key]
    public int Id { get; set; }

    public int K { get; set; }

    public string Label { get; set; } = "";
}

[Table("H24jChainTails")]
public class H24jChainTail
{
    [Key]
    public int Id { get; set; }

    public int K { get; set; }

    public string Title { get; set; } = "";
}

public class H24jChainRow
{
    public int Code { get; set; }

    public string Text { get; set; } = "";
}

public class ChainedJoinProjectionReplacementTests
{
    public static string Decorate(string value)
    {
        return "<" + value + ">";
    }

    [Fact]
    public void SecondJoinProjectionReplacesTheFirstClientProjection()
    {
        using TestDatabase db = Setup(nameof(SecondJoinProjectionReplacesTheFirstClientProjection));

        List<(int Code, string Text)> expected = Heads()
            .Join(Mids(), h => h.K, m => m.K, (h, m) => new H24jChainRow { Code = h.Id, Text = Decorate(m.Label) })
            .Join(Tails(), r => r.Code, t => t.K, (r, t) => new H24jChainRow { Code = t.Id, Text = t.Title })
            .Select(r => (r.Code, r.Text))
            .OrderBy(r => r.Code)
            .ToList();

        List<(int Code, string Text)> actual = db.Table<H24jChainHead>()
            .Join(db.Table<H24jChainMid>(), h => h.K, m => m.K, (h, m) => new H24jChainRow { Code = h.Id, Text = Decorate(m.Label) })
            .Join(db.Table<H24jChainTail>(), r => r.Code, t => t.K, (r, t) => new H24jChainRow { Code = t.Id, Text = t.Title })
            .AsEnumerable()
            .Select(r => (r.Code, r.Text))
            .OrderBy(r => r.Code)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H24jChainHead> Heads()
    {
        return [new H24jChainHead { Id = 1, K = 10 }];
    }

    private static List<H24jChainMid> Mids()
    {
        return [new H24jChainMid { Id = 1, K = 10, Label = "m1" }];
    }

    private static List<H24jChainTail> Tails()
    {
        return [new H24jChainTail { Id = 7, K = 1, Title = "t7" }];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(null, methodName);
        db.Table<H24jChainHead>().Schema.CreateTable();
        db.Table<H24jChainMid>().Schema.CreateTable();
        db.Table<H24jChainTail>().Schema.CreateTable();
        db.Table<H24jChainHead>().AddRange(Heads());
        db.Table<H24jChainMid>().AddRange(Mids());
        db.Table<H24jChainTail>().AddRange(Tails());
        return db;
    }
}
