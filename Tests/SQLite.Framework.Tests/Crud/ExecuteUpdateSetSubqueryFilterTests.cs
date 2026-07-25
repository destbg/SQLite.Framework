using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework;
using SQLite.Framework.Extensions;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("FilteredCountSource")]
public class FilteredCountSourceRow
{
    [Key]
    public int Id { get; set; }

    public bool IsDeleted { get; set; }
}

[Table("FilterSetTarget")]
public class FilterSetTargetRow
{
    [Key]
    public int Id { get; set; }

    public int Total { get; set; }
}

public class ExecuteUpdateSetSubqueryFilterTests
{
    [Fact]
    public void SetValueSubqueryHonorsQueryFilter()
    {
        using TestDatabase db = new(b => b.AddQueryFilter<FilteredCountSourceRow>(s => !s.IsDeleted));
        db.Table<FilteredCountSourceRow>().Schema.CreateTable();
        db.Table<FilterSetTargetRow>().Schema.CreateTable();
        db.Table<FilteredCountSourceRow>().Add(new FilteredCountSourceRow { Id = 1, IsDeleted = false });
        db.Table<FilteredCountSourceRow>().Add(new FilteredCountSourceRow { Id = 2, IsDeleted = true });
        db.Table<FilterSetTargetRow>().Add(new FilterSetTargetRow { Id = 1, Total = 0 });

        int expected = db.Table<FilteredCountSourceRow>().Count();
        Assert.Equal(1, expected);

        db.Table<FilterSetTargetRow>().ExecuteUpdate(s => s.Set(a => a.Total, a => db.Table<FilteredCountSourceRow>().Count()));

        Assert.Equal(expected, db.Table<FilterSetTargetRow>().Select(a => a.Total).First());
    }

    [Fact]
    public void SetValueSubqueryUnderIgnoreQueryFiltersCountsAllRows()
    {
        using TestDatabase db = new(b => b.AddQueryFilter<FilteredCountSourceRow>(s => !s.IsDeleted));
        db.Table<FilteredCountSourceRow>().Schema.CreateTable();
        db.Table<FilterSetTargetRow>().Schema.CreateTable();
        db.Table<FilteredCountSourceRow>().Add(new FilteredCountSourceRow { Id = 1, IsDeleted = false });
        db.Table<FilteredCountSourceRow>().Add(new FilteredCountSourceRow { Id = 2, IsDeleted = true });
        db.Table<FilterSetTargetRow>().Add(new FilterSetTargetRow { Id = 1, Total = 0 });

        db.Table<FilterSetTargetRow>().IgnoreQueryFilters().ExecuteUpdate(s => s.Set(a => a.Total, a => db.Table<FilteredCountSourceRow>().Count()));

        Assert.Equal(2, db.Table<FilterSetTargetRow>().Select(a => a.Total).First());
    }
}
