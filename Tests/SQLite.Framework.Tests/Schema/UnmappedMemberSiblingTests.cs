using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("UnmappedSiblingRows")]
public class UnmappedSiblingRow
{
    [Key]
    public int Id { get; set; }

    public int Amount { get; set; }

    [NotMapped]
    public int Extra { get; set; }

    public int Doubled => Amount * 2;
}

[Table("UnmappedSiblingLinks")]
public class UnmappedSiblingLink
{
    [Key]
    public int Id { get; set; }

    public int RowId { get; set; }
}

public class UnmappedMemberSiblingTests
{
    private static TestDatabase Seed()
    {
        TestDatabase db = new();
        db.Table<UnmappedSiblingRow>().Schema.CreateTable();
        db.Table<UnmappedSiblingRow>().Add(new UnmappedSiblingRow { Id = 1, Amount = 10 });
        db.Table<UnmappedSiblingRow>().Add(new UnmappedSiblingRow { Id = 2, Amount = 20 });
        return db;
    }

    [Fact]
    public void NotMappedPropertyInSelectReadsDefaultValue()
    {
        using TestDatabase db = Seed();

        var rows = db.Table<UnmappedSiblingRow>()
            .OrderBy(e => e.Id)
            .Select(e => new { e.Id, Value = e.Extra })
            .ToList();

        Assert.Equal([(1, 0), (2, 0)], rows.Select(r => (r.Id, r.Value)));
    }

    [Fact]
    public void ComputedPropertyInSelectComputesFromColumns()
    {
        using TestDatabase db = Seed();

        var rows = db.Table<UnmappedSiblingRow>()
            .OrderBy(e => e.Id)
            .Select(e => new { e.Id, Value = e.Doubled })
            .ToList();

        Assert.Equal([(1, 20), (2, 40)], rows.Select(r => (r.Id, r.Value)));
    }

    [Fact]
    public void ComputedPropertyInGroupByThrowsNotSupported()
    {
        using TestDatabase db = Seed();

        Assert.Throws<NotSupportedException>(() =>
            db.Table<UnmappedSiblingRow>().GroupBy(e => e.Doubled).Select(g => g.Key).ToList());
    }

    [Fact]
    public void ComputedPropertyInJoinKeyThrowsNotSupported()
    {
        using TestDatabase db = Seed();
        db.Table<UnmappedSiblingLink>().Schema.CreateTable();
        db.Table<UnmappedSiblingLink>().Add(new UnmappedSiblingLink { Id = 1, RowId = 20 });

        Assert.Throws<NotSupportedException>(() =>
            db.Table<UnmappedSiblingRow>()
                .Join(db.Table<UnmappedSiblingLink>(), r => r.Doubled, l => l.RowId, (r, l) => r.Id)
                .ToList());
    }

    [Fact]
    public void ComputedPropertyInCountPredicateThrowsNotSupported()
    {
        using TestDatabase db = Seed();

        Assert.Throws<NotSupportedException>(() =>
            db.Table<UnmappedSiblingRow>().Count(e => e.Doubled > 25));
    }

    [Fact]
    public void ComputedPropertyInThenByThrowsNotSupported()
    {
        using TestDatabase db = Seed();

        Assert.Throws<NotSupportedException>(() =>
            db.Table<UnmappedSiblingRow>().OrderBy(e => e.Amount).ThenBy(e => e.Doubled).Select(e => e.Id).ToList());
    }

    [Fact]
    public void ComputedPropertyInJoinProjectionComputesFromColumns()
    {
        using TestDatabase db = Seed();
        db.Table<UnmappedSiblingLink>().Schema.CreateTable();
        db.Table<UnmappedSiblingLink>().Add(new UnmappedSiblingLink { Id = 9, RowId = 2 });

        var rows = db.Table<UnmappedSiblingRow>()
            .Join(db.Table<UnmappedSiblingLink>(), r => r.Id, l => l.RowId, (r, l) => new { l.Id, Value = r.Doubled })
            .ToList();

        Assert.Equal([(9, 40)], rows.Select(r => (r.Id, r.Value)));
    }

    [Fact]
    public void ComputedPropertyOffJoinPairMemberThrowsNotSupported()
    {
        using TestDatabase db = Seed();
        db.Table<UnmappedSiblingLink>().Schema.CreateTable();
        db.Table<UnmappedSiblingLink>().Add(new UnmappedSiblingLink { Id = 7, RowId = 1 });

        Assert.Throws<NotSupportedException>(() =>
            db.Table<UnmappedSiblingRow>()
                .Join(db.Table<UnmappedSiblingLink>(), r => r.Id, l => l.RowId, (r, l) => new { r, l })
                .Select(x => new { x.l.Id, Value = x.r.Doubled })
                .ToList());
    }

    [Fact]
    public void InterfaceCastMemberInSelectReadsRealValue()
    {
        using TestDatabase db = new();
        db.Table<UnmappedMemberRow>().Schema.CreateTable();
        db.Table<UnmappedMemberRow>().Add(new UnmappedMemberRow { Id = 5, Stored = true });
        db.Table<UnmappedMemberRow>().Add(new UnmappedMemberRow { Id = 6, Stored = false });

        var rows = db.Table<UnmappedMemberRow>()
            .OrderBy(e => e.Id)
            .Select(e => new { e.Id, Value = ((IUnmappedMemberFlag)e).Hidden })
            .ToList();

        Assert.Equal([(5, true), (6, false)], rows.Select(r => (r.Id, r.Value)));
    }

    [Fact]
    public void ClientMethodOverEntityInSelectComputesFromColumns()
    {
        using TestDatabase db = Seed();

        var rows = db.Table<UnmappedSiblingRow>()
            .OrderBy(e => e.Id)
            .Select(e => new { e.Id, Value = MakeLabel(e) })
            .ToList();

        Assert.Equal([(1, "1:10"), (2, "2:20")], rows.Select(r => (r.Id, r.Value)));
    }

    [Fact]
    public void MemberOnClientMethodResultInSelectComputesFromColumns()
    {
        using TestDatabase db = Seed();

        var rows = db.Table<UnmappedSiblingRow>()
            .OrderBy(e => e.Id)
            .Select(e => new { e.Id, Value = MakeLabel(e).Length })
            .ToList();

        Assert.Equal([(1, 4), (2, 4)], rows.Select(r => (r.Id, r.Value)));
    }

    private static string MakeLabel(UnmappedSiblingRow row)
    {
        return row.Id + ":" + row.Amount;
    }
}
