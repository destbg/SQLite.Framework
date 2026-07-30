using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

[Table("H25aCrossOuters")]
public class H25aCrossOuter
{
    [Key]
    public int Id { get; set; }
}

[Table("H25aCrossInners")]
public class H25aCrossInner
{
    [Key]
    public int Id { get; set; }

    public string Label { get; set; } = "";
}

public static class H25aCrossFns
{
    public static string Decorate(string value)
    {
        return "[" + value + "]";
    }
}

public class SecondFromSourceInMemoryProjectionTests
{
    [Fact]
    public void ASecondFromSourceProjectedInMemoryReadsTheProjectedValue()
    {
        using TestDatabase db = Setup(nameof(ASecondFromSourceProjectedInMemoryReadsTheProjectedValue));

        List<string> expected = OuterRows()
            .SelectMany(_ => InnerRows().Select(r => H25aCrossFns.Decorate(r.Label)))
            .OrderBy(s => s, StringComparer.Ordinal)
            .ToList();

        AssertValuesOrRefusal(expected, () => db.Table<H25aCrossOuter>()
            .SelectMany(_ => db.Table<H25aCrossInner>().Select(r => H25aCrossFns.Decorate(r.Label)))
            .ToList()
            .OrderBy(s => s, StringComparer.Ordinal)
            .ToList());
    }

    private static void AssertValuesOrRefusal<T>(List<T> expected, Func<List<T>> run)
    {
        List<T> actual;
        try
        {
            actual = run();
        }
        catch (NotSupportedException)
        {
            return;
        }

        Assert.Equal(expected, actual);
    }

    private static List<H25aCrossOuter> OuterRows()
    {
        return [new H25aCrossOuter { Id = 1 }];
    }

    private static List<H25aCrossInner> InnerRows()
    {
        return
        [
            new H25aCrossInner { Id = 1, Label = "a" },
            new H25aCrossInner { Id = 2, Label = "b" }
        ];
    }

    private static TestDatabase Setup(string methodName)
    {
        TestDatabase db = new(null, methodName);
        db.Table<H25aCrossOuter>().Schema.CreateTable();
        db.Table<H25aCrossInner>().Schema.CreateTable();
        db.Table<H25aCrossOuter>().AddRange(OuterRows());
        db.Table<H25aCrossInner>().AddRange(InnerRows());
        return db;
    }
}
