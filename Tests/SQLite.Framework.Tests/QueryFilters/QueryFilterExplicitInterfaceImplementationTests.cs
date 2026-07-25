using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public interface IFilterExplicitHidden
{
    bool Hidden { get; }
}

[Table("FilterExplicitImplRows")]
public class FilterExplicitImplRow : IFilterExplicitHidden
{
    [Key]
    public int Id { get; set; }

    public bool HiddenFlag { get; set; }

    bool IFilterExplicitHidden.Hidden => HiddenFlag;
}

public class QueryFilterExplicitInterfaceImplementationTests
{
    [Fact]
    public void ExplicitlyImplementedFilterPropertyThrowsNotSupported()
    {
        using TestDatabase db = new(b => b.AddQueryFilter<IFilterExplicitHidden>(h => !h.Hidden));
        db.Table<FilterExplicitImplRow>().Schema.CreateTable();
        db.Table<FilterExplicitImplRow>().Add(new FilterExplicitImplRow { Id = 1, HiddenFlag = false });
        db.Table<FilterExplicitImplRow>().Add(new FilterExplicitImplRow { Id = 2, HiddenFlag = true });

        Assert.Throws<NotSupportedException>(() => db.Table<FilterExplicitImplRow>().ToList());
    }
}
