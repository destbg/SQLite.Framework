using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("UnmappedEdgeRows")]
public class UnmappedEdgeRow
{
    [Key]
    public int Id { get; set; }

    public int Amount { get; set; }

    public string Name { get; set; } = "";

    public int Doubled => Amount * 2;
}

[Table("UnmappedRecordRows")]
public record UnmappedRecordRow([property: Key] int Id, int Amount)
{
    public int Doubled => Amount * 2;
}

public class UnmappedMemberEdgeShapesTests
{
    [Fact]
    public void PartialEntityProjectionComputedMemberThrowsNotSupported()
    {
        using TestDatabase db = new();
        db.Table<UnmappedEdgeRow>().Schema.CreateTable();
        db.Table<UnmappedEdgeRow>().Add(new UnmappedEdgeRow { Id = 1, Amount = 10, Name = "a" });

        Assert.Throws<NotSupportedException>(() =>
            db.Table<UnmappedEdgeRow>()
                .Select(r => new UnmappedEdgeRow { Id = r.Id, Amount = r.Amount })
                .Select(p => new { Value = p.Doubled })
                .ToList());
    }

    [Fact]
    public void RecordEntityComputedMemberInSelectThrowsNotSupported()
    {
        using TestDatabase db = new();
        db.Table<UnmappedRecordRow>().Schema.CreateTable();
        db.Table<UnmappedRecordRow>().Add(new UnmappedRecordRow(1, 10));

        Assert.Throws<NotSupportedException>(() =>
            db.Table<UnmappedRecordRow>()
                .Select(e => new { e.Id, Value = e.Doubled })
                .ToList());
    }
}
