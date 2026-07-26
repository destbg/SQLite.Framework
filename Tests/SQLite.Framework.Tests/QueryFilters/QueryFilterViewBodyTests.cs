using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H22pFilteredNotes")]
public class H22pFilteredNote
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";

    public int TenantId { get; set; }

    public bool IsDeleted { get; set; }
}

[Table("H22pNoteSummaryView")]
public class H22pNoteSummary
{
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public class H22pTenantScope
{
    public int TenantId { get; set; }
}

public class QueryFilterViewBodyTests
{
    [Fact]
    public void ViewBodyOverASoftDeleteFilteredTableKeepsEveryRow()
    {
        using TestDatabase db = new(b => b.AddQueryFilter<H22pFilteredNote>(n => !n.IsDeleted));
        db.Table<H22pFilteredNote>().Schema.CreateTable();
        List<H22pFilteredNote> rows = Notes();
        db.Table<H22pFilteredNote>().AddRange(rows);

        db.Schema.CreateView<H22pNoteSummary>(() =>
            from n in db.Table<H22pFilteredNote>()
            select new H22pNoteSummary { Id = n.Id, Name = n.Name });

        List<string> expected = rows.OrderBy(n => n.Id).Select(n => n.Name).ToList();
        List<string> actual = db.ReadOnlyTable<H22pNoteSummary>().OrderBy(v => v.Id).Select(v => v.Name).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ViewBodyDoesNotFreezeTheCapturedFilterValue()
    {
        H22pTenantScope scope = new() { TenantId = 1 };
        using TestDatabase db = new(b => b.AddQueryFilter<H22pFilteredNote>(n => n.TenantId == scope.TenantId));
        db.Table<H22pFilteredNote>().Schema.CreateTable();
        List<H22pFilteredNote> rows = Notes();
        db.Table<H22pFilteredNote>().AddRange(rows);

        db.Schema.CreateView<H22pNoteSummary>(() =>
            from n in db.Table<H22pFilteredNote>()
            select new H22pNoteSummary { Id = n.Id, Name = n.Name });

        scope.TenantId = 2;

        List<string> expected = rows.OrderBy(n => n.Id).Select(n => n.Name).ToList();
        List<string> actual = db.ReadOnlyTable<H22pNoteSummary>().OrderBy(v => v.Id).Select(v => v.Name).ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H22pFilteredNote> Notes()
    {
        return
        [
            new H22pFilteredNote { Id = 1, Name = "alpha", TenantId = 1, IsDeleted = false },
            new H22pFilteredNote { Id = 2, Name = "beta", TenantId = 2, IsDeleted = true }
        ];
    }
}
