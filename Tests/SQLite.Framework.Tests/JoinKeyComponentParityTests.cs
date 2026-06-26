using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

file sealed class JbLeft
{
    [Key]
    public int Id { get; set; }

    public int K { get; set; }

    public int? V { get; set; }
}

file sealed class JbRight
{
    [Key]
    public int Id { get; set; }

    public int K { get; set; }

    public int? V { get; set; }
}

public class JoinKeyComponentParityTests
{
    [Fact]
    public void SingleNullCheckKey_MatchesDotNet()
    {
        JbLeft[] lefts = [new JbLeft { Id = 1, K = 1, V = 10 }, new JbLeft { Id = 2, K = 1, V = null }];
        JbRight[] rights = [new JbRight { Id = 1, K = 1, V = 20 }, new JbRight { Id = 2, K = 1, V = null }];
        using TestDatabase db = new();
        db.Table<JbLeft>().Schema.CreateTable();
        db.Table<JbRight>().Schema.CreateTable();
        db.Table<JbLeft>().AddRange(lefts);
        db.Table<JbRight>().AddRange(rights);

        List<string> expected = (from l in lefts
            join r in rights on l.V != null equals r.V != null
            select l.Id + "-" + r.Id).OrderBy(s => s).ToList();
        List<string> actual = (from l in db.Table<JbLeft>()
            join r in db.Table<JbRight>() on l.V != null equals r.V != null
            select l.Id + "-" + r.Id).ToList().OrderBy(s => s).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CompositeNullCheckKey_MatchesDotNet()
    {
        JbLeft[] lefts = [new JbLeft { Id = 1, K = 1, V = 10 }, new JbLeft { Id = 2, K = 1, V = null }];
        JbRight[] rights = [new JbRight { Id = 1, K = 1, V = 20 }, new JbRight { Id = 2, K = 1, V = null }];
        using TestDatabase db = new();
        db.Table<JbLeft>().Schema.CreateTable();
        db.Table<JbRight>().Schema.CreateTable();
        db.Table<JbLeft>().AddRange(lefts);
        db.Table<JbRight>().AddRange(rights);

        List<string> expected = (from l in lefts
            join r in rights on new { l.K, HasV = l.V != null } equals new { r.K, HasV = r.V != null }
            select l.Id + "-" + r.Id).OrderBy(s => s).ToList();
        List<string> actual = (from l in db.Table<JbLeft>()
            join r in db.Table<JbRight>() on new { l.K, HasV = l.V != null } equals new { r.K, HasV = r.V != null }
            select l.Id + "-" + r.Id).ToList().OrderBy(s => s).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void LeftJoinNullCheckKey_MatchesDotNet()
    {
        JbLeft[] lefts = [new JbLeft { Id = 1, K = 1, V = 10 }, new JbLeft { Id = 2, K = 1, V = null }];
        JbRight[] rights = [new JbRight { Id = 1, K = 1, V = 20 }, new JbRight { Id = 2, K = 1, V = null }];
        using TestDatabase db = new();
        db.Table<JbLeft>().Schema.CreateTable();
        db.Table<JbRight>().Schema.CreateTable();
        db.Table<JbLeft>().AddRange(lefts);
        db.Table<JbRight>().AddRange(rights);

        List<string> expected = (from l in lefts
            join r in rights on new { l.K, HasV = l.V != null } equals new { r.K, HasV = r.V != null } into g
            from r in g.DefaultIfEmpty()
            select l.Id + "-" + (r == null ? 0 : r.Id)).OrderBy(s => s).ToList();
        List<string> actual = (from l in db.Table<JbLeft>()
            join r in db.Table<JbRight>() on new { l.K, HasV = l.V != null } equals new { r.K, HasV = r.V != null } into g
            from r in g.DefaultIfEmpty()
            select l.Id + "-" + (r == null ? 0 : r.Id)).ToList().OrderBy(s => s).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NullableRelationalKey_WithNullRows_MatchesDotNet()
    {
        JbLeft[] lefts = [new JbLeft { Id = 1, K = 1, V = null }, new JbLeft { Id = 2, K = 1, V = 20 }];
        JbRight[] rights = [new JbRight { Id = 1, K = 1, V = null }, new JbRight { Id = 2, K = 1, V = 30 }];
        using TestDatabase db = new();
        db.Table<JbLeft>().Schema.CreateTable();
        db.Table<JbRight>().Schema.CreateTable();
        db.Table<JbLeft>().AddRange(lefts);
        db.Table<JbRight>().AddRange(rights);

        List<string> expected = (from l in lefts
            join r in rights on l.V > 15 equals r.V > 15
            select l.Id + "-" + r.Id).OrderBy(s => s).ToList();
        List<string> actual = (from l in db.Table<JbLeft>()
            join r in db.Table<JbRight>() on l.V > 15 equals r.V > 15
            select l.Id + "-" + r.Id).ToList().OrderBy(s => s).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void RelationalKey_NoNullRows_MatchesDotNet()
    {
        JbLeft[] lefts = [new JbLeft { Id = 1, K = 1, V = 5 }, new JbLeft { Id = 2, K = 1, V = 99 }];
        JbRight[] rights = [new JbRight { Id = 1, K = 1, V = 5 }, new JbRight { Id = 2, K = 1, V = 99 }];
        using TestDatabase db = new();
        db.Table<JbLeft>().Schema.CreateTable();
        db.Table<JbRight>().Schema.CreateTable();
        db.Table<JbLeft>().AddRange(lefts);
        db.Table<JbRight>().AddRange(rights);

        List<string> expected = (from l in lefts
            join r in rights on l.V > 15 equals r.V > 15
            select l.Id + "-" + r.Id).OrderBy(s => s).ToList();
        List<string> actual = (from l in db.Table<JbLeft>()
            join r in db.Table<JbRight>() on l.V > 15 equals r.V > 15
            select l.Id + "-" + r.Id).ToList().OrderBy(s => s).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void WhereBooleanEquality_MatchesDotNet()
    {
        JbLeft[] lefts =
        [
            new JbLeft { Id = 1, K = 1, V = 10 },
            new JbLeft { Id = 2, K = 1, V = null },
            new JbLeft { Id = 3, K = 2, V = 10 },
        ];
        using TestDatabase db = new();
        db.Table<JbLeft>().Schema.CreateTable();
        db.Table<JbLeft>().AddRange(lefts);

        List<int> expected = lefts.Where(l => (l.V != null) == (l.K == 1)).Select(l => l.Id).OrderBy(i => i).ToList();
        List<int> actual = db.Table<JbLeft>().Where(l => (l.V != null) == (l.K == 1)).Select(l => l.Id).ToList().OrderBy(i => i).ToList();

        Assert.Equal(expected, actual);
    }
}
