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

    private static TestDatabase Seed()
    {
        TestDatabase db = new();
        db.Table<SearchComparisonRow>().Schema.CreateTable();
        db.Table<SearchComparisonRow>().AddRange(Rows.Select(Clone));
        return db;
    }

    [Fact]
    public void StartsWithComparisonFromColumnThrowsInWhere()
    {
        using TestDatabase db = Seed();

        Exception? ex = Record.Exception(() => db.Table<SearchComparisonRow>()
            .Where(x => x.Name.StartsWith("APP", x.Mode))
            .Select(x => x.Id)
            .ToList());

        Assert.IsType<NotSupportedException>(ex);
    }

    [Fact]
    public void StartsWithIgnoreCaseFromColumnThrowsInWhere()
    {
        using TestDatabase db = Seed();

        Exception? ex = Record.Exception(() => db.Table<SearchComparisonRow>()
            .Where(x => x.Name.StartsWith("APP", x.Loose, null))
            .Select(x => x.Id)
            .ToList());

        Assert.IsType<NotSupportedException>(ex);
    }

    [Fact]
    public void EndsWithIgnoreCaseFromColumnThrowsInWhere()
    {
        using TestDatabase db = Seed();

        Exception? ex = Record.Exception(() => db.Table<SearchComparisonRow>()
            .Where(x => x.Name.EndsWith("PLE", x.Loose, null))
            .Select(x => x.Id)
            .ToList());

        Assert.IsType<NotSupportedException>(ex);
    }

    [Fact]
    public void ContainsComparisonFromColumnThrowsInWhere()
    {
        using TestDatabase db = Seed();

        Exception? ex = Record.Exception(() => db.Table<SearchComparisonRow>()
            .Where(x => x.Name.Contains("PP", x.Mode))
            .Select(x => x.Id)
            .ToList());

        Assert.IsType<NotSupportedException>(ex);
    }

    [Fact]
    public void StartsWithComparisonFromColumnProjectsInSelect()
    {
        using TestDatabase db = Seed();

        List<bool> expected = Rows
            .OrderBy(x => x.Id)
            .Select(x => x.Name.StartsWith("APP", x.Mode))
            .ToList();
        Assert.Equal([true, false, true], expected);

        List<bool> actual = db.Table<SearchComparisonRow>()
            .OrderBy(x => x.Id)
            .Select(x => x.Name.StartsWith("APP", x.Mode))
            .ToList();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void EndsWithIgnoreCaseFromColumnProjectsInSelect()
    {
        using TestDatabase db = Seed();

        List<bool> expected = Rows
            .OrderBy(x => x.Id)
            .Select(x => x.Name.EndsWith("PLE", x.Loose, null))
            .ToList();
        Assert.Equal([true, false, true], expected);

        List<bool> actual = db.Table<SearchComparisonRow>()
            .OrderBy(x => x.Id)
            .Select(x => x.Name.EndsWith("PLE", x.Loose, null))
            .ToList();
        Assert.Equal(expected, actual);
    }

    private static SearchComparisonRow Clone(SearchComparisonRow row)
    {
        return new SearchComparisonRow { Id = row.Id, Name = row.Name, Loose = row.Loose, Mode = row.Mode };
    }
}
