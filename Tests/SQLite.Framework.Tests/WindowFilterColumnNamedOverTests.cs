using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

internal sealed class OverNamedColumnRow
{
    [Key]
    public int Id { get; set; }

    [Column("a OVER b")]
    public int Value { get; set; }
}

public class WindowFilterColumnNamedOverTests
{
    [Fact]
    public void FilterClauseLandsBeforeTheRealOverKeyword()
    {
        using TestDatabase db = new();
        db.Table<OverNamedColumnRow>().Schema.CreateTable();
        db.Table<OverNamedColumnRow>().Add(new OverNamedColumnRow { Id = 1, Value = 10 });
        db.Table<OverNamedColumnRow>().Add(new OverNamedColumnRow { Id = 2, Value = 20 });

        List<OverNamedColumnRow> rows = db.Table<OverNamedColumnRow>().AsEnumerable().ToList();

        List<int> expected = rows
            .Select(_ => rows.Where(x => x.Id > 0).Sum(x => x.Value))
            .ToList();

        Assert.Equal([30, 30], expected);

        List<int> actual = db.Table<OverNamedColumnRow>()
            .OrderBy(r => r.Id)
            .Select(r => SQLiteWindowFunctions.Sum(r.Value).Filter(r.Id > 0).AsValue())
            .ToList();

        Assert.Equal(expected, actual);
    }
}
