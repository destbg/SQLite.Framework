using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("ArrCtorExtraRows")]
public class ArrCtorExtraRow
{
    public ArrCtorExtraRow(int id, string name)
    {
        Id = id;
        Name = name;
    }

    [Key]
    public int Id { get; init; }

    public string Name { get; init; }

    public string Note { get; set; } = "";
}

[Table("ArrCtorHiddenRows")]
public class ArrCtorHiddenRow
{
    private ArrCtorHiddenRow(int id, string name)
    {
        Id = id;
        Name = name;
    }

    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public static ArrCtorHiddenRow Create(int id, string name)
    {
        return new ArrCtorHiddenRow(id, name);
    }
}

[Table("ArrCtorMismatchRows")]
public class ArrCtorMismatchRow
{
    public ArrCtorMismatchRow(int key, string label)
    {
        Id = key;
        Name = label;
    }

    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public class InlineArrayEntityConstructorShapeTests
{
    [Fact]
    public void EntityElementWhoseConstructorNamesDoNotMatchColumnsIsNotMaterialized()
    {
        using TestDatabase db = new();
        db.Table<ArrCtorMismatchRow>().Schema.CreateTable();
        db.Table<ArrCtorMismatchRow>().AddRange([new ArrCtorMismatchRow(1, "Ann")]);

        Assert.ThrowsAny<Exception>(() => db.Table<ArrCtorMismatchRow>()
            .OrderBy(r => r.Id)
            .Select(r => new[] { r })
            .ToList());
    }

    [Fact]
    public void PositionalEntityElementAlsoSetsAPropertyTheConstructorSkips()
    {
        using TestDatabase db = new();
        db.Table<ArrCtorExtraRow>().Schema.CreateTable();
        db.Table<ArrCtorExtraRow>().AddRange(ExtraRows());
        List<ArrCtorExtraRow> local = ExtraRows();

        List<(string Name, string Note)> expected = local
            .OrderBy(r => r.Id)
            .Select(r => new[] { r })
            .Select(a => (a[0].Name, a[0].Note))
            .ToList();

        List<(string Name, string Note)> actual = db.Table<ArrCtorExtraRow>()
            .OrderBy(r => r.Id)
            .Select(r => new[] { r })
            .ToList()
            .Select(a => (a[0].Name, a[0].Note))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void EntityElementWithoutAPublicConstructorIsNotMaterialized()
    {
        using TestDatabase db = new();
        db.Table<ArrCtorHiddenRow>().Schema.CreateTable();
        db.Table<ArrCtorHiddenRow>().AddRange([ArrCtorHiddenRow.Create(1, "Ann")]);

        Assert.ThrowsAny<Exception>(() => db.Table<ArrCtorHiddenRow>()
            .OrderBy(r => r.Id)
            .Select(r => new[] { r })
            .ToList());
    }

    private static List<ArrCtorExtraRow> ExtraRows()
    {
        return
        [
            new ArrCtorExtraRow(1, "Ann") { Note = "first" },
            new ArrCtorExtraRow(2, "Bob") { Note = "second" }
        ];
    }
}
