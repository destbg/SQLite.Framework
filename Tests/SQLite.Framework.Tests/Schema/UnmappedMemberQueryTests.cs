using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public interface IUnmappedMemberFlag
{
    bool Hidden { get; }
}

[Table("UnmappedMemberRows")]
public class UnmappedMemberRow : IUnmappedMemberFlag
{
    [Key]
    public int Id { get; set; }

    public bool Stored { get; set; }

    [NotMapped]
    public bool Extra { get; set; }

    public bool Mirror => Stored;

    bool IUnmappedMemberFlag.Hidden => Stored;
}

public class UnmappedMemberQueryTests
{
    private static TestDatabase Seed()
    {
        TestDatabase db = new();
        db.Table<UnmappedMemberRow>().Schema.CreateTable();
        db.Table<UnmappedMemberRow>().Add(new UnmappedMemberRow { Id = 5, Stored = true });
        db.Table<UnmappedMemberRow>().Add(new UnmappedMemberRow { Id = 6, Stored = false });
        return db;
    }

    [Fact]
    public void ComputedPropertyInSelectReadsRealValue()
    {
        using TestDatabase db = Seed();

        var rows = db.Table<UnmappedMemberRow>()
            .OrderBy(e => e.Id)
            .Select(e => new { e.Id, Value = e.Mirror })
            .ToList();

        Assert.Equal([(5, true), (6, false)], rows.Select(r => (r.Id, r.Value)));
    }

    [Fact]
    public void NotMappedPropertyInWhereThrowsNotSupported()
    {
        using TestDatabase db = Seed();

        Assert.Throws<NotSupportedException>(() =>
            db.Table<UnmappedMemberRow>().Where(e => e.Extra).ToList());
    }

    [Fact]
    public void ComputedPropertyInWhereThrowsNotSupported()
    {
        using TestDatabase db = Seed();

        Assert.Throws<NotSupportedException>(() =>
            db.Table<UnmappedMemberRow>().Where(e => !e.Mirror).ToList());
    }

    [Fact]
    public void NotMappedPropertyInOrderByThrowsNotSupported()
    {
        using TestDatabase db = Seed();

        Assert.Throws<NotSupportedException>(() =>
            db.Table<UnmappedMemberRow>().OrderBy(e => e.Extra).Select(e => e.Id).ToList());
    }

    [Fact]
    public void InterfaceCastMemberInWhereThrowsNotSupported()
    {
        using TestDatabase db = Seed();

        Assert.Throws<NotSupportedException>(() =>
            db.Table<UnmappedMemberRow>().Where(e => !((IUnmappedMemberFlag)e).Hidden).ToList());
    }
}
