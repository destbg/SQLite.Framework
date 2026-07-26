using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[JsonSerializable(typeof(List<int>))]
public partial class GjOuterMemberContext : JsonSerializerContext;

[Table("GjOuterMemberRows")]
public class GjOuterMemberRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public List<int> Nums { get; set; } = [];
}

[Table("GjInnerMemberRows")]
public class GjInnerMemberRow
{
    [Key]
    public int Id { get; set; }

    public int OuterId { get; set; }

    public string Tag { get; set; } = "";
}

[Table("GjThirdMemberRows")]
public class GjThirdMemberRow
{
    [Key]
    public int Id { get; set; }

    public string Note { get; set; } = "";
}

public class GroupJoinResultSelectorOuterMemberTests
{
    [Fact]
    public void ADroppedGroupBesideAnInnerMemberReadMatchesLinq()
    {
        using TestDatabase db = Setup(nameof(ADroppedGroupBesideAnInnerMemberReadMatchesLinq));
        db.Table<GjThirdMemberRow>().Schema.CreateTable();
        db.Table<GjThirdMemberRow>().AddRange(Thirds());

        List<string> expected = Outers()
            .GroupJoin(Inners(), o => o.Id, i => i.OuterId, (o, g) => new { o.Name, Group = g })
            .Join(Thirds(), x => 1, t => t.Id, (x, t) => x.Name + t.Note)
            .OrderBy(v => v)
            .ToList();

        List<string> actual = db.Table<GjOuterMemberRow>()
            .GroupJoin(db.Table<GjInnerMemberRow>(), o => o.Id, i => i.OuterId, (o, g) => new { o.Name, Group = g })
            .Join(db.Table<GjThirdMemberRow>(), x => 1, t => t.Id, (x, t) => x.Name + t.Note)
            .AsEnumerable()
            .OrderBy(v => v)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void GroupJoinResultSelectorReadingAnOuterMemberMatchesLinq()
    {
        using TestDatabase db = Setup(nameof(GroupJoinResultSelectorReadingAnOuterMemberMatchesLinq));

        List<string> expected = Outers()
            .GroupJoin(Inners(), o => o.Id, i => i.OuterId, (o, g) => new { o.Name, Group = g })
            .SelectMany(x => x.Group, (x, i) => x.Name + ":" + i.Tag)
            .OrderBy(v => v)
            .ToList();

        List<string> actual = db.Table<GjOuterMemberRow>()
            .GroupJoin(db.Table<GjInnerMemberRow>(), o => o.Id, i => i.OuterId, (o, g) => new { o.Name, Group = g })
            .SelectMany(x => x.Group, (x, i) => x.Name + ":" + i.Tag)
            .AsEnumerable()
            .OrderBy(v => v)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void GroupJoinResultSelectorNestingTheGroupMatchesLinq()
    {
        using TestDatabase db = Setup(nameof(GroupJoinResultSelectorNestingTheGroupMatchesLinq));

        List<string> expected = Outers()
            .GroupJoin(Inners(), o => o.Id, i => i.OuterId, (o, g) => new { o.Name, Nested = new { Group = g } })
            .SelectMany(x => x.Nested.Group, (x, i) => x.Name + ":" + i.Tag)
            .OrderBy(v => v)
            .ToList();

        List<string> actual = db.Table<GjOuterMemberRow>()
            .GroupJoin(db.Table<GjInnerMemberRow>(), o => o.Id, i => i.OuterId,
                (o, g) => new { o.Name, Nested = new { Group = g } })
            .SelectMany(x => x.Nested.Group, (x, i) => x.Name + ":" + i.Tag)
            .AsEnumerable()
            .OrderBy(v => v)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void UsingTheSameGroupTwiceThrows()
    {
        using TestDatabase db = Setup(nameof(UsingTheSameGroupTwiceThrows));

        Assert.ThrowsAny<Exception>(() => db.Table<GjOuterMemberRow>()
            .GroupJoin(db.Table<GjInnerMemberRow>(), o => o.Id, i => i.OuterId, (o, g) => new { o.Name, Group = g })
            .SelectMany(x => x.Group, (x, i) => new { x.Name, x.Group, i.Tag })
            .SelectMany(x => x.Group, (x, i) => x.Name + ":" + i.Tag)
            .ToList());
    }

    [Fact]
    public void GroupJoinResultSelectorReturningTheGroupItselfThrows()
    {
        using TestDatabase db = Setup(nameof(GroupJoinResultSelectorReturningTheGroupItselfThrows));

        Assert.ThrowsAny<Exception>(() => db.Table<GjOuterMemberRow>()
            .GroupJoin(db.Table<GjInnerMemberRow>(), o => o.Id, i => i.OuterId, (o, g) => g)
            .SelectMany(g => g, (g, i) => i.Tag)
            .ToList());
    }

    [Fact]
    public void FlatteningTheSameGroupTwiceThrows()
    {
        using TestDatabase db = Setup(nameof(FlatteningTheSameGroupTwiceThrows));

        Assert.ThrowsAny<Exception>(() => (from o in db.Table<GjOuterMemberRow>()
                                           join i in db.Table<GjInnerMemberRow>() on o.Id equals i.OuterId into g
                                           from first in g
                                           from second in g
                                           select first.Tag + second.Tag)
            .ToList());
    }

    [Fact]
    public void FlatteningAMemberCollectionWhileAGroupIsOpenIsRejected()
    {
        using TestDatabase db = Setup(nameof(FlatteningAMemberCollectionWhileAGroupIsOpenIsRejected));

        Assert.ThrowsAny<Exception>(() => db.Table<GjOuterMemberRow>()
            .GroupJoin(db.Table<GjInnerMemberRow>(), o => o.Id, i => i.OuterId, (o, g) => new { o.Nums, Group = g })
            .SelectMany(x => x.Nums, (x, n) => n)
            .ToList());
    }

    private static List<GjThirdMemberRow> Thirds()
    {
        return [new GjThirdMemberRow { Id = 1, Note = "!" }];
    }

    private static List<GjOuterMemberRow> Outers()
    {
        return
        [
            new GjOuterMemberRow { Id = 1, Name = "a", Nums = [1] },
            new GjOuterMemberRow { Id = 2, Name = "b", Nums = [2] }
        ];
    }

    private static List<GjInnerMemberRow> Inners()
    {
        return
        [
            new GjInnerMemberRow { Id = 1, OuterId = 1, Tag = "x" },
            new GjInnerMemberRow { Id = 2, OuterId = 2, Tag = "y" }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(b => b.AddJsonContext(GjOuterMemberContext.Default), methodName);
        db.Table<GjOuterMemberRow>().Schema.CreateTable();
        db.Table<GjInnerMemberRow>().Schema.CreateTable();
        db.Table<GjOuterMemberRow>().AddRange(Outers());
        db.Table<GjInnerMemberRow>().AddRange(Inners());
        return db;
    }
}
