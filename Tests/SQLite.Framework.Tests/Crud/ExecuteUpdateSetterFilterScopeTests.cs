using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("FilteredTotal")]
public class FilteredTotalRow
{
    [Key]
    public int Id { get; set; }

    public bool IsDeleted { get; set; }

    public int Total { get; set; }
}

public class ExecuteUpdateSetterFilterScopeTests
{
    [Fact]
    public void IgnoreQueryFiltersInsideASetterSubqueryDisablesFiltersStatementWide()
    {
        using TestDatabase db = new(b => b.AddQueryFilter<FilteredTotalRow>(s => !s.IsDeleted));
        db.Table<FilteredTotalRow>().Schema.CreateTable();
        db.Table<FilteredTotalRow>().AddRange(
        [
            new FilteredTotalRow { Id = 1, IsDeleted = false, Total = 0 },
            new FilteredTotalRow { Id = 2, IsDeleted = true, Total = 0 },
        ]);

        db.Table<FilteredTotalRow>().ExecuteUpdate(s => s.Set(
            t => t.Total,
            t => db.Table<FilteredTotalRow>().IgnoreQueryFilters().Count()));

        List<int> totals = db.Table<FilteredTotalRow>().IgnoreQueryFilters()
            .OrderBy(t => t.Id).Select(t => t.Total).ToList();
        Assert.Equal([2, 2], totals);
    }
}
