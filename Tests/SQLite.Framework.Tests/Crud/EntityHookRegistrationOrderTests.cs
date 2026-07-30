using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public interface IH25iTraced
{
    string Trace { get; set; }
}

[Table("H25iTracedRows")]
public class H25iTracedRow : IH25iTraced
{
    [Key]
    public int Id { get; set; }

    public string Trace { get; set; } = "";
}

public class EntityHookRegistrationOrderTests
{
    [Fact]
    public void AddHooksRunInRegistrationOrderAcrossInterfaceAndConcreteRegistrations()
    {
        using TestDatabase db = new(b => b
            .OnAdd<IH25iTraced>(e => e.Trace += "A")
            .OnAdd<H25iTracedRow>(e => e.Trace += "B")
            .OnAdd<IH25iTraced>(e => e.Trace += "C"));
        db.Table<H25iTracedRow>().Schema.CreateTable();

        db.Table<H25iTracedRow>().Add(new H25iTracedRow { Id = 1 });

        Assert.Equal("ABC", db.ExecuteScalar<string>("SELECT \"Trace\" FROM \"H25iTracedRows\" WHERE \"Id\" = 1"));
    }

    [Fact]
    public void UpdateHooksRunInRegistrationOrderAcrossInterfaceAndConcreteRegistrations()
    {
        using TestDatabase db = new(b => b
            .OnUpdate<IH25iTraced>(e => e.Trace += "A")
            .OnUpdate<H25iTracedRow>(e => e.Trace += "B")
            .OnUpdate<IH25iTraced>(e => e.Trace += "C"));
        db.Table<H25iTracedRow>().Schema.CreateTable();
        H25iTracedRow row = new() { Id = 1 };
        db.Table<H25iTracedRow>().Add(row);

        row.Trace = "";
        db.Table<H25iTracedRow>().Update(row);

        Assert.Equal("ABC", db.ExecuteScalar<string>("SELECT \"Trace\" FROM \"H25iTracedRows\" WHERE \"Id\" = 1"));
    }

    [Fact]
    public void RangeAddHooksRunInRegistrationOrderAcrossInterfaceAndConcreteRegistrations()
    {
        using TestDatabase db = new(b => b
            .OnAdd<IH25iTraced>(e => e.Trace += "A")
            .OnAdd<H25iTracedRow>(e => e.Trace += "B")
            .OnAdd<IH25iTraced>(e => e.Trace += "C"));
        db.Table<H25iTracedRow>().Schema.CreateTable();

        db.Table<H25iTracedRow>().AddRange(
        [
            new H25iTracedRow { Id = 1 },
            new H25iTracedRow { Id = 2 }
        ]);

        List<string> expected = ["ABC", "ABC"];
        List<string> actual = db.Table<H25iTracedRow>().OrderBy(r => r.Id).Select(r => r.Trace).ToList();

        Assert.Equal(expected, actual);
    }
}
