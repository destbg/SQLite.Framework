using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H23bBesideListRows")]
public class H23bBesideListRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public class H23bBesideLabel
{
    public string Text { get; set; } = "";
}

public class H23bBesidePoint
{
    public H23bBesidePoint(int x)
    {
        X = x;
    }

    public int X { get; }
}

public class H23bBesideDto
{
    public int Id { get; set; }

    public H23bBesideLabel Label { get; set; } = new();

    public H23bBesideListRow? Source { get; set; }

    public H23bBesidePoint? Point { get; set; }

    public List<string> Tags { get; } = new();
}

public class H23bBesideCtorDto
{
    public H23bBesideCtorDto(int id)
    {
        Id = id;
    }

    public int Id { get; }

    public List<string> Tags { get; } = new();
}

public class CollectionInitializerBesideOtherBindingsTests
{
    [Fact]
    public void NestedMemberBindingKeepsItsValueBesideACollectionInitializer()
    {
        using TestDatabase db = Setup(nameof(NestedMemberBindingKeepsItsValueBesideACollectionInitializer));

        List<string> expected = Rows().OrderBy(r => r.Id)
            .Select(r => new H23bBesideDto { Id = r.Id, Label = { Text = r.Name }, Tags = { "t" } })
            .ToList()
            .Select(d => d.Id + ":" + d.Label.Text + ":" + string.Join(",", d.Tags))
            .ToList();

        List<string> actual = db.Table<H23bBesideListRow>().OrderBy(r => r.Id)
            .Select(r => new H23bBesideDto { Id = r.Id, Label = { Text = r.Name }, Tags = { "t" } })
            .ToList()
            .Select(d => d.Id + ":" + d.Label.Text + ":" + string.Join(",", d.Tags))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void WholeRowMemberKeepsItsValueBesideACollectionInitializer()
    {
        using TestDatabase db = Setup(nameof(WholeRowMemberKeepsItsValueBesideACollectionInitializer));

        List<string> expected = Rows().OrderBy(r => r.Id)
            .Select(r => new H23bBesideDto { Id = r.Id, Source = r, Tags = { "t" } })
            .ToList()
            .Select(d => d.Id + ":" + (d.Source == null ? "none" : d.Source.Name))
            .ToList();

        List<string> actual = db.Table<H23bBesideListRow>().OrderBy(r => r.Id)
            .Select(r => new H23bBesideDto { Id = r.Id, Source = r, Tags = { "t" } })
            .ToList()
            .Select(d => d.Id + ":" + (d.Source == null ? "none" : d.Source.Name))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ConstructedNestedMemberKeepsItsValueBesideACollectionInitializer()
    {
        using TestDatabase db = Setup(nameof(ConstructedNestedMemberKeepsItsValueBesideACollectionInitializer));

        List<string> expected = Rows().OrderBy(r => r.Id)
            .Select(r => new H23bBesideDto { Id = r.Id, Point = new H23bBesidePoint(r.Id), Tags = { "t" } })
            .ToList()
            .Select(d => d.Id + ":" + (d.Point == null ? "none" : d.Point.X.ToString()))
            .ToList();

        List<string> actual = db.Table<H23bBesideListRow>().OrderBy(r => r.Id)
            .Select(r => new H23bBesideDto { Id = r.Id, Point = new H23bBesidePoint(r.Id), Tags = { "t" } })
            .ToList()
            .Select(d => d.Id + ":" + (d.Point == null ? "none" : d.Point.X.ToString()))
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ConstructorArgumentProjectionAcceptsACollectionInitializer()
    {
        using TestDatabase db = Setup(nameof(ConstructorArgumentProjectionAcceptsACollectionInitializer));

        List<string> expected = Rows().OrderBy(r => r.Id)
            .Select(r => new H23bBesideCtorDto(r.Id) { Tags = { "t" } })
            .ToList()
            .Select(d => d.Id + ":" + string.Join(",", d.Tags))
            .ToList();

        List<string> actual = db.Table<H23bBesideListRow>().OrderBy(r => r.Id)
            .Select(r => new H23bBesideCtorDto(r.Id) { Tags = { "t" } })
            .ToList()
            .Select(d => d.Id + ":" + string.Join(",", d.Tags))
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H23bBesideListRow> Rows()
    {
        return
        [
            new H23bBesideListRow { Id = 1, Name = "alpha" },
            new H23bBesideListRow { Id = 2, Name = "beta" }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(null, methodName);
        db.Table<H23bBesideListRow>().Schema.CreateTable();
        db.Table<H23bBesideListRow>().AddRange(Rows());
        return db;
    }
}
