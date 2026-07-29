using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public interface IH24tStampable
{
    int Stamp { get; set; }
}

[Table("H24tStampAudits")]
public class H24tStampAudit : IH24tStampable
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public int Stamp { get; set; }
}

public class InterfaceRegisteredEntityHookTests
{
    [Fact]
    public void ConcreteRegisteredAddHookStampsTheEntity()
    {
        using TestDatabase db = new(b => b.OnAdd<H24tStampAudit>(e => e.Stamp = 7));
        db.Table<H24tStampAudit>().Schema.CreateTable();

        db.Table<H24tStampAudit>().Add(new H24tStampAudit { Id = 1, Name = "a" });

        Assert.Equal(7, db.Table<H24tStampAudit>().Single().Stamp);
    }

    [Fact]
    public void InterfaceRegisteredAddHookStampsTheEntity()
    {
        using TestDatabase db = new(b => b.OnAdd<IH24tStampable>(e => e.Stamp = 7));
        db.Table<H24tStampAudit>().Schema.CreateTable();

        db.Table<H24tStampAudit>().Add(new H24tStampAudit { Id = 1, Name = "a" });

        Assert.Equal(7, db.Table<H24tStampAudit>().Single().Stamp);
    }

    [Fact]
    public void InterfaceRegisteredUpdateHookStampsTheEntity()
    {
        using TestDatabase db = new(b => b.OnUpdate<IH24tStampable>(e => e.Stamp = 9));
        db.Table<H24tStampAudit>().Schema.CreateTable();
        H24tStampAudit row = new() { Id = 1, Name = "a" };
        db.Table<H24tStampAudit>().Add(row);

        row.Name = "b";
        db.Table<H24tStampAudit>().Update(row);

        Assert.Equal(9, db.Table<H24tStampAudit>().Single().Stamp);
    }

    [Fact]
    public void InterfaceRegisteredAddHookStampsEveryRowOfARange()
    {
        using TestDatabase db = new(b => b.OnAdd<IH24tStampable>(e => e.Stamp = 5));
        db.Table<H24tStampAudit>().Schema.CreateTable();

        db.Table<H24tStampAudit>().AddRange(
        [
            new H24tStampAudit { Id = 1, Name = "a" },
            new H24tStampAudit { Id = 2, Name = "b" },
        ]);

        List<int> expected = [5, 5];
        List<int> actual = db.Table<H24tStampAudit>().OrderBy(r => r.Id).Select(r => r.Stamp).ToList();

        Assert.Equal(expected, actual);
    }
}
