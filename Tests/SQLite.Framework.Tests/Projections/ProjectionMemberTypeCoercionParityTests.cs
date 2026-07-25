using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("pmc_source")]
public class PmcSource
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public class PmcObjectDto
{
    public int Id { get; set; }

    public object? Val { get; set; }
}

public class PmcInterfaceDto
{
    public int Id { get; set; }

    public IComparable? V { get; set; }
}

public class ProjectionMemberTypeCoercionParityTests
{
    private static List<PmcSource> Rows()
    {
        return
        [
            new PmcSource { Id = 1, Name = "Ann" },
            new PmcSource { Id = 2, Name = "Bob" },
            new PmcSource { Id = 3, Name = "Cid" },
        ];
    }

    private static TestDatabase Seed()
    {
        TestDatabase db = new();
        db.Table<PmcSource>().Schema.CreateTable();
        db.Table<PmcSource>().AddRange(Rows());
        return db;
    }

    [Fact]
    public void ObjectTypedMemberKeepsTheBoxedColumnType()
    {
        using TestDatabase db = Seed();
        List<PmcSource> rows = Rows();

        List<object?> expected = rows
            .OrderBy(r => r.Id)
            .Select(r => new PmcObjectDto { Id = r.Id, Val = r.Id })
            .Select(x => x.Val)
            .ToList();

        List<object?> actual = db.Table<PmcSource>()
            .Select(r => new PmcObjectDto { Id = r.Id, Val = r.Id })
            .AsEnumerable()
            .OrderBy(x => x.Id)
            .Select(x => x.Val)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void InterfaceTypedMemberReceivesTheColumnValue()
    {
        using TestDatabase db = Seed();
        List<PmcSource> rows = Rows();

        List<string> expected = rows
            .OrderBy(r => r.Id)
            .Select(r => new PmcInterfaceDto { Id = r.Id, V = r.Name })
            .Select(x => x.V == null ? "null" : x.V.ToString() ?? "null")
            .ToList();

        List<string> actual = db.Table<PmcSource>()
            .Select(r => new PmcInterfaceDto { Id = r.Id, V = r.Name })
            .AsEnumerable()
            .OrderBy(x => x.Id)
            .Select(x => x.V == null ? "null" : x.V.ToString() ?? "null")
            .ToList();

        Assert.Equal(expected, actual);
    }
}
