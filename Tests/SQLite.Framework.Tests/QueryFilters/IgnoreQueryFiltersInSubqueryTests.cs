using System.ComponentModel.DataAnnotations;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class FilteredNoteRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public int Value { get; set; }

    public bool Deleted { get; set; }
}

public class IgnoreQueryFiltersInSubqueryTests
{
    private static TestDatabase SetupDatabase()
    {
        TestDatabase db = new(b => b.AddQueryFilter<FilteredNoteRow>(r => !r.Deleted));
        db.Table<FilteredNoteRow>().Schema.CreateTable();
        db.Table<FilteredNoteRow>().Add(new FilteredNoteRow { Id = 1, Name = "live", Value = 1, Deleted = false });
        db.Table<FilteredNoteRow>().Add(new FilteredNoteRow { Id = 2, Name = "gone", Value = 2, Deleted = true });
        return db;
    }

    [Fact]
    public void JoinInnerSourceKeepsIgnoreQueryFilters()
    {
        using TestDatabase db = SetupDatabase();

        List<string> actual = db.Table<FilteredNoteRow>()
            .Join(
                db.Table<FilteredNoteRow>().IgnoreQueryFilters().Where(b => b.Id > 0),
                a => a.Id,
                b => b.Id - 1,
                (a, b) => b.Name)
            .ToList();

        Assert.Equal(["gone"], actual);
    }

    [Fact]
    public void ContainsSubqueryKeepsIgnoreQueryFilters()
    {
        using TestDatabase db = SetupDatabase();

        List<string> actual = db.Table<FilteredNoteRow>()
            .Where(a => db.Table<FilteredNoteRow>().IgnoreQueryFilters()
                .Where(b => b.Value == 2)
                .Select(b => b.Id - 1)
                .Contains(a.Id))
            .Select(a => a.Name)
            .ToList();

        Assert.Equal(["live"], actual);
    }

    [Fact]
    public void ConcatRightOperandKeepsIgnoreQueryFilters()
    {
        using TestDatabase db = SetupDatabase();

        List<string> actual = db.Table<FilteredNoteRow>().Select(a => a.Name)
            .Concat(db.Table<FilteredNoteRow>().IgnoreQueryFilters().Select(a => a.Name))
            .ToList();

        Assert.Equal(["live", "gone", "live", "gone"], actual);
    }

    [Fact]
    public void CorrelatedCountSubqueryKeepsIgnoreQueryFilters()
    {
        using TestDatabase db = SetupDatabase();

        List<int> actual = db.Table<FilteredNoteRow>()
            .Select(a => db.Table<FilteredNoteRow>().IgnoreQueryFilters().Count(b => b.Value >= a.Value))
            .ToList();

        Assert.Equal([2, 1], actual);
    }
}
