using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public interface IFilterDelegatedHidden
{
    bool Hidden { get; }
}

[Table("FilterDelegatedRows")]
public class FilterDelegatedRow : IFilterDelegatedHidden
{
    [Key]
    public int Id { get; set; }

    public bool IsHidden { get; set; }

    bool IFilterDelegatedHidden.Hidden => IsHidden;
}

public class FilterInheritedHiddenBase
{
    public bool Hidden { get; set; }
}

[Table("FilterInheritedRows")]
public class FilterInheritedRow : FilterInheritedHiddenBase, IFilterDelegatedHidden
{
    [Key]
    public int Id { get; set; }
}

public class QueryFilterDelegatedInterfaceMemberTests
{
    [Fact]
    public void ExplicitMemberDelegatingToRenamedPublicMemberThrowsNotSupported()
    {
        using TestDatabase db = new(b => b.AddQueryFilter<IFilterDelegatedHidden>(h => !h.Hidden));
        db.Table<FilterDelegatedRow>().Schema.CreateTable();
        db.Table<FilterDelegatedRow>().Add(new FilterDelegatedRow { Id = 1, IsHidden = false });

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() => db.Table<FilterDelegatedRow>().ToList());
        Assert.Contains(nameof(FilterDelegatedRow), ex.Message);
        Assert.Contains(nameof(IFilterDelegatedHidden.Hidden), ex.Message);
    }

    [Fact]
    public void InheritedPublicMemberKeepsFiltering()
    {
        using TestDatabase db = new(b => b.AddQueryFilter<IFilterDelegatedHidden>(h => !h.Hidden));
        db.Table<FilterInheritedRow>().Schema.CreateTable();
        db.Table<FilterInheritedRow>().Add(new FilterInheritedRow { Id = 1, Hidden = false });
        db.Table<FilterInheritedRow>().Add(new FilterInheritedRow { Id = 2, Hidden = true });

        List<FilterInheritedRow> rows = db.Table<FilterInheritedRow>().ToList();

        Assert.Single(rows);
        Assert.Equal(1, rows[0].Id);
    }
}
