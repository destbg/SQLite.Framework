using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class FieldDtoSourceRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public sealed class FieldDto
{
    public int Id;

    public string? Name;
}

public sealed class MixedMemberDto
{
    public string? Name;

    public int Id { get; set; }
}

public class FieldDtoMemberInitProjectionTests
{
    private static TestDatabase SetupDatabase()
    {
        TestDatabase db = new();
        db.Table<FieldDtoSourceRow>().Schema.CreateTable();
        db.Table<FieldDtoSourceRow>().Add(new FieldDtoSourceRow { Id = 1, Name = "a" });
        db.Table<FieldDtoSourceRow>().Add(new FieldDtoSourceRow { Id = 2, Name = "b" });
        return db;
    }

    [Fact]
    public void FieldOnlyDtoBindsBothFields()
    {
        using TestDatabase db = SetupDatabase();

        List<string> expected = db.Table<FieldDtoSourceRow>().AsEnumerable()
            .OrderBy(r => r.Id)
            .Select(r => new FieldDto { Id = r.Id, Name = r.Name })
            .Select(d => d.Id + "|" + d.Name)
            .ToList();

        Assert.Equal(["1|a", "2|b"], expected);

        List<string> actual = db.Table<FieldDtoSourceRow>()
            .OrderBy(r => r.Id)
            .Select(r => new FieldDto { Id = r.Id, Name = r.Name })
            .AsEnumerable()
            .Select(d => d.Id + "|" + d.Name)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void MixedFieldAndPropertyDtoBindsBothMembers()
    {
        using TestDatabase db = SetupDatabase();

        List<string> expected = db.Table<FieldDtoSourceRow>().AsEnumerable()
            .OrderBy(r => r.Id)
            .Select(r => new MixedMemberDto { Id = r.Id, Name = r.Name })
            .Select(d => d.Id + "|" + d.Name)
            .ToList();

        Assert.Equal(["1|a", "2|b"], expected);

        List<string> actual = db.Table<FieldDtoSourceRow>()
            .OrderBy(r => r.Id)
            .Select(r => new MixedMemberDto { Id = r.Id, Name = r.Name })
            .AsEnumerable()
            .Select(d => d.Id + "|" + d.Name)
            .ToList();

        Assert.Equal(expected, actual);
    }
}
