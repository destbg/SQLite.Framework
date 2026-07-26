using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("ProjectedIfaceRows")]
public class ProjectedIfaceSourceRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public class ProjectedIfaceValueRow
{
    public ProjectedIfaceValueRow(int id, IComparable value)
    {
        Id = id;
        Value = value;
    }

    public int Id { get; init; }

    public IComparable Value { get; init; }
}

public class ProjectedInterfaceConstructorParameterTests
{
    [Fact]
    public void AProjectedInterfaceParameterKeepsTheProjectedType()
    {
        using TestDatabase db = new();
        db.Table<ProjectedIfaceSourceRow>().Schema.CreateTable();
        db.Table<ProjectedIfaceSourceRow>().AddRange(
        [
            new ProjectedIfaceSourceRow { Id = 1, Name = "Ann" },
            new ProjectedIfaceSourceRow { Id = 2, Name = "Bob" }
        ]);

        List<string> actual = db.Table<ProjectedIfaceSourceRow>()
            .OrderBy(r => r.Id)
            .Select(r => new ProjectedIfaceValueRow(r.Id, r.Name))
            .ToList()
            .Select(r => r.Value.ToString()!)
            .ToList();

        Assert.Equal(["Ann", "Bob"], actual);
    }
}
