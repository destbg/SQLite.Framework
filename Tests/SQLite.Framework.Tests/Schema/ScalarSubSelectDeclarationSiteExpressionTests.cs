using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SQLite.Framework.Enums;
using SQLite.Framework.Tests.Helpers;

namespace SQLite.Framework.Tests;

public enum H26qMode
{
    None = 0,
    Read = 1,
    Write = 2
}

[Table("H26qParsedModes")]
public class H26qParsedMode
{
    [Key]
    public int Id { get; set; }

    public string Kind { get; set; } = "";

    public H26qMode Mode { get; set; }
}

[Table("H26qFormattedModes")]
public class H26qFormattedMode
{
    [Key]
    public int Id { get; set; }

    public H26qMode Mode { get; set; }

    public string Number { get; set; } = "";
}

[Table("H26qCubedValues")]
public class H26qCubedValue
{
    [Key]
    public int Id { get; set; }

    public double Value { get; set; }

    public double Root { get; set; }
}

[Table("H26qUnsignedAmounts")]
public class H26qUnsignedAmount
{
    [Key]
    public int Id { get; set; }

    public ulong Big { get; set; }

    public ulong Half { get; set; }
}

public class ScalarSubSelectDeclarationSiteExpressionTests
{
    [Fact]
    public void EnumParseInAComputedColumnKeepsTheParsedMember()
    {
        using ModelTestDatabase db = new(mb => mb.Entity<H26qParsedMode>()
            .Computed(r => r.Mode, r => Enum.Parse<H26qMode>(r.Kind)));
        db.Schema.CreateTable<H26qParsedMode>();
        db.Table<H26qParsedMode>().AddRange(ParsedRows());

        List<H26qMode> expected = ParsedRows().OrderBy(r => r.Id).Select(r => Enum.Parse<H26qMode>(r.Kind)).ToList();
        List<H26qMode> actual = db.Table<H26qParsedMode>().OrderBy(r => r.Id).Select(r => r.Mode).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void EnumToStringWithTheNumberFormatInAComputedColumnKeepsTheNumberUnderTextStorage()
    {
        using ModelTestDatabase db = new(
            mb => mb.Entity<H26qFormattedMode>().Computed(r => r.Number, r => r.Mode.ToString("D")),
            b => b.UseEnumStorage(EnumStorageMode.Text));
        db.Schema.CreateTable<H26qFormattedMode>();
        db.Table<H26qFormattedMode>().AddRange(FormattedRows());

        List<string> expected = FormattedRows().OrderBy(r => r.Id).Select(r => r.Mode.ToString("D")).ToList();
        List<string> actual = db.Table<H26qFormattedMode>().OrderBy(r => r.Id).Select(r => r.Number).ToList();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CubeRootInAComputedColumnKeepsTheRoot()
    {
        using ModelTestDatabase db = new(mb => mb.Entity<H26qCubedValue>()
            .Computed(r => r.Root, r => Math.Cbrt(r.Value)));
        db.Schema.CreateTable<H26qCubedValue>();
        db.Table<H26qCubedValue>().AddRange(CubedRows());

        List<double> expected = CubedRows().OrderBy(r => r.Id).Select(r => Math.Cbrt(r.Value)).ToList();
        List<double> actual = db.Table<H26qCubedValue>().OrderBy(r => r.Id).Select(r => r.Root).ToList();

        Assert.Equal(expected.Count, actual.Count);
        for (int i = 0; i < expected.Count; i++)
        {
            Assert.Equal(expected[i], actual[i], 9);
        }
    }

    [Fact]
    public void UnsignedDivisionInAComputedColumnReportsAClearMessage()
    {
        using ModelTestDatabase db = new(mb => mb.Entity<H26qUnsignedAmount>()
            .Computed(r => r.Half, r => r.Big / 2UL));

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() => db.Schema.CreateTable<H26qUnsignedAmount>());

        Assert.Contains("computed column", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void LastSearchInAComputedColumnReportsAClearMessage()
    {
        using ModelTestDatabase db = new(mb => mb.Entity<H26qParsedMode>()
            .Computed(r => r.Mode, r => (H26qMode)r.Kind.LastIndexOf("d")));

        NotSupportedException ex = Assert.Throws<NotSupportedException>(() => db.Schema.CreateTable<H26qParsedMode>());

        Assert.Contains("LastIndexOf", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void CubeRootInACheckConstraintAcceptsTheSameRows()
    {
        using ModelTestDatabase db = new(mb => mb.Entity<H26qCubedValue>()
            .Check(r => Math.Cbrt(r.Value) < 5.0, name: "CK_H26qCubedValues_Root"));
        db.Schema.CreateTable<H26qCubedValue>();

        List<H26qCubedValue> allowed = CubedRows().Where(r => Math.Cbrt(r.Value) < 5.0).ToList();
        db.Table<H26qCubedValue>().AddRange(allowed);

        Assert.Equal(allowed.Count, db.Table<H26qCubedValue>().Count());
        Assert.ThrowsAny<Exception>(() =>
            db.Table<H26qCubedValue>().Add(new H26qCubedValue { Id = 99, Value = 1000.0 }));
    }

    private static List<H26qParsedMode> ParsedRows()
    {
        return
        [
            new H26qParsedMode { Id = 1, Kind = "Read" },
            new H26qParsedMode { Id = 2, Kind = "Write" },
            new H26qParsedMode { Id = 3, Kind = "None" }
        ];
    }

    private static List<H26qFormattedMode> FormattedRows()
    {
        return
        [
            new H26qFormattedMode { Id = 1, Mode = H26qMode.Read },
            new H26qFormattedMode { Id = 2, Mode = H26qMode.Write },
            new H26qFormattedMode { Id = 3, Mode = H26qMode.None }
        ];
    }

    private static List<H26qCubedValue> CubedRows()
    {
        return
        [
            new H26qCubedValue { Id = 1, Value = 27.0 },
            new H26qCubedValue { Id = 2, Value = 8.0 },
            new H26qCubedValue { Id = 3, Value = 2.0 }
        ];
    }

}
