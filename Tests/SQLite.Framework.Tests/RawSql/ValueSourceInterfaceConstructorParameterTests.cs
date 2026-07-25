using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public record H21eIfaceValueRow(int Id, IComparable Value);

public record H21eTextValueRow(int Id, string Value);

public class ValueSourceInterfaceConstructorParameterTests
{
    private static List<H21eIfaceValueRow> InterfaceRows()
    {
        return
        [
            new H21eIfaceValueRow(1, "Ann"),
            new H21eIfaceValueRow(2, "Bob")
        ];
    }

    private static List<H21eTextValueRow> TextRows()
    {
        return
        [
            new H21eTextValueRow(1, "Ann"),
            new H21eTextValueRow(2, "Bob")
        ];
    }

    [Fact]
    public void ValuesRangeReadsAnInterfaceTypedConstructorParameter()
    {
        using TestDatabase db = new();
        List<H21eIfaceValueRow> rows = InterfaceRows();

        List<string> expected = rows
            .OrderBy(r => r.Id)
            .Select(r => r.Value.ToString()!)
            .ToList();

        List<string> actual = db.ValuesRange(rows)
            .ToList()
            .OrderBy(r => r.Id)
            .Select(r => r.Value.ToString()!)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SingleValuesRowReadsAnInterfaceTypedConstructorParameter()
    {
        using TestDatabase db = new();
        H21eIfaceValueRow row = new(7, "Zoe");

        List<string> expected = [row.Value.ToString()!];

        List<string> actual = db.Values(row)
            .ToList()
            .Select(r => r.Value.ToString()!)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void RawQueryReadsAnInterfaceTypedConstructorParameter()
    {
        using TestDatabase db = new();

        List<string> expected = [new H21eIfaceValueRow(1, "Ann").Value.ToString()!];

        List<string> actual = db.Query<H21eIfaceValueRow>("SELECT 1 AS \"Id\", 'Ann' AS \"Value\"")
            .Select(r => r.Value.ToString()!)
            .ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ValuesRangeReadsAStringTypedConstructorParameter()
    {
        using TestDatabase db = new();
        List<H21eTextValueRow> rows = TextRows();

        List<string> expected = rows
            .OrderBy(r => r.Id)
            .Select(r => r.Value)
            .ToList();

        List<string> actual = db.ValuesRange(rows)
            .ToList()
            .OrderBy(r => r.Id)
            .Select(r => r.Value)
            .ToList();

        Assert.Equal(expected, actual);
    }
}
