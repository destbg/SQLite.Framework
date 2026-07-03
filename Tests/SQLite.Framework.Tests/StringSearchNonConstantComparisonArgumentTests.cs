using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("SearchComparisonRow")]
public class SearchComparisonRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public bool Loose { get; set; }

    public StringComparison Mode { get; set; }
}

public class StringSearchNonConstantComparisonArgumentTests
{
    private static readonly SearchComparisonRow[] Rows =
    [
        new() { Id = 1, Name = "apple", Loose = true, Mode = StringComparison.OrdinalIgnoreCase },
        new() { Id = 2, Name = "apple", Loose = false, Mode = StringComparison.Ordinal },
        new() { Id = 3, Name = "APPLE", Loose = false, Mode = StringComparison.Ordinal },
    ];

    [Fact]
    public void StartsWithComparisonFromColumn()
    {
        using TestDatabase db = new();
        db.Table<SearchComparisonRow>().Schema.CreateTable();
        db.Table<SearchComparisonRow>().AddRange(Rows.Select(Clone));

        List<int> expected = Rows.Where(x => x.Name.StartsWith("APP", x.Mode)).Select(x => x.Id).ToList();
        Assert.Equal([1, 3], expected);

        List<int> ids = db.Table<SearchComparisonRow>()
            .Where(x => x.Name.StartsWith("APP", x.Mode))
            .Select(x => x.Id)
            .ToList();

        Assert.Equal(expected, ids);
    }

    [Fact]
    public void StartsWithIgnoreCaseFromColumn()
    {
        using TestDatabase db = new();
        db.Table<SearchComparisonRow>().Schema.CreateTable();
        db.Table<SearchComparisonRow>().AddRange(Rows.Select(Clone));

        List<int> expected = Rows.Where(x => x.Name.StartsWith("APP", x.Loose, null)).Select(x => x.Id).ToList();
        Assert.Equal([1, 3], expected);

        List<int> ids = db.Table<SearchComparisonRow>()
            .Where(x => x.Name.StartsWith("APP", x.Loose, null))
            .Select(x => x.Id)
            .ToList();

        Assert.Equal(expected, ids);
    }

    [Fact]
    public void EndsWithIgnoreCaseFromColumn()
    {
        using TestDatabase db = new();
        db.Table<SearchComparisonRow>().Schema.CreateTable();
        db.Table<SearchComparisonRow>().AddRange(Rows.Select(Clone));

        List<int> expected = Rows.Where(x => x.Name.EndsWith("PLE", x.Loose, null)).Select(x => x.Id).ToList();
        Assert.Equal([1, 3], expected);

        List<int> ids = db.Table<SearchComparisonRow>()
            .Where(x => x.Name.EndsWith("PLE", x.Loose, null))
            .Select(x => x.Id)
            .ToList();

        Assert.Equal(expected, ids);
    }

    [Fact]
    public void ContainsComparisonFromColumn()
    {
        using TestDatabase db = new();
        db.Table<SearchComparisonRow>().Schema.CreateTable();
        db.Table<SearchComparisonRow>().AddRange(Rows.Select(Clone));

        List<int> expected = Rows.Where(x => x.Name.Contains("PP", x.Mode)).Select(x => x.Id).ToList();
        Assert.Equal([1, 3], expected);

        List<int> ids = db.Table<SearchComparisonRow>()
            .Where(x => x.Name.Contains("PP", x.Mode))
            .Select(x => x.Id)
            .ToList();

        Assert.Equal(expected, ids);
    }

    private static SearchComparisonRow Clone(SearchComparisonRow row)
    {
        return new SearchComparisonRow { Id = row.Id, Name = row.Name, Loose = row.Loose, Mode = row.Mode };
    }
}
