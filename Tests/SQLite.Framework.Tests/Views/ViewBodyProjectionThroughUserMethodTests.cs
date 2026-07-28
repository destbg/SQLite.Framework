using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H23oViewSourceRows")]
public class H23oViewSourceRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

[Table("H23oDecoratedNameViews")]
public class H23oDecoratedNameView
{
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public static class H23oViewFns
{
    public static string Decorate(string value)
    {
        return "[" + value + "]";
    }
}

public class ViewBodyProjectionThroughUserMethodTests
{
    [Fact]
    public void ViewOverAProjectionThatRunsInMemoryCarriesTheProjectedValue()
    {
        using TestDatabase db = Setup(nameof(ViewOverAProjectionThatRunsInMemoryCarriesTheProjectedValue));

        Exception? creation = Record.Exception(() => db.Schema.CreateView<H23oDecoratedNameView>(() =>
            db.Table<H23oViewSourceRow>()
                .Select(r => new H23oDecoratedNameView { Id = r.Id, Name = H23oViewFns.Decorate(r.Name) })));

        if (creation != null)
        {
            Assert.IsType<NotSupportedException>(creation);
            return;
        }

        List<string> expected = Rows()
            .Select(r => H23oViewFns.Decorate(r.Name))
            .OrderBy(v => v, StringComparer.Ordinal)
            .ToList();

        List<string> actual = db.ReadOnlyTable<H23oDecoratedNameView>()
            .Select(v => v.Name)
            .ToList()
            .OrderBy(v => v, StringComparer.Ordinal)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H23oViewSourceRow> Rows()
    {
        return
        [
            new H23oViewSourceRow { Id = 1, Name = "alpha" },
            new H23oViewSourceRow { Id = 2, Name = "beta" }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(null, methodName);
        db.Table<H23oViewSourceRow>().Schema.CreateTable();
        db.Table<H23oViewSourceRow>().AddRange(Rows());
        return db;
    }
}
