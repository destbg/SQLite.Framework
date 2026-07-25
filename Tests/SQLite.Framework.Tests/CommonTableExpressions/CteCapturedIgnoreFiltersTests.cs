using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("CteFilteredNote")]
public class CteFilteredNoteRow
{
    [Key]
    public int Id { get; set; }

    public bool IsDeleted { get; set; }
}

public class CteCapturedIgnoreFiltersTests
{
    [Fact]
    public void IgnoreQueryFiltersInsideACapturedCteBodyDisablesFiltersStatementWide()
    {
        using TestDatabase db = new(b => b.AddQueryFilter<CteFilteredNoteRow>(s => !s.IsDeleted));
        db.Table<CteFilteredNoteRow>().Schema.CreateTable();
        db.Table<CteFilteredNoteRow>().AddRange(
        [
            new CteFilteredNoteRow { Id = 1, IsDeleted = false },
            new CteFilteredNoteRow { Id = 2, IsDeleted = true },
        ]);

        IQueryable<CteFilteredNoteRow> captured = db.Table<CteFilteredNoteRow>().IgnoreQueryFilters();
        SQLiteCte<CteFilteredNoteRow> cte = db.With(() => captured);

        List<int> ids = cte
            .Join(db.Table<CteFilteredNoteRow>(), c => c.Id, t => t.Id, (c, t) => c.Id)
            .ToList().OrderBy(x => x).ToList();

        Assert.Equal([1, 2], ids);
    }
}
