using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H21kUnqualRows")]
public class H21kUnqualRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

[Table("H21kCondRows")]
public class H21kCondRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public class H21kUnqualDatabase : TestDatabase
{
    public H21kUnqualDatabase([CallerMemberName] string? methodName = null)
        : base(null, methodName)
    {
    }

    public void Prepare(IEnumerable<H21kUnqualRow> items)
    {
        Table<H21kUnqualRow>().Schema.CreateTable();
        Table<H21kUnqualRow>().AddRange(items);
    }

    public List<H21kUnqualRow> LoadAll()
    {
        return Table<H21kUnqualRow>().ToList();
    }
}

public class TableCallShapeMaterializerTests
{
    private static List<H21kUnqualRow> UnqualRows()
    {
        return
        [
            new H21kUnqualRow { Id = 1, Name = "a" },
            new H21kUnqualRow { Id = 2, Name = "b" }
        ];
    }

    private static List<H21kCondRow> CondRows()
    {
        return
        [
            new H21kCondRow { Id = 1, Name = "a" },
            new H21kCondRow { Id = 2, Name = "b" }
        ];
    }

    [Fact]
    public void RowsReadThroughUnqualifiedTableCallMatchLinq()
    {
        using H21kUnqualDatabase db = new();
        db.Prepare(UnqualRows());

        List<(int Id, string Name)> expected = UnqualRows()
            .OrderBy(r => r.Id)
            .Select(r => (r.Id, r.Name))
            .ToList();

        List<(int Id, string Name)> actual = db.LoadAll()
            .OrderBy(r => r.Id)
            .Select(r => (r.Id, r.Name))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void RowsReadThroughConditionalAccessTableCallMatchLinq()
    {
        using TestDatabase owner = new();
        TestDatabase? db = owner;

        db?.Table<H21kCondRow>().Schema.CreateTable();
        db?.Table<H21kCondRow>().AddRange(CondRows());

        List<(int Id, string Name)> expected = CondRows()
            .OrderBy(r => r.Id)
            .Select(r => (r.Id, r.Name))
            .ToList();

        List<H21kCondRow> loaded = db?.Table<H21kCondRow>().ToList() ?? [];

        List<(int Id, string Name)> actual = loaded
            .OrderBy(r => r.Id)
            .Select(r => (r.Id, r.Name))
            .ToList();

        Assert.Equal(expected, actual);
    }
}
