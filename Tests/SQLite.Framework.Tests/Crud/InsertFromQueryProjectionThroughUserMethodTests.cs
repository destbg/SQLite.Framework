using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H23oCopySourceRows")]
public class H23oCopySourceRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

[Table("H23oCopyTargetRows")]
public class H23oCopyTargetRow
{
    [Key]
    public int Id { get; set; }

    public string Name { get; set; } = "";
}

public static class H23oCopyFns
{
    public static string Decorate(string value)
    {
        return "[" + value + "]";
    }
}

public class InsertFromQueryProjectionThroughUserMethodTests
{
    [Fact]
    public void InsertFromQueryOverAProjectionThatRunsInMemoryCarriesTheProjectedValue()
    {
        using TestDatabase db = Setup(nameof(InsertFromQueryOverAProjectionThatRunsInMemoryCarriesTheProjectedValue));

        Exception? failure = Record.Exception(() => db.Table<H23oCopyTargetRow>().InsertFromQuery(
            db.Table<H23oCopySourceRow>()
                .Select(r => new H23oCopyTargetRow { Id = r.Id, Name = H23oCopyFns.Decorate(r.Name) })));

        if (failure != null)
        {
            Assert.IsType<NotSupportedException>(failure);
            return;
        }

        List<string> expected = Rows()
            .Select(r => H23oCopyFns.Decorate(r.Name))
            .OrderBy(v => v, StringComparer.Ordinal)
            .ToList();

        List<string> actual = db.Table<H23oCopyTargetRow>()
            .Select(t => t.Name)
            .ToList()
            .OrderBy(v => v, StringComparer.Ordinal)
            .ToList();

        Assert.Equal(expected, actual);
    }

    private static List<H23oCopySourceRow> Rows()
    {
        return
        [
            new H23oCopySourceRow { Id = 1, Name = "alpha" },
            new H23oCopySourceRow { Id = 2, Name = "beta" }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(null, methodName);
        db.Table<H23oCopySourceRow>().Schema.CreateTable();
        db.Table<H23oCopyTargetRow>().Schema.CreateTable();
        db.Table<H23oCopySourceRow>().AddRange(Rows());
        return db;
    }
}
