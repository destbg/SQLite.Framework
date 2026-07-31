using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H26hAttachedNotes")]
public class H26hAttachedNote
{
    [Key]
    public int Id { get; set; }

    public string Label { get; set; } = "";

    public int OwnerId { get; set; }

    public bool Archived { get; set; }
}

[Table("H26hLocalOwners")]
public class H26hLocalOwner
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public class SchemaQualifiedAttachedTableFilterTests
{
    [Fact]
    public void SchemaQualifiedAttachedTableAppliesTheAttachedDatabaseFilter()
    {
        using TestDatabase main = new();
        using TestDatabase aux = new(b => b.AddQueryFilter<H26hAttachedNote>(n => !n.Archived), useFile: true, "h26hplainaux");
        aux.Table<H26hAttachedNote>().Schema.CreateTable();
        aux.Table<H26hAttachedNote>().AddRange(Notes());

        main.AttachDatabase(aux, "h26haux");

        List<string> expected = Notes()
            .Where(n => !n.Archived)
            .OrderBy(n => n.Id)
            .Select(n => n.Label)
            .ToList();

        List<string> actual = main.Table<H26hAttachedNote>("h26haux")
            .OrderBy(n => n.Id)
            .Select(n => n.Label)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SchemaQualifiedAttachedTableInASubqueryAppliesTheAttachedDatabaseFilter()
    {
        using TestDatabase main = new();
        main.Table<H26hLocalOwner>().Schema.CreateTable();
        main.Table<H26hLocalOwner>().AddRange(Owners());

        using TestDatabase aux = new(b => b.AddQueryFilter<H26hAttachedNote>(n => !n.Archived), useFile: true, "h26hsubaux");
        aux.Table<H26hAttachedNote>().Schema.CreateTable();
        aux.Table<H26hAttachedNote>().AddRange(Notes());

        main.AttachDatabase(aux, "h26haux");

        List<H26hAttachedNote> visible = Notes().Where(n => !n.Archived).ToList();
        List<int> expected = Owners()
            .Where(o => visible.Any(n => n.OwnerId == o.Id))
            .OrderBy(o => o.Id)
            .Select(o => o.Id)
            .ToList();

        List<int> actual = main.Table<H26hLocalOwner>()
            .Where(o => main.Table<H26hAttachedNote>("h26haux").Any(n => n.OwnerId == o.Id))
            .OrderBy(o => o.Id)
            .Select(o => o.Id)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SchemaQualifiedAttachedTablePrefersTheAttachedDatabaseFilterOverTheMainOne()
    {
        using TestDatabase main = new(b => b.AddQueryFilter<H26hAttachedNote>(n => n.Label != "beta"));
        using TestDatabase aux = new(b => b.AddQueryFilter<H26hAttachedNote>(n => !n.Archived), useFile: true, "h26hbothaux");
        aux.Table<H26hAttachedNote>().Schema.CreateTable();
        aux.Table<H26hAttachedNote>().AddRange(Notes());

        main.AttachDatabase(aux, "h26haux");

        List<string> expected = Notes()
            .Where(n => !n.Archived)
            .OrderBy(n => n.Id)
            .Select(n => n.Label)
            .ToList();

        List<string> actual = main.Table<H26hAttachedNote>("h26haux")
            .OrderBy(n => n.Id)
            .Select(n => n.Label)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H26hAttachedNote> Notes()
    {
        return
        [
            new H26hAttachedNote { Id = 1, Label = "alpha", OwnerId = 1, Archived = false },
            new H26hAttachedNote { Id = 2, Label = "beta", OwnerId = 2, Archived = false },
            new H26hAttachedNote { Id = 3, Label = "gamma", OwnerId = 3, Archived = true }
        ];
    }

    private static List<H26hLocalOwner> Owners()
    {
        return
        [
            new H26hLocalOwner { Id = 1, Name = "a" },
            new H26hLocalOwner { Id = 2, Name = "b" },
            new H26hLocalOwner { Id = 3, Name = "c" }
        ];
    }
}
