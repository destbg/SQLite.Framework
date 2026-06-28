using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;
using Xunit;

namespace SQLite.Framework.Tests;

public class MbFkTargetOrderTests
{
    [Fact]
    public void NoTargetCompositeForeignKeyMatchesReorderedParentKey()
    {
        using ModelTestDatabase db = new(model =>
        {
            model.Entity<MbFkParent>().HasKey(p => new { p.Pb, p.Pa });
            model.Entity<MbFkChild>()
                .HasKey(c => c.Id)
                .ForeignKey<MbFkParent>(c => new { c.CpB, c.CpA });
        });

        db.Schema.CreateTable<MbFkParent>();
        db.Schema.CreateTable<MbFkChild>();

        db.Table<MbFkParent>().Add(new MbFkParent { Pa = 10, Pb = 20, Name = "p" });

        MbFkChild child = new() { Id = 1, CpB = 20, CpA = 10, Note = "c" };
        db.Table<MbFkChild>().Add(child);

        MbFkChild read = db.Table<MbFkChild>().Single();
        Assert.Equal(20, read.CpB);
        Assert.Equal(10, read.CpA);
        Assert.Equal("c", read.Note);
    }
}

public class MbFkParent
{
    public int Pa { get; set; }
    public int Pb { get; set; }
    public string Name { get; set; } = "";
}

public class MbFkChild
{
    public int Id { get; set; }
    public int CpB { get; set; }
    public int CpA { get; set; }
    public string Note { get; set; } = "";
}
