using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("mcs_source")]
public class McsSource
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public class McsTwoCtorDto
{
    public McsTwoCtorDto(int id, string name)
    {
        Id = id;
        Name = name;
        Amount = 42;
    }

    public McsTwoCtorDto(int id, string name, int amount)
    {
        Id = id;
        Name = name;
        Amount = amount + 1;
    }

    public int Id { get; }

    public string Name { get; } = "";

    public int Amount { get; }
}

[Table("mcs_ctor_type")]
public class McsCtorTypeEntity
{
    public McsCtorTypeEntity(int id)
    {
        Id = id;
        Name = "";
    }

    public McsCtorTypeEntity(int id, int name)
    {
        Id = id;
        Name = "N" + name;
    }

    [Key]
    public int Id { get; }

    public string Name { get; }
}

public class MaterializerConstructorSelectionParityTests
{
    [Fact]
    public void ProjectionUsesTheConstructorItCalled()
    {
        using TestDatabase db = new();
        db.Table<McsSource>().Schema.CreateTable();
        db.Table<McsSource>().Add(new McsSource { Id = 1, Name = "Ann" });
        db.Table<McsSource>().Add(new McsSource { Id = 2, Name = "Bob" });
        List<McsSource> rows =
        [
            new McsSource { Id = 1, Name = "Ann" },
            new McsSource { Id = 2, Name = "Bob" },
        ];

        List<string> expected = rows
            .OrderBy(r => r.Id)
            .Select(r => new McsTwoCtorDto(r.Id, r.Name))
            .Select(x => x.Id + "|" + x.Name + "|" + x.Amount)
            .ToList();

        List<string> actual = db.Table<McsSource>()
            .Select(r => new McsTwoCtorDto(r.Id, r.Name))
            .AsEnumerable()
            .OrderBy(x => x.Id)
            .Select(x => x.Id + "|" + x.Name + "|" + x.Amount)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void NameMatchingConstructorWithDifferentParameterTypeRoundTrips()
    {
        using TestDatabase db = new();
        db.Table<McsCtorTypeEntity>().Schema.CreateTable();
        db.Table<McsCtorTypeEntity>().Add(new McsCtorTypeEntity(1, 5));

        McsCtorTypeEntity original = new(1, 5);
        McsCtorTypeEntity actual = db.Table<McsCtorTypeEntity>().First();

        Assert.Equal(original.Id, actual.Id);
        Assert.Equal(original.Name, actual.Name);
    }
}
